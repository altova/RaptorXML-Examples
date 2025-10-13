import com.altova.raptorxml.ErrorLog;
import com.altova.raptorxml.LicenseException;
import com.altova.raptorxml.xml.Instance;
import com.altova.raptorxml.xpath.CompileOptions;
import com.altova.raptorxml.xpath.Expression;
import com.altova.raptorxml.xpath.RuntimeOptions;
import com.altova.raptorxml.xpath.Sequence;
import com.altova.raptorxml.xpath.Session;
import com.altova.raptorxml.xpath.StringDict;
import com.altova.raptorxml.xpath.Version;
import com.altova.raptorxml.xpath.Item;
import com.altova.raptorxml.xpath.NodeItem;

/**
 */
public class XPath_InitialContextXmlDocument 
{
	static {
		// load JNI library from install location
		// Windows requires pre-loading of dependent DLLs.
		// the location of these files need to get passed to java with: -Djava.library.path="path_to_product_bin_folder"
		if (System.getProperty("os.name").startsWith("Windows"))
		{
			System.loadLibrary("icudt76");
			System.loadLibrary("icuuc76");
			System.loadLibrary("icuin76");
			System.loadLibrary("libpng16");
		}
		// load RaptorXML+XBRL Server Engine API native library
		// fall back to RaptorXML Server Engine API native library, if XBRL edition is not installed
		try
		{ 
			System.loadLibrary("raptorxmlxbrlapi_jni"); 		
		}
		catch (java.lang.UnsatisfiedLinkError e)
		{
			System.loadLibrary("raptorxmlapi_jni");
		}
	}

	private static void PrintSequence(Sequence items)
	{
		//print the size of the sequence and the contained items type and string value to the console
		//an XPath.Sequence implements the IEnumerable interface
		System.out.print(String.format("XPath.Sequence[%d]{{", items.size())); //number of XPath.Items
		String prefix = "";
		//for each XPath.Item in the Sequence
		for (Item item : items)
		{
			System.out.print(String.format(prefix + "%s('%s')", item.getTypeName(), item.toString()));
			prefix = ", ";
		}
		System.out.println("}");
	}

	private static boolean Run(String fileName, String xpathExpression) throws Exception, LicenseException
	{
		// Create an XPath.Session object that holds and keeps alive the data required by the xpath/xquery/xslt engines
		var session = new Session();

		// Load the Xml document
		ErrorLog logLoad = new ErrorLog();
		var xmlDoc = Instance.CreateFromUrl(fileName, logLoad);
		if (xmlDoc == null || logLoad.getHasErrors())
		{
			System.out.println("Error loading document - " + logLoad);
			return false;
		}
		
		// Create the options object which will be used for the static analysis of the expression and initialize them
		ErrorLog logCompile = new ErrorLog();
		var compileOptions = new CompileOptions(session);

		var nameSpaces = new StringDict();
		nameSpaces.put("ns1", xmlDoc.getDocumentElement().getNamespaceName());
		compileOptions.setStaticallyKnownNamespaces(nameSpaces);
		
		//ex. specify the xpath spec that should be used
		compileOptions.setVersion(Version.V31);

		// Parse and statically analyze the expression to create an expression that can be used for multiple executions
		var expr = Expression.Compile(xpathExpression, compileOptions, logCompile);

		// Check for errors
		if (expr == null || logCompile.getHasErrors())
		{
			System.out.println("Failed to compile xpath expression " + xpathExpression + " - " + logCompile.getText() + ".");
			return false;
		}

		// Create the runtime options used in the dynamic evaluation phase, this can be used to specify dynamic context components - ex. different values for expression.Execute calls
		ErrorLog logExecute = new ErrorLog();
		var runtimeOptions = new RuntimeOptions(session);
		runtimeOptions.setInitialContext(NodeItem.FromInformationItem(xmlDoc.getDocumentItem(), session));

		// Execute the expression
		var result = expr.execute(runtimeOptions, logExecute);

		// Check for runtime errors
		if (result == null || logExecute.getHasErrors())
		{
			System.out.println("Failed to execute xpath expression " + xpathExpression + " - " + logExecute.getText() + ".");
			return false;
		}

		// The result is an XPath.Sequence, do something with it
		// ex. print it's elements to the console
		PrintSequence(result);
		
		return true;
	}

	public static void main(String[] args) 
	{
		if (args.length <= 2)
		{
			try 
			{
				final String defaultFileName = "NanonullOrg.xml";
				final String defaultExpression = "distinct-values(/ns1:OrgChart//*:Department/*:Name) (:uses wildcard to match the elements regardless of their namespace:)";
				
				String fileName = args.length == 0 ? defaultFileName : args[0];
				String xpathExpression = args.length == 1 ? defaultExpression : args[1];

				if (System.getProperty("os.name").startsWith("Windows"))
					Run("..\\..\\..\\" +  fileName, xpathExpression);
				else
					Run("../../../" +  fileName, xpathExpression);
			} 
			catch (Exception e) 
			{
				System.out.println(e.getMessage());
			}
		}
		else
		{
			System.out.println("to compile: javac -d bin -cp \"<RaptorXML_API_Jar_file>\" src\\XPath_InitialContextXmlDocument.java");
			System.out.println("to run: java -cp \"<RaptorXML_bin_folder>;bin\" -Djava.library.path=\"<RaptorXML_bin_folder>\" XPath_InitialContextXmlDocument [<xpath_expression>]");
		}
			System.out.println("Usage: java -cp \"<RaptorXML_bin_folder>;bin\" -Djava.library.path=\"<RaptorXML_bin_folder>\" XPath_InitialContextXmlDocument [<xpath_expression>]");
	}
}
