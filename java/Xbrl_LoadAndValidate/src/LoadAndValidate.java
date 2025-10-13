import com.altova.raptorxml.ErrorLog;
import com.altova.raptorxml.LicenseException;
import com.altova.raptorxml.RaptorXmlInstallationException;
import com.altova.raptorxml.xbrl.AspectType;
import com.altova.raptorxml.xbrl.AspectValue;
import com.altova.raptorxml.xbrl.ConceptAspectValue;
import com.altova.raptorxml.xbrl.ConstraintSet;
import com.altova.raptorxml.xbrl.EntityIdentifierAspectValue;
import com.altova.raptorxml.xbrl.Fact;
import com.altova.raptorxml.xbrl.FactSet;
import com.altova.raptorxml.xbrl.Instance;
import com.altova.raptorxml.xbrl.taxonomy.Concept;
import com.altova.raptorxml.xbrl.taxonomy.Dts;
import com.altova.raptorxml.xbrl.taxonomy.ExtendedLink;
import com.altova.raptorxml.xbrl.taxonomy.FootnoteRelationship;
import com.altova.raptorxml.xbrl.taxonomy.LinkbaseRef;
import com.altova.raptorxml.xbrl.taxonomy.Locator;
import com.altova.raptorxml.xbrl.taxonomy.Resource;
import com.altova.raptorxml.xbrl.taxonomy.SchemaRef;
import com.altova.raptorxml.xbrl.taxonomy.TaxonomySchema;
import com.altova.raptorxml.xbrl.Item;
import com.altova.raptorxml.xbrl.LocationAspectValue;
import com.altova.raptorxml.xbrl.PeriodAspectValue;

/**
 * A very simple and minimalistic example demonstrating how to load XBRL instances
 */
public class LoadAndValidate
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
	 * Local helper to log data on an AspectValue.
	 */
	private static void logAspectValue(AspectValue aspectValue)
	{
		AspectType aspectType = aspectValue.getAspect().getType(); 
		switch (aspectType)
		{
			case Concept:
			{
				ConceptAspectValue conceptValue = (ConceptAspectValue)aspectValue;
				System.out.println("        " + aspectType.name() + ": " + conceptValue.getConcept().getName());
				break;
			}
			case Location:
			{
				LocationAspectValue locationValue = (LocationAspectValue)aspectValue;
				System.out.println("        " + aspectType.name() + ": " + locationValue.toString());
				break;
			}
			case EntityIdentifier:
			{
				EntityIdentifierAspectValue eValue = (EntityIdentifierAspectValue)aspectValue;
				System.out.println("        " + aspectType.name() + ": " + eValue.getIdentifier());
				break;
			}
			case Period:
			{
				PeriodAspectValue pValue = (PeriodAspectValue)aspectValue;
				switch (pValue.getType())
				{
					case Instant:
						System.out.println("        " + aspectType.name() + " Instant: " + pValue.getInstant().toString());
						break;
					case StartEnd:
						System.out.println("        " + aspectType.name() + " StartEnd: " + pValue.getStart() + ".." + pValue.getEnd());
						break;
					case Forever:
						System.out.println("        " + aspectType.name() + " Forever");
						break;
				}
				break;
			}
			default:
				System.out.println("        " + aspectType.name() + ": ");
		}
	}
	
	/**
	 * Local helper function to log data on a fact (which is hopefully an Item.
	 */
	private static void logFact(Item fact)
	{
		System.out.println("    fact: " + fact.getQName().getLocalName());

		// how can we figure out if it is indeed an Item?
		Item item = (Item)fact;
		System.out.println("    value: " + item.toString());
		System.out.println("    xPointer: " + fact.getXPointer());

		System.out.println("    aspects:");
		for (AspectValue aspectValue : fact.getAspectValues())
			logAspectValue(aspectValue);

		System.out.println("    foot notes:");
		for (FootnoteRelationship footNoteRef : fact.getFootnoteRelationships())
			System.out.println("        " + footNoteRef.getTarget().getText());
		
		System.out.println();
	}

	/**
	 * Local helper function to log data on a fact (which is hopefully an Item.
	 */
	private static void logFact_short(Item fact)
	{
		String factValue = fact.getNormalizedValue();
		if (factValue.length() > 50)
			factValue = factValue.substring(0, 50);
		
		System.out.println("    " + fact.getQName().getLocalName() + " = " + factValue);
	}

	/**
	 * Local helper to locate an asset'S concept for a specific namespace
	 * E.g.: get the us-gaap:Asset's concept within the DTS
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
	/**
	 * Load and check XBRL instance document.
	 * Log all facts. If there are more than 10, only log the values.
	 * @param xbrlInstanceFilePath full path to XBRL instance file
	 */
	public static boolean RunLoadAndValidate(String strXBRLInstanceFilePath, String strNamespacePart) throws RaptorXmlInstallationException, LicenseException, Exception
	{
		System.out.println("Loading XBRL instance file " + strXBRLInstanceFilePath);
		ErrorLog errorLog = new ErrorLog();
		
		Instance xbrlInstance = Instance.CreateFromUrl(strXBRLInstanceFilePath, errorLog);

		if (Instance.ValidateDocument(xbrlInstance.getDocumentItem(), errorLog))
			System.out.println("The document is valid.");
		else
			return false;
		
		System.out.println("List of schema references:");
		for (SchemaRef ref : xbrlInstance.getSchemaRefs())
			System.out.println("    " + ref.getXlinkHref());

		System.out.println("List of link-base references:");
		for (LinkbaseRef ref : xbrlInstance.getLinkbaseRefs())
			System.out.println("    " + ref.getXlinkHref());

		System.out.println("List of facts:");
		FactSet facts = null;
		if (strNamespacePart == null)
		{
			facts = xbrlInstance.getFacts();
		}
		else
		{
			ConstraintSet cSet = new ConstraintSet();
			Concept c = getAssetsConceptForNamespace(xbrlInstance.getDts(), strNamespacePart);
			ConceptAspectValue cav = new ConceptAspectValue(c);
			cSet.setConcept(cav);
			facts = xbrlInstance.getChildFacts().filter(cSet);
		}
		
		boolean detailed = facts.size() <= 10; 
		for (Fact fact : facts)
			if (detailed)
				logFact((Item)fact);
			else
				logFact_short((Item)fact);

		System.out.println("List of foot note references:");
		for (ExtendedLink link : xbrlInstance.getFootnoteLinks())
		{
			for ( Resource res : link.getResources())
				System.out.println("    resource: " + res.getElement().getTextContent());
			for ( Locator locator : link.getLocators())
				System.out.println("    link: " + locator.getTargetElement().getQName().getLocalName());
		}

		return true;
	}
	
	public static void main(String[] args) 
	{
		if ((args.length == 1) || (args.length == 2))
		{
			try 
			{
				RunLoadAndValidate(args[0], args.length == 2 ? args[1] : null);
			} 
			catch (Exception e) 
			{
				System.out.println(e.getMessage());
			}
		}
		else
		{
			System.out.println("to compile: javac -d bin -cp \"<RaptorXML_API_Jar_file>\" src\\LoadAndValidate.java");
			System.out.println("to run: java -cp \"<RaptorXML_bin_folder>;bin\" -Djava.library.path=\"<RaptorXML_bin_folder>\" LoadAndValidate <xbrl_instance_file> [<namespace_part>]");
		}
	}
}
