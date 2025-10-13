import java.util.HashMap;

import com.altova.raptorxml.ErrorLog;
import com.altova.raptorxml.LicenseException;
import com.altova.raptorxml.xbrl.ConstraintSet;
import com.altova.raptorxml.xbrl.Context;
import com.altova.raptorxml.xbrl.Instance;

/**
 * This example console application demonstrates how to load an XBRL instance, check for validation errors and search for duplicate contexts.
 * Given any XBRL instance, all duplicate contexts are displayed.
 */
public class FindDuplicateContexts
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
	
	/**
	 * Log duplicate contexts in XBRL instance file.
	 * @param xbrlInstanceFilePath full path to XBRL instance file
	 */
	public static boolean RunFindDuplicateContexts(String strXBRLInstanceFilePath) throws Exception, LicenseException
	{
		System.out.println("Perform FindDuplicateContexts");
		
		System.out.println("Loading XBRL instance file " + strXBRLInstanceFilePath);
		ErrorLog errorLog = new ErrorLog();
		
		Instance xbrlInstance = Instance.CreateFromUrl(strXBRLInstanceFilePath, errorLog);
		if ((xbrlInstance == null) || errorLog.getHasErrors())
		{
			System.out.println("Failed to load XRL instance file - " + errorLog.getText());
			return false;
		}
		
		var contexts = new HashMap<ConstraintSet, Context>();
		for (var context : xbrlInstance.getContexts())
		{
			var cs = new ConstraintSet(context);
			
			if (contexts.containsKey(cs))
				System.out.println("Context " + context.getId() + " is a duplicate of context " + contexts.get(cs).getId());
			else
				contexts.put(cs, context);
		}
		
		return true;
	}

	
	public static void main(String[] args) 
	{
		if (args.length == 1)
		{
			try 
			{
				RunFindDuplicateContexts(args[0]);
			} 
			catch (Exception e) 
			{
				System.out.println(e.getMessage());
			}
		}
		else
		{
			System.out.println("to compile: javac -d bin -cp \"<RaptorXML_API_Jar_file>\" src\\FindDuplicateContexts.java");
			System.out.println("to run: java -cp \"<RaptorXML_bin_folder>;bin\" -Djava.library.path=\"<RaptorXML_bin_folder>\" FindDuplicateContexts <xbrl_instance_file>");
		}
	}

}
