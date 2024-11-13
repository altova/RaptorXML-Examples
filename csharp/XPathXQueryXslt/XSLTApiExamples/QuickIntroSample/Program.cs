using System.Text;
using Err = Altova.RaptorXml.ErrorLog;
using Xml = Altova.RaptorXml.Xml;
using XPath = Altova.RaptorXml.XPath;
using Xslt = Altova.RaptorXml.Xslt;

namespace MyApp
{
	using TFnInvoke = System.Func<XPath.SequenceList, XPath.Session, XPath.Instruction, XPath.Sequence>;
	internal class ExternalFunction : XPath.ExternalFunctionObject
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
	internal class Program
	{
		const string stylesheetSrc = @"<xsl:stylesheet xmlns:xsl=""http://www.w3.org/1999/XSL/Transform"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:err="" http://www.w3.org/2005/xqt-errors"" version=""3.0"" expand-text=""yes"">
	<xsl:template match=""/"">
		<xsl:result-document method=""json"" indent=""yes"" xmlns:ns1=""my-ext"">
			<xsl:map>
				<xsl:map-entry key=""'dot net version'"" select=""ns1:dotnet-version()""/>
				<xsl:map-entry key=""'os version'"" select=""ns1:os-version()""/>
				<xsl:map-entry key=""'my-attr converted'"" select=""let $attr := /doc/@myAttr return Q{my-ext}integer-to-double($attr)""/>
				<xsl:map-entry key=""'try-catch recovered conversion error 1'"">
					<xsl:try>
						<xsl:sequence select=""ns1:integer-to-double(xs:untypedAtomic(/doc/@myAttr div 3))""/>
						<xsl:catch select=""map{'code': string($err:code), 'description' : $err:description}""/>
					</xsl:try>
				</xsl:map-entry>
				<xsl:map-entry key=""'try-catch recovered conversion error 2'"">
					<xsl:try>
						<xsl:sequence select=""ns1:integer-to-double(data(.))""/>
						<xsl:catch select=""map{'code': string($err:code), 'description' : $err:description}""/>
					</xsl:try>
				</xsl:map-entry>
			</xsl:map>
		</xsl:result-document>
	</xsl:template>
</xsl:stylesheet>";
		const string xmldocSrc = @"<?xml version=""1.0""?><doc myAttr=""1337"">This document is used as input for the transformation.</doc>";

		static void Main(string[] args)
		{
			// the engine data session
			var session = new XPath.Session();

			var compileOptions = new Xslt.CompileOptions(session);

			compileOptions.ExternalFunctions = XPath.ExternalFunctions.Create(session, out Err logFnCreate, null, null, GetNativeExtensionFunctions());
			if (compileOptions.ExternalFunctions == null || logFnCreate.HasErrors)
			{
				PrintErrors("error creating the extension function library", logFnCreate);
				return;
			}

			var xsltInstance = LoadXmlFromString(stylesheetSrc);
			if (xsltInstance == null) return;
			var expr = Xslt.Stylesheet.Compile(Xslt.StylesheetProvider.FromNode(XPath.NodeItem.FromInformationItem(xsltInstance.DocumentItem, session)), compileOptions, out Err logCompile);
			if (expr == null || logCompile.HasErrors)
			{
				PrintErrors("error compiling the xslt stylesheet", logCompile);
				return;
			}
			var runtimeOptions = new Xslt.RuntimeOptions(session);

			var xmlInstance = LoadXmlFromString(xmldocSrc);
			if (xmlInstance == null) return;
			runtimeOptions.InitialMatchSelection = XPath.Sequence.FromItem(XPath.NodeItem.FromInformationItem(xmlInstance.DocumentItem, session));
			runtimeOptions.DeliveryFormat = XPath.DeliveryFormat.Serialized;

			var resultList = expr.Execute(runtimeOptions, out Err logExecute);
			if (resultList == null || logExecute.HasErrors)
			{
				PrintErrors("Failed to execute the xslt stylesheet", logExecute);
				return;
			}

			PrintResults(resultList, runtimeOptions.DeliveryFormat);
			System.Console.WriteLine("XSLT 3.0 transformation demonstrating extension function callback to native .NET code was successfully finished.");
		}

		//print the Altova.RaptorXml.ErrorLog to the console
		static void PrintErrors(string msg, Err log)
		{
			System.Console.WriteLine(msg);
			foreach (var error in log)
			{
				System.Console.WriteLine(error.Text);
			}
		}
		static Xml.Instance LoadXmlFromString(string str)
		{
			var xmlInstance = Xml.Instance.CreateFromBuffer(Encoding.Default.GetBytes(str), out Err errLoadXml);
			if (xmlInstance == null || errLoadXml.HasErrors)
			{
				PrintErrors("Failed to load xml from string.", errLoadXml);
			}
			return xmlInstance;
		}

		static XPath.ExternalFunctionObject[] GetNativeExtensionFunctions()
		{
			return new XPath.ExternalFunctionObject[] {
			new ExternalFunction(
				"Q{my-ext}dotnet-version()",
				(XPath.SequenceList paramsList, XPath.Session session, XPath.Instruction callingInstruction) =>
				{
					return XPath.Sequence.FromItem(XPath.AtomicItem.FromString(System.Environment.Version.ToString(), session));
				}),
			new ExternalFunction(
				"Q{my-ext}os-version()",
				(XPath.SequenceList paramsList, XPath.Session session, XPath.Instruction callingInstruction) =>
				{
					return XPath.Sequence.FromItem(XPath.AtomicItem.FromString(System.Environment.OSVersion.ToString(), session));
				}),
			new ExternalFunction(
				"Q{my-ext}integer-to-double($arg1 as xs:integer?) as xs:double? (: this function will do implicit conversions:)",
				(XPath.SequenceList paramsList, XPath.Session session, XPath.Instruction callingInstruction) =>
				{
					System.Console.WriteLine(string.Format("   >> External function object <{0}> called with {1} argument(s) with value <{3}>({2}).", typeof(ExternalFunction).ToString(), paramsList.Count, paramsList[0][0].ToString(), paramsList[0][0].GetTypeName()));
					return paramsList[0];
				})
			};
		}
		static void PrintSequence(XPath.Sequence items)
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
		static void SerializeSequence(XPath.Sequence items, XPath.SerializationParams p)
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
		static void Serialize(XPath.Sequence items, XPath.SerializationMethod m, XPath.Session session)
		{
			var p = new XPath.SerializationParams(session);
			p.Indent = true;
			p.Method = m;
			SerializeSequence(items, p);
		}

		static void PrintResults(XPath.ResultList resultList, XPath.DeliveryFormat deliveryFormat)
		{
			System.Console.WriteLine("Printing result list:");
			foreach (XPath.Result r in resultList)
			{
				if (r.Uri != null && r.Uri.Length > 0)
				{
					System.Console.WriteLine("Result for Uri: " + r.Uri);
				}
				if (deliveryFormat == XPath.DeliveryFormat.Serialized)
				{
					PrintSequence(r.Value);
				}
				else
				{
					//a Result object consists of a Sequence, SerializationParams and a Uri.
					SerializeSequence(r.Value, r.SerializationParams);
				}
			}
		}

	}
}
