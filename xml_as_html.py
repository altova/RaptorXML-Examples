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

# This script uses RaptorXML Python API v2 to demonstrate how to generate HTML with syntax highlighting from an XML Infoset tree.
#
# Example invocation:
#   raptorxml valxml-withxsd --streaming=false --script=xml_as_html.py ExpReport.xml

import builtins, os.path
from altova import xml

def _xml_to_html(node,html,depth=0):
   
    if isinstance(node,xml.ElementInformationItem):
        html.append('<span class="indent">%s</span>' % ' '*depth*3)
        html.append('<span class="elem-op">&lt;</span>')
        if node.prefix:
            html.append('<span class="elem-prefix">%s</span><span class="elem-prefix-sep">:</span>' % node.prefix)
        html.append('<span class="elem-name">%s</span>' % node.local_name)
                   
        for attr in node.namespace_attributes:
            html.append('<span class="nsattr-sep"> </span>')
            if attr.prefix:
                html.append('<span class="nsattr-prefix">%s</span><span class="nsattr-prefix-sep">:</span>' % attr.prefix)
            html.append('<span class="nsattr-name">%s</span>' % attr.local_name)
            html.append('<span class="nsattr-op">=&quot;</span>')
            html.append('<span class="nsattr-value">%s</span>' % attr.normalized_value)
            html.append('<span class="nsattr-op">&quot;</span>')
        
        for attr in node.attributes:
            html.append('<span class="attr-sep"> </span>')
            if attr.prefix:
                html.append('<span class="attr-prefix">%s</span><span class="attr-prefix-sep">:</span>' % attr.prefix)
            html.append('<span class="attr-name">%s</span>' % attr.local_name)
            html.append('<span class="attr-op">=&quot;</span>')
            html.append('<span class="attr-value">%s</span>' % attr.normalized_value)
            html.append('<span class="attr-op">&quot;</span>')        

        if next(node.children,None) is None:
            # Empty element tag
            html.append('<span class="elem-op">/&gt;</span></br>')
            
        else:
            html.append('<span class="elem-op">&gt;</span></br>')
            
            for child in node.children:
                _xml_to_html(child,html,depth+1)
                
            html.append('<span class="indent">%s</span>' % ' '*depth*3)
            html.append('<span class="elem-op">&lt;/</span>')
            if node.prefix:
                html.append('<span class="elem-prefix">%s</span><span class="elem-prefix-sep">:</span>' % node.prefix)
            html.append('<span class="elem-name">%s</span>' % node.local_name)
            html.append('<span class="elem-op">&gt;</span></br>')
    
    elif isinstance(node,xml.CharDataInformationItem):
        if not node.element_content_whitespace:
            html.append('<span class="indent">%s</span>' % ' '*depth*3)
            html.append('<span class="cdata">%s</span></br>' % node.value.replace('&','&amp;').replace('<','&gt;'))

    elif isinstance(node,xml.CommentInformationItem):
        html.append('<span class="indent">%s</span>' % ' '*depth*3)
        html.append('<span class="comment-op">&lt;--</span><span class="comment-content">%s</span><span class="comment-op">--&gt;</span></br>' % node.content)

    elif isinstance(node,xml.ProcessingInstructionInformationItem):
        html.append('<span class="indent">%s</span>' % ' '*depth*3)
        html.append('<span class="pi-op">&lt;?</span><span class="pi-target">%s</span>' % node.target)
        if node.content:
            html.append('<span class="pi-content"> %s</span>' % node.content)
        html.append('<span class="pi-op">?&gt;</span></br>')
  
    elif isinstance(node,xml.DocumentInformationItem):
        html.append('<span class="indent">%s</span>' % ' '*depth*3)
        html.append('<span class="pi-op">&lt;?</span><span class="pi-target">xml</span><span class="pi-content"> version="%s"<span>' % node.version)
        if node.character_encoding_scheme:
            html.append('<span class="pi-content"> encoding="%s"</span>' % node.character_encoding_scheme)
        if node.standalone:
            html.append('<span class="pi-content"> standalone="%s"</span>' % node.standalone)
        html.append('<span class="pi-op">?&gt;</span></br>')
        
        for child in node.children:
            _xml_to_html(child,html,depth)

    elif isinstance(node,xml.Document):
        _xml_to_html(node.document,html,depth)
            
def xml_to_html(node,depth=0):
    """Return an HTML representation of the XML DOM."""

    html = []
    html.append("""\
<html>
<head>
<style>

pre.altova-xml-tree span.elem-op { color: blue; }
pre.altova-xml-tree span.elem-prefix { color: darkred; }
pre.altova-xml-tree span.elem-prefix-sep { color: darkred; }
pre.altova-xml-tree span.elem-name { color: darkred; }

pre.altova-xml-tree span.attr-op { color: blue; }
pre.altova-xml-tree span.attr-prefix { color: red; }
pre.altova-xml-tree span.attr-prefix-sep { color: red; }
pre.altova-xml-tree span.attr-name { color: red; }
pre.altova-xml-tree span.attr-value { color: black; }

pre.altova-xml-tree span.nsattr-op { color: blue; }
pre.altova-xml-tree span.nsattr-prefix { color: red; }
pre.altova-xml-tree span.nsattr-prefix-sep { color: red; }
pre.altova-xml-tree span.nsattr-name { color: red; }
pre.altova-xml-tree span.nsattr-value { color: black; }

pre.altova-xml-tree span.cdata { color: black; }

pre.altova-xml-tree span.comment-op { color: blue; }
pre.altova-xml-tree span.comment-content { color: gray; }

pre.altova-xml-tree span.pi-op { color: darkcyan; }
pre.altova-xml-tree span.pi-target { color: darkcyan; }
pre.altova-xml-tree span.pi-content { color: darkcyan; }

</style>
</head>
<body>
<pre class="altova-xml-tree">
""")
    _xml_to_html(node,html,depth)
    html.append("""\
</pre>
</body>
</html>
""")
    return ''.join(html)

def generate_html(job, instance):
    """Convert the XML DOM to HTML and write it to a file."""
    html = xml_to_html(instance.document)

    # Create a new file in the job output directory
    filepath = os.path.join(job.output_dir, 'output.html')
    with builtins.open(filepath, mode='w', encoding='utf-8') as f:
        f.write(html)
        
    # Register new output file with RaptorXML engine
    job.append_output_filename(filepath)        

# Main entry point, will be called by RaptorXML after the XML instance validation job has finished
def on_xsi_finished(job, instance):
    # instance object will be None if XML Schema validation was not successful
    if instance:
        generate_html(job, instance)

# Main entry point, will be called by RaptorXML after the XBRL instance validation job has finished
def on_xbrl_finished(job, instance):
    # instance object will be None if XBRL 2.1 validation was not successful
    if instance:
        generate_html(job, instance)