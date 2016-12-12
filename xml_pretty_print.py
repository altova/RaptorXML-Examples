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

# This script uses RaptorXML Python API v2 to demonstrate how to create a pretty printed text representation of the XML DOM.
#
# Example invocation:
# raptorxml valxml-withxsd --streaming=false --script=xml_pretty_print.py
# ExpReport.xml

from altova import xml
import builtins
import os.path


def _pretty_print(node, output, depth=0):

    if isinstance(node, xml.ElementInformationItem):
        start_tag = []
        start_tag.append('%s<' % (' ' * depth * 3))
        if node.prefix:
            start_tag.append('%s:' % node.prefix)
        start_tag.append(node.local_name)

        for attr in node.namespace_attributes:
            start_tag.append(' ')
            if attr.prefix:
                start_tag.append('%s:' % attr.prefix)
            start_tag.append('%s="%s"' %
                             (attr.local_name, attr.normalized_value))

        for attr in node.attributes:
            start_tag.append(' ')
            if attr.prefix:
                start_tag.append('%s:' % attr.prefix)
            start_tag.append('%s="%s"' %
                             (attr.local_name, attr.normalized_value))

        if next(node.children, None) is None:
            # Empty element tag
            start_tag.append('/>')
            output.append(''.join(start_tag))

        else:
            start_tag.append('>')
            output.append(''.join(start_tag))

            for child in node.children:
                _pretty_print(child, output, depth + 1)

            end_tag = []
            end_tag.append('%s</' % (' ' * depth * 3))
            if node.prefix:
                end_tag.append('%s:' % node.prefix)
            end_tag.append('%s>' % node.local_name)
            output.append(''.join(end_tag))

    elif isinstance(node, xml.CharDataInformationItem):
        if not node.element_content_whitespace:
            output.append('%s%s' % (
                ' ' * depth * 3, node.value.replace('&', '&amp;').replace('<', '&gt;')))

    elif isinstance(node, xml.CommentInformationItem):
        output.append('%s<!--%s-->' % (' ' * depth * 3, node.content))

    elif isinstance(node, xml.ProcessingInstructionInformationItem):
        xmlpi = []
        xmlpi.append('%s<?%s' % (' ' * depth * 3, node.target))
        if node.content:
            xmlpi.append(' %s' % node.content)
        xmlpi.append('?>')
        output.append(''.join(xmlpi))

    elif isinstance(node, xml.DocumentInformationItem):
        xml_decl = []
        xml_decl.append('%s<?xml version="%s"' %
                        (' ' * depth * 3, node.version))
        if node.character_encoding_scheme:
            xml_decl.append(' encoding="%s"' % node.character_encoding_scheme)
        if node.standalone:
            xml_decl.append(' standalone="%s"' % node.standalone)
        xml_decl.append('?>')
        output.append(''.join(xml_decl))

        for child in node.children:
            _pretty_print(child, output, depth)

    elif isinstance(node, xml.Document):
        _pretty_print(node.document, output, depth)


def pretty_print(node, depth=0):
    """Return a pretty printed text representation of the XML DOM."""

    output = []
    _pretty_print(node, output, depth)
    return '\n'.join(output)


def generate_output(job, instance):
    """Save the XML DOM to as a pretty printed XML text file."""

    # Create a new file in the job output directory
    filepath = os.path.join(job.output_dir, 'output.xml')
    with builtins.open(filepath, mode='w', encoding='utf-8') as f:
        f.write(pretty_print(instance.document))

    # Register new output file with RaptorXML engine
    job.append_output_filename(filepath)

# Main entry point, will be called by RaptorXML after the XML instance
# validation job has finished


def on_xsi_finished(job, instance):
    # instance object will be None if XML Schema validation was not successful
    if instance:
        generate_output(job, instance)

# Main entry point, will be called by RaptorXML after the XBRL instance
# validation job has finished


def on_xbrl_finished(job, instance):
    # instance object will be None if XBRL 2.1 validation was not successful
    if instance:
        generate_output(job, instance)
