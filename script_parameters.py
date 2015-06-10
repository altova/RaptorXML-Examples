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

# This script demonstrates how to supply additional parameters to RaptorXML Python API v2 scripts.
# To specify one or more script parameters on the command line, use the --script-param="key:value" option.
# To specify one or more script parameters in XMLSpy, edit the "Script Parameters" field in the RaptorXML Server Options dialog.
#
# Example invocation:
#	raptorxml valxml-withxsd --script=script_parameters.py --script-param="mystring:Lorem ipsum" --script-param="myint:99" --script-param="foo:bar" ExpReport.xml

from altova import *

def print_params(params):
	# Print out all supplied script parameters
	for key, value in params.items():
		print(key, '=', value)
		
	# Access some specific parameters (using predefined defaults if parameter was not specified)
	mystring = params.get('mystring', 'hello world')
	myint = int(params.get('myint', 42))	# Manually cast to int, as script parameters are always represented as strings
	print('mystring =', mystring)
	print('myint =', myint)

# Main entry point, will be called by RaptorXML after the XML instance validation job has finished
def on_xsi_finished(job, instance):
	print_params(job.script_params)

# Main entry point, will be called by RaptorXML after the XBRL instance validation job has finished
def on_xbrl_finished(job, instance):
	print_params(job.script_params)