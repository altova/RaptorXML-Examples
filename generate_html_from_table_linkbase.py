# Copyright 2022 Altova GmbH
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
__copyright__ = "Copyright 2022 Altova GmbH"
__license__ = 'http://www.apache.org/licenses/LICENSE-2.0'

# This script uses RaptorXML Python API v2 to generate HTML tables according to the layout specified in the XBRL Table linkbase.
#
# This script supports the following script parameters:
#
#   Parameter                 Type
#   ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
#   single-output             boolean         Specify true to generate either one large HTML file containing all the tables or false to generate a separate HTML file per XBRL table resource.
#   max-rows                  integer         Specify the maximum number of rows for a single HTML table. Tables that exceed the maximum number of rows are generated as multiple HTML tables with the same header.
#   lang                      string          Specify the label language.
#   label_role                string          Specify the label role (default: http://www.xbrl.org/2008/role/label).
#   additional_label_role     string          Specify a role for additional labels.
#   elimination               boolean         Specify true to perform empty table row/column elimination (avoids generation empty HTML table rows/columns).
#   elimination_aspect_nodes  boolean         Specify true to perform empty table row/column elimination (avoids generation empty HTML table rows/columns) for rows/columns that only contain aspect nodes.
#   parameters                JSON            Specify any required XBRL formula linkbase parameters (see http://manual.altova.com/RaptorXML/raptorxmlxbrlserver/rxadditional_formulaparams_formats.htm for more information).
#
# Example invocation:
#   raptorxmlxbrl valxbrl --script=generate_html_from_table_linkbase.py --script-param="elimination:true" nanonull.xbrl


import os, datetime, json
from altova import xml, xsd, xbrl

X = xbrl.table.AxisType.X
Y = xbrl.table.AxisType.Y
Z = xbrl.table.AxisType.Z

unit_symbols = {
    xml.QName('USD','http://www.xbrl.org/2003/iso4217'): '$',
    xml.QName('EUR','http://www.xbrl.org/2003/iso4217'): '&euro;',
    xml.QName('GBP','http://www.xbrl.org/2003/iso4217'): '&pound;',
    xml.QName('JPY','http://www.xbrl.org/2003/iso4217'): '&yen;'
}

def html_head():
    return """<?xml version="1.0" encoding="utf-8"?>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
<style type="text/css">
.error { color: red }
table { border-collapse:collapse; border: 0.22em solid black; background-color: white; color: black;} 
td, th { border-left: 0.1em solid black; border-left: 0.1em solid black; border-top: 0.1em solid black; padding: 0.5em; text-align: center; } 
thead tr th.rollup { border-top-style: none; } 
tbody tr th.rollup { border-left-style: none; } 
tbody tr:nth-of-type(even) { background-color: #EAEFFF; } 
thead, tbody tr th { background-color: #C6D8FF; } 
thead { border-bottom: 0.19em solid black; } 
thead tr:first-of-type th:first-of-type, tbody tr th:last-of-type { border-right: 0.18em solid black; } 
</style>
</head>
"""

xml_escape_table = str.maketrans({
    "<": "&lt;",
    ">": "&gt;",
    "&": "&amp;",
    '"': "&quot;",
    "'": "&apos;"
})

def xml_escape(text):
    return str.translate(text, xml_escape_table)

def write_html(job, filename, body):
    html = []
    html.append(html_head())
    html.append('<body>\n')
    html.extend(body)
    html.append('</body>\n')
    html.append('</html>\n')

    # Write generated HTML to output file
    path = os.path.join(job.output_dir,filename)
    with open(path,'w',encoding='utf-8') as f:
        f.writelines(html)
    # Register new output file with RaptorXML engine
    job.append_output_filename(path)

def element_text(elem):
    text = []
    for child in elem.children:
        if isinstance(child,xml.CharDataInformationItem):
            if not child.element_content_whitespace:
                text.append(xml_escape(child.value))
    return ''.join(text)

def serialize_element(elem, include_self=True):
    text = []
    if include_self:
        text.append('&lt;%s&gt;' % elem.local_name)
    for child in elem.children:
        if isinstance(child,xml.ElementInformationItem):
            text.append(serialize_element(child))
        elif isinstance(child,xml.CharDataInformationItem):
            if not child.element_content_whitespace:
                text.append(child.value)
    if include_self:
        text.append('&lt;/%s&gt;' % elem.local_name)
    return ''.join(text)

