using Altova.RaptorXml;
using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using Xbrl = Altova.RaptorXml.Xbrl;

namespace XbrlTablesToExcel
{
    class XbrlTablesToExcel
    {
        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<Options>(args).MapResult((Options o) => Run(o), (err) => 1);
        }

        static int Run(Options o)
        {
            SetupLogger(o.LogFile);

            Xbrl.Taxonomy.Dts dts = null;
            if (o.EntryPointUrl != null)
            {
                Logger.Debug("Loading DTS {0}", o.EntryPointUrl);
                ErrorLog log;
                dts = Xbrl.Taxonomy.Dts.CreateFromUrl(o.EntryPointUrl, out log);
                if (log.HasErrors)
                {
                    Logger.Error(log);
                    return 1;
                }
            }

            var writer = new ExcelWriter(new DefaultTableStyle(), o.XOffset, o.YOffset, o.EmptyRowsAfterTitle);
            return ConvertReportToExcel(dts, writer, o.InputFile, o.OutputFile);
        }

        static int ConvertReportToExcel(Xbrl.Taxonomy.Dts dts, ExcelWriter writer, string fileIn, string fileOut)
        {
            try
            {
                var report = new Report(fileIn, dts);
                var wb = writer.WriteReport(report);

                Logger.Debug("Saving Excel workbook to {0}", fileOut);
                wb.SaveAs(fileOut);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return 1;
            }

            Logger.Info("Finished conversion of {0} to {1}", fileIn, fileOut);
            return 0;
        }

        class Options
        {
            [Usage()]
            public static IEnumerable<Example> Examples
            {
                get
                {
                    return new List<Example>() {
                        new Example("Convert XBRL instance to Excel workbook", new Options { InputFile = "input.xbrl", OutputFile = "output.xlsx" })
                    };
                }
            }

            [Value(0, Required = true, MetaName = "XBRL", HelpText = "Input XBRL file")]
            public string InputFile { get; set; }
            [Value(1, Required = true, MetaName = "XLSX", HelpText = "Output XLSX file")]
            public string OutputFile { get; set; }

            [Option("dts", Required = false, HelpText = "Preloads the given DTS and uses it for validation of XBRL instance files. SchemaRefs in XBRL instance files will be ignored.")]
            public string EntryPointUrl { get; set; }

            [Option("x-offset", Default = 0, Required = false, HelpText = "Specify the number of empty columns that will be inserted before the generated table.")]
            public int XOffset { get; set; }
            [Option("y-offset", Default = 0, Required = false, HelpText = "Specify the number of empty rows that will be inserted before the generated table.")]
            public int YOffset { get; set; }
            [Option("empty-rows-after-title", Default = 1, Required = false, HelpText = "Specify the number of empty rows that will be inserted after the worksheet title.")]
            public int EmptyRowsAfterTitle { get; set; }

            [Option("log", Required = false, HelpText = "Log to file.")]
            public string LogFile { get; set; }
        }

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        static void SetupLogger(string logfile)
        {
            // Rules for mapping loggers to targets
            var config = new NLog.Config.LoggingConfiguration();
            config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, new NLog.Targets.ConsoleTarget("logconsole") { Layout = new NLog.Layouts.SimpleLayout("${level:uppercase=true} ${message}") });
            if (logfile != null)
                config.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, new NLog.Targets.FileTarget("logfile") { FileName = logfile });

            // Apply config           
            NLog.LogManager.Configuration = config;
        }
    }
}
