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

using Altova.RaptorXml.XPath;
using XPath = Altova.RaptorXml.XPath;

namespace XPathApiExamples
{
	public class TraceMsgCallback : XPath.TraceMsgCallbackObject
	{
		public TraceMsgCallback(XPath.Session session)
			: base(session)
		{
		}

		public override void OnFnTrace(Sequence value, AtomicItem label)
		{
			System.Console.WriteLine(string.Format("   >>Trace begin - label: '{0}'", label.ToString()));
			System.Console.Write("      value: ");
			XPathExecutor.PrintSequence(value);
			System.Console.WriteLine("   >>Trace end");
		}
	}
	public class TraceMsg : XPathExecutor
	{
		protected override void InitRuntimeOptions(XPath.RuntimeOptions runtimeOptions)
		{
			base.InitRuntimeOptions(runtimeOptions);
			runtimeOptions.TraceCallback = new TraceMsgCallback(session);
		}
		public override string GetDefaultExpr()
		{
			return "2!trace(math:sqrt(.), 'Calculating sqrt of ' || .)";
		}

		public static int Main(string[] args)
		{
			return SampleRunner<TraceMsg>.Run(args);
		}
	}
}
