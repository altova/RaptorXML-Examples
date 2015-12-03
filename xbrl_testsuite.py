# Copyright 2015 Altova GmbH
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
__copyright__ = 'Copyright 2015 Altova GmbH'
__license__ = 'http://www.apache.org/licenses/LICENSE-2.0'

# Executes the XBRL 2.1 conformance test suite.
#
# This script drives Altova RaptorXML+XBRL to execute the XBRL 2.1 test suite files in http://www.xbrl.org/2014/XBRL-CONF-2014-12-10.zip. See http://www.xbrl.org/2008/xbrl-conf-cr4-2008-07-02.htm for more information.
#
# Example usage:
#
# Show available options
#   raptorxmlxbrl script xbrl_testsuite.py /path/to/XBRL-CONF-2014-12-10/xbrl.xml -h
# Create a CSV summary file
#   raptorxmlxbrl script xbrl_testsuite.py /path/to/XBRL-CONF-2014-12-10/xbrl.xml --log xbrl_testsuite.log --csv-report xbrl_testsuite.csv
# Create an XML summary file
#   raptorxmlxbrl script xbrl_testsuite.py /path/to/XBRL-CONF-2014-12-10/xbrl.xml --log xbrl_testsuite.log --xml-report xbrl_testsuite.xml
# Run only specific testcases
#   raptorxmlxbrl script xbrl_testsuite.py /path/to/XBRL-CONF-2014-12-10/xbrl.xml --log xbrl_testsuite.log --csv-report xbrl_testsuite.xml --testcase "DQC_0004." "DQC_0005."

import altova_api.v2.xml as xml
import altova_api.v2.xsd as xsd
import altova_api.v2.xbrl as xbrl

import argparse,concurrent.futures,datetime,logging,multiprocessing,time
from urllib.parse import urljoin

class ValidationError (Exception):
    """User-defined exception representing a validation error."""
    def __init__(self,value):
        self.value = value
    def __str__(self):
        return str(self.value)

def attr_val(elem,attr_name):
    """Returns the value of attribute *attr_name* on element *elem* or None if no such attribute does not exists."""
    attr = elem.find_attribute(attr_name)
    if attr:
        val = attr.schema_normalized_value
        if val is None:
            val = attr.normalized_value
        return val
    return None

def attr_val_bool(elem,attr_name):
    """Returns the boolean value of attribute *attr_name* on element *elem* or None if no such attribute does not exists."""
    attr = elem.find_attribute(attr_name)
    return attr.normalized_value.strip() in ('1','true') if attr else None

def elem_val(elem):
    """Returns the text value of element *elem*."""
    val = elem.schema_normalized_value
    if val is None:
        text = []
        for child in elem.children:
            if isinstance(child,xml.CharDataInformationItem):
                text.append(child.value)
        val = ''.join(text)
    return val

def parse_variation(variation_elem):
    """Parses the <variation> element and returns a dict containing meta-information about the given variation."""

    variation = {
        'id': attr_val(variation_elem,'id'),
        'name': attr_val(variation_elem,'name')
    }

    for elem in variation_elem.element_children():
        if elem.local_name == 'description':
            variation['description'] = elem.serialize(omit_start_tag=True)
        elif elem.local_name == 'data':
            data = {}
            for elem2 in elem.element_children():
                if elem2.local_name in ('instance','linkbase','xsd'):
                    if attr_val_bool(elem2,'readMeFirst'):
                        data[elem2.local_name] = urljoin(elem2.base_uri,elem_val(elem2))
                else:
                    logging.warning('Testcase file %s contains unknown <data> child element <%s>',elem2.document.uri,elem2.local_name)
            variation['data'] = data
        elif elem.local_name == 'result':
            expected = attr_val(elem,'expected')
            if expected is None:
                expected = 'invalid' if elem.find_child_element(('error',elem.namespace_name)) else 'valid'
            variation['result'] = expected
        else:
            logging.warning('Testcase file %s contains unknown <variation> child element <%s>',elem.document.uri,elem.local_name)

    return variation

