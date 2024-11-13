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
using Xsd = Altova.RaptorXml.Xsd;
using Xslt = Altova.RaptorXml.Xslt;

namespace XSLTApiExamples
{
	using System.Text;
	using TFnInvoke = System.Func<XPath.SequenceList, XPath.Session, XPath.Instruction, XPath.Sequence>;
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
	public interface IExecuteXslt
	{
		int ExecuteXslt(Xslt.StylesheetProvider source);
		Xslt.StylesheetProvider GetDefaultXslt();
	}
	public class XsltExecutor : IExecuteXslt
	{
		protected XPath.Session session;

		public XsltExecutor()
		{
			//Step 1: create an XPath.Session object that holds and keeps alive the data required by the xpath/xquery/xslt engines
			session = new XPath.Session();
		}

		public int ExecuteXslt(Xslt.StylesheetProvider source)
		{
			System.Console.WriteLine("Executing xslt: " /*+ stylesheet.ToString()*/);
			//Step 2: create the options object which will be used for the static analysis of the expression
			var compileOptions = new Xslt.CompileOptions(session);
			InitCompileOptions(compileOptions);

			//Step 3: parse and statically analyze the expression to create an expression that can be used for multiple executions

			var expr = Xslt.Stylesheet.Compile(source, compileOptions, out Err logCompile);

			//Step 3a: check for errors
			if (expr == null || logCompile.HasErrors)
			{
				PrintErrors("Failed to compile xslt stylesheet", logCompile);
				return 1;
			}

			//Step 4: create the runtime options used in the dynamic evaluation phase, this can be used to specify dynamic context components - ex. different values for expression.Execute calls
			var runtimeOptions = new Xslt.RuntimeOptions(session);
			InitRuntimeOptions(runtimeOptions);

			//Step 5: now execute the expression
			var resultList = expr.Execute(runtimeOptions, out Err logExecute);

			//Step 5a: check for runtime errors
			if (resultList == null || logExecute.HasErrors)
			{
				PrintErrors("Failed to execute the xslt stylesheet", logExecute);
				return 2;
			}

			//Step 6: a successful xslt execution returns a ResultList, if only one result is expect its value can be accessed with resultList.MainValue
			foreach (XPath.Result r in resultList)
			{
				if (r.Uri != null && r.Uri.Length > 0)
				{
					System.Console.WriteLine("Result for Uri: " + r.Uri);
				}
				if (runtimeOptions.DeliveryFormat == XPath.DeliveryFormat.Serialized)
				{
					PrintSequence(r.Value);
				}
				else
				{
					//a Result object consists of a Sequence, SerializationParams and a Uri.
					SerializeSequence(r.Value, r.SerializationParams);
				}
			}

			return 0;
		}

		protected virtual void InitCompileOptions(Xslt.CompileOptions compileOptions)
		{
			//ex. specify the xslt spec that should be used
			compileOptions.Version = XPath.Version.V31;

			var fnVersion = new ExternalFunction(
				"Q{my-ext}dotnet-version()",
				(XPath.SequenceList paramsList, XPath.Session session, XPath.Instruction callingInstruction) =>
				{
					return XPath.Sequence.FromItem(XPath.AtomicItem.FromString(System.Environment.Version.ToString(), session));
				});
			var fnGuid = new ExternalFunction(
				"Q{my-ext}os-version()",
				(XPath.SequenceList paramsList, XPath.Session session, XPath.Instruction callingInstruction) =>
				{
					return XPath.Sequence.FromItem(XPath.AtomicItem.FromString(System.Environment.OSVersion.ToString(), session));
				});

			XPath.ExternalFunctionObject[] fnList = { fnVersion, fnGuid };
			var fnLib = XPath.ExternalFunctions.Create(session, out Err log, null, null, fnList);
			if (fnLib == null || log.HasErrors)
			{
				PrintErrors("error creating the extension function library", log);
			}
			else
			{
				compileOptions.ExternalFunctions = fnLib;
			}
			/*compileOptions.DefaultSerializationParams = new XPath.SerializationParams(session);
			compileOptions.DefaultSerializationParams.Indent = false;
			compileOptions.DefaultSerializationParams.Method = XPath.SerializationMethod.Html;
			compileOptions.DefaultSerializationParams.HtmlVersion = "5.0";*/

		}

