import java.nio.charset.StandardCharsets;

import com.altova.raptorxml.ErrorLog;
import com.altova.raptorxml.LicenseException;
import com.altova.raptorxml.xml.Instance;
import com.altova.raptorxml.xpath.AtomicItem;
import com.altova.raptorxml.xpath.DeliveryFormat;
import com.altova.raptorxml.xpath.ExternalFunctionObject;
import com.altova.raptorxml.xpath.ExternalFunctions;
import com.altova.raptorxml.xpath.Instruction;
import com.altova.raptorxml.xpath.NodeItem;
import com.altova.raptorxml.xpath.Result;
import com.altova.raptorxml.xpath.ResultList;
import com.altova.raptorxml.xpath.Sequence;
import com.altova.raptorxml.xpath.SequenceList;
import com.altova.raptorxml.xpath.SerializationMethod;
import com.altova.raptorxml.xpath.SerializationParams;
import com.altova.raptorxml.xpath.Session;
import com.altova.raptorxml.xpath.Version;
import com.altova.raptorxml.xslt.CompileOptions;
import com.altova.raptorxml.xslt.RuntimeOptions;
import com.altova.raptorxml.xslt.Stylesheet;
import com.altova.raptorxml.xslt.StylesheetProvider;

/**
 */
public class Xslt_Test 
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

	private static boolean Run(StylesheetProvider  stylesheetProvider) throws Exception, LicenseException
	{
		ErrorLog errorLog = new ErrorLog();
		
		// create XPath session
		Session session = new Session();
		
		// create the XQuery compile options
		CompileOptions compileOptions = new CompileOptions(session);
		
		// specify the xslt spec that should be used
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

		compileOptions.setDefaultSerializationParams( new SerializationParams(session));
		compileOptions.getDefaultSerializationParams().setIndent(false);
		compileOptions.getDefaultSerializationParams().setMethod(SerializationMethod.Html);
		compileOptions.getDefaultSerializationParams().setHtmlVersion("5.0");
		
		Stylesheet stylesheet = Stylesheet.Compile(stylesheetProvider, compileOptions, errorLog);
		if (errorLog.getHasErrors())
		{
			System.out.println(errorLog.getText());
			return false;
		}

		// prepare XPath run-time options
		RuntimeOptions runtimeOptions = new RuntimeOptions(session);

		var xmlInstance = Instance.CreateFromBuffer(
			   ("<?xml version=\"1.0\" encoding=\"UTF-8\"?> " + 
		        "<PERSONAE PLAY=\"OTHELLO\"> " +
			    "<TITLE>Dramatis Personae</TITLE> " +
			    "<PERSONA>DUKE OF VENICE</PERSONA> " +
			    "<PERSONA>BRABANTIO, a senator.</PERSONA> " +
			    "<PERSONA>Other Senators.</PERSONA> " +
			    "<PERSONA>GRATIANO, brother to Brabantio.</PERSONA> " +
			    "<PERSONA>LODOVICO, kinsman to Brabantio.</PERSONA> " +
			    "<PERSONA>OTHELLO, a noble Moor in the service of the Venetian state.</PERSONA> " +
			    "<PERSONA>CASSIO, his lieutenant.</PERSONA> " +
			    "<PERSONA>IAGO, his ancient.</PERSONA> " +
			    "<PERSONA>RODERIGO, a Venetian gentleman.</PERSONA> " +
			    "<PERSONA>MONTANO, Othello's predecessor in the government of Cyprus.</PERSONA> " +
			    "<PERSONA>Clown, servant to Othello. </PERSONA> " +
			    "<PERSONA>DESDEMONA, daughter to Brabantio and wife to Othello.</PERSONA> " +
			    "<PERSONA>EMILIA, wife to Iago.</PERSONA> " +
			    "<PERSONA>BIANCA, mistress to Cassio.</PERSONA> " +
			    "<PERSONA>Sailor, Messenger, Herald, Officers,  " +
			    "         Gentlemen, Musicians, and Attendants.</PERSONA> " +
			    "</PERSONAE>" ).getBytes(StandardCharsets.UTF_8), errorLog);

		if (xmlInstance == null || errorLog.getHasErrors())
		{
			System.out.println("couldn't load xml" + errorLog);
			return false;
		}

		runtimeOptions.setInitialMatchSelection(Sequence.FromItem(NodeItem.FromInformationItem(xmlInstance.getDocumentItem(), session)));
		runtimeOptions.setDeliveryFormat(DeliveryFormat.Raw);

		// execute the XPath expression
		ResultList resultList = stylesheet.execute(runtimeOptions, errorLog);
		if (errorLog.getHasErrors())
		{
			System.out.println(errorLog.getText());
			return false;
		}

		// A successful xquery execution returns a ResultList, if only one result is expect its value can be accessed with resultList.MainValue		
		for (Result r : resultList)
		{
			if (r.getUri() != null && r.getUri().length() > 0)
			{
				System.out.println("Result for Uri: " + r.getUri());
			}
			if (runtimeOptions.getDeliveryFormat() == DeliveryFormat.Serialized)
			{
				//print the size of the sequence and the contained items type and string value to the console
				//an XPath.Sequence implements the IEnumerable interface
				System.out.print(String.format("XPath.Sequence[%d]{{", r.getValue().size())); //number of XPath.Items
				String prefix = "";
				for (var item : r.getValue())
				{
					System.out.print(String.format(prefix + "%s('%s')", item.getTypeName(), item.toString()));
					prefix = ", ";
				}
				System.out.println("}");
			}
			else
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
		}
		
		return true;
	}

	public static void main(String[] args) 
	{
		if (args.length <= 1)
		{
			try 
			{
				final String defaultXslt = 
					"<xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"3.0\" expand-text=\"yes\"> " +
					"<xsl:strip-space elements=\"PERSONAE\"/> " + 
					"<xsl:template match=\"PERSONAE\"> " +
					"<xsl:result-document method=\"html\" html-version=\"5\"> " +
					   "<html> " +
					     "<head> " +
					       "<title>The Cast of {@PLAY}</title> " +
					     "</head> " +
					     "<body> " +
					       "<xsl:apply-templates/> " +
					     "</body> " +
					     "<xsl:text>&#x0A;</xsl:text> " +
					     "<xsl:comment select=\" 'Java version via native extension call: ' || Q{my-ext}java-version() \" /> " +
					     "<xsl:text>&#x0A;</xsl:text> " +
					     "<xsl:comment select=\" 'System os version via native extension call: ' || Q{my-ext}os-version() \" /> " +
					     "<xsl:text>&#x0A;</xsl:text> " +
					   "</html> " +
					"</xsl:result-document> " +
					"</xsl:template> " +
					 "<xsl:template match=\"TITLE\"> " +
					   "<h1>{.}</h1> " +
					 "</xsl:template> " +
					 "<xsl:template match=\"PERSONA[count(tokenize(., ',')) = 2]\"> " +
					   "<p><b>{substring-before(., ',')}</b>: {substring-after(., ',')}</p> " +
					 "</xsl:template>  " +
					 "<xsl:template match=\"PERSONA\"> " +
					   "<p><b>{.}</b></p> " +
					 "</xsl:template> " +
					"</xsl:stylesheet>";
				
				if (args.length > 0)
					Run(StylesheetProvider.FromLocation(defaultXslt));
				else
					Run(StylesheetProvider.FromText(defaultXslt));
			} 
			catch (Exception e) 
			{
				System.out.println(e.getMessage());
			}
		}
		else
		{
			System.out.println("to compile: javac -d bin -cp \"<RaptorXML_API_Jar_file>\" src\\Xslt_Test.java");
			System.out.println("to run: java -cp \"<RaptorXML_bin_folder>;bin\" -Djava.library.path=\"<RaptorXML_bin_folder>\" Xslt_Test [<xslt_file_path>]");
		}
	}
}