def load_testcase(testcase_uri):
    """Loads the testcase file and returns a dict with the testcase meta-information."""
    logging.info('Loading testcase %s',testcase_uri)

    # Load the testcase file
    instance, log = xml.Instance.create_from_url(testcase_uri)
    # Check for any fatal errors
    if not instance:
        raise ValidationError('\n'.join(error.text for error in log))
    testcase_elem = instance.document_element

    testcase = {
        'uri': instance.uri,
        'name': attr_val(testcase_elem,'name'),
        'description': attr_val(testcase_elem,'description'),
        'owner': attr_val(testcase_elem,'owner')
    }

    # Iterate over all <variation> child elements
    variations = []
    variation_ids = set()
    for elem in testcase_elem.element_children():
        if elem.local_name == 'variation':
            variation = parse_variation(elem)
            variations.append(variation)
            if variation['id'] in variation_ids:
                logging.warning('Testcase file %s contains variations with duplicate id %s',testcase_uri,variation['id'])
        else:
            logging.warning('Testcase file %s contains unknown <testcase> child element <%s>',elem.document.uri,elem.local_name)
    testcase['variations'] = variations

    return testcase

def load_testsuite(index_uri):
    """Loads the testcases specified in the given testsuite index file and returns a dict with all testcase meta-information."""
    logging.info('Loading testsuite index %s',index_uri)

    # Load the testcase index file
    instance, log = xml.Instance.create_from_url(index_uri)
    # Check for any fatal errors
    if not instance:
        raise ValidationError('\n'.join(error.text for error in log))
    testcases_elem = instance.document_element

    testsuite = {
        'uri': instance.uri,
        'name': attr_val(testcases_elem,'name'),
        'date': attr_val(testcases_elem,'date')
    }

    # Iterate over all <testcase> child elements and parse the testcase file
    testcases = []
    for elem in testcases_elem.element_children():
        if elem.local_name == 'testcase':
            # Get the value of the @uri attribute and make any relative uris absolute to the base uri
            uri = urljoin(elem.base_uri,attr_val(elem,'uri'))
            # Load the testcase file
            testcases.append(load_testcase(uri))
    testsuite['testcases'] = testcases

    return testsuite

def execute_variation(testcase,variation):
    """Peforms the actual XBRL instance or taxonomy validation and returns 'PASS' if the actual outcome is conformant with the result specified in the variation."""
    logging.info('[%s, %s (%s)] Start executing variation',testcase['name'],variation['name'],variation['id'])

    if 'instance' in variation['data']:
        logging.info('[%s, %s (%s)] Validating instance %s',testcase['name'],variation['name'],variation['id'],variation['data']['instance'])
        instance, error_log = xbrl.Instance.create_from_url(variation['data']['instance'],treat_inconsistencies_as_errors=True)
    elif 'xsd' in variation['data']:
        logging.info('[%s, %s (%s)] Validating taxonomy schema %s',testcase['name'],variation['name'],variation['id'],variation['data']['xsd'])
        dts, error_log = xbrl.taxonomy.DTS.create_from_url(variation['data']['xsd'],treat_inconsistencies_as_errors=True)
    elif 'linkbase' in variation['data']:
        logging.info('[%s, %s (%s)] Validating linkbase %s',testcase['name'],variation['name'],variation['id'],variation['data']['linkbase'])
        dts, error_log = xbrl.taxonomy.DTS.create_from_url(variation['data']['linkbase'],treat_inconsistencies_as_errors=True)
    else:
        raise RuntimeError('Unknown entry point in testcase %s variation %s (%s)' % (testcase['name'],variation['name'],variation['id']))
    if error_log.has_errors() and logging.getLogger().isEnabledFor(logging.DEBUG):
        logging.debug('[%s, %s (%s)] Error log:\n%s',testcase['name'],variation['name'],variation['id'],'\n'.join(error.text for error in error_log))

    actual = 'invalid' if error_log.has_errors() else 'valid'
    expected = variation['result']
    passed = actual == expected

    logging.info('[%s, %s (%s)] Finished executing variation: %s (%s == %s)',testcase['name'],variation['name'],variation['id'],'PASS' if passed else 'FAIL',actual,expected)
    return 'PASS' if passed else 'FAIL', actual

