# Copyright 2015, 2016 Altova GmbH
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
__copyright__ = "Copyright 2015, 2016 Altova GmbH"
__license__ = 'http://www.apache.org/licenses/LICENSE-2.0'

# This script uses RaptorXML Python API v2 to print out some statistics about the instance and its supporting DTS.
#
# Example invocation:
#   raptorxmlxbrl valxbrl --script=instance_statistics.py nanonull.xbrl

from altova import *


def print_dts_statistics(dts):

    # Please note that many taxonomy.DTS properties return generator objects
    # which cannot be used with len() directly

    print('DTS contains %d documents' % sum(1 for _ in dts.documents))
    print('DTS contains %d taxonomy documents' %
          sum(1 for _ in dts.taxonomy_schemas))
    print('DTS contains %d linkbase documents' % sum(1 for _ in dts.linkbases))

    print('DTS contains %d concepts' % sum(1 for _ in dts.concepts))
    # Please note that hypercube and dimension concepts are also members of
    # the xbrli:item substitution group
    print('DTS contains %d item concepts' % (sum(1 for _ in dts.items) -
                                             sum(1 for _ in dts.hypercubes) - sum(1 for _ in dts.dimensions)))
    print('DTS contains %d tuple concepts' % sum(1 for _ in dts.tuples))
    print('DTS contains %d hypercubes' % sum(1 for _ in dts.hypercubes))
    print('DTS contains %d dimensions' % sum(1 for _ in dts.dimensions))

    print('DTS contains %d formula parameters' %
          sum(1 for _ in dts.parameters))
    print('DTS contains %d formulas' % sum(1 for _ in dts.formulas))
    print('DTS contains %d assertions' % sum(1 for _ in dts.assertions))
    print('DTS contains %d tables' % sum(1 for _ in dts.tables))


def print_instance_statistics(instance):

    # Please note that many xbrl.Instance properties return generator objects
    # which cannot be used with len() directly

    print('Instance contains %d contexts' % sum(1 for _ in instance.contexts))
    print('Instance contains %d units' % sum(1 for _ in instance.units))

    # Print statistics about reported facts
    print('Instance contains %d facts' % len(instance.facts))
    print('Instance contains %d nil facts' % len(instance.nil_facts))
    print('Instance contains %d top-level item facts' %
          len(instance.child_items))
    print('Instance contains %d top-level tuple facts' %
          len(instance.child_tuples))

    facts_with_footnotes = 0
    for role in instance.footnote_link_roles():
        network = instance.footnote_base_set(role).network_of_relationships()
        facts_with_footnotes += sum(1 for _ in network.roots)
    print('Instance contains %d facts with attached footnotes' %
          facts_with_footnotes)

    # Print statistics about embedded footnotes
    footnotes = {}
    for footnote_link in instance.footnote_links:
        for footnote in footnote_link.resources:
            if footnote.xml_lang in footnotes:
                footnotes[footnote.xml_lang] += 1
            else:
                footnotes[footnote.xml_lang] = 1
    print('Instance contains %d footnote resources' % sum(footnotes.values()))
    for lang in footnotes.keys():
        print('Instance contains %d footnote resources in language %s' %
              (footnotes[lang], lang))

# Main entry point, will be called by RaptorXML after the XBRL taxonomy
# (DTS) validation job has finished


def on_dts_finished(job, dts):
    # instance object will be None if validation was not successful
    if instance:
        print_dts_statistics(dts)

# Main entry point, will be called by RaptorXML after the XBRL instance
# validation job has finished


def on_xbrl_finished(job, instance):
    # instance object will be None if validation was not successful
    if instance:
        print_dts_statistics(instance.dts)
        print_instance_statistics(instance)
