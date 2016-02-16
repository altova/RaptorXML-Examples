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
using Xdt = Altova.RaptorXml.Xbrl.Xdt;

namespace DimensionalRelationshipSetTraversal
{
    /// <summary>
    /// This example console application demonstrates how to load an XBRL instance, check for validation errors and traverse the dimensional relationship set.
    /// </summary>
    /// <remarks>
    /// Given any XBRL instance, the dimensional relationship set for each linkrole are displayed.
    /// </remarks>
    class Program
    {
        static string GetDefinition(Taxonomy.Dts dts, string roleUri)
        {
            var roleType = dts.GetRoleType(roleUri);
            if (roleType != null)
            {
                var definition = roleType.Definition;
                if (definition != null)
                    return definition.Value;
            }
            return roleUri;
        }

        static string GetLabel(Taxonomy.Concept concept)
        {
            // Try to find a standard English label
            var labels = concept.GetLabels("http://www.xbrl.org/2003/role/label", null, "en");
            if (labels.Count > 0)
                return labels.First().Text;

            // Fallback to any other label that is assigned to the concept
            labels = concept.Labels;
            if (labels.Count > 0)
                return labels.First().Text;

            // If there are no labels, display the concept QName
            return concept.QName.ToString();
        }

        static void DisplayDomainMembers(Xdt.DRS drs, Taxonomy.Item item, string linkRole, int level = 1)
        {
            System.Console.WriteLine(String.Format("{0}{1}", new String(' ', level * 3), GetLabel(item)));
            foreach (var rel in drs.GetDomainMemberRelationships(item, linkRole))
            {
                DisplayDomainMembers(drs, rel.Target, rel.TargetRole ?? linkRole, level + 1);
            }
        }

        static void DisplayDimensions(Xdt.DRS drs, Xdt.Dimension dimension, string linkRole, int level = 1)
        {
            System.Console.WriteLine(String.Format("{0}{1}", new String(' ', level * 3), GetLabel(dimension)));
            foreach (var rel in drs.GetDimensionDomainRelationships(dimension, linkRole))
            {
                DisplayDomainMembers(drs, rel.Target, rel.TargetRole ?? linkRole, level + 1);
            }
        }

        static void DisplayHypercubes(Xdt.DRS drs, Xdt.Hypercube hypercube, string linkRole, int level = 1)
        {
            System.Console.WriteLine(String.Format("{0}{1}", new String(' ', level * 3), GetLabel(hypercube)));
            foreach (var rel in drs.GetHypercubeDimensionRelationships(hypercube, linkRole))
            {
                DisplayDimensions(drs, rel.Target, rel.TargetRole ?? linkRole, level + 1);
            }
        }

        static void DisplayPrimaryItems(Xdt.DRS drs, Taxonomy.Item item, string linkRole, int level = 1)
        {
            System.Console.WriteLine(String.Format("{0}{1}", new String(' ', level * 3), GetLabel(item)));
            foreach (var rel in drs.GetHashypercubeRelationships(item, linkRole))
            {
                DisplayHypercubes(drs, rel.Target, rel.TargetRole ?? linkRole, level + 1);
            }
            foreach (var rel in drs.GetDomainMemberRelationships(item, linkRole))
            {
                DisplayPrimaryItems(drs, rel.Target, rel.TargetRole ?? linkRole, level + 1);
            }
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

            // Display the dimensional relationship tree tree for each linkrole
            var drs = instance.Dts.DimensionalRelationshipSet;
            foreach (var linkRole in drs.LinkRoles)
            {
                System.Console.WriteLine(String.Format("{0} - {1}", GetDefinition(instance.Dts, linkRole), linkRole));
                foreach (var concept in drs.GetRoots(linkRole))
                {
                    DisplayPrimaryItems(drs, concept, linkRole);
                }
                System.Console.WriteLine();
            }

            return 0;
        }
    }
}
