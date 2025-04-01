using Altova.RaptorXml;
using System;
using System.Linq;
using System.Text;
using Oim = Altova.RaptorXml.Oim;
using Xbrl = Altova.RaptorXml.Xbrl;
using Xml = Altova.RaptorXml.Xml;

namespace OIMReportBuilderExample
{
    internal class OIMReportBuilderExample
    {
        // Path to the output directory - the xBRL-CSV report will be created in this directory
        internal static string OutputFolder = "C:\\temp\\OIMReportBuilderTest";

        // Taxonomy entrypoint and respective metadata json to extend
        // make sure to install the specified taxonomy first using the Altova Taxonomy Manager
        internal static string EntryPointDtsUrl = "http://www.eba.europa.eu/eu/fr/xbrl/crr/fws/corep/4.0/mod/corep_lr.xsd";
        internal static string ExtendsJsonUrl = "http://www.eba.europa.eu/eu/fr/xbrl/crr/fws/corep/4.0/mod/corep_lr.json";
        
        // Reference date of the report
        internal static System.DateTime ReferenceDate = System.DateTime.Today;
        internal static Xbrl.PeriodAspectValue ReferenceDateAV = Xbrl.PeriodAspectValue.CreateFromInstant(ReferenceDate.AddDays(1));
        
        // Entity Identifier and Scheme
        internal static Xml.QName EntityIdentifier = new Xml.QName("DUMMYLEI123.IND", "https://eurofiling.info/eu/rs", "rs");
        internal static Xbrl.EntityIdentifierAspectValue EntityIdentifierAV = new Xbrl.EntityIdentifierAspectValue(EntityIdentifier.LocalName, EntityIdentifier.NamespaceName);
        
        // Report base currency
        internal static string BaseCurrency = "EUR";
        internal static Xbrl.UnitAspectValue BaseCurrencyAV = Xbrl.UnitAspectValue.CreateFromIso4217Currency(BaseCurrency);
        
        // Accuracy settings
        internal static int MonetaryDecimals = -3;
        internal static int IntegerDecimals = 0;
        internal static int PercentageDecimals = 4;
        internal static int DecimalsDecimals = 0;

        internal static Oim.ReportBuilder ReportBuilder;
        internal static Xbrl.Taxonomy.Dts EntryPointDts;

        // Creates report.json file in the output folder extending the metadata json of the entrypoint taxonomy
        static void WriteReportJSON(string path, string extendsURI)
        {
            var writer = new System.IO.StreamWriter(path, false, new UTF8Encoding(false));
            writer.WriteLine("{");
            writer.WriteLine("\t\"documentInfo\": {");
            writer.WriteLine("\t\t\"documentType\": \"https://xbrl.org/2021/xbrl-csv\",");
            writer.WriteLine("\t\t\"extends\": [ \"{0}\" ]", extendsURI);
            writer.WriteLine("\t}");
            writer.WriteLine("}");
            writer.Close();
        }

        // Creates parameters.csv in the output folder
        static void WriteParametersCSV(string path)
        {
            var writer = new System.IO.StreamWriter(path, false, new UTF8Encoding(false));
            writer.WriteLine("name,value");
            writer.WriteLine("entityID,{0}:{1}", EntityIdentifier.Prefix, EntityIdentifier.LocalName);
            writer.WriteLine("refPeriod,{0}", ReferenceDate.ToString("yyyy-MM-dd"));
            writer.WriteLine("baseCurrency,{0}:{1}", "iso4217", BaseCurrency);
            writer.WriteLine("baseLanguage,{0}", "en");
            writer.WriteLine("decimalsInteger,{0}", IntegerDecimals);
            writer.WriteLine("decimalsMonetary,{0}", MonetaryDecimals);
            writer.WriteLine("decimalsPercentage,{0}", PercentageDecimals);
            writer.WriteLine("decimalsDecimal,{0}", DecimalsDecimals);
            writer.Close();
        }

        // Returns a ConstraintSet with aspect values set for period and entity identifier
        static Xbrl.ConstraintSet GetReportConstraints()
        {
            Xbrl.ConstraintSet cs = new Xbrl.ConstraintSet();
            cs.Add(ReferenceDateAV);
            cs.Add(EntityIdentifierAV);
            return cs;
        }

        // Add a filing indicator fact to the report
        static void AddFilingIndicator(string filingIndicatorCode, bool value)
        {
            const string XbrlFilingIndicatorsNamespace = "http://www.xbrl.org/taxonomy/int/filing-indicators/REC/2021-02-03";

            var xbrlFilingIndicatorFiledConcept = EntryPointDts.ResolveConcept("filed", XbrlFilingIndicatorsNamespace) as Xbrl.Taxonomy.Item;
            var xbrlFilingIndicatorTemplateDimension = EntryPointDts.ResolveConcept("template", XbrlFilingIndicatorsNamespace) as Xbrl.Xdt.Dimension;

            var cs = GetReportConstraints();
            cs.Add(new Xbrl.ConceptAspectValue(xbrlFilingIndicatorFiledConcept));
            cs.Add(Xbrl.TypedDimensionAspectValue.CreateFromString(xbrlFilingIndicatorTemplateDimension, filingIndicatorCode));

            ReportBuilder.AddNonNumericItemFact(cs, value ? "true" : "false");
        }

