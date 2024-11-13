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

using Err = Altova.RaptorXml.ErrorLog;
using Xml = Altova.RaptorXml.Xml;
using XPath = Altova.RaptorXml.XPath;

namespace XPathApiExamples
{
	public class InitialContextXmlDocument : XPathExecutor
	{
		Xml.Instance LoadedXmlDoc = null;

		void LoadXmlDoc()
		{
			if (LoadedXmlDoc == null)
			{
				var fileName = @"c:\Program Files\Altova\RaptorXMLXBRLServer2025\examples\NanonullOrg.xml";
				var xmlDoc = Xml.Instance.CreateFromUrl(fileName, out Err loadErr);
				if (xmlDoc == null || loadErr.HasErrors)
				{
					PrintErrors("error loading document", loadErr);
				}
				else
				{
					LoadedXmlDoc = xmlDoc;
				}
			}
		}

		protected override void InitCompileOptions(XPath.CompileOptions compileOptions)
		{
			base.InitCompileOptions(compileOptions);
			LoadXmlDoc();
			if (LoadedXmlDoc != null)
			{
				compileOptions.StaticallyKnownNamespaces = new XPath.StringDict() { { "ns1", LoadedXmlDoc.DocumentElement.NamespaceName } };
			}
		}
		protected override void InitRuntimeOptions(XPath.RuntimeOptions runtimeOptions)
		{
			base.InitRuntimeOptions(runtimeOptions);
			if (LoadedXmlDoc != null)
			{
				//Create and set the initial context node
				runtimeOptions.InitialContext = XPath.NodeItem.FromInformationItem(LoadedXmlDoc.DocumentItem, session);
			}
		}
		public override string GetDefaultExpr()
		{
			return "distinct-values(/ns1:OrgChart//*:Department/*:Name) (:uses wildcard to match the elements regardless of their namespace:)";
		}

		public static int Main(string[] args)
		{
			return SampleRunner<InitialContextXmlDocument>.Run(args);
		}
	}
}
