import com.altova.raptorxml.ErrorLog;
import com.altova.raptorxml.LicenseException;
import com.altova.raptorxml.ProductInfo;
import com.altova.raptorxml.xpath.AtomicItem;
import com.altova.raptorxml.xpath.CompileOptions;
import com.altova.raptorxml.xpath.Expression;
import com.altova.raptorxml.xpath.ExternalFunctionObject;
import com.altova.raptorxml.xpath.ExternalFunctions;
import com.altova.raptorxml.xpath.Instruction;
import com.altova.raptorxml.xpath.Item;
import com.altova.raptorxml.xpath.RuntimeOptions;
import com.altova.raptorxml.xpath.Sequence;
import com.altova.raptorxml.xpath.SequenceList;
import com.altova.raptorxml.xpath.Session;
import com.altova.raptorxml.xsd.XSDAnySimpleType;
import com.altova.raptorxml.xsd.XSDString;

/**
 * Test XPath expressions with functions that call back into user code.
 * 
 * Consult the 'main' method on how to compile and run the sample. 
 */
public class XPath_ExternalFunction 
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

	private static boolean Run(String userFunctionSignature, String xpathExpression) throws Exception, LicenseException
	{
		ErrorLog errorLog = new ErrorLog();
		
		// prepare XPath compile options
		Session xpathSession = new Session();
		CompileOptions compileOptions = new CompileOptions(xpathSession);
		if (userFunctionSignature != null)
		{
			// extend ExternalFunctionObject and override onInvoke to provide code for the external function.
			ExternalFunctionObject userFunction = new ExternalFunctionObject(userFunctionSignature)
			{
				@Override
				public Sequence onInvoke(SequenceList args, Session rSession, Instruction instruction) throws Exception, Throwable {
					// implement what you promised in the signature
					
					String text = "Hello ";
					boolean bFirst = true;
					for (Item arg : args.get(0))
					{
						if (arg instanceof AtomicItem)
						{
							XSDAnySimpleType data = ((AtomicItem)arg).getAnySimpleType();
							if (! (data instanceof XSDString))
								throw new com.altova.raptorxml.xpath.Error("Unexpected parameter type.");
									
							if (bFirst)
								bFirst = false;
							else
								text += ", ";
							
							// this assumes that the parameter type is xs:string or xs:string*. 
							// testing of type and throwing a user exception would be nice here 
							text += data.asString();
						}
						else
							throw new com.altova.raptorxml.xpath.Error("Unexpected parameter type.");
					}
						
					return Sequence.FromItem(AtomicItem.FromString(text, xpathSession));
				}
			};
			
			ExternalFunctionObject[] userFunctions = new ExternalFunctionObject[] { userFunction }; 
			ExternalFunctions externalFunctions = ExternalFunctions.Create(xpathSession, errorLog, null, null, userFunctions);
			if (errorLog.getHasErrors())
			{
				System.out.println(errorLog.getText());
				return false;
			}
			compileOptions.setExternalFunctions(externalFunctions);
		}
		
		Expression xpath = Expression.Compile(xpathExpression, compileOptions, errorLog);
		if (errorLog.getHasErrors())
		{
			System.out.println(errorLog.getText());
			return false;
		}

		// prepare XPath run-time options
		RuntimeOptions runtimeOptions = new RuntimeOptions(xpathSession);

		// execute the XPath expression
		Sequence sequ = xpath.execute(runtimeOptions, errorLog);
		if (errorLog.getHasErrors())
		{
			System.out.println(errorLog.getText());
			return false;
		}

		// log the result
		for (Item item : sequ)
		{
			if (item instanceof AtomicItem)
			{
				AtomicItem atomicItem = (AtomicItem)item;
				XSDAnySimpleType data = atomicItem.getAnySimpleType();

				if (data instanceof XSDString)
				{
					XSDString strData = (XSDString)data;
					System.out.println(strData.getValue());
				}
				else
					System.out.println(data.toString());

				System.out.println();
			}
			else
			{
				System.out.println("Unexpected result type.");
				return false;
			}
		}

		return true;
	}

	public static void main(String[] args) 
	{
		if (args.length <= 2)
		{
			try 
			{
				final String defaultUserFunctionSignature = "Q{user}sayHello($arg1 as xs:string) as xs:string";
				final String defaultXpathExpression = "Q{user}sayHello('world')";
				
				Run(args.length > 0 ? args[0] : defaultUserFunctionSignature, args.length > 1 ? args[1] : defaultXpathExpression);
			} 
			catch (Exception e) 
			{
				System.out.println(e.getMessage());
			}
		}
		else
		{
			System.out.println("to compile: javac -d bin -cp \"<RaptorXML_API_Jar_file>\" src\\XPath_ExternalFunction.java");
			System.out.println("to run: java -cp \"<RaptorXML_bin_folder>;bin\" -Djava.library.path=\"<RaptorXML_bin_folder>\" XPath_ExternalFunction [<signature>] [<xpath_expression>]");
		}
	}
}
