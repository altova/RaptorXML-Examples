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

namespace FindDuplicateUnits
{
    /// <summary>
    /// This example console application demonstrates how to load an XBRL instance, check for validation errors and search for duplicate units.
    /// </summary>
    /// <remarks>
    /// Given any XBRL instance, all duplicate units are displayed.
    /// </remarks>
    class Program
    {
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

            // Find all duplicate units
            var unitDict = new Dictionary<Xbrl.UnitAspectValue, Xbrl.Unit>();
            foreach (var unit in instance.Units)
            {
                try
                {
                    unitDict.Add(unit.AspectValue, unit);
                }
                catch (ArgumentException)
                {
                    System.Console.WriteLine(String.Format("Unit {0} is a duplicate of unit {1}.", unit.Id, unitDict[unit.AspectValue].Id));
                }
            }
            System.Console.WriteLine();

            return 0;
        }
    }
}
