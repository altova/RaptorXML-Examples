import com.altova.raptorxml.ErrorLog;
import com.altova.raptorxml.LicenseException;
import com.altova.raptorxml.xpath.CompileOptions;
import com.altova.raptorxml.xpath.Expression;
import com.altova.raptorxml.xpath.RuntimeOptions;
import com.altova.raptorxml.xpath.Sequence;
import com.altova.raptorxml.xpath.Session;
import com.altova.raptorxml.xpath.Version;
import com.altova.raptorxml.xpath.Item;

/**
 */
public class XPath_GettingStarted 
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

	private static boolean Run(String xpathExpression) throws Exception, LicenseException
	{
		//Step 1: create an XPath.Session object that holds and keeps alive the data required by the xpath/xquery/xslt engines
		var session = new Session();
		
		//Step 2: create the options object which will be used for the static analysis of the expression and initialize them
		ErrorLog logCompile = new ErrorLog();
		var compileOptions = new CompileOptions(session);

		//ex. specify the xpath spec that should be used
		compileOptions.setVersion(Version.V31);

		//Step 3: parse and statically analyze the expression to create an expression that can be used for multiple executions
		var expr = Expression.Compile(xpathExpression, compileOptions, logCompile);

		//Step 3a: check for errors
		if (expr == null || logCompile.getHasErrors())
		{
			System.out.println("Failed to compile xpath expression " + xpathExpression + " - " + logCompile.getText() + ".");
			return false;
		}

		//Step 4: create the runtime options used in the dynamic evaluation phase, this can be used to specify dynamic context components - ex. different values for expression.Execute calls
		ErrorLog logExecute = new ErrorLog();
		var runtimeOptions = new RuntimeOptions(session);

		//Step 5: now execute the expression
		var result = expr.execute(runtimeOptions, logExecute);

		//Step 5a: check for runtime errors
		if (result == null || logExecute.getHasErrors())
		{
			System.out.println("Failed to execute xpath expression " + xpathExpression + " - " + logExecute.getText() + ".");
			return false;
		}

		//Step 6: the result is an XPath.Sequence, do something with it
		// ex. print it's elements to the console
		PrintSequence(result);
		
		return true;
	}

	public static void main(String[] args) 
	{
		if (args.length <= 1)
		{
			try 
			{
				final String defaultExpression = "'Hello from xpath!', 'The current date/time is:', current-dateTime(), static-base-uri()";
				
				String xpathExpression = args.length == 0 ? defaultExpression : args[0];
				Run(xpathExpression);
			} 
			catch (Exception e) 
			{
				System.out.println(e.getMessage());
			}
		}
		else
		{
			System.out.println("to compile: javac -d bin -cp \"<RaptorXML_API_Jar_file>\" src\\XPath_GettingStarted.java");
			System.out.println("to run: java -cp \"<RaptorXML_bin_folder>;bin\" -Djava.library.path=\"<RaptorXML_bin_folder>\" XPath_GettingStarted [<xpath_expression>]");
		}
	}
}
