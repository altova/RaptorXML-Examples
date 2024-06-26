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
using Xml = Altova.RaptorXml.Xml;
namespace WorkingWithXml
{
	class Program
	{
		public static int Main(string[] args)
		{
			// the session object holds the data required by the xpath/xquery/xslt engines
			var session = new XPath.Session();


			var fileName = @"c:\Program Files\Altova\RaptorXMLXBRLServer2024\examples\NanonullOrg.xml";
			var xmlDoc = Xml.Instance.CreateFromUrl(fileName, out Err loadErr);
			if (xmlDoc == null || loadErr.HasErrors)
			{
				Prn.PrintErrors(String.Format("Failed to load xml file {0}.", fileName), loadErr);
				return 1;
			}

			//get the default element namespace from the root element
			var defaultNs = xmlDoc.DocumentElement.FindInscopeNamespace("");

			var runtimeOptions = new XPath.RuntimeOptions(session);
			runtimeOptions.InitialContext = XPath.NodeItem.FromInformationItem(xmlDoc.DocumentItem, session);
			//Prn.PrintSequence(XPath.Sequence.FromItem(runtimeOptions.InitialContext));
			string exprFormatStr = "distinct-values(/{0}OrgChart//{0}Department/{0}Name) (:{1}:)";
			var myList = new List<string>();
			myList.Add( String.Format(exprFormatStr, "", "this expression requires the default element namespace to be set") );
			myList.Add( String.Format(exprFormatStr, "*:", "this expression uses wildcards to match the nodes by local names only, regardless of their namespace") );
			myList.Add( String.Format(exprFormatStr, "prefix1:", "use a different prefix that is mapped to the namespace-uri in the StaticallyKnownNamespaces") );
			myList.Add( String.Format("/{0}OrgChart//{0}Office/{0}Name (:this expression uses the XPath 3.1 EQName notation:)", "Q{" + defaultNs + "}") ); // specifies the namespace-uri in the nodetests explicitly, rather than relying on in-scope-namespaces & DefaultElementNamespace

			foreach (var strExpr in myList)
			{
				System.Console.WriteLine(String.Format("Processing xpath expression: {0}",strExpr));
				var compileOptions = new XPath.CompileOptions(session);
				//compileOptions.DefaultElementNamespace = defaultNs; //comment out this line to see the effect of empty DefaultElementNamespace
				compileOptions.StaticallyKnownNamespaces = new XPath.StringDict() { { "prefix1", defaultNs } };
				var expr = XPath.Expression.Compile(strExpr, compileOptions, out Err logCompile);
				if (expr == null || logCompile.HasErrors)
				{
					Prn.PrintErrors(String.Format("Failed to compile xpath expression {0}.", strExpr), logCompile);
					return 1;
				}

				//now execute
				var res = expr.Execute(runtimeOptions, out Err execErr);
				if (res != null)
				{
					Prn.PrintSequence(res);
				}
				System.Console.WriteLine();
			}
			return 0;
		}
	}
}
