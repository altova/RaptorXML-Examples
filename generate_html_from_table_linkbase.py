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

# This script uses RaptorXML Python API v2 to generate HTML tables according to the layout specified in the XBRL Table linkbase.
#
# This script supports the following script parameters:
#
#   Parameter               Type
#   ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
#   single-output           boolean         Specify true to generate either one large HTML file containing all the tables or false to generate a separate HTML file per XBRL table resource.
#   max-rows                integer         Specify the maximum number of rows for a single HTML table. Tables that exceed the maximum number of rows are generated as multiple HTML tables with the same header.
#   lang                    string          Specify the label language.
#   elimination             boolean         Specify true to perform empty table row/column elimination (avoids generation empty HTML table rows/columns).
#   parameters              JSON            Specify any required XBRL formula linkbase parameters (see http://manual.altova.com/RaptorXML/raptorxmlxbrlserver/rxadditional_formulaparams_formats.htm for more information).
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
<meta charset="utf-8"/>
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
                text.append(child.value)
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

def concept_label(concept, preferred_label=None, lang=None):
    labels = list(concept.labels(label_role=preferred_label,lang=lang))
    if len(labels):
        return labels[0].text
    else:
        return str(concept.qname)

def generate_label(html, header, lang=None):
    labels = list(header.structural_node.definition_node.labels(lang=lang))
    if len(labels):
        html.append('<p class="label">%s</p>\n' % labels[0].text)
        return

    tagged_cs = header.structural_node.constraint_sets
    if len(tagged_cs) == 1:
        cs = next(iter(tagged_cs.values()))
    else:
        cs = tagged_cs.get(None)
    if cs:
        for aspect in cs.values():
            if isinstance(aspect,xbrl.ConceptAspectValue):
                html.append('<p class="label">%s</p>\n' % concept_label(aspect.concept,header.structural_node.preferred_label,lang))

            elif isinstance(aspect,xbrl.EntityIdentifierAspectValue):
                html.append('<p class="label">%s [%s]</p>\n' % (aspect.identifier, aspect.scheme))

            elif isinstance(aspect,xbrl.PeriodAspectValue):
                if aspect.period_type == xbrl.PeriodType.INSTANT:
                    html.append('<p class="label">%s</p>\n' % aspect.instant.strftime('%d. %B %Y'))
                elif aspect.period_type == xbrl.PeriodType.START_END:
                    html.append('<p class="label">%s to %s</p>\n' % (aspect.start.strftime('%d. %B %Y'), aspect.end.strftime('%d. %B %Y')))
                elif aspect.period_type == xbrl.PeriodType.FOREVER:
                    html.append('<p class="label">Forever</p>\n')

            elif isinstance(aspect,xbrl.SegmentAspectValue) or isinstance(aspect,xbrl.ScenarioAspectValue):
                for elem in aspect.elements:
                    html.append('<p class="label">%s</p>\n' % serialize_element(elem))

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
                html.append('<p class="label">%s</p>\n' % text)

            elif isinstance(aspect,xbrl.ExplicitDimensionAspectValue):
                if aspect.value:
                    html.append('<p class="label">%s</p>\n' % concept_label(aspect.value,None,lang))
                else:
                    html.append('<p class="label">Absent</p>\n')
            elif isinstance(aspect,xbrl.TypedDimensionAspectValue):
                if aspect.value:
                    html.append('<p class="label">%s</p>\n' % serialize_element(aspect.value,False))
                else:
                    html.append('<p class="label">Absent</p>\n')

def generate_table_caption(html, table, z, lang=None):
    html.append('<caption>\n')
    for header in table.axis(Z).slice(z):
        generate_label(html,header,lang)
    labels = list(table.structural_table.definition_table.labels(lang=lang))
    if len(labels):
        html.append('<p class="label">%s</p>\n' % labels[0].text)
    html.append('</caption>\n')

def generate_table_head(html, table, lang=None):
    x_axis = table.axis(X)

    html.append('<thead>\n')
    # For each header row in the x-axis
    for row in range(x_axis.row_count):
        html.append('<tr>\n')
        # Left-top header cell
        if row == 0:
            html.append('<th colspan="%d" rowspan="%d">\n' % (table.axis(Y).row_count,x_axis.row_count))
        # For each slice on the x-axis
        for header in x_axis.row(row):
            if header.structural_node.is_rollup():
                html.append('<th class="rollup" colspan="%d">\n' % header.span)
            else:
                html.append('<th colspan="%d">\n' % header.span)
            generate_label(html,header,lang)
            html.append('</th>\n')
        html.append('</tr>\n')
    html.append('</thead>\n')

def generate_table_body(html, table, y_range, z, lang=None):
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
                    html.append('<th rowspan="%d" class="rollup">\n' % header.span)
                else:
                    html.append('<th rowspan="%d">\n' % header.span)
                generate_label(html,header,lang)
                html.append('</th>\n')
        # Data cells with fact values
        for x in range(table.axis(X).slice_count):
            html.append('<td>\n')
            for fact in table.cell(x,y,z).facts:
                html.append('<p class="fact">%s</p>\n' % fact.normalized_value)
            html.append('</td>\n')
        html.append('</tr>\n')
    html.append('</tbody>\n')

def generate_table(job, instance, deftable, params):
    single_output_file = job.script_params.get('single-output','true') == 'true'
    lang = job.script_params.get('lang',None)
    max_rows = int(job.script_params.get('max-rows','10000'))

    body = []

    # Create layout model for the given definition table
    print('Calculating table layout for table "%s"...' % deftable.id)
    (tableset, errorlog) = deftable.generate_layout_model(instance, **params)
    if errorlog.has_errors():
        # Catch any errors during table resolution and layout process
        body.append('<p class="error">%s</p>\n' % error.text.replace('\n','</br>') for error in errorlog.errors)
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
                        generate_table_caption(body, table, z, lang)
                        generate_table_head(body, table, lang)
                        generate_table_body(body, table, range(y,min(y+max_rows, table.axis(Y).slice_count)), z, lang)
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
        'table_elimination': json.loads(job.script_params.get('elimination','true'))
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
