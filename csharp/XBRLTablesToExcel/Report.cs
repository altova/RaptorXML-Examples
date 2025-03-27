using Altova.RaptorXml;
using System;
using System.Collections.Generic;
using System.Linq;
using Xbrl = Altova.RaptorXml.Xbrl;
using Oim = Altova.RaptorXml.Oim;

namespace XbrlTablesToExcel
{
    public class Report
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public string Url { get { return Instance.Uri; } }
        public Xbrl.Instance Instance { get; private set; }
        public Xbrl.Taxonomy.Dts Dts { get { return Instance.Dts;  } }
        public Xbrl.Table.Layout.TableModel TableModel { get; private set; }
        public Xbrl.Context ReportingContext { get; private set; }
        public string ReportingCurrency { get; private set; }
        public string ReportingEntityIdentifier { get { return ReportingContext?.Entity.Identifier.Value; } }
        public string ReportingEntityScheme { get { return ReportingContext?.Entity.Identifier.Scheme; } }
        public DateTime? ReferenceDate
        {
            get
            {
                var period = ReportingContext?.Period;
                if (period?.Instant != null)
                    return period.Instant.Value.AddDays(-1);
                else if (period?.EndDate != null)
                    return period.EndDate.Value.AddDays(-1);
                return null;
            }
        }
        public string EntryPointName
        {
            get
            {
                var dts = this.Instance.Dts;
                var moduleType = dts.Schema.ResolveTypeDefinition("moduleType", "http://www.eurofiling.info/xbrl/ext/model");
                if (moduleType != null)
                {
                    var moduleItem = dts.GetItemsByType(moduleType, false).FirstOrDefault();
                    if (moduleItem != null)
                        return XbrlUtils.GetLabel(moduleItem);
                }
                return null;
            }
        }
        public string EntryPointUrl { get { return this.Instance.SchemaRefs.First().XlinkHref; } }
        public Dictionary<string, bool> FilingIndicators { get; private set; }

        public bool GetFilingIndicator(string filingIndicator)
        {
            if (FilingIndicators.Count == 0)
                return true;
            if (FilingIndicators.TryGetValue(filingIndicator, out bool value))
                return value;
            return false;
        }

        public Report(string url, Xbrl.Taxonomy.Dts dts=null)
        {
            Logger.Debug("Loading XBRL instance {0}", url);
            ErrorLog errorLog = null;
            Oim.OIM oim = null;

            // Detect the document type of the XBRL instance (xBRL-XML, xBRL-JSON, xBRL-CSV)
            string docType = Oim.OIM.DetectDocumentType(url);
            switch (docType)
            {
                case "https://xbrl.org/2021/xbrl-csv":
                    oim = Oim.OIM.CreateFromCSV(url, out errorLog);
                    if (oim == null || errorLog.HasErrors)
                        throw new ApplicationException(errorLog.ToString());
                    break;
                case "https://xbrl.org/2021/xbrl-json":
                    oim = Oim.OIM.CreateFromJSON(url, out errorLog);
                    if (oim == null || errorLog.HasErrors)
                        throw new ApplicationException(errorLog.ToString());
                    break;
                case "https://xbrl.org/2021/xbrl-xml":
                    // will be loaded later
                    break;
                default:
                    throw new ApplicationException(String.Format("Unexpected document type: {0}", docType));
            }

            Xbrl.Instance instance = null;
            if (oim != null)
            {
                // convert xBRL-CSV and xBRL-JSON reports to xBRL-XML
                var xmlDoc = oim.ToXML(out errorLog);
                if (errorLog.HasErrors)
                    throw new ApplicationException(errorLog.ToString());

                instance = Xbrl.Instance.CreateFromElement(xmlDoc.DocumentElement, out errorLog);
            }
            else
            {
                instance = Xbrl.Instance.CreateFromUrl(url, XbrlUtils.GetCreateInstanceOptions(dts), out errorLog);
            }

            if (instance == null || errorLog.HasErrors)
                throw new ApplicationException(errorLog.ToString());

            Init(instance);
        }
        public Report(Xbrl.Instance instance)
        {
            Init(instance);
        }

        public TableTree GetTableTree()
        {
            return new TableTree(this);
        }

        void Init(Xbrl.Instance instance)
        {
            this.Instance = instance;

            Logger.Debug("Processing XBRL table linkbase");

            ErrorLog tableErrorLog;
            this.TableModel = this.Instance.GenerateLayoutModel(XbrlUtils.GetTableLayoutOptions(), out tableErrorLog);
            if (this.TableModel == null || tableErrorLog.HasErrors)
                throw new ApplicationException(tableErrorLog.ToString());

            foreach (var context in this.Instance.Contexts)
            {
                if (context.Period.IsInstant && context.DimensionAspectValues.Count == 0)
                {
                    this.ReportingContext = context;
                    break;
                }
            }
            if (this.ReportingContext == null)
            {
                foreach (var context in this.Instance.Contexts)
                {
                    if (context.DimensionAspectValues.Count == 0)
                    {
                        this.ReportingContext = context;
                        break;
                    }
                }
            }

            if (this.Instance.Units.Count > 0)
            {
                var units = new Dictionary<string, uint>();
                foreach (var unit in this.Instance.Units)
                {
                    if (unit.AspectValue.IsMonetary)
                        units.Add(unit.AspectValue.Iso4217Currency, 0);
                }
                foreach (var fact in this.Instance.Facts)
                {
                    var item = fact as Xbrl.Item;
                    if (item != null && item.UnitAspectValue != null && item.UnitAspectValue.IsMonetary)
                        units[item.UnitAspectValue.Iso4217Currency] += 1;
                }
                var mostUsedCurrency = units.OrderByDescending(x => x.Value).First();
                if (mostUsedCurrency.Value > 0)
                    this.ReportingCurrency = mostUsedCurrency.Key;
            }

            FilingIndicators = XbrlUtils.FilingIndicators(Instance);
        }
    }
}
