# RaptorXML-Examples in C# #

Examples for using the [C# API](http://manual.altova.com/RaptorXML/dotnetapiv2/html/) of [RaptorXML+XBRL Server](http://www.altova.com/raptorxml.html).

#### Load-Validate  
These example console applications show how to load an XBRL instance, check for validation errors and perform one small additional feature for demonstration purposes.

##### XBRLTablesToExcel
* an example demonstrating how to load XBRL instances, process the table linkbase and convert XBRL tables to Excel tables. This example can also transform EBA and EIOPA instances to Excel workbooks.

##### XBRLImportTest
* a very simple and minimalistic example demonstrating how to load XBRL instances and insert fact information into a single large DB table

##### CalculationLinkbaseTraversal
* traverse the calculation linkbase arcs

##### DimensionalRelationshipSetTraversal
* traverse the dimensional relationship set

##### FindDuplicateContexts
* search for duplicate contexts

##### FindDuplicateUnits
* search for duplicate units

##### FindFactsByName
* search for specific facts (by concept name)

##### FindFactsWithSameAspects
* search for specific facts (by concept name) that share the same aspects

##### InstanceStatistics
* access the data model of the XBRL instance and the supporting DTS

##### PresentationLinkbaseTraversal
* traverse the presentation linkbase arcs

##### XPathXQueryXslt
* Introductory examples for the XPath, XQuery and XSLT engine usage via the C# API
