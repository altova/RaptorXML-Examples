using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using Xml = Altova.RaptorXml.Xml;
using Xsd = Altova.RaptorXml.Xsd;
using Xbrl = Altova.RaptorXml.Xbrl;
using System.Text;

namespace XbrlTablesToExcel
{
    class ExcelWriter
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        ITableStyle Style;
        int XOffset = 0;
        int YOffset = 0;
        int EmptyRowsAfterTitle = 1;

        public ExcelWriter(ITableStyle style, int xOffset, int yOffset, int emptyRowsAfterTitle)
        {
            Style = style;
            XOffset = xOffset;
            YOffset = yOffset;
            EmptyRowsAfterTitle = emptyRowsAfterTitle;
        }

        void WriteTitle(Table table, IXLWorksheet ws, int xOffset = 0, int yOffset = 0)
        {
            var range = ws.Range(yOffset + 1, xOffset + 1, yOffset + 1, xOffset + (int)table.ColCount + (table.HasOpenAspects ? (int)table.OpenAspectCount : (int)table.YAxis.RowCount + 1));
            Style.ApplyTitleFormat(range).Value = table.Title;
        }

        int WriteZHeaders(Table table, IXLWorksheet ws, int xOffset = 0, int yOffset = 0)
        {
            var constraints = table.GetCell(0, 0).ConstraintSet;
            var yaxisCols = table.HasOpenAspects ? table.OpenAspectCount : (int)table.YAxis.RowCount + 1;
            var axis = table.ZAxis;

            int row = 0;
            foreach (var header in axis.GetSlice(table.ZSlice))
            {
                if (header.DefinitionNode is Xbrl.Table.AspectNode)
                {
                    string rcCode = XbrlUtils.GetOpenAspectRCCode(axis, header.DefinitionNode as Xbrl.Table.AspectNode, row);
                    string zAxisLabel = XbrlUtils.GetOpenAspectHeaderLabel(axis, header.DefinitionNode as Xbrl.Table.AspectNode, row);
                    string label = rcCode != null ? rcCode + " " + zAxisLabel : zAxisLabel;
                    var aspect = header.DefinitionNode.ParticipatingAspects.FirstOrDefault();

                    var range = ws.Range(yOffset + row + 1, xOffset + 1, yOffset + row + 1, xOffset + yaxisCols);
                    Style.ApplyHeaderFormat(range, HeaderType.ZAxis, true).Value = label;

                    var cell = ws.Cell(yOffset + row + 1, xOffset + yaxisCols + 1).AsRange();
                    WriteTableDataCell(table.Dts, constraints[aspect], cell);
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }
                else
                {
                    string rcCode = XbrlUtils.GetRCCode(header);
                    string zAxisLabel = XbrlUtils.GetHeaderLabel(axis, header, row);
                    string label = rcCode != null ? rcCode + " " + zAxisLabel : zAxisLabel;

                    var range = ws.Range(yOffset + row + 1, xOffset + 1, yOffset + row + 1, xOffset + yaxisCols);
                    Style.ApplyHeaderFormat(range, HeaderType.ZAxis).Value = label;
                }
                ++row;
            }
            return row;
        }