def get_labels(resource, label_role, additional_label_role, lang):
    labels = list(resource.labels(label_role=label_role, lang=lang))
    labels.sort(key=lambda x: x.effective_role)
    additional_labels = []
    if len(labels) == 0:
        if label_role:
            labels = list(resource.labels(lang=lang))
            labels.sort(key=lambda x: x.effective_role)
    elif additional_label_role:
        additional_labels = list(resource.labels(label_role=additional_label_role, lang=lang))
        additional_labels.sort(key=lambda x: x.effective_role)
    return (labels, additional_labels)

def format_label(resource, label_role, additional_label_role, lang):
    labels, additional_labels = get_labels(resource, label_role, additional_label_role, lang)
    if len(labels) > 0:
        str_additional_label = ' [%s]' % (additional_labels[0]) if len(additional_labels) > 0 else ''
        return labels[0].text + str_additional_label
    return None
    
def concept_label(concept, preferred_label=None, lang=None):
    label = format_label(concept, preferred_label, None, lang)
    return label if label else str(concept.qname)

def generate_label(html, header, html_element='span', label_role=None, additional_label_role=None, lang=None):
    label = format_label(header.structural_node.definition_node, label_role, additional_label_role, lang)
    if label:
        html.append('<{element} class="label">{value}</{element}>\n'.format(element=html_element, value=xml_escape(label)))
        return
        
    tagged_cs = header.structural_node.constraint_sets
    if len(tagged_cs) == 1:
        cs = next(iter(tagged_cs.values()))
    else:
        cs = tagged_cs.get(None)
    if cs:
        for aspect in cs.values():
            if isinstance(aspect,xbrl.ConceptAspectValue):
                html.append('<{element} class="label">{value}</{element}>\n'.format(element=html_element, value=xml_escape(concept_label(aspect.concept, header.structural_node.preferred_label if header.structural_node.preferred_label else label_role, lang))))

            elif isinstance(aspect,xbrl.EntityIdentifierAspectValue):
                html.append('<{element} class="label">{identifier} [{scheme}]</element>\n'.format(element=html_element, identifier=aspect.identifier, scheme=aspect.scheme))

            elif isinstance(aspect,xbrl.PeriodAspectValue):
                if aspect.period_type == xbrl.PeriodType.INSTANT:
                    html.append('<{element} class="label">{value}</{element}>\n'.format(element=html_element, value=aspect.instant.strftime('%d. %B %Y')))
                elif aspect.period_type == xbrl.PeriodType.START_END:
                    html.append('<{element} class="label">{from_} to {to}</{element}>\n'.format(element=html_element, from_=aspect.start.strftime('%d. %B %Y'), to=aspect.end.strftime('%d. %B %Y')))
                elif aspect.period_type == xbrl.PeriodType.FOREVER:
                    html.append('<{element} class="label">Forever</{element}>\n'.format(element=html_element))

            elif isinstance(aspect,xbrl.SegmentAspectValue) or isinstance(aspect,xbrl.ScenarioAspectValue):
                for elem in aspect.elements:
                    html.append('<{element} class="label">{value}</{element}>\n'.format(element=html_element, value=xml_escape(serialize_element(elem))))

            elif isinstance(aspect,xbrl.UnitAspectValue):
                text = ''
                numerator = list(aspect.numerator)
                denominator = list(aspect.denominator)
                for qname in numerator:
                    if qname in unit_symbols:
                        text += unit_symbols[qname]
                    else:
                        text += '{%s}:%s ' % (qname.namespace_name, qname.local_name)
                if len(denominator):
                    text += ' / '
                    for qname in denominator:
                        if qname in unit_symbols:
                            text += unit_symbols[qname]
                        else:
                            text += '{%s}:%s ' % (qname.namespace_name, qname.local_name)
                html.append('<{element} class="label">{value}</{element}>\n'.format(element=html_element, value=xml_escape(text)))

            elif isinstance(aspect,xbrl.ExplicitDimensionAspectValue):
                if aspect.value:
                    html.append('<{element} class="label">{value}</{element}>\n'.format(element=html_element, value=xml_escape(concept_label(aspect.value, label_role, lang))))
                else:
                    html.append('<{element} class="label">Absent</{element}>\n'.format(element=html_element))
            elif isinstance(aspect,xbrl.TypedDimensionAspectValue):
                if aspect.value:
                    html.append('<{element} class="label">{value}</{element}>\n'.format(element=html_element, value=xml_escape(serialize_element(aspect.value,False))))
                else:
                    html.append('<{element} class="label">Absent</{element}>\n'.format(element=html_element))

