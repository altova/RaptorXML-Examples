import java.util.HashMap;
import java.util.Map;

import com.altova.raptorxml.ErrorLog;
import com.altova.raptorxml.LicenseException;
import com.altova.raptorxml.xbrl.Instance;
import com.altova.raptorxml.xbrl.Unit;
import com.altova.raptorxml.xbrl.taxonomy.Dts;
import com.altova.raptorxml.xbrl.taxonomy.ExtendedLink;
import com.altova.raptorxml.xbrl.taxonomy.Footnote;
import com.altova.raptorxml.xbrl.taxonomy.Resource;

/**
 * This example console application demonstrates how to access the data model of the XBRL instance and the supporting DTS.
 * Given any XBRL instance, some statistics about the XBRL instance the it's DTS are displayed.
 */
public class InstanceStatistics
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
	 * Log statistics on content of XBRL instance file.
	 * @param xbrlInstanceFilePath full path to XBRL instance file
	 */
	public static boolean RunInstanceStatistics(String strXBRLInstanceFilePath) throws Exception, LicenseException
	{
		System.out.println("Loading XBRL instance file " + strXBRLInstanceFilePath);
		ErrorLog errorLog = new ErrorLog();
		
		Instance xbrlInstance = Instance.CreateFromUrl(strXBRLInstanceFilePath, errorLog);
		if ((xbrlInstance == null) || errorLog.getHasErrors())
		{
			System.out.println("Failed to load XRL instance file - " + errorLog.getText());
			return false;
		}
		
		// DTS statistics
		Dts dts = xbrlInstance.getDts();
		System.out.println("DTS contains " + dts.getDocuments().size() + " documents.");
		System.out.println("DTS contains " + dts.getTaxonomySchemas().size() + " taxonomy schemas.");
		System.out.println("DTS contains " + dts.getLinkbases().size() + " linkbases.");
		
		System.out.println("DTS contains " + dts.getConcepts().size() + " concepts");
		System.out.println("DTS contains " + dts.getTuples().size() + " tuples");
		System.out.println("DTS contains " + (dts.getItems().size() - dts.getHypercubes().size() - dts.getDimensions().size()) + " non-xdt items");
		System.out.println("DTS contains " + dts.getHypercubes().size() + " hypercubes");
		System.out.println("DTS contains " + dts.getDimensions().size() + " dimensions");
		
		System.out.println("DTS contains " + dts.getParameters().size() + " parameters");
		System.out.println("DTS contains " + dts.getAssertions().size() + " assertions");
		System.out.println("DTS contains " + dts.getFormulas().size() + " formulas");
		System.out.println("DTS contains " + dts.getTables().size() + " tables");
		
		System.out.println("DTS contains " + dts.getDefinitionLinkRoles().size() + " definition linkroles");
		System.out.println("DTS contains " + dts.getPresentationLinkRoles().size() + " presentation linkroles");
		System.out.println("DTS contains " + dts.getCalculationLinkRoles().size() + " calculation linkroles");
		System.out.println("DTS contains " + dts.getLabelLinkRoles().size() + " label linkroles");
		System.out.println("DTS contains " + dts.getReferenceLinkRoles().size() + " reference linkroles");
		
		System.out.println();
		
		// instance statistics
		int contexts = 0, instantContexts = 0, startEndContexts = 0, foreverContexts = 0;
		for (com.altova.raptorxml.xbrl.Context c : xbrlInstance.getContexts())
		{
			contexts++;
			
			switch (c.getPeriodAspectValue().getType())
			{
				case Instant: instantContexts++; break;
				case StartEnd: startEndContexts++; break;
				case Forever: foreverContexts++; break;
			}
		}
		System.out.println("Instance contains " + contexts + " contexts.");
		System.out.println("Instance contains " + instantContexts + " instance period contexts.");
		System.out.println("Instance contains " + startEndContexts + " start/end period contexts.");
		System.out.println("Instance contains " + foreverContexts + " forever period contexts.");
		
		int units = 0, simpleUnits = 0, currencyUnits = 0;
		for (Unit u : xbrlInstance.getUnits())
		{
			units++;
			if (u.getAspectValue().getIsSimple()) simpleUnits++;
			if (u.getAspectValue().getIsMonetary()) currencyUnits++;
		}
		System.out.println("Instance contains " + units + " units.");
		System.out.println("Instance contains " + simpleUnits + " simple units.");
		System.out.println("Instance contains " + currencyUnits + " currency units.");

		System.out.println("Instance contains " + xbrlInstance.getFacts().size() + " facts.");
		System.out.println("Instance contains " + xbrlInstance.getNilFacts().size() + " nil facts.");
		System.out.println("Instance contains " + xbrlInstance.getChildItems().size() + " top-level item facts.");
		System.out.println("Instance contains " + xbrlInstance.getChildTuples().size() + " top-level tuple facts.");
		
		// footnotes
		Map<String, Integer> footnoteCounts = new HashMap<String, Integer>();
		for (ExtendedLink link : xbrlInstance.getFootnoteLinks())
			for (Resource res : link.getResources())
				if (res instanceof Footnote)
				{
					Footnote fn = (Footnote)res;
					String lang = fn.getXmlLang();
					Integer count = footnoteCounts.get(lang);
					if (count == null)
						footnoteCounts.put(lang, 1);
					else
						footnoteCounts.put(lang, count + 1);
				}
		if (footnoteCounts.isEmpty())
			System.out.println("Instance contains no footnote resources.");
		else
			for (String lang : footnoteCounts.keySet())
				System.out.println("Instance contains " + footnoteCounts.get(lang) + " footnote resources in language " + lang + ".");
		
		return true;
	}

	public static void main(String[] args) 
	{
		if (args.length == 1)
		{
			try 
			{
				RunInstanceStatistics(args[0]);
			} 
			catch (Exception e) 
			{
				System.out.println(e.getMessage());
			}
		}
		else
		{
			System.out.println("to compile: javac -d bin -cp \"<RaptorXML_API_Jar_file>\" src\\InstanceStatistics.java");
			System.out.println("to run: java -cp \"<RaptorXML_bin_folder>;bin\" -Djava.library.path=\"<RaptorXML_bin_folder>\" InstanceStatistics [<signature>] [<xpath_expression>]");
		}
	}

}
