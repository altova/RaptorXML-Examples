import java.math.BigDecimal;
import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

import com.altova.raptorxml.ErrorLog;
import com.altova.raptorxml.LicenseException;
import com.altova.raptorxml.xbrl.Instance;
import com.altova.raptorxml.xbrl.taxonomy.CalculationRelationship;
import com.altova.raptorxml.xbrl.taxonomy.CalculationRelationshipNetwork;
import com.altova.raptorxml.xbrl.taxonomy.Concept;
import com.altova.raptorxml.xbrl.taxonomy.Dts;
import com.altova.raptorxml.xbrl.taxonomy.LabelCollection;
import com.altova.raptorxml.xbrl.taxonomy.LinkRoleSet;
import com.altova.raptorxml.xbrl.taxonomy.RoleType;

/**
 * This example console application demonstrates how to load an XBRL instance, check for validation errors and traverse the calculation linkbase arcs.
 * Given any XBRL instance, the calculation linkbase trees for each calculation linkrole are displayed.
 */
public class CalculationLinkbaseTraversal
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
	 * Local helper for traversal
	 */
	 private static void DisplayTreeNode(CalculationRelationshipNetwork network, Concept concept)
	 {
		 DisplayTreeNode(network, concept, null, 1);
	 }
	 private static void DisplayTreeNode(CalculationRelationshipNetwork network, Concept concept, BigDecimal weight, int level)
	 {
	     if (weight == null)
	         System.out.println(" ".repeat(level * 3) + GetLabelForConcept(concept));
	     else
	    	 System.out.println(" ".repeat(level * 3) + weight + " * " + GetLabelForConcept(concept));
	     for (CalculationRelationship rel : network.getRelationshipsFrom(concept))
	     {
	         // Display the tree nodes recursively (DFS)
	         DisplayTreeNode(network, rel.getTarget(), rel.getWeight(), level + 1);
	     }
	 }
	
	/**
	 * Log statistics on content of XBRL instance file.
	 * @param xbrlInstanceFilePath full path to XBRL instance file
	 */
	public static boolean RunCalculationLinkbaseTraversal(String strXBRLInstanceFilePath) throws Exception, LicenseException
	{
		System.out.println("Loading XBRL instance file " + strXBRLInstanceFilePath);
		ErrorLog errorLog = new ErrorLog();
		
		Instance xbrlInstance = Instance.CreateFromUrl(strXBRLInstanceFilePath, errorLog);
		if ((xbrlInstance == null) || errorLog.getHasErrors())
		{
			System.out.println("Failed to load XRL instance file - " + errorLog.getText());
			return false;
		}
		
		Dts dts = xbrlInstance.getDts();

		// Display the calculation tree for each link role - sort the roles, before-hand
		LinkRoleSet linkRoles = xbrlInstance.getDts().getCalculationLinkRoles();
		List<String> list = new ArrayList<String>(linkRoles);
		Collections.sort(list, (lr1, lr2) -> {return GetDefinitionForRole(dts, lr1).compareTo(GetDefinitionForRole(dts, lr2)); });

		for (String linkrole : list)
		{
			System.out.println(GetDefinitionForRole(xbrlInstance.getDts(), linkrole) + " - " + linkrole);
			
		    var network = xbrlInstance.getDts().getCalculationNetwork(linkrole);
		    for (var concept : network.getRoots())
		        DisplayTreeNode(network, concept);
		    
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
				RunCalculationLinkbaseTraversal(args[0]);
			} 
			catch (Exception e) 
			{
				System.out.println(e.getMessage());
			}
		}
		else
		{
			System.out.println("to compile: javac -d bin -cp \"<RaptorXML_API_Jar_file>\" src\\CalculationLinkbaseTraversal.java");
			System.out.println("to run: java -cp \"<RaptorXML_bin_folder>;bin\" -Djava.library.path=\"<RaptorXML_bin_folder>\" CalculationLinkbaseTraversal <xbrl_instance_file> [<namespace_part>]");
		}
	}

}
