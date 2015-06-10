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

# This script uses RaptorXML Python API v2 to print out all the DRS (Dimensional relationship set) networks found in the DTS as simple trees
#
# Example invocation:
#	raptorxmlxbrl valxbrl --script=dimensional_relationship_set_traversal.py nanonull.xbrl


from altova import *

def concept_label(concept, label_role=None):
	if not label_role:		
		label_role = xbrl.taxonomy.ROLE_LABEL
	# Find all labels matching the given criteria
	labels = list(concept.labels(label_role=label_role, lang='en'))
	if not labels:
		# If not labels are found fallback to concept QName
		return str(concept.qname)
	# Return text of first label found
	return labels[0].text

def print_domain_member(drs, linkrole, domain_member, level=1):
	print('\t'*level, concept_label(domain_member))
	
	# Iterate over domain-member relationships
	for rel in drs.domain_member_relationships(domain_member, linkrole):
		# Access the additional definitionArc attributes using rel.usable
		print_domain_member(drs, linkrole, rel.target, level+1)
	
def print_dimension(drs, linkrole, dimension, level=1):
	print('\t'*level, concept_label(dimension))
	
	# Iterate over dimension-domain relationships
	for rel in drs.dimension_domain_relationships(dimension, linkrole):
		print_domain_member(drs, linkrole, rel.target, level+1)
	
def print_hypercube(drs, linkrole, hypercube, all, level=1):
	if all:
		print('\t'*level, concept_label(hypercube))
	else:
		print('\t'*level, '!', concept_label(hypercube))
	
	# Iterate over hypercube-dimension relationships
	for rel in drs.hypercube_dimension_relationships(hypercube, linkrole):
		# Access the additional definitionArc attributes using rel.closed and rel.context_element
		print_dimension(drs, linkrole, rel.target, level+1)
	
def print_primary_item(drs, linkrole, pi, level=1):
	print('\t'*level, concept_label(pi))
	
	# Iterate over all/notAll relationships
	for rel in drs.hashypercube_relationships(pi, linkrole):
		print_hypercube(drs, linkrole, rel.target, rel.arcrole == xbrl.taxonomy.ARCROLE_ALL, level+1)
	
	# Iterate over domain-member relationships
	for rel in drs.domain_member_relationships(pi, linkrole):
		print_primary_item(drs, linkrole, rel.target, level+1)

def linkrole_definition(dts, linkrole):
	try:
		# Return the human readable roleType definition string for the given linkrole if present
		return dts.role_type(linkrole).definition.value
	except:
		# Otherwise just return the linkrole URI
		return linkrole
		
def print_dimensional_relationship_set(dts, linkrole):
	print(linkrole_definition(dts, linkrole))
	
	drs = dts.dimensional_relationship_set()
	# Iterate over all root primary item concepts
	for pi in drs.roots(linkrole):
		print_primary_item(drs, linkrole, pi)	

def print_dimensional_networks(dts):
	# Iterate over all definition extended link roles
	for linkrole in dts.definition_link_roles(None):
		print_dimensional_relationship_set(dts, linkrole)

# Main entry point, will be called by RaptorXML after the XBRL taxonomy (DTS) validation job has finished
def on_dts_finished(job, dts):
	# dts object will be None if validation was not successful
	if dts:
		print_dimensional_networks(dts)

# Main entry point, will be called by RaptorXML after the XBRL instance validation job has finished
def on_xbrl_finished(job, instance):
	# instance object will be None if validation was not successful
	if instance:
		print_dimensional_networks(instance.dts)