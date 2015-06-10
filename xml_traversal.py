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

# This script uses RaptorXML Python API v2 to demonstrate how to navigate through an XML Infoset tree.
#
# Example invocation:
#	raptorxml valxml-withxsd --script=xml_traversal.py ExpReport.xml

from altova import *

def print_character(char, depth):
	# Ignore whitespace
	if not char.element_content_whitespace:
		# Please note that multiple characters are merged into a single string
		print("\t"*depth, 'CDATA', char.value)

def print_comment(comment, depth):
	print("\t"*depth, 'COMMENT', comment.content)

def print_pi(pi, depth):
	print("\t"*depth, 'PROCESSING INSTRUCTION', pi.target, pi.content)

def print_attribute(attr, depth):
	print("\t"*depth, 'ATTRIBUTE', '{%s}:%s' % (attr.namespace_name, attr.local_name), attr.normalized_value)

def print_element(elem, depth):
	print("\t"*depth, 'ELEMENT', '{%s}:%s' % (elem.namespace_name, elem.local_name))

	# Print attributes
	for attr in elem.attributes:
		print_attribute(attr,depth+1)
	# Print element content
	for child in elem.children:
		print_item(child, depth+1)

def print_item(item, depth=1):
	if isinstance(item, xml.ElementInformationItem):
		print_element(item, depth)
	elif isinstance(item, xml.CharDataInformationItem):
		print_character(item, depth)
	elif isinstance(item, xml.CommentInformationItem):
		print_comment(item, depth)
	elif isinstance(item, xml.ProcessingInstructionInformationItem):
		print_pi(item, depth)

def print_document(doc):
	print('DOCUMENT', doc.base_uri)
	for child in doc.children:
		print_item(child)

# Main entry point, will be called by RaptorXML after the XML instance validation job has finished
def on_xsi_finished(job, instance):
	# instance object will be None if XML Schema validation was not successful
	if instance:
		print_document(instance.document)

# Main entry point, will be called by RaptorXML after the XBRL instance validation job has finished
def on_xbrl_finished(job, instance):
	# instance object will be None if XBRL 2.1 validation was not successful
	if instance:
		print_document(instance.document)