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
using XPath = Altova.RaptorXml.XPath;

namespace XPathApiExamples
{
	public interface IExecuteXPath
	{
		int ExecuteXPath(string strExpr);
		string GetDefaultExpr();
	}
	public class XPathExecutor : IExecuteXPath
	{
		protected XPath.Session session;

		public XPathExecutor()
		{
			//Step 1: create an XPath.Session object that holds and keeps alive the data required by the xpath/xquery/xslt engines
			session = new XPath.Session();
		}

		public int ExecuteXPath(string strExpr)
		{
			System.Console.WriteLine("Executing xpath: " + strExpr);
			//Step 2: create the options object which will be used for the static analysis of the expression
			var compileOptions = new XPath.CompileOptions(session);
			InitCompileOptions(compileOptions);

			//Step 3: parse and statically analyze the expression to create an expression that can be used for multiple executions
			var expr = XPath.Expression.Compile(strExpr, compileOptions, out Err logCompile);

			//Step 3a: check for errors
			if (expr == null || logCompile.HasErrors)
			{
				PrintErrors(string.Format("Failed to compile xpath expression {0}.", strExpr), logCompile);
				return 1;
			}

			//Step 4: create the runtime options used in the dynamic evaluation phase, this can be used to specify dynamic context components - ex. different values for expression.Execute calls
			var runtimeOptions = new XPath.RuntimeOptions(session);
			InitRuntimeOptions(runtimeOptions);

			//Step 5: now execute the expression
			var result = expr.Execute(runtimeOptions, out Err logExecute);

			//Step 5a: check for runtime errors
			if (result == null || logExecute.HasErrors)
			{
				PrintErrors("Failed to execute the xpath expression", logExecute);
				return 2;
			}

			//Step 6: the result is an XPath.Sequence, do something with it
			// ex. print it's elements to the console
			PrintSequence(result);

			return 0;
		}

		protected virtual void InitCompileOptions(XPath.CompileOptions compileOptions)
		{
			//ex. specify the xpath spec that should be used
			compileOptions.Version = XPath.Version.V31;

			//static-base-uri is used to resolve relative paths ex. in fn:doc, fn:unparsed-text, etc.
			//Note: compileOptions.StaticBaseUri must be in the lexical space of xs:anyURI
			compileOptions.StaticBaseUri = new System.Uri(System.IO.Directory.GetCurrentDirectory()).AbsoluteUri + '/' + System.Uri.EscapeDataString(System.IO.Path.GetRandomFileName());
		}

		protected virtual void InitRuntimeOptions(XPath.RuntimeOptions runtimeOptions)
		{
		}

		//print the Altova.RaptorXml.ErrorLog to the console
		protected static void PrintErrors(string msg, Err log)
		{
			System.Console.WriteLine(msg);
			foreach (var error in log)
			{
				System.Console.WriteLine(error.Text);
			}
		}
		public static void PrintSequence(XPath.Sequence items)
		{
			//print the size of the sequence and the contained items type and string value to the console
			//an XPath.Sequence implements the IEnumerable interface
			System.Console.Write(string.Format("XPath.Sequence[{0}]{{", items.Count)); //number of XPath.Items
			string prefix = "";
			//foreach XPath.Item in the Sequence
			foreach (var item in items)
			{
				System.Console.Write(string.Format(prefix + "{0}('{1}')", item.GetTypeName(), item.ToString()));
				prefix = ", ";
			}
			System.Console.WriteLine("}");
		}

		public virtual string GetDefaultExpr()
		{
			return "'Hello from xpath!', 'The current date/time is:', current-dateTime(), static-base-uri()";
		}

	}

	public class SampleRunner<T> where T : IExecuteXPath, new()
	{
		public static int Run(string[] args)
		{
			var o = new T();
			string strExpr = args.Length > 0 ? args[0] : o.GetDefaultExpr();
			return o.ExecuteXPath(strExpr);
		}
	};

	public class GettingStarted
	{
		public static int Main(string[] args)
		{
			return SampleRunner<XPathExecutor>.Run(args);
		}
	}
}
