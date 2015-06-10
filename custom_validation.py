# Copyright 2015 Altova GmbH
# 
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
# 
#	  http://www.apache.org/licenses/LICENSE-2.0
# 
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
__copyright__ = "Copyright 2015 Altova GmbH"
__license__ = 'http://www.apache.org/licenses/LICENSE-2.0'

# This script uses RaptorXML Python API v2 to demonstrate how to add additional validation rules and report custom errors.

from altova import *

def check_custom_rules(instance, error_log):
	# For demonstration purposes, let's say that only the numeric value 123 is ever allowed!
	
	# Iterate over every fact in the instance
	for fact in instance.facts:
		# Ignore tuples
		if isinstance(fact, xbrl.Tuple):
			continue

		if fact.concept.is_numeric():
			# Check the effective numeric value (which takes also the precision and decimals attributes into account)
			if fact.effective_numeric_value != 123:
				# Raise error that the value is incorrect
				# location can be used to specify the default location for the whole error line. XMLSpy automatically jumps to the location of the first error after validation.
				error_log.report(xbrl.Error.create('Value {fact:value} of fact {fact} must be equal to 123.', location='fact:value', fact=fact))		
		else:
			# Raise error that the type is incorrect
			# location can be used to specify the default location for the whole error line. XMLSpy automatically jumps to the location of the first error after validation.
			error_log.report(xbrl.Error.create('Fact {fact} has non-numeric type {type}.', location='fact', fact=fact, type=fact.concept.type_definition))

# Main entry point, will be called by RaptorXML after the XBRL instance validation job has finished
def on_xbrl_finished(job, instance):
	# instance object will be None if XBRL 2.1 validation was not successful
	if instance:
		check_custom_rules(instance, job.error_log)