// Copyright 2024 Altova GmbH
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

using XPath = Altova.RaptorXml.XPath;
using Err = Altova.RaptorXml.ErrorLog;
using Xsd = Altova.RaptorXml.Xsd;
using Prn = XPathDataUtils.Print;

namespace GettingStartedWithVariables
{
	class Program
	{
		public static int Main(string[] args)
		{
			// the session object holds the data required by the xpath/xquery/xslt engines
			var session = new XPath.Session();

			string s = args.Length > 0 ? args[0] : "(-1 to 1)!(. div $x)";

			var compileOptions = new XPath.CompileOptions(session);
			compileOptions.Version = XPath.Version.V31;

			var expr = XPath.Expression.Compile(s, compileOptions, out Err logCompile);
			if (expr == null || logCompile.HasErrors)
			{
				Prn.PrintErrors(String.Format("Failed to compile xpath expression {0}.", s), logCompile);
				return 1;
			}

			//Succesfully compiled expression {0}, now execute it!
			var runtimeOptions = new XPath.RuntimeOptions(session);

			//Create and set variable name and value
			var varName = XPath.AtomicItem.FromQName(new Xsd.QName("x"), session);

			runtimeOptions.ExternalVariables = new XPath.MapItem(session)
				{
					 { varName, XPath.Sequence.FromItem(XPath.AtomicItem.FromDouble(0, session)) }
				};

			System.Console.WriteLine("\nDividing with xs:double(0) results in -INF, NAN, INF");
			ExecuteAndPrint(expr, runtimeOptions);

			// Note: one can't modify the map directly with ExternalVariables.Add(varName, varValue0), 
			// XPath.MapItem has an immutable copy on write internal implementation
			// the Add(k,v) creates a new updated internal implementation on a temporary XPath.MapItem
			// workaround reassign the modified object to runtimeOptions.ExternalVariables
			var varMap = new XPath.MapItem(runtimeOptions.ExternalVariables);
			varMap.Clear();
			varMap.Add(varName, XPath.Sequence.FromItem(XPath.AtomicItem.FromInt(0, session)));
			runtimeOptions.ExternalVariables = varMap;
			System.Console.WriteLine("\nDemonstrating error handling: Dividing by xs:int(0)");
			ExecuteAndPrint(expr, runtimeOptions);

			return 0;
		}
		private static XPath.Sequence? ExecuteAndPrint(XPath.Expression expr, XPath.RuntimeOptions runtimeOptions)
		{
			var result = expr.Execute(runtimeOptions, out Err logExecute);
			if (result == null || logExecute.HasErrors)
			{
				Prn.PrintErrors("Failed to execute the xpath expression", logExecute);
			}
			else
			{
				Prn.PrintSequence(result);
			}
			return result;
		}
	}
}
