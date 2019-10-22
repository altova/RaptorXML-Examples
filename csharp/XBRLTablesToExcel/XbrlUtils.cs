using System;
using System.Collections.Generic;
using System.Linq;
using Xml = Altova.RaptorXml.Xml;
using Xsd = Altova.RaptorXml.Xsd;
using Xbrl = Altova.RaptorXml.Xbrl;

namespace XbrlTablesToExcel
{
    internal class XbrlUtils
    {
        internal static string LabelLanguage = "en";
        internal static string FilingIndicatorsNamespace = "http://www.eurofiling.info/xbrl/ext/filing-indicators";

        internal static string GetLabel(object item, bool bFallbackToIdOrName = true)
        {
            if (item is Xbrl.Table.Layout.AxisHeader)
                return GetLabel(item as Xbrl.Table.Layout.AxisHeader, bFallbackToIdOrName);
            else if (item is Xbrl.Taxonomy.Concept)
                return GetLabel(item as Xbrl.Taxonomy.Concept, bFallbackToIdOrName);
            else if (item is Xbrl.Taxonomy.Resource)
                return GetLabel(item as Xbrl.Taxonomy.Resource, bFallbackToIdOrName);
            else if (item is Xml.ElementInformationItem)
                return (item as Xml.ElementInformationItem).QName.ToString();
            else
                return null;
        }
        internal static string GetLabelWithLanguage(Xbrl.Taxonomy.LabelCollection labels, string lang, string fallbackValue)
        {
            Xbrl.Taxonomy.Label result = null;
            string lowerCaseLang = lang.ToLowerInvariant();
            foreach (var label in labels)
            {
                string lowerCaseLabelLang = label.XmlLang.ToLowerInvariant();
                if (lowerCaseLabelLang == lowerCaseLang)
                {
                    result = label;
                    break;
                }
                if (lowerCaseLabelLang == "en")
                    result = label; // fallback to english
                if (result == null)
                    result = label; // fallback to any other language if no label with XmlLang == lang or "en" is found
            }
            return result == null ? fallbackValue : result.Text; // fallback to fallbackvalue if no label is found at all
        }
        internal static string GetLabel(Xbrl.Table.Layout.AxisHeader header, bool bFallbackToId = true)
        {
            var labels = header.DefinitionNode.GetLabels("http://www.xbrl.org/2008/role/label", null, null);
            if (labels.Count == 0)
                labels = header.Axis.GetDefinitionBreakdown(header.Row).GetLabels("http://www.xbrl.org/2008/role/label", null, null);
            return GetLabelWithLanguage(labels, LabelLanguage, bFallbackToId ? header.DefinitionNode.Id : null);
        }
        internal static string GetLabel(Xbrl.Taxonomy.Concept concept, bool bFallbackToQName = true)
        {
            var labels = concept.GetLabels("http://www.xbrl.org/2003/role/label", null, null);
            return GetLabelWithLanguage(labels, LabelLanguage, bFallbackToQName ? concept.QName.ToString() : null);
        }
        internal static string GetLabel(Xbrl.Taxonomy.Resource resource, bool bFallbackToId = true)
        {
            var labels = resource.GetLabels("http://www.xbrl.org/2008/role/label", null, null);
            return GetLabelWithLanguage(labels, LabelLanguage, bFallbackToId ? resource.Id : null);
        }
        internal static string GetVerboseLabel(Xbrl.Table.Layout.AxisHeader header, bool bFallbackToId = true)
        {
            var labels = header.DefinitionNode.GetLabels("http://www.xbrl.org/2003/role/verboseLabel", null, null);
            if (labels.Count == 0)
                labels = header.Axis.GetDefinitionBreakdown(header.Row).GetLabels("http://www.xbrl.org/2003/role/verboseLabel", null, null);
            return GetLabelWithLanguage(labels, LabelLanguage, bFallbackToId ? header.DefinitionNode.Id : null);
        }
        internal static string GetVerboseLabel(Xbrl.Taxonomy.Concept concept, bool bFallbackToQName = true)
        {
            var labels = concept.GetLabels("http://www.xbrl.org/2003/role/verboseLabel", null, null);
            return GetLabelWithLanguage(labels, LabelLanguage, bFallbackToQName ? concept.QName.ToString() : null);
        }
        internal static string GetVerboseLabel(Xbrl.Taxonomy.Resource resource, bool bFallbackToLabel = true)
        {
            var labels = resource.GetLabels("http://www.xbrl.org/2008/role/verboseLabel", null, null);
            return GetLabelWithLanguage(labels, LabelLanguage, bFallbackToLabel ? GetLabel(resource, true) : null);
        }
        internal static string GetRCCode(Xbrl.Table.Layout.AxisHeader header)
        {
            var labels = header.DefinitionNode.GetLabels("http://www.eurofiling.info/xbrl/role/rc-code", null, null);
            if (labels.Count == 0)
                labels = header.Axis.GetDefinitionBreakdown(header.Row).GetLabels("http://www.eurofiling.info/xbrl/role/rc-code", null, null);
            if (labels.Count > 0)
                return labels.First().Text;
            return null;
        }
        internal static string GetRCCode(Xbrl.Taxonomy.Resource resource)
        {
            var labels = resource.GetLabels("http://www.eurofiling.info/xbrl/role/rc-code", null, null);
            if (labels.Count == 0)
                return null;
            return labels.First().Text;
        }
        internal static string GetOpenAspectHeaderLabel(Xbrl.Table.Layout.Axis axis, Xbrl.Table.AspectNode openAspectNode, int row)
        {
            string openAspectLabel = GetLabel(openAspectNode, false);
            if (openAspectLabel == null)
            {
                // Use breakdown label if the aspect node is the only child                
                var breakdown = axis.GetDefinitionBreakdown((uint)row);
                if (breakdown != null && breakdown.TreeRelationships.Count == 1)
                    openAspectLabel = GetLabel(breakdown, false);
            }
            if (openAspectLabel == null)
            {
                var aspect = openAspectNode.ParticipatingAspects.FirstOrDefault();
                if (aspect != null)
                    openAspectLabel = GetLabel(aspect.Dimension, true);
            }
            // Fallback to the id
            if (openAspectLabel == null)
                openAspectLabel = openAspectNode.Id;
            return openAspectLabel;
        }
        internal static string GetOpenAspectRCCode(Xbrl.Table.Layout.Axis axis, Xbrl.Table.AspectNode openAspectNode, int row)
        {
            string openAspectRC = GetRCCode(openAspectNode);
            if (openAspectRC == null)
                openAspectRC = GetRCCode(axis.GetDefinitionBreakdown((uint)row));
            return openAspectRC;
        }

