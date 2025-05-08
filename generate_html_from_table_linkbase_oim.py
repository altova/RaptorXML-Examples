# Copyright 2025 Altova GmbH
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
__copyright__ = "Copyright 2025 Altova GmbH"
__license__ = 'http://www.apache.org/licenses/LICENSE-2.0'

# This script uses RaptorXML Python API v2 to generate HTML tables according to the layout specified in the XBRL Table linkbase.


import os, datetime, json, argparse, pathlib
import altova_api.v2.xml as xml
import altova_api.v2.xbrl as xbrl
import altova_api.v2.xbrl.oim as oim


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

def write_html(cmdlArgs, filename, body):
    html = []
    html.append(html_head())
    html.append('<body>\n')
    html.extend(body)
    html.append('</body>\n')
    html.append('</html>\n')

    # Write generated HTML to output file
    path = os.path.join(cmdlArgs.OUTPUT_DIR, filename)
    with open(path,'w',encoding='utf-8') as f:
        f.writelines(html)

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
    
def generate_table(cmdlArgs, instance, deftable, params):
    single_output_file = cmdlArgs.single_output
    lang = cmdlArgs.lang
    label_role = cmdlArgs.label_role
    additional_label_role = cmdlArgs.additional_label_role
    max_rows = 10000 if cmdlArgs.max_rows is None else cmdlArgs.max_rows

    print(label_role)

    body = []
    
    # Create layout model for the given definition table
    print('Calculating table layout for table "%s"...' % deftable.id)
    (tableset, errorlog) = deftable.generate_layout_model(instance, **params)
    if errorlog.has_errors():
        # Catch any errors during table resolution and layout process
        body.extend('<p class="error">%s</p>\n' % error.text.replace('\n','</br>') for error in errorlog.errors)
        if not single_output_file:
            write_html(cmdlArgs, deftable.id+'.html', body)
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
                            write_html(cmdlArgs, '%s_%d_%d_%d.html' % (deftable.id, table_idx, z, y / max_rows), body)
                            body = []
            else:
                body.append('<p class="error">Table %s is empty (no data found)!</p>\n' % deftable.id)
                if not single_output_file:
                    write_html(cmdlArgs, deftable.id+'.html', body)
                    body = []
            table_idx += 1
            
    return body

def generate_tables(cmdlArgs, instance):
    single_output_file = cmdlArgs.single_output
    params = {
        'table_elimination': cmdlArgs.elimination,
        'xbrl.table_eliminate_empty_aspectnode_rows_cols': cmdlArgs.elimination_aspect_nodes
    }

    # ensure output directory exists
    pathlib.Path(cmdlArgs.OUTPUT_DIR).mkdir(parents=True, exist_ok=True)

    # Generate HTML output file for each definition table in the table linkbase
    body = []
    for deftable in instance.dts.tables:
        table = generate_table(cmdlArgs, instance, deftable, params)
        if single_output_file:
            body.extend(table)

    if single_output_file:
        write_html(cmdlArgs, 'tables.html', body)


def load_instance(cmdlArgs):
    # try to load report package
    reportPackage, log = xbrl.ReportPackage.create_from_url(cmdlArgs.FILE)
    docURL = cmdlArgs.FILE
    if reportPackage and not log.has_errors():
        reportInfos = list(reportPackage.report_infos)
        if len(reportInfos) != 1:
            raise Exception("Only report package containing exactly one report are supported!")
        ri = reportInfos[0]
        if ri.report_type == xbrl.ReportType.IXBRL:
            raise Exception("Inline XBRL reports are not supported!")
        if len(ri.document_urls) != 1:
            raise Exception("Only reports with one input file are supported!")
        docURL = ri.document_urls[0]

    docType = oim.OIM.detect_document_type(docURL)
    match docType:
        case "https://xbrl.org/2021/xbrl-xml":
            instance, log = xbrl.Instance.create_from_url(docURL)
            if log.has_errors():
                raise Exception(str(log))
            return instance

        case "https://xbrl.org/2021/xbrl-csv":
            oimInstance, log = oim.OIM.create_from_csv(docURL)
            if log.has_errors():
                raise Exception(str(log))
            xmlDoc = oimInstance.to_xml()
            if not xmlDoc:
                raise Exception("Conversion to xBRL-XML failed!")
            instance, log = xbrl.Instance.create_from_document(xmlDoc)
            if log.has_errors():
                raise Exception(str(log))
            return instance
        
        case "https://xbrl.org/2021/xbrl-json":
            oimInstance, log = oim.OIM.create_from_json(docURL)
            if log.has_errors():
                raise Exception(str(log))
            xmlDoc = oimInstance.to_xml()
            if not xmlDoc:
                raise Exception("Conversion to xBRL-XML failed!")
            instance, log = xbrl.Instance.create_from_document(xmlDoc)
            if log.has_errors():
                raise Exception(str(log))
            return instance

        case _:
            raise Exception("Unknown document type: %s" % (docType))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description='This script uses Altova RaptorXML+XBRL Python API v2 to generate HTML tables according to the layout specified in the XBRL Table linkbase.')
    parser.add_argument('FILE', help="the xBRL-XML, xBRL-CSV or xBRL-JSON input file")
    parser.add_argument('OUTPUT_DIR', help="the path to the output directory")
    parser.add_argument('--single-output', default=False, action='store_true', help="generate one large HTML file containing all the tables, instead of a separate HTML file per XBRL table resource")
    parser.add_argument('--max-rows', type=int, help="specifies the maximum number of rows for a single HTML table. Tables that exceed the maximum number of rows are generated as multiple HTML tables with the same header")
    parser.add_argument('--lang', help='specifies the label language')
    parser.add_argument('--label-role', default="http://www.xbrl.org/2008/role/label", help='specifies the label role')
    parser.add_argument('--additional-label-role', help="specifies a role for additional labels")
    parser.add_argument('--elimination', default=False, action='store_true', help="perform empty table row/column elimination (avoids generation of empty HTML table rows/columns)")
    parser.add_argument('--elimination-aspect-nodes', default=False, action='store_true', help="perform empty table row/column elimination (avoids generation of empty HTML table rows/columns) for rows/columns that only contain aspect nodes")
    cmdlArgs = parser.parse_args()
    instance = load_instance(cmdlArgs)
    if instance:
        generate_tables(cmdlArgs, instance)




    
    
