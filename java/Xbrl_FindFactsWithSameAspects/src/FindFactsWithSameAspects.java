import com.altova.raptorxml.ErrorLog;
import com.altova.raptorxml.LicenseException;
import com.altova.raptorxml.RaptorXmlInstallationException;
import com.altova.raptorxml.xbrl.ConceptAspectValue;
import com.altova.raptorxml.xbrl.ConstraintSet;
import com.altova.raptorxml.xbrl.Fact;
import com.altova.raptorxml.xbrl.Instance;
import com.altova.raptorxml.xbrl.taxonomy.Concept;
import com.altova.raptorxml.xbrl.taxonomy.Dts;
import com.altova.raptorxml.xbrl.Item;

/**
 * This example console application demonstrates how to load an XBRL instance, check for validation errors and search for specific facts (by concept name) that share the same aspects.
 * Given a SEC EDGAR filing, the values of all paired Assets and LiabilitiesAndStockholdersEquity facts are displayed. This is similar to implicit filtering as defined by the Formula 1.0 specification.
 */
public class FindFactsWithSameAspects 
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
	 * local helper function.
	 */
	 private static Concept GetUsGaapConcept(Dts dts, String name)
	 {
	     // Find the us-gaap namespace referenced within the DTS
	     String usgaap_namespace = null;
	     for (var taxonomy : dts.getTaxonomySchemas())
	     {
	         if (taxonomy.getTargetNamespace().startsWith("http://fasb.org/us-gaap/"))
	         {
	             usgaap_namespace = taxonomy.getTargetNamespace();
	             break;
	         }
	     }
	     if (usgaap_namespace == null)
	         return null;

	     // Find the us-gaap concept within the DTS
	     return dts.resolveConcept(name, usgaap_namespace);
	 }

	/**
	 * Log facts with same aspects in XBRL instance file.
	 * @param xbrlInstanceFilePath full path to XBRL instance file
	 */
	public static boolean RunFindFactsWithSameAspects(String strXBRLInstanceFilePath) throws RaptorXmlInstallationException, LicenseException, Exception
	{
		System.out.println("Perform FindFactsWithSameAspects");
		
		System.out.println("Loading XBRL instance file " + strXBRLInstanceFilePath);
		ErrorLog errorLog = new ErrorLog();
		
		Instance xbrlInstance = Instance.CreateFromUrl(strXBRLInstanceFilePath, errorLog);
		if ((xbrlInstance == null) || errorLog.getHasErrors())
		{
			System.out.println("Failed to load XRL instance file - " + errorLog.getText());
			return false;
		}
		
		var assetsConcept = GetUsGaapConcept(xbrlInstance.getDts(),"Assets");
		if (assetsConcept == null)
		{
		    System.out.println("Taxonomy does not contain an Assets concept.");
		    return true;
		}
		var liabilitiesConcept = GetUsGaapConcept(xbrlInstance.getDts(), "LiabilitiesAndStockholdersEquity");
		if (liabilitiesConcept == null)
		{
		    System.out.println("Taxonomy does not contain an LiabilitiesAndStockholdersEquity concept.");
		    return true;
		}

		// Find all US-GAAP Assets facts in the XBRL instance (filter instance facts by the concept aspect)
		var cs = new ConstraintSet();
		cs.setConcept(new ConceptAspectValue(assetsConcept));
		var assetsFacts = xbrlInstance.getFacts().filter(cs);

		for (Fact assetsFact : assetsFacts)
		{
			if (!(assetsFact instanceof Item))
				throw new Exception("Unexpected type error.");
			
			Item assetsFactItem = (Item)assetsFact;

		    // Find all instance facts that share the same aspect values as the current assetsFact apart from the concept aspect.
		    cs = new ConstraintSet(assetsFact);
		    cs.setConcept(new ConceptAspectValue(liabilitiesConcept));
		    var liabilitiesFacts = xbrlInstance.getFacts().filter(cs);

		    for (Fact liabilitiesFact : liabilitiesFacts)
		    {
				if (!(liabilitiesFact instanceof Item))
					throw new Exception("Unexpected type error.");
				
				Item liabilitiesFactItem = (Item)liabilitiesFact;

				System.out.println("Assets                           fact in context " + assetsFactItem.getContext().getId() + " has the effective numeric value " + assetsFactItem.getEffectiveNumericValue() + ".");
		        System.out.println("LiabilitiesAndStockholdersEquity fact in context " + liabilitiesFactItem.getContext().getId() + " has the effective numeric value " + liabilitiesFactItem.getEffectiveNumericValue() + ".");
		        System.out.println();
		    }			
		}
		
		return true;
	}
	
	public static void main(String[] args) 
	{
		if (args.length == 1)
		{
			try 
			{
				RunFindFactsWithSameAspects(args[0]);
			} 
			catch (Exception e) 
			{
				System.out.println(e.getMessage());
			}
		}
		else
		{
			System.out.println("to compile: javac -d bin -cp \"<RaptorXML_API_Jar_file>\" src\\FindFactsWithSameAspects.java");
			System.out.println("to run: java -cp \"<RaptorXML_bin_folder>;bin\" -Djava.library.path=\"<RaptorXML_bin_folder>\" FindFactsWithSameAspects [<signature>] [<xpath_expression>]");
		}
	}

}
