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
__copyright__ = "Copyright 2015 Altova GmbH"
__license__ = 'http://www.apache.org/licenses/LICENSE-2.0'

# This script uses Altova RaptorXML+XBRL Python API v2 to demonstrate how to add additional validation rules and report custom errors.
#
# Example invocations:
#
# Validate a single filing
#   raptorxmlxbrl valxbrl --script=custom_validation.py instance.xbrl
# Validate a single filing with additional options
#   raptorxmlxbrl valxbrl --script=custom_validation.py --script-param=myvalue:456 instance.xbrl
#
# Using Altova RaptorXML+XBRL Server with XMLSpy client:
#
# 1a.   Copy custom_validation.py to the Altova RaptorXML Server script directory /etc/scripts/ (default C:\Program Files\Altova\RaptorXMLXBRLServer2016\etc\scripts\) or
# 1b.   Edit the <server.script-root-dir> tag in /etc/server_config.xml
# 2.    Start Altova RaptorXML+XBRL server.
# 3.    Start Altova XMLSpy, open Tools|Manage Raptor Servers... and connect to the running server
# 4.    Create a new configuration and rename it to e.g. "CUSTOM"
# 5.    Select the XBRL Instance property page and then set the script property to custom_validation.py
# 6.    Select the new "CUSTOM" configuration in Tools|Raptor Servers and Configurations
# 7.    Open an instance file
# 8.    Validate instance file with XML|Validate XML on Server (Ctrl+F8)


from altova import *
import decimal

def check_custom_rules(instance, error_log, myvalue):
    # For demonstration purposes, let's say that only the numeric value myvalue is ever allowed in XBRL facts!

    # Iterate over every fact in the instance
    for fact in instance.facts:
        # Ignore tuples
        if isinstance(fact, xbrl.Tuple):
            continue

        if fact.concept.is_numeric():
            # Check the effective numeric value (which takes also the precision and decimals attributes into account)
            if fact.effective_numeric_value != myvalue:
                # Raise error that the value is incorrect
                # location can be used to specify the default location for the whole error line. XMLSpy automatically jumps to the location of the first error after validation.
                error_log.report(xbrl.Error.create('Value {fact:value} of fact {fact} must be equal to {myvalue}.', location='fact:value', fact=fact, myvalue=xml.Error.Param(str(myvalue),tooltip='Use the myvalue option to specify a different value!',quotes=False)))
        else:
            # Raise error that the type is incorrect
            # location can be used to specify the default location for the whole error line. XMLSpy automatically jumps to the location of the first error after validation.
            error_log.report(xbrl.Error.create('Fact {fact} has non-numeric type {type}.', location='fact', fact=fact, type=fact.concept.type_definition))

# Main entry point, will be called by RaptorXML after the XBRL instance validation job has finished
def on_xbrl_finished(job, instance):
    # instance object will be None if XBRL 2.1 validation was not successful
    if instance:
        check_custom_rules(instance, job.error_log, decimal.Decimal(job.script_params.get('myvalue','123')))
