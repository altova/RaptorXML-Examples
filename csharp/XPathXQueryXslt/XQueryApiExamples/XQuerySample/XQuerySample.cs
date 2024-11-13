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
using XQuery = Altova.RaptorXml.XQuery;

namespace XQueryApiExamples
{
	using TFnInvoke = System.Func<XPath.SequenceList, XPath.Session, XPath.Instruction, XPath.Sequence>;
	public interface IExecuteXQuery
	{
		int ExecuteXQuery(string strExpr);
		string GetDefaultExpr();
	}

	public class ExternalFunction : XPath.ExternalFunctionObject
	{
		TFnInvoke fnInvoke;
		public ExternalFunction(string signatureExpr, TFnInvoke fnInvoke)
			: base(signatureExpr) 
		{
			this.fnInvoke = fnInvoke;
		}
		public override XPath.Sequence OnInvoke(XPath.SequenceList args, XPath.Session rSession, XPath.Instruction rInstruction)
		{
			return fnInvoke(args, rSession, rInstruction);
		}
	}

	public class XQueryExecutor : IExecuteXQuery
	{
		protected XPath.Session session;

		public XQueryExecutor()
		{
			//Step 1: create an XPath.Session object that holds and keeps alive the data required by the xpath/xquery/xslt engines
			session = new XPath.Session();
		}

		public int ExecuteXQuery(string strExpr)
		{
			System.Console.WriteLine("Executing xquery: " + strExpr);
			//Step 2: create the options object which will be used for the static analysis of the expression
			var compileOptions = new XQuery.CompileOptions(session);
			InitCompileOptions(compileOptions);

			//Step 3: parse and statically analyze the expression to create an expression that can be used for multiple executions
			var expr = XQuery.Expression.Compile(strExpr, compileOptions, out Err logCompile);

			//Step 3a: check for errors
			if (expr == null || logCompile.HasErrors)
			{
				PrintErrors(string.Format("Failed to compile xquery expression {0}.", strExpr), logCompile);
				return 1;
			}

			//Step 4: create the runtime options used in the dynamic evaluation phase, this can be used to specify dynamic context components - ex. different values for expression.Execute calls
			var runtimeOptions = new XQuery.RuntimeOptions(session);
			InitRuntimeOptions(runtimeOptions);

			//Step 5: now execute the expression
			XPath.ResultList resultList = expr.Execute(runtimeOptions, out Err logExecute);

			//Step 5a: check for runtime errors
			if (resultList == null || logExecute.HasErrors)
			{
				PrintErrors("Failed to execute the xquery expression", logExecute);
				return 2;
			}

			//Step 6: a successful xquery execution returns a ResultList, if only one result is expect its value can be accessed with resultList.MainValue
			foreach (XPath.Result r in resultList)
			{
				//a Result object consists of a Sequence, SerializationParams and a Uri.
				SerializeSequence(r.Value, r.SerializationParams);
			}

			return 0;
		}

		protected virtual void InitCompileOptions(XQuery.CompileOptions compileOptions)
		{
			//ex. specify the xquery spec that should be used, for xquery 1.0 the value Version.V1 will be promoted to engine Version.V2
			compileOptions.Version = XPath.Version.V31;

			//static-base-uri is used to resolve relative paths ex. in fn:doc, fn:unparsed-text, etc.
			//Note: compileOptions.StaticBaseUri must be in the lexical space of xs:anyURI
			compileOptions.StaticBaseUri = new System.Uri(System.IO.Directory.GetCurrentDirectory()).AbsoluteUri + '/' + System.Uri.EscapeDataString(System.IO.Path.GetRandomFileName());

			var fnVersion = new ExternalFunction(
				"Q{my-ext}dotnet-version()",
				(XPath.SequenceList args, XPath.Session session, XPath.Instruction n) => {
					return XPath.Sequence.FromItem(XPath.AtomicItem.FromString(System.Environment.Version.ToString(), session));
				});
			var fnGuid = new ExternalFunction(
				"Q{my-ext}os-version()",
				(XPath.SequenceList args, XPath.Session session, XPath.Instruction n) => { 
					return XPath.Sequence.FromItem(XPath.AtomicItem.FromString(System.Environment.OSVersion.ToString(), session));
				});

			XPath.ExternalFunctionObject[] fnList = { fnVersion, fnGuid };
			var fnLib = XPath.ExternalFunctions.Create(session, out Err log, null, null, fnList );
			if (fnLib == null || log.HasErrors)
			{
				PrintErrors("error creating the extension function library", log);
			}
			else
			{
				compileOptions.ExternalFunctions = fnLib;
			}
			compileOptions.DefaultSerializationParams = new XPath.SerializationParams(session);
			compileOptions.DefaultSerializationParams.Indent = true;
		}

		protected virtual void InitRuntimeOptions(XQuery.RuntimeOptions runtimeOptions)
		{
			runtimeOptions.DeliveryFormat = XPath.DeliveryFormat.Raw;
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
		public static void SerializeSequence(XPath.Sequence items, XPath.SerializationParams p)
		{
			var ret = items.Serialize(p, out Err log);
			if (ret == null || log.HasErrors)
			{
				PrintErrors("serialization failed", log);
			}
			else
			{
				System.Console.WriteLine(ret);
			}
		}
		public static void Serialize(XPath.Sequence items, XPath.SerializationMethod m, XPath.Session session)
		{
			var p = new XPath.SerializationParams(session);
			p.Indent = true;
			p.Method = m;
			SerializeSequence(items, p);
		}
		public virtual string GetDefaultExpr()
		{
			return @"comment{ 'The static base-uri of the expression: ', static-base-uri() },
element doc { <greet>Hello from xquery!</greet>, element date{ attribute desc {'The current date/time is:'}, attribute value {  current-dateTime() }},
element dotnet-version { Q{my-ext}dotnet-version() }, element os-version { Q{my-ext}os-version() }}";
		}

	}

	public class SampleRunner<T> where T : IExecuteXQuery, new()
	{
		public static int Run(string[] args)
		{
			var o = new T();
			string strExpr = args.Length > 0 ? args[0] : o.GetDefaultExpr();
			return o.ExecuteXQuery(strExpr);
		}
	};

	public class GettingStarted
	{
		public static int Main(string[] args)
		{
			return SampleRunner<XQueryExecutor>.Run(args);
		}
	}
}
