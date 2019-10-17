using System.Collections.Generic;
using Xbrl = Altova.RaptorXml.Xbrl;

namespace XbrlTablesToExcel
{
    public class Table
    {
        public Report Report;
        public Xbrl.Table.Layout.Table LayoutTable;
        public uint ZSlice;

        public Xbrl.Taxonomy.Dts Dts => Report.Dts;
        public Xbrl.Table.Table DefinitionTable => LayoutTable.DefinitionTable;
        public string Title => XbrlUtils.GetVerboseLabel(DefinitionTable);
        public string RcCode => XbrlUtils.GetRCCode(DefinitionTable);

        public List<Xbrl.Table.AspectNode> OpenAspects;
        public bool HasOpenAspects => OpenAspects.Count > 0;
        public int OpenAspectCount => OpenAspects.Count;

        public int ColCount => (int)LayoutTable.Shape.X;
        public int RowCount => (int)LayoutTable.Shape.Y;

        public Xbrl.Table.Layout.Axis XAxis => LayoutTable.GetAxis(Xbrl.Table.AxisType.X);
        public Xbrl.Table.Layout.Axis YAxis => LayoutTable.GetAxis(Xbrl.Table.AxisType.Y);
        public Xbrl.Table.Layout.Axis ZAxis => LayoutTable.GetAxis(Xbrl.Table.AxisType.Z);


        public Table(Report report, Xbrl.Table.Layout.Table table, uint zSlice = 0)
        {
            Report = report;
            LayoutTable = table;
            ZSlice = zSlice;
            OpenAspects = GetOpenAspects();
        }

        public bool IsRowEmpty(int y)
        {
            for (int x = 0; x < LayoutTable.Shape.X; ++x)
            {
                if (LayoutTable.GetCell((uint)x, (uint)y, ZSlice).Facts.Count > 0)
                    return false;
            }
            return true;
        }

        public Xbrl.Table.Layout.Cell GetCell(int x, int y)
        {
            return LayoutTable.GetCell((uint)x, (uint)y, ZSlice);
        }

        public List<Xbrl.Table.AspectNode> GetOpenAspects()
        {
            var openAspects = new List<Xbrl.Table.AspectNode>();
            foreach (var header in YAxis.GetSlice(0))
            {
                if (header.DefinitionNode is Xbrl.Table.AspectNode)
                    openAspects.Add(header.DefinitionNode as Xbrl.Table.AspectNode);
            }
            return openAspects;
        }

        public List<string> GetZHeaders()
        {
            int row = 0;
            var zHeaders = new List<string>();
            var constraints = GetCell(0, 0).ConstraintSet;
            foreach (var header in ZAxis.GetSlice(ZSlice))
            {
                string label = XbrlUtils.GetHeaderLabel(ZAxis, header, row);
                if (header.DefinitionNode is Xbrl.Table.AspectNode)
                {
                    var aspect = (header.DefinitionNode as Xbrl.Table.AspectNode).ParticipatingAspect;
                    var val = constraints[aspect];
                    if (val.Aspect.Type == Xbrl.AspectType.Dimension)
                    {
                        if (val.Aspect.Dimension.IsExplicit)
                            label = XbrlUtils.GetLabel((val as Xbrl.ExplicitDimensionAspectValue).Value);
                        else
                            label = string.Format("{0} {1}", label, (val as Xbrl.TypedDimensionAspectValue).Value.SchemaNormalizedValue);
                    }
                }
                zHeaders.Add(label);
                ++row;
            }
            return zHeaders;
        }
    }
}
