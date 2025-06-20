﻿using System;
using System.Collections.Generic;
using System.Linq;
using Xml = Altova.RaptorXml.Xml;
using Xsd = Altova.RaptorXml.Xsd;
using Xbrl = Altova.RaptorXml.Xbrl;
using Altova.RaptorXml.Xbrl.Taxonomy;

namespace XbrlTablesToExcel
{
    internal class XbrlUtils
    {
        internal static string LabelLanguage = "en";
        internal static string FilingIndicatorsNamespace = "http://www.eurofiling.info/xbrl/ext/filing-indicators";

        internal static string GetLabel(object item, bool bFallbackToIdOrName = true, params string[] avoidLabels)
        {
            if (item is Xbrl.Table.Layout.AxisHeader)
                return GetLabel(item as Xbrl.Table.Layout.AxisHeader, bFallbackToIdOrName, avoidLabels);
            else if (item is Xbrl.Taxonomy.Concept)
                return GetLabel(item as Xbrl.Taxonomy.Concept, bFallbackToIdOrName, avoidLabels);
            else if (item is Xbrl.Taxonomy.Resource)
                return GetLabel(item as Xbrl.Taxonomy.Resource, bFallbackToIdOrName, avoidLabels);
            else if (item is Xml.ElementInformationItem)
                return (item as Xml.ElementInformationItem).QName.ToString();
            else
                return null;
        }
        internal static string GetLabelWithLanguage(Xbrl.Taxonomy.LabelCollection labels, string lang, string fallbackValue, params string[] avoidLabels )
        {
            Xbrl.Taxonomy.Label result = null;
            string lowerCaseLang = lang.ToLowerInvariant();
            foreach (var label in labels)
            {
                string lowerCaseLabelLang = label.XmlLang.ToLowerInvariant();
                if (lowerCaseLabelLang == lowerCaseLang)
                {
                    result = label;
                    if (!avoidLabels.Contains(label.Text, StringComparer.OrdinalIgnoreCase))
                        break;
                }
                if (lowerCaseLabelLang == "en")
                    result = label; // fallback to english
                if (result == null)
                    result = label; // fallback to any other language if no label with XmlLang == lang or "en" is found
            }
            return result == null ? fallbackValue : result.Text; // fallback to fallbackvalue if no label is found at all
        }
        internal static string GetLabel(Xbrl.Table.Layout.AxisHeader header, bool bFallbackToId = true, params string[] avoidLabels )
        {
            if (header != null)
            {
                var labels = header.DefinitionNode.GetLabels("http://www.xbrl.org/2008/role/label", null, null);
                if (labels.Count == 0)
                    labels = header.Axis.GetDefinitionBreakdown(header.Row).GetLabels("http://www.xbrl.org/2008/role/label", null, null);
                return GetLabelWithLanguage(labels, LabelLanguage, bFallbackToId ? header.DefinitionNode.Id : null, avoidLabels);
            }
            return null;
        }
        internal static string GetLabel(Xbrl.Taxonomy.Concept concept, bool bFallbackToQName = true, params string[] avoidLabels )
        {
            if (concept != null)
            {
                var labels = concept.GetLabels("http://www.xbrl.org/2003/role/label", null, null);
                return GetLabelWithLanguage(labels, LabelLanguage, bFallbackToQName ? concept.QName.ToString() : null, avoidLabels);
            }
            return null;
        }
        internal static string GetLabel(Xbrl.Taxonomy.Resource resource, bool bFallbackToId = true, params string[] avoidLabels )
        {
            if (resource != null)
            {
                var labels = resource.GetLabels("http://www.xbrl.org/2008/role/label", null, null);
                return GetLabelWithLanguage(labels, LabelLanguage, bFallbackToId ? resource.Id : null, avoidLabels);
            }
            return null;
        }
        internal static string GetVerboseLabel(Xbrl.Table.Layout.AxisHeader header, bool bFallbackToId = true, params string[] avoidLabels)
        {
            if (header != null)
            {
                var labels = header.DefinitionNode.GetLabels("http://www.xbrl.org/2003/role/verboseLabel", null, null);
                if (labels.Count == 0)
                    labels = header.Axis.GetDefinitionBreakdown(header.Row).GetLabels("http://www.xbrl.org/2003/role/verboseLabel", null, null);
                return GetLabelWithLanguage(labels, LabelLanguage, bFallbackToId ? header.DefinitionNode.Id : null, avoidLabels);
            }
            return null;
        }
        internal static string GetVerboseLabel(Xbrl.Taxonomy.Concept concept, bool bFallbackToQName = true, params string[] avoidLabels)
        {
            if (concept != null)
            {
                var labels = concept.GetLabels("http://www.xbrl.org/2003/role/verboseLabel", null, null);
                return GetLabelWithLanguage(labels, LabelLanguage, bFallbackToQName ? concept.QName.ToString() : null, avoidLabels);
            }
            return null;
        }
        internal static string GetVerboseLabel(Xbrl.Taxonomy.Resource resource, bool bFallbackToLabel = true, params string[] avoidLabels)
        {
            if (resource != null)
            {
                var labels = resource.GetLabels("http://www.xbrl.org/2008/role/verboseLabel", null, null);
                return GetLabelWithLanguage(labels, LabelLanguage, bFallbackToLabel ? GetLabel(resource, true, avoidLabels) : null, avoidLabels);
            }
            return null;
        }
        internal static string GetRCCode(Xbrl.Table.Layout.AxisHeader header)
        {
            if (header != null)
            {
                var labels = header.DefinitionNode.GetLabels("http://www.eurofiling.info/xbrl/role/rc-code", null, null);
                if (labels.Count == 0)
                    labels = header.Axis.GetDefinitionBreakdown(header.Row).GetLabels("http://www.eurofiling.info/xbrl/role/rc-code", null, null);
                if (labels.Count > 0)
                    return labels.First().Text;
            }
            return null;
        }
        internal static string GetRCCode(Xbrl.Taxonomy.Resource resource)
        {
            if (resource != null)
            {
                var labels = resource.GetLabels("http://www.eurofiling.info/xbrl/role/rc-code", null, null);
                if (labels.Count == 0)
                    return null;
                return labels.First().Text;
            }
            return null;
        }

