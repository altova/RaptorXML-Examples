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
	//The xpath.ExternalFunctions class provides a function extension mechanism and represents a collection of valid, pre-analyzed XPath.ExternalFunctionObject(s) extension functions.
	//    It can be instantiated with the static method XPath.ExternalFunctions.Create.
	//    The same XPath.ExternalFunctions object can be re-used on multiple xpath.CompileOptions, xquery.CompileOptions or xslt.CompileOptions

	// The xpath.ExternalFunctionObject class represents an extension function with native implementation based on the engine api.
	class FnExtNumericConv : XPath.ExternalFunctionObject
	{
		//The signature must have the form: EQName "(" ParamList? ")" ( "as" SequenceType )? and will be parsed and validated during the call to xpath.ExternalFunctions.Create(...)
		//A call to a function matching the name and arity provided by the signature in the constructor will call on the object the member method XPath.Sequence OnInvoke(XPath.SequenceList args, XPath.Session session, XPath.Instruction rInstruction) and propagate the returned xpath.Sequence to the executing expression.

		public FnExtNumericConv(string EQNamespaceUri)
			: base(string.Format("{0}integer-to-double($arg1 as xs:integer?) as xs:double?", EQNamespaceUri))
		{
		}
		public override XPath.Sequence OnInvoke(XPath.SequenceList args, XPath.Session session, XPath.Instruction instruction)
		{
			//Both the xpath.Sequence(s) from args and the returned xpath.Sequence are subject to the function conversion rules as defined in the W3C specifications.
			System.Console.WriteLine(string.Format("  >>External function object <{0}> called with {1} argument(s).", this.GetType().ToString(), args.Count));
			bool afterFirst = false;
			foreach (var s in args)
			{
				if (afterFirst)
				{
					System.Console.Write(", ");
				}
				XPathExecutor.PrintSequence(s);
				afterFirst = true;
			}
			System.Console.WriteLine("  >>Call to external function ends.");
			return args[0];
		}
	}
	class FnExtEnvVersion : XPath.ExternalFunctionObject
	{
		public FnExtEnvVersion(string EQNamespaceUri)
			: base(string.Format("{0}env-version() as xs:string?", EQNamespaceUri))
		{
		}
		public override XPath.Sequence OnInvoke(XPath.SequenceList args, XPath.Session session, XPath.Instruction instruction)
		{
			System.Console.WriteLine(string.Format("  >>External function object <{0}> called with {1} argument(s).", this.GetType().ToString(), args.Count));
			System.Console.WriteLine("  >>Call to external function ends.");
			return XPath.Sequence.FromItem(XPath.AtomicItem.FromString(System.Environment.Version.ToString(), session));
		}
	}
	public class ExternalFunction : XPathExecutor
	{
		const string FnNamespaceUri = "native-external-fn";
		static readonly string EQNamespaceUri = "Q{" + FnNamespaceUri + "}";

		protected override void InitCompileOptions(XPath.CompileOptions compileOptions)
		{
			base.InitCompileOptions(compileOptions);
			//XPath.ExternalFunctions.Create pre-processes the function signatures: syntax checking, resolving of namespace prefixes and schema type resolution.
			//If the ns_map or the schema parameter is None, then the default built -in values are used from XPath specification.
			compileOptions.StaticallyKnownNamespaces = new XPath.StringDict { { "ns1", FnNamespaceUri } };
			// In case of invalid signature syntax, unknown prefix or unknown type the xpath.ExternalFunctions is None and the xml.ErrorLog contains the error(s).
			var nativeFunctionLib = XPath.ExternalFunctions.Create(session, out Err err, null, null, new FnExtNumericConv(EQNamespaceUri), new FnExtEnvVersion(EQNamespaceUri));
			if (nativeFunctionLib == null || err.HasErrors)
			{
				PrintErrors("error compiling external function library", err);
			}
			else
			{
				compileOptions.ExternalFunctions = nativeFunctionLib;
			}
		}
		public override string GetDefaultExpr()
		{
			return string.Format("{0}integer-to-double(xs:untypedAtomic('1')) (:call to native .net code. Set a breakpoint in the OnInvoke(...) method to inspect parameters:),\n ns1:env-version()", EQNamespaceUri);
		}

		public static int Main(string[] args)
		{
			return SampleRunner<ExternalFunction>.Run(args);
		}

	}
}
