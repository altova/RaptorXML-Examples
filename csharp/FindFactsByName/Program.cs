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

namespace FindFactsByName
{
    /// <summary>
    /// This example console application demonstrates how to load an XBRL instance, check for validation errors and search for specific facts (by concept name).
    /// </summary>
    /// <remarks>
    /// Given a SEC EDGAR filing, the values of all Assets facts are displayed.
    /// </remarks>
    class Program
    {
        static Taxonomy.Concept GetAssetsConcept(Taxonomy.Dts dts)
        {
            // Find the us-gaap namespace referenced within the DTS
            string usgaap_namespace = null;
            foreach (var taxonomy in dts.TaxonomySchemas)
            {
                if (taxonomy.TargetNamespace.StartsWith("http://fasb.org/us-gaap/"))
                {
                    usgaap_namespace = taxonomy.TargetNamespace;
                    break;
                }
            }
            if (usgaap_namespace == null)
                return null;

            // Get the us-gaap:Assets concept wihtin the DTS
            return dts.ResolveConcept("Assets", usgaap_namespace);
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
                System.Console.WriteLine(String.Format("Failed to load XBRL instance file {0}.",args[0]));
                foreach (var error in log)
                {
                    System.Console.WriteLine(error.Text);
                }
                return 1;
            }

            var concept = GetAssetsConcept(instance.Dts);
            if (concept == null)
            {
                System.Console.WriteLine("Taxonomy does not contain an Assets concept.");
                return 1;
            }

            // Find all US-GAAP Assets facts in the XBRL instance (filter instance facts by the concept aspect)
            var cs = new Xbrl.ConstraintSet();
            cs.Concept = new Xbrl.ConceptAspectValue(concept);
            var facts = instance.Facts.Filter(cs);
            
            System.Console.WriteLine(String.Format("Found {0} Assets facts.",facts.Count));
            foreach (Xbrl.Item fact in facts)
            {
                System.Console.WriteLine(String.Format("Assets fact in context {0} has the effective numeric value {1}.", fact.Context.Id, fact.EffectiveNumericValue));
            }
            System.Console.WriteLine();

            return 0;
        }
    }
}
