# Introduction on using RaptorXML XPath, XQuery, XSLT engines in C# code #
This is a quick-intro on how-to write a C# program that uses the [.NET API](https://www.altova.com/manual/en/raptorapi/dotnetapiv2/2.10.0/html/html/R_Project_Documentation.html) of the [Altova RaptorXML+XBRL Server](http://www.altova.com/raptorxml.html) to provide access to the XML query and transformation languages [XPath 3.1](https://www.w3.org/TR/xpath-31/), [XQuery 3.1](https://www.w3.org/TR/xquery-31/) and [XSLT 3.0](https://www.w3.org/TR/xslt-30/)

Note: This document assumes at least basic familiarity with the C# programming language and tools.

## Table of content
- [Prerequisites](#id-Prerequisites)
- [Getting started](#id-GettingStarted)
- [Basic XPath, XQuery and XSLT processing](#id-BasicProcessing)
  - [Checking for errors](#id-ErrorChecking)
  - [Processing an XPath.Sequence](#id-XPathSequence)
- [Configuring the Compile and RuntimeOptions](#id-AdvProcessing)
  - [Namespaces](#id-Namespaces)
  - [Creating atomic items](#id-CreateAtomic)
  - [Creating sequences](#id-CreateSequence)
  - [External variables](#id-Variables)
  - [Serialization](#id-Serialization)
- [Loading XML documents](#id-LoadXml)
  - [Load from string/buffer](#id-LoadXmlBuff)
  - [Load from disk](#id-LoadXmlDisk)
- [Writing native extension functions in .NET](#id-NativeFunctions)
  - [Creating a generic extension function class](#id-GenericFunction)
- [A working example](#id-Sample)



<div id='id-Prerequisites'/>

## Prerequisites
* Altova RaptorXML+XBRL Server 2025 installed and with valid and active license (see ***[the manual](https://www.altova.com/manual/RaptorXML/raptorxmlxbrlserver/srvstpwin_licensing_registerapp.html)*** for license registration, activation)
* .NET installed (ex. .NET Core 8 LTS, minimum supported version is .NET Framework 4.8)
* optional: .NET development environment ex. Microsoft Visual Studio or Visual Studio Code, but a simple text editor and dotnet.exe on the PATH will also suffice, for execution call `dotnet.exe run` in the directory containing the cs and the corresponding csproj file. [^1]

<div id='id-GettingStarted'/>

## Getting started
* Create a new C# application or open an existing one.
* Add an assembly reference to raptorxmlxbrlapi to the projects - ex. either via the context menu or by adding a `<ItemGroup><Reference Include="raptorxmlxbrlapi"><HintPath>C:\Program Files\Altova\RaptorXMLXBRLServer2025\bin\raptorxmlxbrlapi.dll</HintPath></Reference></ItemGroup>` entry to the .csproj file.
* The classes referred from `raptorxmlxbrlapi.dll` are in the `Altova.RaptorXml` namespace, add some using directives to reduce the length of the code:
  ```cs
  using Err = Altova.RaptorXml.ErrorLog;
  using Xml = Altova.RaptorXml.Xml;
  using XPath = Altova.RaptorXml.XPath;
  using Xslt = Altova.RaptorXml.Xslt;
  using Xsd = Altova.RaptorXml.Xsd;
  using XQuery = Altova.RaptorXml.XQuery;
  ```

<div id='id-BasicProcessing'/>

## Basic XPath, XQuery and XSLT processing
### Basic XPath, XQuery and XSLT processing all follow a similar pattern:

   1. create a new `XPath.Session` object which is required for the lifetime management of engine specific objects and data (like: compiled expressions, sequences, items, etc...)

   2. create and initialize the `CompileOptions` corresponding for the settings of the static context components

   3. create the *Executable* object with one of the corresponding static method `XPath.Expression.Compile`, `XQuery.Expression.Compile` or `Xslt.Stylesheet.Compile`. In case of a syntax or a static error, the returned value is null and the `ErrorLog log` out parameter contains the error(s).
   Note: there is a slight difference between the different targets when specifying the source:
      - for XPath the input is a string
      - for XQuery the input is an `XQuery.ExpressionProvider` which can be created `FromText` (this is the default) or `FromLocation` to load the content of a file
      - for Xslt the input is an `Xslt.StylesheetProvider` which can be created `FromText`, `FromLocation` or `FromNode(XPath.NodeItem n)` 

   4. create and initialize the `RuntimeOptions` corresponding for the settings of the dynamic context components

   5. call the `Execute` method on the compiled object. In case of a runtime error the returned result is null and the `ErrorLog log` out parameter contains the error.

   6. on success, process the return value:
      - XPath returns an `XPath.Sequence`
      - XQuery and Xslt return an `XPath.ResultList` which is an iterable container of `XPath.Result` object(s), that contains the calculated output `Uri` and `SerializationParams` in addition to the `Value` sequence.

<div id='id-ErrorChecking'/>

### Checking for errors
The returned objects should be tested for null and the `ErrorLog log` out parameter for the `HasErrors` field:
```cs
if (ret == null || log.HasErrors) {
    foreach (var error in log) {
        System.Console.WriteLine(error.Text);
    }
}
```

<div id='id-XPathSequence'/>

### Printing the content of an XPath.Sequence
Print the size of the sequence and the contained items type and string value to the console. Note this will not descend into XPath.ArrayItem(s) and XPath.MapItem(s).
```cs
public static void PrintSequence(XPath.Sequence items)
{
    System.Console.Write(string.Format("XPath.Sequence[{0}]{{", items.Count));
    string prefix = "";
    foreach (var item in items) {
        System.Console.Write(string.Format(prefix + "{0}('{1}')", item.GetTypeName(), item.ToString()));
        prefix = ", ";
    }
    System.Console.WriteLine("}");
}
```

<div id='id-AdvProcessing'/>

## Configuring the Compile and RuntimeOptions

<div id='id-Namespaces'/>

### Namespaces
QNames are used in many places in XML and also in XPath/XQuery/XSLT. Resolved QNames are tuples of <local-name, namespace-uri, prefix> - in the XPath datamodel another term ExpandedName is used for the pair<local-name, namespace-uri> and these are used in many places ex. for matching element and attribute names in filtering expressions, function and variable name resolution, etc. The prefix mapping to the namespace-uri depends on the currently in-scope namespace mappings. For XPath and XQuery one can use the `compileOptions.StaticallyKnownNamespaces` to specify the initial namespace mappings. These are optionally combined with the default ones as controlled by the `compileOptions.UseStandardNamespacePrefixes` property.

```cs
const string MyNamespaceUri = "my-namespace-uri";
compileOptions.StaticallyKnownNamespaces = new XPath.StringDict() { { "prefix1", MyNamespaceUri } };
var strExpr = string.Format("parse-xml('<a xmlns=\"{0}\"><b/><c xmlns=\"different-ns\"/></a>')//prefix1:*", MyNamespaceUri);

var expr = XPath.Expression.Compile(strExpr, compileOptions, out Err logCompile);
```

<div id='id-CreateAtomic'/>

### Creating atomic items
Atomic items are created with the From...(..., session) static methods that take usually a built-in xs atomic schema type from the `Xsd` namespace with a few exception where native C# types can be directly used.
```cs
XPath.AtomicItem.FromString("lorem ipsum", session)
```
```cs
XPath.AtomicItem.FromInt(123, session)
```
```cs
XPath.AtomicItem.FromInteger(new Xsd.Integer("123"), session)
```
<div id='id-CreateSequence'/>

### Creating sequences
A new sequence empty sequence can be created with a session object.
```cs
new XPath.Sequence(session);
```
A singleton (a sequence containing one item with the FromItem static method. 
```cs
XPath.Sequence.FromItem(XPath.AtomicItem.FromString("lorem", session));
```

<div id='id-Variables'/>

### External variables
External variables must be set via an XPath.MapItem and their name must be a AtomiItem of type xs:QName.
```cs
var varName = XPath.AtomicItem.FromQName(new Xsd.QName("x"), session);
var varValue = XPath.AtomicItem.FromDouble(0, session);
runtimeOptions.ExternalVariables = new XPath.MapItem(session) { { varName, XPath.Sequence.FromItem(varValue) } };
```
Note: In XPath grammar there is no possibility to mark a variable as external, so any non-local variable is treated as such. In XQuery `"external"` variables are defined as such in the prolog, otherwise it leads to a compile error. To allow executing xpath statements in the xquery engine one can use the `CompileOptions.AllowUndeclaredVariables` property to relax this rule.


<div id='id-Serialization'/>

### Serialization
```cs
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
```

<div id='id-LoadXml'/>

## Loading XML documents

<div id='id-LoadXmlBuff'/>

### Load from string/buffer
```cs
    var xmlInstance = Xml.Instance.CreateFromBuffer(Encoding.Default.GetBytes(@"<?xml version=""1.0"" encoding=""UTF-8""?><PERSONAE PLAY=""OTHELLO"">
  <TITLE>Dramatis Personae</TITLE>
  <PERSONA>DUKE OF VENICE</PERSONA>
  <!-- ... -->
  <PERSONA>Sailor, Messenger, Herald, Officers, 
           Gentlemen, Musicians, and Attendants.</PERSONA>
</PERSONAE>"), out Err log);

if (xmlInstance == null || loadErr.HasErrors)
{
    PrintErrors("error loading document", loadErr);
}
else
{
    runtimeOptions.InitialContext = XPath.NodeItem.FromInformationItem(xmlInstance.DocumentItem, session);
}
```

<div id='id-LoadXmlDisk'/>

### Load from disk

```cs
var xmlInstance = Xml.Instance.CreateFromUrl(fileName, out Err loadErr);
if (xmlInstance == null || loadErr.HasErrors)
{
    PrintErrors("error loading document", loadErr);
}
else
{
    runtimeOptions.InitialContext = XPath.NodeItem.FromInformationItem(xmlInstance.DocumentItem, session);
}
```
Additionally to the previous methods, one can (re)use a document returned by a previous execution -ex. loaded by `fn:doc`, `fn:parse-xml`, created by node constructors in xquery or xslt, etc.

<div id='id-NativeFunctions'/>

## Writing native extension functions in .NET

`XPath.ExternalFunctions` represent a sealed collection extension function objects with native callback to .NET. It can be constructed with the static method *XPath.ExternalFunctions.Create* and can be shared by multiple compiled expressions, by setting it on their *CompileOptions.ExternalFunctions*.

```cs
compileOptions.ExternalFunctions = XPath.ExternalFunctions.Create(session, out Err err, /*namespaces*/null, /*schema*/null, new FnExtNumericConv());
//XPath.StringDict namespaces - when null use the built-in namespaces 
//Xsd.Schema schema - when null use the built-in schema-types
```

The function objects must derive from `XPath.ExternalFunctionObject`. The signature must have the form `EQName "(" ParamList? ")" ( "as" SequenceType )?`  and will be parsed and validated during the call to *XPath.ExternalFunctions.Create(...)*.

During XPath, XQuery, Xslt evaluation a call to a function matching the name and arity provided by the signature in the constructor will trigger a call on the objects `XPath.Sequence OnInvoke(XPath.SequenceList args, XPath.Session session, XPath.Instruction locationInstruction)` method and propagate the returned xpath.Sequence to the executing expression.
 The number of Sequence provided via the args will match the arity of the function and contain the parameter values for the actual call. Note: These sequences are not sanitized - one must not store any xpath function item for later use from these -neither directly nor indirectly (ex. in a map or array). Calling such a function outside of their original execution scope and context is undefined behavior.

 If the function name is specified with a prefix instead of a BracedURILiteral, then there must be a valid namespace mapping for that prefix in the namespaces parameter of the ExternalFunctions.Create call. For the function names similar restrictions apply as for the XQuery 3.1 user defined functions (reserved function names, reserved namespaces).

Note: Both the XPath.Sequence(s) from the args and the returned XPath.Sequence are subject to the function conversion rules as defined in the W3C specifications  (optional node atomization, type promotion, etc.).


```cs
class FnExtEnvVersion : XPath.ExternalFunctionObject
{
    public FnExtEnvVersion()
        //The namespace URI of the function is specified in a URIQualifiedName 
        : base("Q{foo-bar}env-version() as xs:string?") 
    {
    }

    public override XPath.Sequence OnInvoke(XPath.SequenceList args, XPath.Session session, XPath.Instruction instruction)
    {
        //arity of this function === 0 === args.Count
        System.Console.WriteLine(string.Format("  >>External function object <{0}> called with {1} argument(s).", this.GetType().ToString(), args.Count));
        System.Console.WriteLine("  >>Call to external function ends.");
        return XPath.Sequence.FromItem(XPath.AtomicItem.FromString(System.Environment.Version.ToString(), session));
    }
}

...

var nativeFunctionLib = XPath.ExternalFunctions.Create(session, out Err err, null, null, new FnExtNumericConv());
if (nativeFunctionLib == null || err.HasErrors)
{
    PrintErrors("error compiling external function library", err);
}
else
{
    compileOptions.ExternalFunctions = nativeFunctionLib;
}

// The function is identified by the expanded name and arity
// with a matching prefix mapping the function can be invoked via the form prefix1:local-name($arg1, $arg2, ...)
var expr = XPath.Expression.Compile("Q{foo-bar}env-version()", compileOptions, out Err logCompile);
var result = expr.Execute(new XPath.RuntimeOptions(session), out Err logExecute);
```

<div id='id-GenericFunction'/>


### Creating a generic extension function class
By using the System.Func delegate one has the possibility to create a reusable ExternalFunction object where the signature and the callback function (ex. lambda function) gets specified during object construction.
```cs
using TFnInvoke = System.Func<XPath.SequenceList, XPath.Session, XPath.Instruction, XPath.Sequence>;
public class ExternalFunction : XPath.ExternalFunctionObject
{
    TFnInvoke fnInvoke;
    public ExternalFunction(string signatureExpr, TFnInvoke fnInvoke)
        : base(signatureExpr)
    {
        this.fnInvoke = fnInvoke;
    }
    public override XPath.Sequence OnInvoke(XPath.SequenceList args, XPath.Session session, XPath.Instruction callingInstruction)
    {
        return fnInvoke(args, session, callingInstruction);
    }
}
```
```cs
var fnVersion = new ExternalFunction(
    "Q{my-ext}dotnet-version()",
    (XPath.SequenceList paramsList, XPath.Session session, XPath.Instruction callingInstruction) => {
        return XPath.Sequence.FromItem(XPath.AtomicItem.FromString(System.Environment.Version.ToString(), session));
    });

var fnGuid = new ExternalFunction(
    "Q{my-ext}os-version()",
    (XPath.SequenceList paramsList, XPath.Session session, XPath.Instruction callingInstruction) => {
        return XPath.Sequence.FromItem(XPath.AtomicItem.FromString(System.Environment.OSVersion.ToString(), session));
    });

compileOptions.ExternalFunctions = XPath.ExternalFunctions.Create(session, out Err log, null, null, fnVersion, fnGuid);
//define a ns1->my-ext prefix namespace mapping
compileOptions.StaticallyKnownNamespaces = new XPath.StringDict { { "ns1", "my-ext" } };

var expr = XPath.Expression.Compile("ns1:dotnet-version(), ns1:os-version()", compileOptions, out Err logCompile);
var result = expr.Execute(new XPath.RuntimeOptions(session), out Err logExecute);
```


<div id='id-Sample'/>

## A working example
Create the following two files in an empty directory. To execute it, open a command shell, change to that directory, then call `dotnet.exe run`.

### The `Program.csproj` file
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net48</TargetFramework>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>disable</Nullable>
    <Platforms>x64;</Platforms>
    <StartupObject>MyApp.Program</StartupObject>
    <BaseOutputPath>..\..\build</BaseOutputPath>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="raptorxmlxbrlapi">
      <HintPath>C:\Program Files\Altova\RaptorXMLXBRLServer2025\bin\raptorxmlxbrlapi.dll</HintPath>
    </Reference>
  </ItemGroup>
</Project>
```

### The `Program.cs` file
```cs
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
```

[^1]: For .NET development introduction see the following  [Tutorial: Create a simple C# console app in Visual Studio](https://learn.microsoft.com/en-us/visualstudio/get-started/csharp/tutorial-console?view=vs-2022).