        void WriteXAxis(Table table, IXLWorksheet ws, int xOffset = 0, int yOffset = 0)
        {
            var axis = table.XAxis;
            int shapeX = (int)axis.Shape.X;
            int shapeY = (int)axis.Shape.Y;

            if (table.HasOpenAspects)
            {
                // Set column widths
                ws.Columns(xOffset + 1, xOffset + (int)table.OpenAspectCount + shapeX).Width = 15;

                for (int x = 0; x < table.OpenAspectCount; ++x)
                {
                    // Write open header columns
                    var range = ws.Range(yOffset + 1, xOffset + x + 1, yOffset + shapeY, xOffset + x + 1);
                    Style.ApplyHeaderFormat(range, HeaderType.XAxis, true).Value = XbrlUtils.GetOpenAspectHeaderLabel(table, table.OpenAspects[x]);

                    // Write open header RC codes
                    string rcCode = XbrlUtils.GetOpenAspectRCCode(table, table.OpenAspects[x]);
                    range = ws.Cell(yOffset + shapeY + 1, xOffset + x + 1).AsRange();
                    Style.ApplyHeaderRcCodeFormat(range, HeaderType.XAxis, true).Value = rcCode ?? (10 * (x + 1)).ToString();
                }

                xOffset += (int)table.OpenAspectCount;
            }
            else
            {
                // Set column widths
                int yaxisCols = (int)table.YAxis.RowCount + 1;
                ws.Columns(xOffset + yaxisCols + 1, xOffset + yaxisCols + shapeX).Width = 15;

                // Write top left cell
                var range = ws.Range(yOffset + 1, xOffset + 1, yOffset + shapeY + 1, xOffset + yaxisCols);
                Style.ApplyTitleHeaderFormat(range).Value = table.Title;

                xOffset += yaxisCols;
            }

            // Write X header rows
            for (int y = 0; y < shapeY; ++y)
            {
                for (int x = 0; x < shapeX; ++x)
                {
                    var header = axis.GetHeader((uint)x, (uint)y);

                    if (!header.IsRollup)
                    {
                        string value = null;
                        if (header.DefinitionNode is Xbrl.Table.AspectNode)
                            value = XbrlUtils.GetOpenAspectHeaderLabel(table, header.DefinitionNode as Xbrl.Table.AspectNode);
                        else
                            value = XbrlUtils.GetHeaderLabel(axis, header, y);

                        int span = (int)header.Span;
                        var finalRollupChild = header.LastRollupDescendent;

                        var range = ws.Range(yOffset + y + 1, xOffset + x + 1, yOffset + y + 1, xOffset + x + span);
                        if (span == 1 && finalRollupChild != null)
                            range = ws.Range(y + yOffset + 1, x + xOffset + 1, yOffset + (int)finalRollupChild.Row + 1, xOffset + x + 1);
                        Style.ApplyHeaderFormat(range, HeaderType.XAxis).Value = value;

                        if (span > 1 && finalRollupChild != null)
                        {
                            var range2 = ws.Range(yOffset + y + 2, xOffset + (int)finalRollupChild.Slice + 1, yOffset + (int)finalRollupChild.Row + 1, xOffset + (int)finalRollupChild.Slice + 1);
                            Style.ApplyHeaderFormat(range2, HeaderType.XAxis);

                            ws.Cell(yOffset + y + 1, xOffset + (int)finalRollupChild.Slice + 1).Style.Border.BottomBorder = XLBorderStyleValues.None;
                            ws.Cell(yOffset + y + 2, xOffset + (int)finalRollupChild.Slice + 1).Style.Border.TopBorder = XLBorderStyleValues.None;
                        }

                        x += span - 1;
                    }
                }
            }

            // Write row with RC codes
            for (int x = 0; x < shapeX; ++x)
            {
                var header = XbrlUtils.GetAxisHeader(axis, x);
                string rcCode = header != null ? XbrlUtils.GetRCCode(header) : XbrlUtils.GetRCCode(axis.GetDefinitionBreakdown(0));

                var range = ws.Cell(yOffset + shapeY + 1, xOffset + x + 1).AsRange();
                Style.ApplyHeaderRcCodeFormat(range, HeaderType.XAxis).Value = rcCode ?? (10 * (x + 1)).ToString();
            }
        }

