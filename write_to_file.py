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

# This RaptorXML Python API v2 script demonstrates how to generate additional output files.
#
# Example invocation:
#	raptorxml valxml-withxsd --script=write_to_file.py ExpReport.xml

import builtins, os
from altova import *

def write_to_file(job, instance):
	# Create a new file in the job output directory
	filepath = os.path.join(job.output_dir, 'my_script_output.txt')
	with builtins.open(filepath, mode='w', encoding='utf-8') as f:
		# Use the instance object and write any generated content to f
		# For example, lets output the name of the root element
		
		# instance object will be None if XML validation was not successful
		if instance:
			f.write('The root element is <'+instance.document_element.local_name+'>')
		else:
			f.write('XML instance is not valid')
	
	# Register new output file with RaptorXML engine
	job.append_output_filename(filepath)

# This entry point will be called by RaptorXML after the XML instance validation job (valxml-withxsd) has finished
def on_xsi_finished(job, instance):
	write_to_file(job, instance)
	
# Main entry point, will be called by RaptorXML after the XBRL instance validation job has finished
def on_xbrl_finished(job, instance):
	write_to_file(job, instance)