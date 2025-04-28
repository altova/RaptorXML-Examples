# Copyright 2025 Altova GmbH
# 
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
# 
#     http://www.apache.org/licenses/LICENSE-2.0
# 
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
__copyright__ = "Copyright 2025 Altova GmbH"
__license__ = 'http://www.apache.org/licenses/LICENSE-2.0'

# This script uses RaptorXML Python API v2 to generate an xBRL-CSV report using the oim.ReportBuilder class.
# Invoce with RatproXMLXBRL script oim_reportbuilder_example.py


import os, datetime, pathlib
import altova_api.v2.xml as xml
import altova_api.v2.xbrl as xbrl
import altova_api.v2.xbrl.oim as oim

# Path to the output directory - the xBRL-CSV report will be created in this directory
output_folder = "C:\\temp\\OIMReportBuilderTest"
output_folder_path = pathlib.Path(output_folder)

# Name of the report pacakge and its top level directory
report_package_name = "test"

# Taxonomy entrypoint and respective metadata json to extend
# make sure to install the specified taxonomy first using the Altova Taxonomy Manager
entry_point_dts_url = "http://www.eba.europa.eu/eu/fr/xbrl/crr/fws/corep/4.0/mod/corep_lr.xsd"
extends_json_url = "http://www.eba.europa.eu/eu/fr/xbrl/crr/fws/corep/4.0/mod/corep_lr.json"

# Reference date of the report
reference_date = datetime.date.today()
reference_date_av = xbrl.PeriodAspectValue.from_instant(reference_date)

# Entity Identifier and Scheme
entity_identifier = xml.QName("DUMMYLEI123.IND", "https://eurofiling.info/eu/rs", "rs")
entity_identifier_av = xbrl.EntityIdentifierAspectValue(entity_identifier.local_name, entity_identifier.namespace_name)

# Report base currency and language
base_currency = "EUR"
base_currency_av = xbrl.UnitAspectValue.from_iso4217_currency(base_currency)
base_language = "en"

# Accuracy settings
monetary_decimals = -3
percentage_decimals = 4

report_builder = None
entry_point_dts = None

report_json_content = """{{
    "documentInfo": {{
        "documentType": "https://xbrl.org/2021/xbrl-csv",
        "extends": ["{base}"]
    }}
}}
"""
# Creates report.json file in the output folder extending the metadata json of the entrypoint taxonomy
def write_report_json(report_json_path):
    with open(report_json_path, "w", encoding="utf-8") as f:
        f.write(report_json_content.format(base = extends_json_url))

# Returns a ConstraintSet with aspect values set for period and entity identifier
def get_report_constraints():
    cs = xbrl.ConstraintSet()
    cs.add(reference_date_av)
    cs.add(entity_identifier_av)
    return cs

# Add a filing indicator fact to the report
def add_filing_indicator(filing_indicator_code, value):
    xbrlFilingIndicatorsNamespace = "http://www.xbrl.org/taxonomy/int/filing-indicators/REC/2021-02-03"
    xbrlFilingIndicatorFiledConcept = entry_point_dts.resolve_concept(("filed", xbrlFilingIndicatorsNamespace))
    xbrlFilingIndicatorTemplateDimension = entry_point_dts.resolve_concept(("template", xbrlFilingIndicatorsNamespace))
    cs = get_report_constraints()
    cs.add(xbrl.ConceptAspectValue(xbrlFilingIndicatorFiledConcept))
    cs.add(xbrl.TypedDimensionAspectValue.from_string(xbrlFilingIndicatorTemplateDimension, filing_indicator_code))
    report_builder.add_non_numeric_item_fact(cs, "true" if value else "false")

# Add a qualified name fact to the report for given table cell
def add_qname_fact(cell, value):
    cs = get_report_constraints()
    cs.update(cell.constraint_set)
    report_builder.add_qname_item_fact(cs, value)

# Add a numeric fact to the report for given table cell
def add_numeric_fact(cell, unit, value, decimals):
    cs = get_report_constraints()
    cs.update(cell.constraint_set)
    cs.add(unit)
    report_builder.add_numeric_item_fact(cs, str(value), str(decimals))
                                         