        void WriteYAxis(Table table, IXLWorksheet ws, int xOffset = 0, int yOffset = 0)
        {
            var axis = table.YAxis;
            int shapeX = (int)axis.Shape.X;
            int shapeY = (int)axis.Shape.Y;

            bool[] arrVerticalTextCol = new bool[shapeY];
            for (uint y = 0; y < shapeY; ++y)
            {
                foreach (var header in axis.GetRow(y))
                {
                    if (header.Span > 1 && !header.IsRollup)
                    {
                        arrVerticalTextCol[y] = true;
                        break;
                    }
                }
            }

            int nMin = 0;
            for (int i = 1; i < shapeY; ++i)
            {
                if (arrVerticalTextCol[nMin] != arrVerticalTextCol[i])
                {
                    ws.Columns(xOffset + nMin + 1, xOffset + i + 1).Width = arrVerticalTextCol[nMin] ? 2 : 21;
                    nMin = i;
                }
            }
            ws.Columns(xOffset + nMin + 1, xOffset + shapeY).Width = nMin < arrVerticalTextCol.Length && arrVerticalTextCol[nMin] ? 2 : 21;
            ws.Column(xOffset + shapeY + 1).Width = 5;

            for (int y = 0; y < shapeX; ++y)
            {
                for (int x = 0; x < shapeY; ++x)
                {
                    var axisHeader = axis.GetHeader((uint)y, (uint)x);
                    if (!axisHeader.IsRollup)
                    {
                        var value = XbrlUtils.GetHeaderLabel(axis, axisHeader, y);
                        var finalRollupChild = axisHeader.LastRollupDescendent;

                        int span = (int)axisHeader.Span;
                        if (span > 1)
                        {
                            if (finalRollupChild != null)
                            {
                                if (y == finalRollupChild.Slice)
                                {
                                    var range = ws.Range(yOffset + y + 1, xOffset + x + 1, yOffset + y + 1, xOffset + (int)finalRollupChild.Row + 1);
                                    Style.ApplyHeaderFormat(range, HeaderType.YAxis).Value = value;
                                    if (y == axisHeader.Slice)
                                    {
                                        var range2 = ws.Range(yOffset + y + 2, xOffset + x + 1, yOffset + y + span, xOffset + x + 1);
                                        Style.ApplyHeaderFormat(range2, HeaderType.YAxis);

                                        range.FirstCell().Style.Border.BottomBorder = XLBorderStyleValues.None;
                                        range2.FirstCell().Style.Border.TopBorder = XLBorderStyleValues.None;
                                    }
                                    else
                                    {
                                        var range2 = ws.Range(yOffset + y - span + 2, xOffset + x + 1, yOffset + y, xOffset + x + 1);
                                        Style.ApplyHeaderFormat(range2, HeaderType.YAxis);

                                        range.FirstCell().Style.Border.TopBorder = XLBorderStyleValues.None;
                                        range2.LastCell().Style.Border.BottomBorder = XLBorderStyleValues.None;
                                    }
                                }
                            }
                            else
                            {
                                if (y == axisHeader.Slice)
                                {
                                    var range = ws.Range(yOffset + y + 1, xOffset + x + 1, yOffset + y + span, xOffset + x + 1);
                                    Style.ApplyHeaderFormat(range, HeaderType.YAxis).Value = value;
                                    range.Style.Alignment.TextRotation = 90;
                                }
                            }
                        }
                        else
                        {
                            var range = ws.Cell(yOffset + y + 1, xOffset + x + 1).AsRange();
                            if (finalRollupChild != null)
                                range = ws.Range(yOffset + y + 1, xOffset + x + 1, yOffset + y + 1, xOffset + (int)finalRollupChild.Row + 1);
                            Style.ApplyHeaderFormat(range, HeaderType.YAxis).Value = value;
                            if (arrVerticalTextCol[x] && finalRollupChild == null)
                                range.Style.Alignment.TextRotation = 90;
                        }
                    }
                }
            }

            // Write RC code column
            for (int y = 0; y < shapeX; ++y)
            {
                var axisHeader = XbrlUtils.GetAxisHeader(axis, y);
                var rcCode = axisHeader != null ? XbrlUtils.GetRCCode(axisHeader) : XbrlUtils.GetRCCode(axis.GetDefinitionBreakdown(0));

                var range = ws.Cell(yOffset + y + 1, xOffset + shapeY + 1).AsRange();
                Style.ApplyHeaderRcCodeFormat(range, HeaderType.YAxis).Value = rcCode ?? (10 * (y + 1)).ToString();
            }
        }

