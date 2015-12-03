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

# This script uses RaptorXML Python API v2 to print out all the presentation networks found in the DTS as simple trees
#
# Example invocation:
#   raptorxmlxbrl valxbrl --script=presentation_linkbase_traversal.py nanonull.xbrl

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

def print_tree_level(network, concept, preferred_label_role=None, level=1):
    # Print label of concept
    print('\t'*level, concept_label(concept, preferred_label_role))

    # Iterate over all child concepts
    for rel in network.relationships_from(concept):
        # Recurse for each child concept considering the preferredLabel attribute on the presentationArcs
        print_tree_level(network, rel.target, rel.preferred_label, level+1)

def linkrole_definition(dts, linkrole):
    try:
        # Return the human readable roleType definition string for the given linkrole if present
        return dts.role_type(linkrole).definition.value
    except:
        # Otherwise just return the linkrole URI
        return linkrole

def print_presentation_tree(dts, linkrole):
    print(linkrole_definition(dts, linkrole))

    # Get the effective network of presentation relationships for the given linkrole URI
    network = dts.presentation_base_set(linkrole).network_of_relationships()

    # Iterate over all root concepts
    for root in network.roots:
        print_tree_level(network, root)

def print_presentation_linkbase(dts):
    # Iterate over all presentation extended link roles
    for linkrole in dts.presentation_link_roles():
        print_presentation_tree(dts, linkrole)

# Main entry point, will be called by RaptorXML after the XBRL taxonomy (DTS) validation job has finished
def on_dts_finished(job, dts):
    # dts object will be None if validation was not successful
    if dts:
        print_presentation_linkbase(dts)

# Main entry point, will be called by RaptorXML after the XBRL instance validation job has finished
def on_xbrl_finished(job, instance):
    # instance object will be None if validation was not successful
    if instance:
        print_presentation_linkbase(instance.dts)