def run_oim_report_builder_example():
    global entry_point_dts, report_builder
    # load EBA 4.0 COREP LR taxonomy
    dtsOptions = {
        "preload_xbrl_schemas": True,
        "preload_formula_schemas": True,
        "preload_table_schemas": True,
        "table_linkbase_namespace": "##detect"
    }
    entry_point_dts, log = xbrl.taxonomy.DTS.create_from_url(entry_point_dts_url, **dtsOptions )
    if entry_point_dts is None or log.has_errors():
        raise Exception(str(log))
    
    output_folder_path.mkdir(parents=True, exist_ok=True)

    # write reports.json and parameters.csv
    report_json_path = output_folder_path / "report.json"
    write_report_json(report_json_path)
    report_parameters = {
        "entityID": "%s:%s" %(entity_identifier.prefix, entity_identifier.local_name),
        "refPeriod": reference_date.strftime("%Y-%m-%d"),
        "baseCurrency": "iso4217:%s" %(base_currency),
        "baseLanguage": "en",
        "decimalsMonetary": str(monetary_decimals),
        "decimalsPercentage": str(percentage_decimals),
        "decimalsInteger": "0",
        "decimalsDecimal": "0"
    }

    report_builder = oim.ReportBuilder(entry_point_dts)
    report_builder.add_schemaRef(entry_point_dts_url)

    tables_to_write = []
    tableOptions = {
        "table_elimination": False,
        "preserve_empty_aspect_nodes": False,
        "table_elimination_aspect_nodes": True
    }

    for table in entry_point_dts.find_tables("eba_tC_00.01"):
        add_filing_indicator("C_00.01", True)
        tables_to_write.append("c_00.01.csv")

        ts, log = table.generate_layout_model(None, **tableOptions)
        if ts is None or log.has_errors():
            raise Exception(str(log))
        
        # add values to table C_00.01
        layoutTable = ts[0]
        add_qname_fact(layoutTable.cell(0, 0), xml.QName("x1","http://www.eba.europa.eu/xbrl/crr/dict/dom/AS"))
        add_qname_fact(layoutTable.cell(0, 1), xml.QName("x6","http://www.eba.europa.eu/xbrl/crr/dict/dom/SC"))
        break

    for table in entry_point_dts.find_tables("eba_tC_48.01"):
        add_filing_indicator("C_48.01", True)
        tables_to_write.append("c_48.01.csv")

        ts, log = table.generate_layout_model(None, **tableOptions)
        if ts is None or log.has_errors():
            raise Exception(str(log))
        
        # add values to table C_48.01
        layoutTable = ts[0]
        add_numeric_fact(layoutTable.cell(0, 0), base_currency_av, 1000000, -3)
        add_numeric_fact(layoutTable.cell(1, 0), base_currency_av, 2000000, -3)
        break

    oimReport = report_builder.close_document()

    toCSVOptions = {
        "use_existing_csv_metadata": True,
        "oim_xbrl_namespace": "##detect",
        "csv_table": tables_to_write,
        "oim_report_param": report_parameters
    }
    oimReport.to_csv(str(report_json_path), **toCSVOptions)

    # create report package
    # The first parameter specifies the name of the top level directory. This must be the same as the file name of the report pacakge.
    # The second parameter specifies the type of the report package. For xBRL-CSV reports Unconstrained and NonInlineXBRL are valid values.
    reportPackageBuilder = xbrl.ReportPackageBuilder(report_package_name, xbrl.ReportPackageType.NonInlineXBRL)
    reportPackageBuilder.add_file_from_url("reports/report.json", str(report_json_path))
    reportPackageBuilder.add_file_from_url("reports/parameters.csv", str(output_folder_path / "parameters.csv"))
    reportPackageBuilder.add_file_from_url("reports/FilingIndicators.csv", str(output_folder_path / "FilingIndicators.csv"))
    for table in tables_to_write:
        reportPackageBuilder.add_file_from_url("reports/%s" % (table), str(output_folder_path / table))

    reportPackage, log = reportPackageBuilder.finalize(str(output_folder_path / ("%s.xbr" % (report_package_name))))
    if log.has_errors():
        raise Exception(str(log))


if __name__ == '__main__':
    run_oim_report_builder_example()