        void WriteTableDataCell(Xbrl.Taxonomy.Dts dts, Xsd.AnySimpleType val, IXLRange range)
        {
            if (val.IsBoolean)
                Style.ApplyDataCellFormat(range, DataCellType.Boolean).Value = (bool)(val as Xsd.Boolean);
            else if (val.IsInteger)
                Style.ApplyDataCellFormat(range, DataCellType.Integer).Value = (decimal)(val as Xsd.Integer);
            else if (val.IsDecimal)
                Style.ApplyDataCellFormat(range, DataCellType.Decimal).Value = (decimal)(val as Xsd.Decimal);
            else if (val.IsFloat)
                Style.ApplyDataCellFormat(range, DataCellType.Decimal).Value = (decimal)(val as Xsd.Float);
            else if (val.IsDouble)
                Style.ApplyDataCellFormat(range, DataCellType.Decimal).Value = (decimal)(val as Xsd.Double);
            else if (val is Xsd.Date)
                Style.ApplyDataCellFormat(range, DataCellType.Date).Value = (DateTime)(val as Xsd.Date);
            else if (val is Xsd.DateTime)
                Style.ApplyDataCellFormat(range, DataCellType.DateTime).Value = (DateTime)(val as Xsd.DateTime);
            else if (val is Xsd.QName)
            {
                var qnameConcept = dts.ResolveConcept(val as Xsd.QName);
                var label = qnameConcept != null ? XbrlUtils.GetLabel(qnameConcept) : null;
                Style.ApplyDataCellFormat(range, DataCellType.Label).Value = label ?? (val as Xsd.QName).ToString();
            }
            else
                Style.ApplyDataCellFormat(range, DataCellType.Text).Value = val.LexicalValue;
        }
        void WriteTableDataCell(Xbrl.Taxonomy.Dts dts, Xml.ElementInformationItem elem, IXLRange range)
        {
            var val = elem.SchemaActualValue;
            if (val != null)
                WriteTableDataCell(dts, val, range);
            else
                Style.ApplyDataCellFormat(range, DataCellType.Text).Value = elem.SchemaNormalizedValue;
        }
        void WriteTableDataCell(Xbrl.Taxonomy.Dts dts, Xbrl.Item item, IXLRange range)
        {
            var concept = item.Concept;
            if (concept.IsNumeric)
            {
                if (concept.IsMonetary)
                {
                    Style.ApplyDataCellFormat(range, DataCellType.Monetary).Value = item.NumericValue;
                }
                else if (XbrlUtils.IsPercentItem(dts, concept))
                {
                    Style.ApplyDataCellFormat(range, DataCellType.Monetary).Value = item.NumericValue;
                }
                else
                {
                    range.DataType = XLDataType.Number;
                    range.Value = item.NumericValue;

                    if (item.InferredDecimals.IsInfinity)
                        range.Style.NumberFormat.Format = "#,##0.######";
                    else if (item.InferredDecimals.Value > 0)
                        range.Style.NumberFormat.Format = "#,##0." + new string('0', item.InferredDecimals.Value);
                    else
                        range.Style.NumberFormat.NumberFormatId = 3; // #,##0
                }
            }
            else if (concept.IsBoolean)
            {
                Style.ApplyDataCellFormat(range, DataCellType.Boolean).Value = item.BooleanValue;
            }
            else if (concept.IsEnum)
            {
                WriteTableDataCell(dts, item.EnumValue, range);
            }
            else
                WriteTableDataCell(dts, item.Element, range);

            foreach (var footnote in item.Footnotes)
            {
                var cell = range.FirstCell();
                cell.Comment.Style.Alignment.SetAutomaticSize();
                cell.Comment.AddText(footnote.Text);
            }
        }
        void WriteTableDataCell(Xbrl.Taxonomy.Dts dts, Xbrl.Taxonomy.Item item, IXLRange range)
        {
            Style.ApplyDataCellFormat(range, DataCellType.Label).Value = XbrlUtils.GetLabel(item) ?? item.Name;
        }
        void WriteTableDataCell(Xbrl.Taxonomy.Dts dts, Xbrl.AspectValue val, IXLRange range)
        {
            if (val != null)
            {
                if (val.Aspect.Type == Xbrl.AspectType.Dimension)
                {
                    if (val is Xbrl.ExplicitDimensionAspectValue)
                        WriteTableDataCell(dts, (val as Xbrl.ExplicitDimensionAspectValue).Value, range);
                    else
                        WriteTableDataCell(dts, (val as Xbrl.TypedDimensionAspectValue).Value, range);
                }
                else
                    throw new NotImplementedException(string.Format("Formatting of {0} aspect values not supported", val.Aspect.Type));
            }
        }
        void WriteTableDataCell(Xbrl.Taxonomy.Dts dts, Xbrl.Table.Layout.Cell cell, IXLRange range)
        {
            var facts = cell.Facts;
            if (facts.Count == 0)
            {
                var constraintSet = cell.ConstraintSet;
                var concept = constraintSet.Concept?.Concept as Xbrl.Taxonomy.Item;
                if (concept != null && !dts.DimensionalRelationshipSet.IsDimensionallyValid(concept, constraintSet))
                    Style.ApplyDataInvalidCellFormat(range);
                else
                    Style.ApplyDataEmptyCellFormat(range);
            }
            else
            {
                if (facts.Count > 1)
                    Console.WriteLine("Warning: Cell contains multiple facts, only first is displayed");

                var item = facts[0] as Xbrl.Item;
                if (item != null && !item.XsiNil)
                    WriteTableDataCell(dts, item, range);
            }
        }