def execute_testsuite(testsuite,args):
    """Runs all testcase variations in parallel and returns a dict with the results of each testcase variation."""
    logging.info('Start executing %s variations in %d testcases',sum(len(testcase['variations']) for testcase in testsuite['testcases']),len(testsuite['testcases']))
    start = time.time()

    results = {}
    with concurrent.futures.ThreadPoolExecutor(max_workers=args.max_workers) as executor:

        # Schedule processing of all variations as futures
        futures = {}
        for testcase in testsuite['testcases']:
            if args.testcase_numbers and testcase['number'] not in args.testcase_numbers:
                continue
            for variation in testcase['variations']:
                if args.variation_ids and variation['id'] not in args.variation_ids:
                    continue
                futures[executor.submit(execute_variation,testcase,variation)] = (testcase['uri'],variation['id'])

        # Wait for all futures to finish
        for future in concurrent.futures.as_completed(futures):
            variation_key = futures[future]
            try:
                results[variation_key] = future.result()
            except:
                results[variation_key] = 'EXCEPTION','invalid'
                logging.exception('Exception raised during testcase execution:')

    runtime = time.time() - start
    logging.info('Finished executing testcase variations in %fs',runtime)
    return results,runtime

def calc_conformance(results):
    """Returns a tuple with the number of total and failed testcase variations and the conformance as percentage."""
    total = len(results)
    failed = sum(1 for status,_ in results.values() if status != 'PASS')
    conformance = (total-failed)*100/total
    return total,failed,conformance

def xml_escape(str):
    return str.replace('<','&lt;').replace('&','&amp;').replace('"','&quot;')

def write_csv_report(path,testsuite,results,runtime,relative_uris):
    """Writes testsuite run results to csv file."""
    total,failed,conformance = calc_conformance(results)
    with open(path,'w') as csvfile:
        testsuite_path, testsuite_index = testsuite['uri'].rsplit('/',1)

        csvfile.write('Date,Total,Failed,Conformance,Runtime,Testsuite,Testcase,Variation,ReadMeFirst,Status,Actual,Expected\n')
        csvfile.write('"{:%Y-%m-%d %H:%M:%S}",{},{},{:.2f},{:.1f},{}\n'.format(datetime.datetime.now(),total,failed,conformance,runtime,testsuite['uri']))
        for testcase in testsuite['testcases']:
            csvfile.write(',,,,,,%s\n'%testcase['name'])
            for variation in testcase['variations']:
                variation_key = (testcase['uri'],variation['id'])
                if variation_key in results:
                    data_type, data_uri = list(variation['data'].items())[0]
                    if relative_uris:
                        data_uri = data_uri[len(testsuite_path)+1:]
                    status, actual = results[variation_key]
                    expected = variation['result']
                    csvfile.write(',,,,,,,{} ({}),{},{},{},{}\n'.format(variation['name'],variation['id'],data_uri,status,actual,expected))

def write_xml_report(path,testsuite,results,runtime,relative_uris):
    """Writes testsuite run results to xml file."""
    total,failed,conformance = calc_conformance(results)
    with open(path,'w') as xmlfile:
        testsuite_path, testsuite_index = testsuite['uri'].rsplit('/',1)
        testsuite_uri = testsuite['uri'] if not relative_uris else testsuite_index

        xmlfile.write('<?xml version="1.0" encoding="UTF-8"?>\n')
        xmlfile.write('<testsuite\n\txmlns="http://www.altova.com/testsuite/results"\n')
        if relative_uris:
            xmlfile.write('\txml:base="{}/"\n'.format(testsuite_path))
        xmlfile.write('\turi="{}"\n\tname="{}"\n\ttotal="{}"\n\tfailed="{}"\n\tconformance="{}"\n\truntime="{}"\n\texecution-date="{:%Y-%m-%dT%H:%M:%S}"\n\tprocessor="Altova RaptorXML+XBRL Server">\n'.format(testsuite_uri,testsuite['name'],total,failed,conformance,runtime,datetime.datetime.now()))
        for testcase in testsuite['testcases']:
            testcase_uri = testcase['uri'] if not relative_uris else testcase['uri'][len(testsuite_path)+1:]
            xmlfile.write('\t<testcase\n\t\turi="{}"\n\t\tname="{}">\n'.format(testcase_uri,testcase['name']))
            for variation in testcase['variations']:
                variation_key = (testcase['uri'],variation['id'])
                if variation_key in results:
                    data_type, data_uri = list(variation['data'].items())[0]
                    if relative_uris:
                        data_uri = data_uri[len(testsuite_path)+1:]
                    xmlfile.write('\t\t<variation\n\t\t\tid="{}"\n\t\t\tname="{}"\n\t\t\t{}="{}">\n'.format(variation['id'],xml_escape(variation['name']),data_type,data_uri))
                    status, actual = results[variation_key]
                    expected = variation['result']
                    xmlfile.write('\t\t\t<result\n\t\t\t\tstatus="{}"\n\t\t\t\tactual="{}"\n\t\t\t\texpected="{}"/>\n'.format(status,actual,expected))
                    xmlfile.write('\t\t</variation>\n')
            xmlfile.write('\t</testcase>\n')
        xmlfile.write('</testsuite>\n')

