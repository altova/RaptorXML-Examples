using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Altova.RaptorXml;
using Xbrl = Altova.RaptorXml.Xbrl;

namespace XBRLImportTest
{
    // A very simple and minimalistic example demonstrating how to load XBRL instances and insert fact information into a single large DB table.
    class Program
    {
        //CREATE TABLE 
        //    [Facts] (
        //        [instance_url] TEXT (255) NOT NULL, 
        //        [concept_name] TEXT (255) NOT NULL, 
        //        [concept_namespace] TEXT (255) NOT NULL, 
        //        [entity_identifier] TEXT (255) NOT NULL, 
        //        [period_start] DATETIME NULL, 
        //        [period_end] DATETIME NULL, 
        //        [strval] MEMO NULL, 
        //        [numval] NUMERIC (28, 5) NULL, 
        //        [currency] TEXT (255) NULL) ; 

        static String CONNECTION_STRING = "Provider=Microsoft.ACE.OLEDB.12.0; Data source={0}";
        static String INSERT_QUERY = "INSERT INTO Facts VALUES(@url, @name, @ns, @identifier, @start, @end, @strval, @numval, @currency)";

        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: XBRLImportTest instance.xbrl");
                return;
            }
            string url = args[0]; // @"C:\Temp\msft-20170930\msft-20170930.xml";

            System.Data.OleDb.OleDbConnection conn = new System.Data.OleDb.OleDbConnection();
            conn.ConnectionString = String.Format(CONNECTION_STRING, @"xbrl.accdb");

            try
            {
                // Open DB connection
                conn.Open();


                // Load XBRL instance
                var settings = new Xbrl.InstanceSettings();
                
                // Custom catalogs or taxonomy packages
                //settings.CatalogUrls.Add(@"mycatalog.xml");
                //settings.TaxonomyPackageUrls.Add(@"mytaxonomypkg.zip");

                // In case of fixed taxonomy entrypoints, preload the DTS to increase the performance when bulk importing multiple instances
                // settings.DTS = Xbrl.Taxonomy.Dts.CreateFromUrl(@"entrypoint.xsd", out log);

                ErrorLog log;
                var inst = Xbrl.Instance.CreateFromUrl(url, settings, out log);
                if (log.HasErrors)
                    throw new Exception(log.Text);

                // Insert each fact into DB (assuming instance does not contain tuples)
                foreach (Xbrl.Item fact in inst.Facts)
                {
                    // Access fact and aspect values
                    var name = fact.QName.LocalName;
                    var ns = fact.QName.NamespaceName;

                    var identifier = fact.EntityIdentifierAspectValue.Identifier;

                    DateTime? period_start = null;
                    DateTime? period_end = null;
                    var period = fact.PeriodAspectValue;
                    switch(period.Type)
                    {
                        case Xbrl.PeriodType.Instant:
                            period_start = period.Instant;
                            break;
                        case Xbrl.PeriodType.StartEnd:
                            period_start = period.Start;
                            period_end = period.End;
                            break;
                        case Xbrl.PeriodType.Forever:
                            break;
                    }

                    var strval = fact.NormalizedValue;

                    decimal? numval = null;
                    string currency = null;
                    if (fact.Concept.IsNumeric)
                    {
                        // Use EffectiveNumericValue to get XBRL rounded value taking into account @decimals/@precision
                        numval = fact.NumericValue;
                        var unit = fact.UnitAspectValue;
                        if (unit.IsMonetary)
                            currency = unit.Iso4217Currency;
                    }

                    // Insert fact row
                    OleDbCommand cmd = new OleDbCommand(INSERT_QUERY, conn);
                    cmd.Parameters.Add("@url", OleDbType.VarChar).Value = url;
                    cmd.Parameters.Add("@name", OleDbType.VarChar).Value = name;
                    cmd.Parameters.Add("@ns", OleDbType.VarChar).Value = ns;
                    cmd.Parameters.Add("@identifier", OleDbType.VarChar).Value = identifier;
                    cmd.Parameters.Add("@start", OleDbType.DBTimeStamp).Value = (object)period_start ?? DBNull.Value;
                    cmd.Parameters.Add("@end", OleDbType.DBTimeStamp).Value = (object)period_end ?? DBNull.Value;
                    cmd.Parameters.Add("@strval", OleDbType.LongVarChar).Value = (object)strval ?? DBNull.Value;
                    cmd.Parameters.Add("@numval", OleDbType.Numeric).Value = (object)numval ?? DBNull.Value;
                    cmd.Parameters.Add("@currency", OleDbType.VarChar).Value = (object)currency ?? DBNull.Value;
                    cmd.ExecuteNonQuery();
                }


                Console.WriteLine(String.Format("Imported XBRL instance '{0}' successfully!", url));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed: " + ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }
    }
}