        // Add a qualified name fact to the report for given table cell
        static void AddQNameFact(Xbrl.Table.Layout.Cell cell, Xml.QName value)
        {
            var cs = GetReportConstraints();
            cs.UnionWith(cell.ConstraintSet);
            ReportBuilder.AddQNameItemFact(cs, value);
        }

        // Add a numeric fact to the report for given table cell
        static void AddNumericFact(Xbrl.Table.Layout.Cell cell, Xbrl.UnitAspectValue unit, decimal value, int decimals)
        {
            var cs = GetReportConstraints();
            cs.UnionWith(cell.ConstraintSet);
            cs.Add(unit);
            ReportBuilder.AddNumericItemFact(cs, value.ToString(), decimals.ToString());
        }


        internal static void RunOIMReportBuilderExample()
        {
            var dtsOptions = new Xbrl.Taxonomy.DtsSettings();
            dtsOptions.PreloadXbrlSchemas = true;
            dtsOptions.PreloadFormulaSchemas = true;
            dtsOptions.PreloadTableSchemas = true;
            dtsOptions.TableLinkbaseNamespace = "##detect";

            ErrorLog errorLog;
            // load EBA 4.0 COREP LR taxonomy
            EntryPointDts = Xbrl.Taxonomy.Dts.CreateFromUrl(EntryPointDtsUrl, dtsOptions, out errorLog);
            if (EntryPointDts == null || errorLog.HasErrors)
                throw new ApplicationException(errorLog.ToString());

            System.IO.Directory.CreateDirectory(OutputFolder);
            var reportJSONPath = System.IO.Path.Combine(OutputFolder, "report.json");

            // write reports.json and parameters.csv
            WriteReportJSON(reportJSONPath, ExtendsJsonUrl);
            WriteParametersCSV(System.IO.Path.Combine(OutputFolder, "parameters.csv"));

            ReportBuilder = new Oim.ReportBuilder(EntryPointDts);
            ReportBuilder.AddSchemaRef(EntryPointDtsUrl);

            var tableLayoutOptions = new Xbrl.Table.TableLayoutSettings();
            tableLayoutOptions.TableElimination = false;
            tableLayoutOptions.PreserveEmptyAspectNodes = false;
            tableLayoutOptions.TableEliminationAspectNodes = true;

            var toCSVOptions = new Oim.Settings(dtsOptions);
            toCSVOptions.UseExisting = true;
            toCSVOptions.TablesToWrite = new Oim.CSVTableNameList();
            toCSVOptions.OimXbrlNamespace = "##detect";

            var table_C_00_01 = EntryPointDts.FindTables("eba_tC_00.01").FirstOrDefault();
            if (table_C_00_01 != null)
            {
                AddFilingIndicator("C_00.01", true);
                toCSVOptions.TablesToWrite.Add("c_00.01.csv");

                var ts = table_C_00_01.GenerateLayoutModel(null, tableLayoutOptions, out errorLog);
                if (ts == null || errorLog.HasErrors)
                    throw new ApplicationException(errorLog.ToString());

                // add values to table C_00.01
                var layoutTable = ts.FirstOrDefault();
                AddQNameFact(layoutTable.GetCell(0, 0), new Xml.QName("x1", "http://www.eba.europa.eu/xbrl/crr/dict/dom/AS"));
                AddQNameFact(layoutTable.GetCell(0, 1), new Xml.QName("x6", "http://www.eba.europa.eu/xbrl/crr/dict/dom/SC"));

            }

            var table_C_48_01 = EntryPointDts.FindTables("eba_tC_48.01").FirstOrDefault();
            if (table_C_48_01 != null)
            {
                AddFilingIndicator("C_48.01", true);
                toCSVOptions.TablesToWrite.Add("c_48.01.csv");

                var ts = table_C_48_01.GenerateLayoutModel(null, tableLayoutOptions, out errorLog);
                if (ts == null || errorLog.HasErrors)
                    throw new ApplicationException(errorLog.ToString());

                // add values to table C_48.01
                var layoutTable = ts.FirstOrDefault();
                AddNumericFact(layoutTable.GetCell(0, 0), BaseCurrencyAV, 1000000, -3);
                AddNumericFact(layoutTable.GetCell(1, 0), BaseCurrencyAV, 2000000, -3);
            }

            var oim = ReportBuilder.CloseDocument();

            if (!oim.ToCSV(reportJSONPath, out errorLog, toCSVOptions))
                throw new ApplicationException(errorLog.ToString());
        }

        static void Main(string[] args)
        {
            try
            {
                RunOIMReportBuilderExample();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