def generate_cell_data(html, facts, label_role=None, lang=None):
    if len(facts):
        for fact in facts:
            if fact.xsi_nil:
                value = 'N/A'
            elif isinstance(fact.concept, xbrl.taxonomy.Tuple):
                value = fact.concept.name
            elif fact.concept.is_enum():
                value = concept_label(fact.enum_value, label_role, lang)
            elif fact.concept.is_numeric():
                value = str(fact.effective_numeric_value)
            else:
                value = fact.normalized_value
            html.append('<p class="fact">%s</p>\n' % xml_escape(value))
    else:
        html.append('&#xA0;') # No-Break Space
                    
def generate_table_caption(html, table, z, label_role=None, additional_label_role=None, lang=None):
    html.append('<caption>\n')
    previous_breakdown = None
    for header in table.axis(Z).slice(z):
        breakdown = table.axis(Z).structural_breakdown(header.row).definition_breakdown
        if previous_breakdown != breakdown:
            previous_breakdown = breakdown
            breakdown_label = format_label(breakdown, label_role, additional_label_role, lang)
            if breakdown_label:
                html.append('<p class="label">%s</p>\n' % xml_escape(breakdown_label))
        generate_label(html, header, 'p', label_role, additional_label_role, lang )
    table_label = format_label(table.structural_table.definition_table, label_role, additional_label_role, lang)
    if table_label:
        html.append('<p class="label">%s</p>\n' % xml_escape(table_label))
    html.append('</caption>\n')

def header_with_only_rollup_children(header):
    if header.span != 1:
        return False
    for child in header.children:
        if not child.structural_node.is_rollup():
            return False
    return True

def get_open_aspect_definition_breakdown(dts, definition_table, open_aspect_node):
    breakdown_tree_network = dts.network_of_relationships(
        definition_table.extended_link.qname,
        definition_table.extended_link.xlink_role,
        xml.QName("breakdownTreeArc", "http://xbrl.org/2014/table"),
        "http://xbrl.org/arcrole/2014/breakdown-tree"
        )
    table_breakdown_network = dts.network_of_relationships(
        definition_table.extended_link.qname,
        definition_table.extended_link.xlink_role,
        xml.QName("tableBreakdownArc", "http://xbrl.org/2014/table"),
        "http://xbrl.org/arcrole/2014/table-breakdown"
        )
    if breakdown_tree_network and table_breakdown_network:
        for breakdownTreeRel in breakdown_tree_network.relationships_to(open_aspect_node):
            for tableBreakdownRel in table_breakdown_network.relationships_to(breakdownTreeRel.source_resource):
                if tableBreakdownRel.source_resource == definition_table:
                    return breakdownTreeRel.source_resource
    return None


def get_open_aspect_header_label(dts, table, open_aspect_node, label_role, additional_label_role, lang):
    label = format_label(open_aspect_node, label_role, additional_label_role, lang)
    if not label:
        # fallback to breakdown label
        breakdown = get_open_aspect_definition_breakdown(dts, table.definition_table, open_aspect_node)
        if breakdown:
            label = format_label(breakdown, label_role, additional_label_role, lang)

    if not label:
        # fallback to first participating aspect label
        participating_aspects = list(open_aspect_node.participating_aspects)
        if len(participating_aspects) > 0:
            label = format_label(participating_aspects[0], label_role, additional_label_role, lang)

    return label if label else ''


def generate_table_head(html, dts, table, label_role=None, additional_label_role=None, lang=None):
    x_axis = table.axis(X)
    y_axis = table.axis(Y)

    html.append('<thead>\n')
    # For each header row in the x-axis
    for row in range(x_axis.row_count):
        html.append('<tr>\n')
        # Left-top header cell
        if row == 0 and table.axis(Y).row_count > 0:
            # special handling for open aspect nodes
            aspect_node_headers = 0
            for y_axis_row in range(y_axis.row_count):
                for header in y_axis.row(y_axis_row):
                    if isinstance(header.definition_node, xbrl.table.AspectNode):
                        aspect_node_headers += 1
                        html.append('<th rowspan="{rowspan}"><span>{label}</span></th>'.format(rowspan=x_axis.row_count, label=get_open_aspect_header_label(dts, table, header.definition_node, label_role, additional_label_role, lang)))
                    break
            if aspect_node_headers < table.axis(Y).row_count:
                html.append('<th colspan="%d" rowspan="%d"/>\n' % (table.axis(Y).row_count - aspect_node_headers, x_axis.row_count))
        # For each slice on the x-axis
        for header in x_axis.row(row):
            if header.structural_node.is_rollup():
                if not header.parent.structural_node.is_rollup() and not header_with_only_rollup_children(header.parent):
                    html.append('<th class="rollup" colspan="%d" rowspan="%d">\n' % (header.span, x_axis.row_count - header.row))
                else:
                    continue
            else:
                rowspan = x_axis.row_count - header.row if header_with_only_rollup_children(header) else 1
                html.append('<th colspan="%d" rowspan="%d">\n' % (header.span, rowspan))
                generate_label(html, header, 'span', label_role, additional_label_role, lang)
            html.append('</th>\n')
        html.append('</tr>\n')
    html.append('</thead>\n')