        internal static string GetHeaderLabel(Xbrl.Table.Layout.Axis axis, Xbrl.Table.Layout.AxisHeader header, int row)
        {
            string label = GetLabel(header, false);
            if (label == null)
                label = GetLabel(axis.GetDefinitionBreakdown((uint)row), false);
            return label;
        }
        internal static string GetFilingIndicatorCode(Xbrl.Table.Table table)
        {
            var label = table.GetLabels("http://www.eurofiling.info/xbrl/role/filing-indicator-code", null, null).FirstOrDefault();
            return label == null ? null : label.Text;
        }
        internal static Xbrl.Table.Layout.AxisHeader GetAxisHeader(Xbrl.Table.Layout.Axis axis, int slice)
        {
            uint i = 0;
            Xbrl.Table.Layout.AxisHeader axisHeader;
            do
            {
                ++i;
                axisHeader = axis.GetHeader((uint)slice, axis.Shape.Y - i);
            }
            while (axisHeader != null && axisHeader.DefinitionNode is Xbrl.Table.AspectNode);
            return axisHeader;
        }
        internal static string GetWorksheetNameForTable(Xbrl.Table.Table table)
        {
            string rcCode = GetRCCode(table);
            if (rcCode != null)
                return rcCode;

            // Workaround for SRB entry point
            string filingIndicator = GetFilingIndicatorCode(table);
            string label = GetLabel(table);
            return String.Format("{0} - {1}", filingIndicator, label);
        }
        internal static Xbrl.InstanceSettings GetCreateInstanceOptions(Xbrl.Taxonomy.Dts dts = null)
        {
            var options = new Xbrl.InstanceSettings();
            options.PreloadXbrlSchemas = true;
            options.PreloadFormulaSchemas = true;
            options.PreloadTableSchemas = true;
            options.TableLinkbaseNamespace = "##detect";
            options.DTS = dts;
            options.UTR = Xbrl.UnitsRegistry.DefaultUTR;
            return options;
        }
        internal static Xbrl.Table.TableLayoutSettings GetTableLayoutOptions()
        {
            var options = new Xbrl.Table.TableLayoutSettings();
            options.TableElimination = false;
            options.PreserveEmptyAspectNodes = true;
            return options;
        }
        internal static Dictionary<string, bool> FilingIndicators(Xbrl.Instance instance)
        {
            var qnameFiled = new Xml.QName("filed", FilingIndicatorsNamespace);
            var conceptFilingIndicator = instance.Dts.ResolveConcept("filingIndicator", FilingIndicatorsNamespace) as Xbrl.Taxonomy.Item;
            var filingIndicators = new Dictionary<string, bool>();
            foreach (var fi in instance.Facts.Filter(conceptFilingIndicator, null, null))
            {
                var filedAttr = fi.Element.FindAttribute(qnameFiled);
                filingIndicators.Add(fi.Element.SchemaNormalizedValue, filedAttr == null || filedAttr.SchemaActualValue as Xsd.Boolean == true);
            }
            return filingIndicators;
        }
        static internal bool IsPercentItem(Xbrl.Taxonomy.Dts dts, Xbrl.Taxonomy.Item item)
        {
            var percentItemTypeDef = dts.Schema.ResolveTypeDefinition("percentItemType", "http://www.xbrl.org/dtr/type/numeric");
            return percentItemTypeDef != null && item.TypeDefinition.IsDerivedFrom(percentItemTypeDef);
        }
        static internal bool IsTableRowEmpty(Xbrl.Table.Layout.Table table, int y, uint z)
        {
            for (uint x = 0; x < table.Shape.X; ++x)
            {
                if (table.GetCell(x, (uint)y, z).Facts.Count > 0)
                    return false;
            }
            return true;
        }
        static internal bool HasOpenAspectNodes(Xbrl.Table.Layout.Axis axis)
        {
            return axis.GetSlice(0).Any(header => header.DefinitionNode is Xbrl.Table.AspectNode);
        }
        static internal Xbrl.Table.Layout.TableSet FindTableSet(Xbrl.Table.Layout.TableModel tableModel, Xbrl.Table.Table table)
        {
            return tableModel.Where(tableSet => tableSet.DefinitionTable == table).FirstOrDefault();
        }
    }
}
