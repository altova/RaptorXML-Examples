import com.altova.raptorxml.ErrorLog;
import com.altova.raptorxml.LicenseException;
import com.altova.raptorxml.xbrl.Instance;
import com.altova.raptorxml.xbrl.taxonomy.Concept;
import com.altova.raptorxml.xbrl.taxonomy.Dts;
import com.altova.raptorxml.xbrl.taxonomy.LabelCollection;
import com.altova.raptorxml.xbrl.taxonomy.RoleType;
import com.altova.raptorxml.xbrl.xdt.DRS;

/**
 * This example console application demonstrates how to load an XBRL instance, check for validation errors and traverse the dimensional relationship set.
 * Given any XBRL instance, the dimensional relationship set for each linkrole are displayed.
 */
public class DimensionalRelationshipSetTraversal
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
	 * Local helper for traversal
	 */
	private static String GetDefinitionForRole(Dts dts, String roleUri)
	{
	    RoleType roleType = dts.getRoleType(roleUri);
	    if (roleType != null)
	    {
	        var definition = roleType.getDefinition();
	        if (definition != null)
	            return definition.getValue();
	    }
	    return roleUri;
	}

	/**
	 * Local helper for traversal
	 */
	private static String GetLabelForConcept(Concept concept)
	{
	    // Try to find a standard English label
	    LabelCollection labels = concept.getLabels("http://www.xbrl.org/2003/role/label", null, "en");
	    if (labels.size() > 0)
	        return labels.iterator().next().getText();

	    // Fallback to any other label that is assigned to the concept
	    labels = concept.getLabels();
	    if (labels.size() > 0)
	        return labels.iterator().next().getText();

	    // If there are no labels, display the concept QName
	    return concept.getQName().toString();
	}

	/**
	 * Local helpers for DimensionalRelationshipSetTraversal
	 */
    private static void DisplayDomainMembers(DRS drs, com.altova.raptorxml.xbrl.taxonomy.Item item, String linkRole, int level)
    {
        System.out.println(" ".repeat(level * 3) + GetLabelForConcept(item));
        for (var rel : drs.getDomainMemberRelationships(item, linkRole))
            DisplayDomainMembers(drs, rel.getTarget(), rel.getTargetRole() != null ? rel.getTargetRole() : linkRole, level + 1);
    }

    private static void DisplayDimensions(com.altova.raptorxml.xbrl.xdt.DRS drs, com.altova.raptorxml.xbrl.xdt.Dimension dimension, String linkRole, int level)
    {
        System.out.println(" ".repeat(level * 3) + GetLabelForConcept(dimension));
        for (var rel : drs.getDimensionDomainRelationships(dimension, linkRole))
            DisplayDomainMembers(drs, rel.getTarget(), rel.getTargetRole() != null ? rel.getTargetRole() : linkRole, level + 1);
    }

    private static void DisplayHypercubes(com.altova.raptorxml.xbrl.xdt.DRS drs, com.altova.raptorxml.xbrl.xdt.Hypercube hypercube, String linkRole, int level)
    {
        System.out.println(" ".repeat(level * 3) + GetLabelForConcept(hypercube));
        for (var rel : drs.getHypercubeDimensionRelationships(hypercube, linkRole))
            DisplayDimensions(drs, rel.getTarget(), rel.getTargetRole() != null ? rel.getTargetRole() : linkRole, level + 1);
    }

    private static void DisplayPrimaryItems(com.altova.raptorxml.xbrl.xdt.DRS drs, com.altova.raptorxml.xbrl.taxonomy.Item item, String linkRole, int level)
    {
        System.out.println(" ".repeat(level * 3) + GetLabelForConcept(item));
        for (var rel : drs.getHasHypercubeRelationships(item, linkRole))
            DisplayHypercubes(drs, rel.getTarget(), rel.getTargetRole() != null ? rel.getTargetRole() : linkRole, level + 1);
        for (var rel : drs.getDomainMemberRelationships(item, linkRole))
            DisplayPrimaryItems(drs, rel.getTarget(), rel.getTargetRole() != null ? rel.getTargetRole() : linkRole, level + 1);
    }
			
	/**
	 * Log statistics on content of XBRL instance file.
	 * Compare with C# test application DimensionalRelationshipSetTraversal.
	 * @param xbrlInstanceFilePath full path to XBRL instance file
	 */
	public static boolean RunDimensionalRelationshipSetTraversal(String strXBRLInstanceFilePath) throws Exception, LicenseException
	{
		System.out.println("Perform a DimensionalRelationshipSetTraversal");
		
		System.out.println("Loading XBRL instance file " + strXBRLInstanceFilePath);
		ErrorLog errorLog = new ErrorLog();
		
		Instance xbrlInstance = Instance.CreateFromUrl(strXBRLInstanceFilePath, errorLog);
		if ((xbrlInstance == null) || errorLog.getHasErrors())
		{
			System.out.println("Failed to load XRL instance file - " + errorLog.getText());
			return false;
		}
		
		Dts dts = xbrlInstance.getDts();
		DRS drs = dts.getDimensionalRelationshipSet();

		// Display the dimensional relationship tree tree for each link role
		for (String linkrole : drs.getLinkRoles())
		{
			System.out.println(GetDefinitionForRole(xbrlInstance.getDts(), linkrole) + " - " + linkrole);
			
		    for (var concept : drs.getPrimaryItems(linkrole))
		        DisplayPrimaryItems(drs, concept, linkrole, 1);
		    
		    System.out.println();
		}
		
		return true;
	}
	
	public static void main(String[] args) 
	{
		if (args.length == 1)
		{
			try 
			{
				RunDimensionalRelationshipSetTraversal(args[0]);
			} 
			catch (Exception e) 
			{
				System.out.println(e.getMessage());
			}
		}
		else
		{
			System.out.println("to compile: javac -d bin -cp \"<RaptorXML_API_Jar_file>\" src\\DimensionalRelationshipSetTraversal.java");
			System.out.println("to run: java -cp \"<RaptorXML_bin_folder>;bin\" -Djava.library.path=\"<RaptorXML_bin_folder>\" DimensionalRelationshipSetTraversal <xbrl_instance_file> [<namespace_part>]");
		}
	}

}
