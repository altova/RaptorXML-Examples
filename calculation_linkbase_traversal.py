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

from altova import *

def concept_label(concept):
	# Find all labels matching the given criteria
	labels = list(concept.labels(label_role=xbrl.taxonomy.ROLE_LABEL, lang='en'))
	if not labels:
		# If not labels are found fallback to concept QName
		return str(concept.qname)
	# Return text of first label found
	return labels[0].text

def print_tree_level(network, concept, weight=None, level=1):
	# Print label of concept
	if weight:
		print('\t'*level, weight, '*', concept_label(concept))
	else:
		print('\t'*level, weight, '*', concept_label(concept))
	
	# Iterate over all child concepts
	for rel in network.relationships_from(concept):
		# Recurse for each child concept
		print_tree_level(network, rel.target, rel.weight, level+1)

def print_calculation_tree(dts, linkrole):
	try:
		# Print the roleType definition string for the given linkrole if present
		print(dts.role_type(linkrole).definition.value)
	except:
		# Otherwise just print the linkrole URI
		print(linkrole)

	# Get the effective network of calculation arc relationships with the given linkrole URI
	network = dts.calculation_base_set(linkrole).network_of_relationships()

	# Iterate over all root concepts
	for root in network.roots:
		print_tree_level(network, root)

# Main entry point, will be called by RaptorXML after the XBRL instance validation has finished
def on_xbrl_finished(job, instance):
	dts = instance.dts

	# Iterate over all calculation extended link roles
	for linkrole in dts.calculation_link_roles():
		print_calculation_tree(dts, linkrole)