		protected virtual void InitRuntimeOptions(Xslt.RuntimeOptions runtimeOptions)
		{
			var xmlInstance = Xml.Instance.CreateFromBuffer(Encoding.Default.GetBytes(@"<?xml version=""1.0"" encoding=""UTF-8""?><PERSONAE PLAY=""OTHELLO"">
    <TITLE>Dramatis Personae</TITLE>
    <PERSONA>DUKE OF VENICE</PERSONA>
    <PERSONA>BRABANTIO, a senator.</PERSONA>
    <PERSONA>Other Senators.</PERSONA>
    <PERSONA>GRATIANO, brother to Brabantio.</PERSONA>
    <PERSONA>LODOVICO, kinsman to Brabantio.</PERSONA>
    <PERSONA>OTHELLO, a noble Moor in the service of the Venetian state.</PERSONA>
    <PERSONA>CASSIO, his lieutenant.</PERSONA>
    <PERSONA>IAGO, his ancient.</PERSONA>
    <PERSONA>RODERIGO, a Venetian gentleman.</PERSONA>
    <PERSONA>MONTANO, Othello's predecessor in the government of Cyprus.</PERSONA>
    <PERSONA>Clown, servant to Othello. </PERSONA>
    <PERSONA>DESDEMONA, daughter to Brabantio and wife to Othello.</PERSONA>
    <PERSONA>EMILIA, wife to Iago.</PERSONA>
    <PERSONA>BIANCA, mistress to Cassio.</PERSONA>
    <PERSONA>Sailor, Messenger, Herald, Officers, 
             Gentlemen, Musicians, and Attendants.</PERSONA>
  </PERSONAE>"), out Err log);
			if (xmlInstance == null || log.HasErrors)
			{
				PrintErrors("couldn't load xml", log);
			}
			else
			{
				runtimeOptions.InitialMatchSelection = XPath.Sequence.FromItem(XPath.NodeItem.FromInformationItem(xmlInstance.DocumentItem, session));
			}
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
		public virtual Xslt.StylesheetProvider GetDefaultXslt()
		{
			return Xslt.StylesheetProvider.FromText(
				@"<xsl:stylesheet xmlns:xsl=""http://www.w3.org/1999/XSL/Transform""
    version=""3.0""
    expand-text=""yes"">
    
 <xsl:strip-space elements=""PERSONAE""/>   
    
 <xsl:template match=""PERSONAE"">
<xsl:result-document method=""html"" html-version=""5"">
   <html>
     <head>
       <title>The Cast of {@PLAY}</title>
     </head>
     <body>
       <xsl:apply-templates/>
     </body>
     <xsl:text>&#x0A;</xsl:text>
     <xsl:comment select="" 'Dot net version via native extension call: ' || Q{my-ext}dotnet-version() "" />
     <xsl:text>&#x0A;</xsl:text>
     <xsl:comment select="" 'System os version via native extension call: ' || Q{my-ext}os-version() "" />
     <xsl:text>&#x0A;</xsl:text>
   </html>
</xsl:result-document>
</xsl:template>
 
 <xsl:template match=""TITLE"">
   <h1>{.}</h1>
 </xsl:template>
 
 <xsl:template match=""PERSONA[count(tokenize(., ',')) = 2]"">
   <p><b>{substring-before(., ',')}</b>: {substring-after(., ',')}</p>
 </xsl:template> 

 <xsl:template match=""PERSONA"">
   <p><b>{.}</b></p>
 </xsl:template>

</xsl:stylesheet>"
			);
		}

	}

	public class SampleRunner<T> where T : IExecuteXslt, new()
	{
		public static int Run(string[] args)
		{
			var o = new T();
			Xslt.StylesheetProvider stylesheetProvider = args.Length > 0 ? Xslt.StylesheetProvider.FromLocation(args[0]) : o.GetDefaultXslt();
			return o.ExecuteXslt(stylesheetProvider);
		}
	};

	public class GettingStarted
	{
		public static int Main(string[] args)
		{
			return SampleRunner<XsltExecutor>.Run(args);
		}
	}
}
