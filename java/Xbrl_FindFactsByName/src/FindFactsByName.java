import com.altova.raptorxml.ErrorLog;
import com.altova.raptorxml.LicenseException;
import com.altova.raptorxml.RaptorXmlInstallationException;
import com.altova.raptorxml.xbrl.ConceptAspectValue;
import com.altova.raptorxml.xbrl.ConstraintSet;
import com.altova.raptorxml.xbrl.Fact;
import com.altova.raptorxml.xbrl.Instance;
import com.altova.raptorxml.xbrl.taxonomy.Concept;
import com.altova.raptorxml.xbrl.taxonomy.Dts;
import com.altova.raptorxml.xbrl.taxonomy.TaxonomySchema;
import com.altova.raptorxml.xbrl.Item;

/**
 * This example console application demonstrates how to load an XBRL instance, check for validation errors and search for specific facts (by concept name).
 * Given a SEC EDGAR filing, the values of all Assets facts are displayed.
 */
public class FindFactsByName 
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
	 * Local helper to locate an asset's concept for a specific namespace
	 * E.g.: get the us-gaap:Asset's concept within the DTS
	 * @throws Exception 
	 */
	private static Concept getAssetsConceptForNamespace(Dts dts, String strNamesspacePart) throws Exception
	{
		for (TaxonomySchema taxonomy : dts.getTaxonomySchemas())
			if (taxonomy.getTargetNamespace().contains(strNamesspacePart))
			{
				Concept c = dts.resolveConcept("Assets", taxonomy.getTargetNamespace());
				if (c.isNull())
					throw new Exception("Invalid namespace selector '" + strNamesspacePart + " used for conecpt selection. No asset concept found.");

				return c;
			}
	
		throw new Exception("Invalid namespace selector '" + strNamesspacePart + " used for concept selection. Schema not found.");
	}

	/**
	 * Search for specific facts (by concept name)
	 * @param xbrlInstanceFilePath full path to XBRL instance file
	 */
	public static boolean RunFindFactsByName(String strXBRLInstanceFilePath) throws RaptorXmlInstallationException, LicenseException, Exception
	{
		System.out.println("Loading XBRL instance file " + strXBRLInstanceFilePath);
		ErrorLog errorLog = new ErrorLog();
		
		Instance xbrlInstance = Instance.CreateFromUrl(strXBRLInstanceFilePath, errorLog);
		if ((xbrlInstance == null) || errorLog.getHasErrors())
		{
			System.out.println("Failed to load XRL instance file - " + errorLog.getText());
			return false;
		}
		
        // Find all US-GAAP Assets facts in the XBRL instance (filter instance facts by the concept aspect)
		Concept concept = getAssetsConceptForNamespace(xbrlInstance.getDts(), "http://fasb.org/us-gaap/");
		if (concept == null)
		{
			System.out.println("Taxonomy does not contain any Assets concept.");
			return false;
		}
		
        var cs = new ConstraintSet();
        cs.setConcept (new ConceptAspectValue(concept));
        var facts = xbrlInstance.getFacts().filter(cs);
        
        System.out.println("Found "+ facts.size() + " Assets facts.");
		for (Fact fact : facts)
		{
			if (!(fact instanceof Item))
				throw new Exception("Unexpected type error.");
			
			Item factItem = (Item)fact;

            System.out.println("Assets fact in context " + factItem.getContext().getId() + " has the effective numeric value " + factItem.getEffectiveNumericValue() + ".");
		}
		
		return true;
	}
	
	public static void main(String[] args) 
	{
		if (args.length == 1)
		{
			try 
			{
				RunFindFactsByName(args[0]);
			} 
			catch (Exception e) 
			{
				System.out.println(e.getMessage());
			}
		}
		else
		{
			System.out.println("to compile: javac -d bin -cp \"<RaptorXML_API_Jar_file>\" src\\FindFactsByName.java");
			System.out.println("to run: java -cp \"<RaptorXML_bin_folder>;bin\" -Djava.library.path=\"<RaptorXML_bin_folder>\" FindFactsByName <SEC_EDGAR_xbrl_file>");
		}
	}

}
