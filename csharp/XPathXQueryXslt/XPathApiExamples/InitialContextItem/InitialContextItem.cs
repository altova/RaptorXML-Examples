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
	public class InitialContextItem : XPathExecutor
	{
		protected override void InitRuntimeOptions(XPath.RuntimeOptions runtimeOptions)
		{
			base.InitRuntimeOptions(runtimeOptions);
			//Create and set the initial context item
			runtimeOptions.InitialContext = XPath.AtomicItem.FromInt(1337, session);
		}
		public override string GetDefaultExpr()
		{
			return "if(. instance of xs:numeric) then 'numeric' else 'non-numeric', ., 'context position is ' || position(), 'context size is ' || last()";
		}

		public static int Main(string[] args)
		{
			return SampleRunner<InitialContextItem>.Run(args);
		}
	}
}
