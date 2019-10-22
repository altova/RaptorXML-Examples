using ClosedXML.Excel;

namespace XbrlTablesToExcel
{
    enum HeaderType
    {
        XAxis,
        YAxis,
        ZAxis,
        Other
    }
    enum DataCellType
    {
        Boolean,
        Integer,
        Decimal,
        Percentage,
        Monetary,
        Date,
        DateTime,
        Label,
        QName,
        Text
    }

    interface ITableStyle
    {
        IXLRange ApplyTOCFormat(IXLRange range, int depth, bool isTable, bool isReported);
        IXLRange ApplyTitleFormat(IXLRange range);
        IXLRange ApplyTitleHeaderFormat(IXLRange range);
        IXLRange ApplyHeaderFormat(IXLRange range, HeaderType axis, bool bIsOpenAspect = false);
        IXLRange ApplyHeaderRcCodeFormat(IXLRange range, HeaderType axis, bool bIsOpenAspect = false);
        IXLRange ApplyDataCellFormat(IXLRange range, DataCellType type);
        IXLRange ApplyDataEmptyCellFormat(IXLRange range);
        IXLRange ApplyDataInvalidCellFormat(IXLRange range);
        IXLTable ApplyTableFormat(IXLTable table);
    }
}