        internal static Xbrl.Table.Breakdown GetOpenAspectDefinitionBreakdown(Table table, Xbrl.Table.AspectNode openAspectNode)
        {
            var breakdownTreeNetwork = table.Dts.GetNetworkOfRelationships(
                table.DefinitionTable.ExtendedLink.QName,
                table.DefinitionTable.ExtendedLink.XlinkRole,
                new Xml.QName("breakdownTreeArc", "http://xbrl.org/2014/table"),
                "http://xbrl.org/arcrole/2014/breakdown-tree"
                );
            var tableBreakdownNetwork = table.Dts.GetNetworkOfRelationships(
                table.DefinitionTable.ExtendedLink.QName,
                table.DefinitionTable.ExtendedLink.XlinkRole,
                new Xml.QName("tableBreakdownArc", "http://xbrl.org/2014/table"),
                "http://xbrl.org/arcrole/2014/table-breakdown"
                );
            if (breakdownTreeNetwork != null && tableBreakdownNetwork != null)
            {
                foreach (var breakdownTreeRel in breakdownTreeNetwork.GetRelationshipsTo(openAspectNode))
                {
                    foreach (var tableBreakdownRel in tableBreakdownNetwork.GetRelationshipsTo(breakdownTreeRel.SourceResource))
                    {
                        if (tableBreakdownRel.SourceResource == table.DefinitionTable)
                            return breakdownTreeRel.SourceResource as Xbrl.Table.Breakdown;
                    }
                }
            }
            return null;
        }

        internal static string GetOpenAspectHeaderLabel(Xbrl.Table.Layout.Axis axis, Xbrl.Table.AspectNode openAspectNode, int row)
        {
            string openAspectLabel = GetLabel(openAspectNode, false);
            if (openAspectLabel == null)
            {
                // Use breakdown label if the aspect node is the only child                
                var breakdown = axis.GetDefinitionBreakdown((uint)row);
                if (breakdown != null && breakdown.TreeRelationships.Count == 1)
                    openAspectLabel = GetLabel(breakdown, false, "rows", "columns"); // avoid a generic "rows" or "columns" as label
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

        internal static string GetOpenAspectHeaderLabel(Table table, Xbrl.Table.AspectNode openAspectNode)
        {
            string openAspectLabel = GetLabel(openAspectNode, false);
            if (openAspectLabel == null)
            {
                // Use breakdown label if the aspect node is the only child                
                var breakdown = GetOpenAspectDefinitionBreakdown(table, openAspectNode);
                if (breakdown != null && breakdown.TreeRelationships.Count == 1)
                    openAspectLabel = GetLabel(breakdown, false, "rows", "columns" ); // avoid a generic "rows" or "columns" as label
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
            {
                var breakdown = axis.GetDefinitionBreakdown((uint)row);
                openAspectRC = GetRCCode(breakdown);
            }
            return openAspectRC;
        }

        internal static string GetOpenAspectRCCode(Table table, Xbrl.Table.AspectNode openAspectNode)
        {
            string openAspectRC = GetRCCode(openAspectNode);
            if (openAspectRC == null)
            {
                var breakdown = GetOpenAspectDefinitionBreakdown(table, openAspectNode);
                if (breakdown != null)
                    openAspectRC = GetRCCode(breakdown);
            }
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
            options.PreserveEmptyAspectNodes = false;
            options.TableEliminationAspectNodes = true;

            return options;
        }
        internal static Dictionary<string, bool> FilingIndicators(Xbrl.Instance instance)
        {
            // Note: This only checks for tuple-based filing indicators. General filing indicator support will be added to Xbrl.Instance.
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
