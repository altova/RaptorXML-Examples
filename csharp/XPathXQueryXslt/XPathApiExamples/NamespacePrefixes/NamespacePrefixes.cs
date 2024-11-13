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

namespace XPathApiExamples
{
	public class NamespacePrefixes : XPathExecutor
	{
		const string MyNamespaceUri = "my-namespace-uri";
		protected override void InitCompileOptions(XPath.CompileOptions compileOptions)
		{
			base.InitCompileOptions(compileOptions);
			compileOptions.StaticallyKnownNamespaces = new XPath.StringDict() { { "prefix1", MyNamespaceUri } };
		}
		public override string GetDefaultExpr()
		{
			return string.Format("parse-xml('<a xmlns=\"{0}\"><b/><c xmlns=\"different-ns\"/></a>')//prefix1:*", MyNamespaceUri);
		}

		public static int Main(string[] args)
		{
			return SampleRunner<NamespacePrefixes>.Run(args);
		}

	}
}
