// Copyright 2016 Altova GmbH
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbrl = Altova.RaptorXml.Xbrl;
using Taxonomy = Altova.RaptorXml.Xbrl.Taxonomy;

namespace InstanceStatistics
{
    /// <summary>
    /// This example console application demonstrates how to access the data model of the XBRL instance and the supporting DTS.
    /// </summary>
    /// <remarks>
    /// Given any XBRL instance, some statistics about the XBRL instance the it's DTS are displayed.
    /// </remarks>
    class Program
    {
        static void DisplayDtsStatistics(Taxonomy.Dts dts)
        {
            System.Console.WriteLine(String.Format("DTS contains {0} documents.", dts.Documents.Count));
            System.Console.WriteLine(String.Format("DTS contains {0} taxonomy schemas.", dts.TaxonomySchemas.Count));
            System.Console.WriteLine(String.Format("DTS contains {0} linkbases.", dts.Linkbases.Count));
            System.Console.WriteLine();

            System.Console.WriteLine(String.Format("DTS contains {0} concepts.", dts.Concepts.Count));
            System.Console.WriteLine(String.Format("DTS contains {0} tuples.", dts.Tuples.Count));
            System.Console.WriteLine(String.Format("DTS contains {0} non-xdt items.", dts.Items.Count - dts.Hypercubes.Count - dts.Dimensions.Count));
            System.Console.WriteLine(String.Format("DTS contains {0} hypercubes.", dts.Hypercubes.Count));
            System.Console.WriteLine(String.Format("DTS contains {0} dimensions.", dts.Dimensions.Count));
            System.Console.WriteLine();

            System.Console.WriteLine(String.Format("DTS contains {0} parameters.", dts.Parameters.Count));
            System.Console.WriteLine(String.Format("DTS contains {0} assertions.", dts.Assertions.Count));
            System.Console.WriteLine(String.Format("DTS contains {0} formulas.", dts.Formulas.Count));
            System.Console.WriteLine(String.Format("DTS contains {0} tables.", dts.Tables.Count));
            System.Console.WriteLine();

            System.Console.WriteLine(String.Format("DTS contains {0} definition linkroles.", dts.GetDefinitionLinkRoles().Count));
            System.Console.WriteLine(String.Format("DTS contains {0} presentation linkroles.", dts.GetPresentationLinkRoles().Count));
            System.Console.WriteLine(String.Format("DTS contains {0} calculation linkroles.", dts.GetCalculationLinkRoles().Count));
            System.Console.WriteLine(String.Format("DTS contains {0} label linkroles.", dts.GetLabelLinkRoles().Count));
            System.Console.WriteLine(String.Format("DTS contains {0} reference linkroles.", dts.GetReferenceLinkRoles().Count));
            System.Console.WriteLine();
        }

        static void DisplayInstanceStatistics(Xbrl.Instance instance)
        {
            System.Console.WriteLine(String.Format("Instance contains {0} contexts.", instance.Contexts.Count));
            System.Console.WriteLine(String.Format("Instance contains {0} instant period contexts.", instance.Contexts.Count(c => c.PeriodAspectValue.Type == Xbrl.PeriodType.Instant)));
            System.Console.WriteLine(String.Format("Instance contains {0} start/end period contexts.", instance.Contexts.Count(c => c.PeriodAspectValue.Type == Xbrl.PeriodType.StartEnd)));
            System.Console.WriteLine(String.Format("Instance contains {0} forever period contexts.", instance.Contexts.Count(c => c.PeriodAspectValue.Type == Xbrl.PeriodType.Forever)));
            System.Console.WriteLine();

            System.Console.WriteLine(String.Format("Instance contains {0} units.", instance.Units.Count));
            System.Console.WriteLine(String.Format("Instance contains {0} simple units.", instance.Units.Count(u => u.AspectValue.IsSimple)));
            System.Console.WriteLine(String.Format("Instance contains {0} currency units.", instance.Units.Count(u => u.AspectValue.IsMonetary)));
            System.Console.WriteLine();

            System.Console.WriteLine(String.Format("Instance contains {0} facts.", instance.Facts.Count));
            System.Console.WriteLine(String.Format("Instance contains {0} nil facts.", instance.NilFacts.Count));
            System.Console.WriteLine(String.Format("Instance contains {0} top-level item facts.", instance.ChildItems.Count));
            System.Console.WriteLine(String.Format("Instance contains {0} top-level tuple facts.", instance.ChildTuples.Count));
            System.Console.WriteLine();

            var footnoteCounts = new Dictionary<string, int>();
            foreach (var footnoteLink in instance.FootnoteLinks)
            {
                foreach (var footnote in footnoteLink.Resources.OfType<Taxonomy.Footnote>())
                {
                    int count;
                    footnoteCounts.TryGetValue(footnote.XmlLang, out count);
                    footnoteCounts[footnote.XmlLang] = count + 1;
                }
            }
            if (footnoteCounts.Count > 0)
            {
                foreach (var lang in footnoteCounts)
                {
                    System.Console.WriteLine(String.Format("Instance contains {0} footnote resources in language {1}.", lang.Value, lang.Key));
                }
            }
            else
            {
                System.Console.WriteLine("Instance does not contain any footnote resources.");
            }
            System.Console.WriteLine();
        }

        static int Main(string[] args)
        {
            // Check if a command line argument was specified
            if (args.Length == 0)
            {
                System.Console.WriteLine("Please specify an XBRL instance file.");
                return 1;
            }

            // Load XBRL instance into memory
            Altova.RaptorXml.ErrorLog log;
            var instance = Xbrl.Instance.CreateFromUrl(args[0], out log);

            // Check for validation errors
            if (instance == null || log.HasErrors)
            {
                // Report validation errors
                System.Console.WriteLine(String.Format("Failed to load XBRL instance file {0}.", args[0]));
                foreach (var error in log)
                {
                    System.Console.WriteLine(error.Text);
                }
                return 1;
            }

            // Display some statistics about the XBRL instance and it's DTS
            DisplayDtsStatistics(instance.Dts);
            DisplayInstanceStatistics(instance);

            return 0;
        }
    }
}