def generate_table_body(html, table, y_range, z, label_role=None, additional_label_role=None, lang=None):
    y_axis = table.axis(Y)

    html.append('<tbody>\n')
    # For each slice on the y-axis
    for y in y_range:
        html.append('<tr>\n')
        # For each header row in the y-axis slice
        for header in y_axis.slice(y):
            # Only generate <th> elements in rows where the header cell starts a vertical span
            if header.slice == y:
                if header.structural_node.is_rollup():
                    if not header.parent.structural_node.is_rollup() and not header_with_only_rollup_children(header.parent):
                        html.append('<th colspan="%d" rowspan="%d" class="rollup">\n' % (y_axis.row_count - header.row, header.span))
                    else:
                        continue
                else:
                    colspan = y_axis.row_count - header.row if header_with_only_rollup_children(header) else 1
                    html.append('<th colspan="%d" rowspan="%d">\n' % (colspan, header.span))
                    generate_label(html, header, 'span', label_role, additional_label_role, lang)
                html.append('</th>\n')
        # Data cells with fact values
        for x in range(table.axis(X).slice_count):
            html.append('<td>\n')
            generate_cell_data(html, table.cell(x,y,z).facts, label_role, lang)
            html.append('</td>\n')
        html.append('</tr>\n')
    html.append('</tbody>\n')
    
def generate_table(job, instance, deftable, params):
    single_output_file = job.script_params.get('single-output','true') == 'true'
    lang = job.script_params.get('lang',None)
    label_role = job.script_params.get('label_role', 'http://www.xbrl.org/2008/role/label')
    additional_label_role = job.script_params.get('additional_label_role', None)
    max_rows = int(job.script_params.get('max-rows','10000'))

    print(label_role)

    body = []
    
    # Create layout model for the given definition table
    print('Calculating table layout for table "%s"...' % deftable.id)
    (tableset, errorlog) = deftable.generate_layout_model(instance, **params)
    if errorlog.has_errors():
        # Catch any errors during table resolution and layout process
        body.extend('<p class="error">%s</p>\n' % error.text.replace('\n','</br>') for error in errorlog.errors)
        if not single_output_file:
            write_html(job,deftable.id+'.html', body)
            body = []
    else:
        table_idx = 0
        for table in tableset:
            print('Generating HTML for table "%s"...' % deftable.id)
            # Check for empty table after empty row/column elimination
            if not table.is_empty():
                for z in range(table.axis(Z).slice_count):
                    for y in range(0, table.axis(Y).slice_count, max_rows):
                        body.append('<table>\n')    
                        generate_table_caption(body, table, z, label_role, additional_label_role, lang)
                        generate_table_head(body, instance.dts, table, label_role, additional_label_role, lang)
                        generate_table_body(body, table, range(y,min(y+max_rows, table.axis(Y).slice_count)), z, label_role, additional_label_role, lang)
                        body.append('</table>\n')
                        if not single_output_file:
                            write_html(job, '%s_%d_%d_%d.html' % (deftable.id, table_idx, z, y / max_rows), body)
                            body = []
            else:
                body.append('<p class="error">Table %s is empty (no data found)!</p>\n' % deftable.id)
                if not single_output_file:
                    write_html(job,deftable.id+'.html', body)
                    body = []
            table_idx += 1
            
    return body

def generate_tables(job, instance):
    single_output_file = job.script_params.get('single-output','true') == 'true'
    params = {
        'formula_parameters': json.loads(job.script_params.get('parameters','null')),
        'table_elimination': json.loads(job.script_params.get('elimination','true')),
        'xbrl.table_eliminate_empty_aspectnode_rows_cols': json.loads(job.script_params.get('elimination_aspect_nodes','true'))
    }

    # Generate HTML output file for each definition table in the table linkbase
    body = []
    for deftable in instance.dts.tables:
        table = generate_table(job, instance, deftable, params)
        if single_output_file:
            body.extend(table)

    if single_output_file:
        write_html(job,'tables.html', body)

# Main entry point, will be called by RaptorXML after the XBRL instance validation job has finished
def on_xbrl_finished(job, instance):
    # instance object will be None if XBRL 2.1 validation was not successful
    if instance:
        generate_tables(job, instance)