        void WriteTableData(Table table, IXLWorksheet ws, int xOffset = 0, int yOffset = 0)
        {
            int row = 0;
            for (int y = 0; y < table.RowCount; ++y)
            {
                bool emptyOpenAspects = false;
                if (table.OpenAspectCount > 0)
                {
                    if (table.IsRowEmpty(y))
                        continue;

                    var constraints = table.GetCell(0, y).ConstraintSet;
                    for (int x = 0; x < table.OpenAspectCount; ++x)
                    {
                        var cell = ws.Cell(yOffset + row + 1, xOffset + x + 1).AsRange();
                        var val = constraints[table.OpenAspects[x].ParticipatingAspect];
                        if (val != null)
                            WriteTableDataCell(table.Dts, val, cell);
                        else
                            emptyOpenAspects = true;
                    }
                }

                if (!emptyOpenAspects)
                {
                    for (int x = 0; x < table.ColCount; ++x)
                    {
                        var cell = ws.Cell(yOffset + row + 1, xOffset + table.OpenAspectCount + x + 1).AsRange();
                        WriteTableDataCell(table.Dts, table.GetCell(x, y), cell);
                    }
                }

                ++row;
            }

            var tbl = ws.Range(yOffset, xOffset + 1, yOffset + row, xOffset + table.OpenAspectCount + table.ColCount).CreateTable("data");
            tbl.ShowHeaderRow = false;
            tbl.ShowTotalsRow = false;
            Style.ApplyTableFormat(tbl);
        }

        public void WriteTable(Table table, IXLWorksheet ws, int xOffset, int yOffset)
        {
            Logger.Debug("Writing Table {0} z={1} worksheet", table.Title, table.ZSlice);

            WriteTitle(table, ws, xOffset, yOffset);
            yOffset += EmptyRowsAfterTitle + 1;

            int zHeaderRows = WriteZHeaders(table, ws, xOffset, yOffset);
            if (zHeaderRows > 0)
                yOffset += zHeaderRows + 1;

            if (table.HasOpenAspects)
            {
                WriteTableData(table, ws, xOffset, yOffset + (int)table.XAxis.RowCount + 1);
            }
            else
            {
                WriteTableData(table, ws, xOffset + (int)table.YAxis.RowCount + 1, yOffset + (int)table.XAxis.RowCount + 1);
                WriteYAxis(table, ws, xOffset, yOffset + (int)table.XAxis.RowCount + 1);
            }
            WriteXAxis(table, ws, xOffset, yOffset);
        }