def print_results(testsuite,results,runtime):
    """Writes testsuite run summary to console."""
    total,failed,conformance = calc_conformance(results)
    for testcase in testsuite['testcases']:
        for variation in testcase['variations']:
            variation_key = (testcase['uri'],variation['id'])
            if variation_key in results:
                status, actual = results[variation_key]
                expected = variation['result']
                if status != 'PASS':
                    print('ERROR: Testcase %s, variation %s (%s) FAILED; actual [%s]; expected [%s]' % (testcase['name'], variation['name'], variation['id'], actual, expected))
    print('Conformance: %.2f%% (%d failed testcase variations out of %d)' % (conformance,failed,total))

def run_xbrl_testsuite(uri,args):
    """Load and execute the conformance testsuite."""
    try:
        testsuite = load_testsuite(uri)
        results, runtime = execute_testsuite(testsuite,args)
        logging.info('Start generating testsuite report')
        if args.csv_file:
            write_csv_report(args.csv_file,testsuite,results,runtime,args.relative_uris)
        if args.xml_file:
            write_xml_report(args.xml_file,testsuite,results,runtime,args.relative_uris)
        if not args.csv_file and not args.xml_file:
            print_results(testsuite,results,runtime)
        logging.info('Finished generating testsuite report')
    except:
        logging.exception('Testsuite run aborted with exception:')

def setup_logging(args):
    """Initializes Python logging module."""
    if args.log_file:
        logging.basicConfig(format='%(asctime)s %(levelname)s %(message)s',filename=args.log_file,filemode='w',level=logging.DEBUG if args.log_level == 'DEBUG' else logging.INFO)
    else:
        logging.getLogger().addHandler(logging.NullHandler())
    console = logging.StreamHandler()
    console.setLevel(logging.WARNING)
    console.setFormatter(logging.Formatter('%(levelname)s %(message)s'))
    logging.getLogger().addHandler(console)

def parse_args():
    """Parse command line arguments"""
    parser = argparse.ArgumentParser(description='Execute the XBRL 2.1 conformance testsuite using Altova RaptorXML+XBRL')
    parser.add_argument('uri', metavar='INDEX', help='main testsuite index file')
    parser.add_argument('-l','--log', metavar='LOG_FILE', dest='log_file', help='log output file')
    parser.add_argument('--log-level', metavar='LOG_LEVEL', dest='log_level', choices=['INFO','DEBUG'], default='INFO', help='log level (INFO|DEBUG)')
    parser.add_argument('--csv-report', metavar='CSV_FILE', dest='csv_file', help='write testsuite results to csv')
    parser.add_argument('--xml-report', metavar='XML_FILE', dest='xml_file', help='write testsuite results to xml')
    parser.add_argument('--relative-uris', dest='relative_uris', action='store_true', help='write testcase uris relative to testsuite index file')
    parser.add_argument('-t','--testcase', metavar='TESTCASE_NUMBER', dest='testcase_numbers', nargs='*', help='limit execution to only this testcase number')
    parser.add_argument('-v','--variation', metavar='VARIATION_ID', dest='variation_ids', nargs='*', help='limit execution to only this variation id')
    parser.add_argument('-w','--workers', metavar='MAX_WORKERS', type=int, dest='max_workers', default=multiprocessing.cpu_count(), help='limit number of workers')
    return parser.parse_args()

def main():
    # Parse command line arguments
    args = parse_args()

    # Setup logging
    setup_logging(args)

    # Run the testsuite
    run_xbrl_testsuite(args.uri,args)

if __name__ == '__main__':
    start = time.time()
    main()
    end = time.time()
    logging.info('Finished testsuite run in %fs',end-start)
