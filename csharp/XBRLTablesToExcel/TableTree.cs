using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Xbrl = Altova.RaptorXml.Xbrl;

namespace XbrlTablesToExcel
{
    public struct TableTreeLine
    {
        public TableTreeLine(Table table, string title, int depth = 0)
        {
            Table = table;
            Title = title;
            Depth = depth;
        }

        public Table Table;
        public string Title;
        public int Depth;

        public bool IsGroup => Table == null;
        public bool IsTable => Table != null;
        public bool IsReported => Table?.IsReported ?? false;
    }

    public class TableTree : IEnumerable<TableTreeLine>
    {
        public List<TableTreeLine> Lines { get; set; }
        public int Count => Lines.Count;

        public IEnumerator<TableTreeLine> GetEnumerator()
        {
            return Lines.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public TableTree(Report report)
        {
            Lines = new List<TableTreeLine>();

            var netGroupTable = report.Dts.GetGenericNetwork("http://www.xbrl.org/2003/role/link", "http://www.eurofiling.info/xbrl/arcrole/group-table");
            if (netGroupTable != null)
            {
                foreach (var root in netGroupTable.Roots)
                {
                    TraverseTableTree(report, netGroupTable, root);
                }
            }
            else
            {
                foreach (var tableSet in report.TableModel)
                {
                    foreach (var table in tableSet)
                    {
                        for (uint z = 0; z < table.Shape.Z; ++z)
                        {
                            if (table.ContainsFactsInZSlice(z))
                            {
                                var tableInfo = new Table(report, table, z);
                                Lines.Add(new TableTreeLine(tableInfo, tableInfo.Title));
                            }
                        }
                    }
                }
                Lines.Sort((x, y) => x.Title.CompareTo(y.Title));
            }
        }
        void TraverseTableTree(Report report, Xbrl.Taxonomy.GenericRelationshipNetwork netGroupTable, object node, int depth = 0)
        {
            if (node is Xbrl.Table.Table)
            {
                var defTable = node as Xbrl.Table.Table;
                foreach (var table in XbrlUtils.FindTableSet(report.TableModel, defTable))
                {
                    var tableInfo = new Table(report, table);
                    if (table.GetAxis(Xbrl.Table.AxisType.Z).SliceCount > 1 || XbrlUtils.HasOpenAspectNodes(table.GetAxis(Xbrl.Table.AxisType.Z)))
                    {
                        Lines.Add(new TableTreeLine(null, tableInfo.Title, depth));
                        for (uint z = 0; z < table.Shape.Z; ++z)
                        {
                            if (table.ContainsFactsInZSlice(z))
                            {
                                tableInfo = new Table(report, table, z);
                                Lines.Add(new TableTreeLine(tableInfo, string.Join(", ", tableInfo.GetZHeaders()), depth + 1));
                            }
                        }
                    }
                    else if (table.ContainsFacts || tableInfo.IsReported)
                    {
                        Lines.Add(new TableTreeLine(tableInfo, tableInfo.Title, depth));
                    }
                }
            }
            else
            {
                if (ShowGroupHeader(netGroupTable, node))
                    Lines.Add(new TableTreeLine(null, XbrlUtils.GetLabel(node), depth++));

                foreach (var rel in netGroupTable.GetRelationshipsFrom(node))
                {
                    TraverseTableTree(report, netGroupTable, rel.Target, depth);
                }
            }
        }

        static bool ShowGroupHeader(Xbrl.Taxonomy.GenericRelationshipNetwork netGroupTable, object node)
        {
            if (netGroupTable.GetRelationshipsFrom(node).Count == 1)
            {
                var child = netGroupTable.GetRelationshipsFrom(node).First().Target;
                if (child is Xbrl.Table.Table)
                {
                    var tableLabel = Regex.Replace(XbrlUtils.GetVerboseLabel(child as Xbrl.Table.Table).ToLower(), @"[^0-9a-zA-Z]", string.Empty);
                    var groupLabel = Regex.Replace(XbrlUtils.GetLabel(node).ToLower(), @"[^0-9a-zA-Z]", string.Empty);
                    return !tableLabel.Contains(groupLabel);
                }
            }
            return true;
        }
    }
}