        public IXLWorksheet WriteReportInfo(Report report, XLWorkbook wb, int xOffset, int yOffset)
        {
            Logger.Debug("Writing Report Info worksheet");

            var ws = wb.AddWorksheet("Report Info");
            ws.Column(xOffset + 1).Width = 2;
            ws.Column(xOffset + 2).Width = 21;
            ws.Column(xOffset + 3).Width = 41;

            Style.ApplyTitleFormat(ws.Range(yOffset + 1, xOffset + 1, yOffset + 1, xOffset + 3)).Value = "Report Info";
            yOffset += EmptyRowsAfterTitle + 1;

            int yOffsetStart = yOffset + 1;

            Style.ApplyHeaderFormat(ws.Range(yOffset + 1, xOffset + 1, yOffset + 1, xOffset + 2).Merge(), HeaderType.Other).Value = "Entry Point";
            Style.ApplyDataCellFormat(ws.Cell(yOffset + 1, xOffset + 3).AsRange(), DataCellType.Text).Value = report.EntryPointName;
            ++yOffset;

            Style.ApplyHeaderFormat(ws.Range(yOffset + 1, xOffset + 1, yOffset + 1, xOffset + 2).Merge(), HeaderType.Other).Value = "Entry Point URL";
            Style.ApplyDataCellFormat(ws.Cell(yOffset + 1, xOffset + 3).AsRange(), DataCellType.Text).Value = report.EntryPointUrl;
            ++yOffset;

            Style.ApplyHeaderFormat(ws.Range(yOffset + 1, xOffset + 1, yOffset + 1, xOffset + 2).Merge(), HeaderType.Other).Value = "Entity Identifier";
            Style.ApplyDataCellFormat(ws.Cell(yOffset + 1, xOffset + 3).AsRange(), DataCellType.Text).Value = report.ReportingEntityIdentifier;
            ++yOffset;

            Style.ApplyHeaderFormat(ws.Range(yOffset + 1, xOffset + 1, yOffset + 1, xOffset + 2).Merge(), HeaderType.Other).Value = "Entity Scheme";
            Style.ApplyDataCellFormat(ws.Cell(yOffset + 1, xOffset + 3).AsRange(), DataCellType.Text).Value = report.ReportingEntityScheme;
            ++yOffset;

            Style.ApplyHeaderFormat(ws.Range(yOffset + 1, xOffset + 1, yOffset + 1, xOffset + 2).Merge(), HeaderType.Other).Value = "Reporting Currency";
            Style.ApplyDataCellFormat(ws.Cell(yOffset + 1, xOffset + 3).AsRange(), DataCellType.Text).Value = report.ReportingCurrency;
            ++yOffset;

            Style.ApplyHeaderFormat(ws.Range(yOffset + 1, xOffset + 1, yOffset + 1, xOffset + 2).Merge(), HeaderType.Other).Value = "Reference Date";
            Style.ApplyDataCellFormat(ws.Cell(yOffset + 1, xOffset + 3).AsRange(), DataCellType.Date).Value = report.ReferenceDate;
            ++yOffset;

            if (report.FilingIndicators.Count > 0)
            {
                ws.Cell(yOffset, xOffset + 3).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                
                Style.ApplyHeaderFormat(ws.Cell(yOffset + 1, xOffset + 1).AsRange(), HeaderType.Other).Value = "Filing Indicators";
                ws.Range(yOffset + 1, xOffset + 1, yOffset + report.FilingIndicators.Count, xOffset + 1).Merge();
                ws.Range(yOffset + 1, xOffset + 1, yOffset + report.FilingIndicators.Count, xOffset + 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Cell(yOffset + 1, xOffset + 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Cell(yOffset + 1, xOffset + 1).Style.Alignment.TextRotation = 90;

                foreach (var code in report.FilingIndicators.Keys.OrderBy(x => x))
                {
                    Style.ApplyHeaderFormat(ws.Cell(yOffset + 1, xOffset + 2).AsRange(), HeaderType.Other).Value = code;
                    Style.ApplyDataCellFormat(ws.Cell(yOffset + 1, xOffset + 3).AsRange(), DataCellType.Boolean).Value = report.FilingIndicators[code];
                    ++yOffset;
                }
            }

            ws.Range(yOffsetStart, xOffset + 3, yOffset, xOffset + 3).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            return ws;
        }

        public void WriteReport(Report report, XLWorkbook wb)
        {
            Logger.Debug("Writing Report {0} workbook", report.Url);

            int xOffset = XOffset;
            int yOffset = YOffset;

            var ws = wb.AddWorksheet("TOC");
            Style.ApplyTitleFormat(ws.Range(yOffset + 1, xOffset + 1, yOffset + 1, xOffset + 1)).Value = "Table of Contents";
            yOffset += EmptyRowsAfterTitle + 1;

            var cell = ws.Cell(yOffset + 1, xOffset + 1);
            Style.ApplyTOCFormat(cell.AsRange(), 0, false, true).Value = "Report Info";
            cell.Hyperlink = new XLHyperlink(WriteReportInfo(report, wb, XOffset, YOffset).Cell(YOffset + 1, XOffset + 1));
            ++yOffset;

            int maxDepth = 0;
            var worksheetNames = new HashSet<string>();
            foreach (var tableTreeLine in report.GetTableTree())
            {
                cell = ws.Cell(yOffset + 1, xOffset + tableTreeLine.Depth + 1);
                Style.ApplyTOCFormat(cell.AsRange(), tableTreeLine.Depth, tableTreeLine.IsTable, tableTreeLine.IsReported).Value = tableTreeLine.Title;

                if (tableTreeLine.IsReported)
                {
                    var sheetName = CreateSheetName(tableTreeLine.Table, worksheetNames);
                    var wsTable = wb.AddWorksheet(sheetName);
                    WriteTable(tableTreeLine.Table, wsTable, XOffset, YOffset);
                    cell.Hyperlink = new XLHyperlink(wsTable.Cell(YOffset + 1, XOffset + 1), sheetName);
                }

                maxDepth = Math.Max(maxDepth, tableTreeLine.Depth);
                ++yOffset;
            }

            if (maxDepth > 0)
            {
                ws.Range(YOffset + 1, XOffset + 1, YOffset + 1, XOffset + maxDepth + 1).Merge();
                ws.Columns(xOffset + 1, maxDepth + xOffset).Width = 3;
            }
            ws.Column(xOffset + maxDepth + 1).Width = 50;
        }
        public XLWorkbook WriteReport(Report report)
        {
            var wb = new XLWorkbook();
            WriteReport(report, wb);
            return wb;
        }


        // Create valid sheet name from label and sheet-number
        static string CreateSheetName(Table table, HashSet<string> names)
        {
            var suffixes = GetTableSuffixNames(table);
            var suffix = suffixes.Count > 0 ? string.Join(".", suffixes) : null;
            var label = XbrlUtils.GetWorksheetNameForTable(table.DefinitionTable);

            StringBuilder builder = new StringBuilder(label.Length);
            int maxBaseNameLength = Math.Max(suffix != null ? 25 - suffix.Length : 28, 15);
            int i = 0;
            int nParanCount = 0;
            foreach (char c in label.Trim())
            {
                switch (c)
                {
                    case '(':
                    case '[':
                        ++nParanCount;
                        break;
                    case ')':
                    case ']':
                        --nParanCount;
                        break;
                    case '\\':
                    case '/':
                    case '?':
                    case '*':
                    case ':':
                        break;
                    default:
                        if (nParanCount == 0)
                        {
                            ++i;
                            builder.Append(c);
                        }
                        break;
                }
                if (i >= maxBaseNameLength)
                    break;
            }

            var baseName = builder.ToString().Trim();
            if (suffix != null && baseName.Length + suffix.Length > 25)
                suffix = suffix.Substring(0, 25 - baseName.Length); // adjust suffix length to allow at least 15 chars basename

            i = 0;
            string sheetName = baseName;
            while (true)
            {
                if (suffix != null)
                    sheetName = baseName + " (" + suffix + (i != 0 ? "." + i.ToString() : "") + ")";
                else
                    sheetName = baseName + (i != 0 ? "." + i.ToString() : "");
                if (!names.Contains(sheetName))
                {
                    names.Add(sheetName);
                    break;
                }
                ++i;
            }

            return sheetName;
        }
        static List<string> GetTableSuffixNames(Table table)
        {
            var suffixes = new List<string>();
            foreach (var header in table.ZAxis.GetSlice(table.ZSlice))
            {
                string label = null;
                if (header.DefinitionNode is Xbrl.Table.AspectNode)
                {
                    var constraints = table.GetCell(0, 0).ConstraintSet;
                    var aspect = header.DefinitionNode.ParticipatingAspects.FirstOrDefault();
                    if (aspect.Type == Xbrl.AspectType.Dimension)
                    {
                        if (aspect.Dimension.IsExplicit)
                        {
                            label = XbrlUtils.GetLabel((constraints[aspect] as Xbrl.ExplicitDimensionAspectValue).Value);
                            if (label == null || label.Length > 3)
                                label = (constraints[aspect] as Xbrl.ExplicitDimensionAspectValue).Value.Name;
                        }
                        else
                        {
                            var typedDimAspect = (constraints[aspect] as Xbrl.TypedDimensionAspectValue);
                            label = typedDimAspect != null ? typedDimAspect.Value.SchemaNormalizedValue : null;
                        }
                    }
                    else
                        label = constraints[aspect].ToString();
                }
                else if (table.ZAxis.SliceCount > 1)
                {
                    label = XbrlUtils.GetRCCode(header);
                    if (label == null)
                        label = XbrlUtils.GetLabel(header);
                }
                if (label != null)
                    suffixes.Add(System.Text.RegularExpressions.Regex.Replace(label, @"[/\\?*:[\]]", ""));
            }
            return suffixes;
        }
    }
}