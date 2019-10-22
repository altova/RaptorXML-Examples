using ClosedXML.Excel;

namespace XbrlTablesToExcel
{
    class DefaultTableStyle : ITableStyle
    {
        public IXLRange ApplyTOCFormat(IXLRange range, int depth, bool isTable, bool isReported)
        {
            range.DataType = XLDataType.Text;
            if (isTable)
            {
                if (isReported)
                {
                    range.Style.Font.Bold = depth == 0;
                    range.Style.Font.Underline = XLFontUnderlineValues.Single;
                }
                else
                {
                    range.Style.Font.FontColor = XLColor.LightGray;
                }
            }
            else
            {
                range.Style.Font.Bold = depth == 0;
                range.Style.Font.Underline = XLFontUnderlineValues.Single;
            }
            range.Style.NumberFormat.NumberFormatId = 49; // @
            return range;
        }

        public IXLRange ApplyTitleFormat(IXLRange range)
        {
            if (range.ColumnCount() > 1)
                range.Merge();
            range.Style.Alignment.WrapText = true;
            range.Style.Border.OutsideBorder = XLBorderStyleValues.None;
            range.Style.Fill.BackgroundColor = XLColor.FromTheme(XLThemeColor.Accent1);
            range.Style.Font.FontColor = XLColor.FromTheme(XLThemeColor.Text1);
            range.Style.Font.Bold = true;
            range.Style.NumberFormat.NumberFormatId = 49; // @
            range.DataType = XLDataType.Text;
            return range;
        }
        public IXLRange ApplyTitleHeaderFormat(IXLRange range)
        {
            range.Merge();
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            range.Style.Alignment.WrapText = true;
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Fill.BackgroundColor = XLColor.FromTheme(XLThemeColor.Background2);
            range.Style.Font.Bold = true;
            range.Style.Font.FontColor = XLColor.FromTheme(XLThemeColor.Text1);
            range.Style.NumberFormat.NumberFormatId = 49; // @
            range.DataType = XLDataType.Text;
            return range;
        }
        public IXLRange ApplyHeaderFormat(IXLRange range, HeaderType axis, bool bIsOpenAspect = false)
        {
            if (range.ColumnCount() > 1 || range.RowCount() > 1)
                range.Merge();

            if (axis == HeaderType.XAxis && range.ColumnCount() > 1)
                range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            else if (axis == HeaderType.YAxis && range.RowCount() > 1)
                range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            range.Style.Alignment.WrapText = axis != HeaderType.YAxis;
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Fill.BackgroundColor = bIsOpenAspect ? XLColor.FromTheme(XLThemeColor.Background2, 0.4) : XLColor.FromTheme(XLThemeColor.Background2);
            range.Style.Font.FontColor = XLColor.FromTheme(XLThemeColor.Text1);
            range.Style.NumberFormat.NumberFormatId = 49; // @
            range.DataType = XLDataType.Text;
            return range;
        }
        public IXLRange ApplyHeaderRcCodeFormat(IXLRange range, HeaderType axis, bool bIsOpenAspect = false)
        {
            range.DataType = XLDataType.Text;
            range.Style.Alignment.WrapText = true;
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Fill.BackgroundColor = XLColor.FromTheme(XLThemeColor.Background2, -0.2);
            range.Style.Font.FontColor = XLColor.FromTheme(XLThemeColor.Text1);
            range.Style.NumberFormat.NumberFormatId = 49; // @
            range.DataType = XLDataType.Text;
            return range;
        }
        public IXLRange ApplyDataCellFormat(IXLRange range, DataCellType type)
        {
            //range.Style.Fill.BackgroundColor = XLColor.FromTheme(XLThemeColor.Background1);
            switch (type)
            {
                case DataCellType.Boolean:
                    range.DataType = XLDataType.Boolean;
                    break;
                case DataCellType.Percentage:
                    range.DataType = XLDataType.Number;
                    range.Style.NumberFormat.NumberFormatId = 9; // 0%
                    break;
                case DataCellType.Integer:
                    range.DataType = XLDataType.Number;
                    range.Style.NumberFormat.NumberFormatId = 1; // 0
                    break;
                case DataCellType.Decimal:
                    range.DataType = XLDataType.Number;
                    range.Style.NumberFormat.Format = "#,##0.######";
                    break;
                case DataCellType.Monetary:
                    range.DataType = XLDataType.Number;
                    range.Style.NumberFormat.NumberFormatId = 4; // #,##0.00
                    break;
                case DataCellType.Date:
                    range.DataType = XLDataType.DateTime;
                    range.Style.NumberFormat.NumberFormatId = 14; // d/m/yyyy
                    break;
                case DataCellType.DateTime:
                    range.DataType = XLDataType.DateTime;
                    range.Style.NumberFormat.NumberFormatId = 22; // m/d/yyyy H:mm
                    break;
                case DataCellType.Label:
                    range.DataType = XLDataType.Text;
                    range.Style.Alignment.WrapText = true;
                    range.Style.NumberFormat.NumberFormatId = 49; // @
                    break;
                case DataCellType.Text:
                    range.DataType = XLDataType.Text;
                    range.Style.Alignment.WrapText = true;
                    range.Style.NumberFormat.NumberFormatId = 49; // @
                    break;
            }
            return range;
        }
        public IXLRange ApplyDataEmptyCellFormat(IXLRange range)
        {
            //range.Style.Fill.BackgroundColor = XLColor.FromTheme(XLThemeColor.Background1);
            return range;
        }
        public IXLRange ApplyDataInvalidCellFormat(IXLRange range)
        {
            range.Style.Border.DiagonalBorder = XLBorderStyleValues.Thin;
            range.Style.Border.DiagonalUp = true;
            range.Style.Border.DiagonalDown = true;
            range.Style.Fill.BackgroundColor = XLColor.FromTheme(XLThemeColor.Text2);
            return range;
        }
        public IXLTable ApplyTableFormat(IXLTable table)
        {
            table.Theme = XLTableTheme.None;
            table.DataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            return table;
        }
    }
}
