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
using Xsd = Altova.RaptorXml.Xsd;

namespace XPathApiExamples
{
	public class ExternalVariables : XPathExecutor
	{
		protected override void InitRuntimeOptions(XPath.RuntimeOptions runtimeOptions)
		{
			base.InitRuntimeOptions(runtimeOptions);
			//Create and set a variable
			//The variable name must be a valid QName item and the value an XPath.Sequence
			var varName = XPath.AtomicItem.FromQName(new Xsd.QName("x"), session);
			bool testError = false; //set to true to get a division by zero error;
			var varValue = testError ? XPath.AtomicItem.FromInt(0, session) : XPath.AtomicItem.FromDouble(0, session);
			runtimeOptions.ExternalVariables = new XPath.MapItem(session)
				{
					{ varName, XPath.Sequence.FromItem(varValue) }
				};
		}

		public override string GetDefaultExpr()
		{
			return "(-1 to 1)!(. div $x) (:This is an XPath comment: expect -INF, NaN, INF when $x is of type xs:double, or Division by zero error if $x is instance of xs:integer:)";
		}

		public static int Main(string[] args)
		{
			return SampleRunner<ExternalVariables>.Run(args);
		}

	}
}
