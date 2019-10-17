# XBRLTablesToExcel C# example

The following example demonstrats how to load an XBRL instance, process the table linkbase and convert XBRL tables to Excel tables. This example can also transform EBA and EIOPA instances to Excel workbooks.
The examples is using the [C# API](http://manual.altova.com/RaptorXML/dotnetapiv2/html/) of [RaptorXML+XBRL Server](http://www.altova.com/raptorxml.html) engine to process XBRL files and [ClosedXML](https://github.com/ClosedXML/ClosedXML) to generate Excel workbooks.


##### Prerequisits
* Visual Studio 2017 Community edition or higher
* Altova [RaptorXML+XBRL Server](http://www.altova.com/raptorxml.html) 2020 64-bit version

##### Compilation
* Open the solution in Visual Studio
* If Altova [RaptorXML+XBRL Server](http://www.altova.com/raptorxml.html) 2020 was not installed in the default location C:\Program Files\Altova\RaptorXMLXBRLServer2020\
    * Delete the existing reference to raptorxmlxbrlapi and add a new reference by browsing to the raptorxmlxbrlapi.dll
* Build the solution

##### Customizations
* You can easily change the table styles by modifying the code in the DefaultTableStyle.cs file

##### Example usage
* XBRLTablesToExcel in.xbrl out.xlsx
