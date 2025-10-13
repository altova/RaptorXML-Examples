import com.altova.raptorxml.ErrorLog;
import com.altova.raptorxml.LicenseException;
import com.altova.raptorxml.xpath.AtomicItem;
import com.altova.raptorxml.xpath.ExternalFunctionObject;
import com.altova.raptorxml.xpath.ExternalFunctions;
import com.altova.raptorxml.xpath.Instruction;
import com.altova.raptorxml.xpath.Result;
import com.altova.raptorxml.xpath.ResultList;
import com.altova.raptorxml.xpath.Sequence;
import com.altova.raptorxml.xpath.SequenceList;
import com.altova.raptorxml.xpath.SerializationParams;
import com.altova.raptorxml.xpath.Session;
import com.altova.raptorxml.xpath.Version;
import com.altova.raptorxml.xquery.CompileOptions;
import com.altova.raptorxml.xquery.Expression;
import com.altova.raptorxml.xquery.ExpressionProvider;
import com.altova.raptorxml.xquery.RuntimeOptions;

/**
 */
public class XQuery_ExternalFunction 
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

	private static boolean Run(String xquery) throws Exception, LicenseException
	{
		ErrorLog errorLog = new ErrorLog();
		
		// create XPath session
		Session session = new Session();
		
		// create the XQuery compile options
		CompileOptions compileOptions = new CompileOptions(session);
		
		//ex. specify the xquery spec that should be used, for xquery 1.0 the value Version.V1 will be promoted to engine Version.V2
		compileOptions.setVersion(Version.V31);

		
		// define an external function
		ExternalFunctionObject userFunction_JavaVersion = new ExternalFunctionObject("Q{my-ext}java-version()")
		{
			@Override
			public Sequence onInvoke(SequenceList args, Session rSession, Instruction instruction) throws Exception, Throwable 
			{
				return Sequence.FromItem(AtomicItem.FromString(System.getProperty("java.version"), session));
			}
		};

		// define another external function
		ExternalFunctionObject userFunction_OSVersion = new ExternalFunctionObject("Q{my-ext}os-version()")
		{
			@Override
			public Sequence onInvoke(SequenceList args, Session rSession, Instruction instruction) throws Exception, Throwable 
			{
				return Sequence.FromItem(AtomicItem.FromString(System.getProperty("os.version"), session));
			}
		};

		ExternalFunctionObject[] userFunctions = new ExternalFunctionObject[] { userFunction_JavaVersion, userFunction_OSVersion }; 
		ExternalFunctions externalFunctions = ExternalFunctions.Create(session, errorLog, null, null, userFunctions);
		if (errorLog.getHasErrors())
		{
			System.out.println(errorLog.getText());
			return false;
		}
		compileOptions.setExternalFunctions(externalFunctions);

		compileOptions.setDefaultSerializationParams(new SerializationParams(session));
		compileOptions.getDefaultSerializationParams().setIndent(true);
		
		Expression xq = Expression.Compile(new ExpressionProvider(xquery), compileOptions, errorLog);
		if (errorLog.getHasErrors())
		{
			System.out.println(errorLog.getText());
			return false;
		}

		// prepare XPath run-time options
		RuntimeOptions runtimeOptions = new RuntimeOptions(session);

		// execute the XPath expression
		ResultList resultList = xq.execute(runtimeOptions, errorLog);
		if (errorLog.getHasErrors())
		{
			System.out.println(errorLog.getText());
			return false;
		}

		// A successful xquery execution returns a ResultList, if only one result is expect its value can be accessed with resultList.MainValue		
		for (Result r : resultList)
		{
			//a Result object consists of a Sequence, SerializationParams and a Uri.
			var text = r.getValue().serialize(r.getSerializationParams(), errorLog);
			if (text == null || errorLog.getHasErrors())
			{
				System.out.println("serialization failed - " + errorLog);
				return false;
			}
			else
				System.out.println(text);
		}
		
		return true;
	}

	public static void main(String[] args) 
	{
		if (args.length <= 1)
		{
			try 
			{
				final String defaultXquery = "comment{ 'The static base-uri of the expression: ', static-base-uri() }, " +
											 "element doc { <greet>Hello from xquery!</greet>, element date{ attribute desc {'The current date/time is:'}, attribute value {  current-dateTime() }}, " +
											 "element java-version { Q{my-ext}java-version() }, element os-version { Q{my-ext}os-version() }}";
				
				Run(args.length > 0 ? args[0] : defaultXquery);
			} 
			catch (Exception e) 
			{
				System.out.println(e.getMessage());
			}
		}
		else
		{
			System.out.println("to compile: javac -d bin -cp \"<RaptorXML_API_Jar_file>\" src\\XQuery_ExternalFunction.java");
			System.out.println("to run: java -cp \"<RaptorXML_bin_folder>;bin\" -Djava.library.path=\"<RaptorXML_bin_folder>\" XQuery_ExternalFunction [<xquery>]");
		}
			System.out.println("Usage: java -cp \"<RaptorXML_bin_folder>;bin\" -Djava.library.path=\"<RaptorXML_bin_folder>\" XQuery_ExternalFunction [<xquery>]");
	}
}
