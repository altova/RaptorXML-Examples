# XBRLTablesToExcel C# example

The following example demonstrats how to load an XBRL instance, process the table linkbase and convert XBRL tables to Excel tables. This example can also transform EBA and EIOPA instances to Excel workbooks.
The example is using the [C# API](http://manual.altova.com/RaptorXML/dotnetapiv2/html/) of [RaptorXML+XBRL Server](http://www.altova.com/raptorxml.html) engine to process XBRL files and [ClosedXML](https://github.com/ClosedXML/ClosedXML) to generate Excel workbooks. A Microsoft Office installation is not required for the generation of the Excel report.


##### Prerequisits
* Visual Studio 2017 Community edition or higher
* Altova [RaptorXML+XBRL Server](http://www.altova.com/raptorxml.html) 2020 64-bit version

##### Compilation
* Open the solution in Visual Studio
* Install/Restore missing NuGet package references
    * Either make sure the Package Restore options in Tools|Options|NuGet Package Manager are enabled
    * Or right click on the XbrlTablesToExcel project and select Manage NuGet Packages, then click on Restore packages at the top of the page
    * For additional information see [Troubleshooting package restore errors](https://docs.microsoft.com/en-us/nuget/consume-packages/package-restore-troubleshooting)
* If Altova [RaptorXML+XBRL Server](http://www.altova.com/raptorxml.html) 2020 was not installed in the default location `C:\Program Files\Altova\RaptorXMLXBRLServer2020\`
    * Delete the existing reference to raptorxmlxbrlapi and add a new reference by browsing to `raptorxmlxbrlapi.dll`
* Build the solution

##### Customizations
* You can easily change the table styles by modifying the code in the DefaultTableStyle.cs file

##### Example usage
* Generate an Excel report from an EBA/EIOPA XBRL instance

    XBRLTablesToExcel in.xbrl out.xlsx

* To list the available options

    XBRLTablesToExcel --help

* To enable additional debug output

    XBRLTablesToExcel in.xbrl out.xlsx --log verbose.log
