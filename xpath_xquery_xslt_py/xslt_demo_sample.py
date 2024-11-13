"""
An introductory sample demonstrating the usage of XSLT engine from Altova RaptorXML+XBRL Python API.
-add an external function library to extend the execution with native python code
-configure and compile the XSLT stylesheet
-perform a transformation
-serialize/print the results
"""

import platform
import sys
from typing import List

from altova_api.v2 import xml
from altova_api.v2 import xpath
from altova_api.v2 import xsd
from altova_api.v2 import xslt

XMLDOC_SRC_STRING = """<?xml version="1.0"?>
<doc myAttr="1337">This document is used as input for the transformation.</doc>"""

XSLT_SRC_STRING = """<xsl:stylesheet
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:err="http://www.w3.org/2005/xqt-errors"
    version="3.0" expand-text="yes">
    <xsl:param name="param1" select="-1" />
    <xsl:template match="/">
        <xsl:result-document method="json" indent="yes" xmlns:ns1="my-ext">
            <xsl:map>
                <xsl:map-entry key="'python version'" select="ns1:python-version()"/>
                <xsl:map-entry key="'os version'" select="ns1:os-version()"/>
                <xsl:map-entry key="'my-attr converted'"
                  select="let $attr := /doc/@myAttr return Q{my-ext}integer-to-double($attr)"/>
                <xsl:map-entry key="'try-catch recovered conversion error 1'">
                    <xsl:try>
                        <xsl:sequence select="ns1:integer-to-double(xs:untypedAtomic(/doc/@myAttr div 3))"/>
                        <xsl:catch select="map{'code': string($err:code), 'description' : $err:description}"/>
                    </xsl:try>
                </xsl:map-entry>
                <xsl:map-entry key="'try-catch recovered conversion error 2'">
                    <xsl:try>
                        <xsl:sequence select="ns1:integer-to-double(data(.))"/>
                        <xsl:catch select="map{'code': string($err:code), 'description' : $err:description}"/>
                    </xsl:try>
                </xsl:map-entry>
                <xsl:map-entry key="'try-catch recovered from an error in the extension function callback'">
                    <xsl:try>
                        <xsl:sequence select="Q{my-ext}error('Catch me if you can!')"/>
                        <xsl:catch select="map{'code': string($err:code), 'description' : $err:description}"/>
                    </xsl:try>
                </xsl:map-entry>
                <xsl:map-entry key="'value of param1'" select="$param1" />
            </xsl:map>
        </xsl:result-document>
    </xsl:template>
</xsl:stylesheet>"""

class ExtensionFunction(xpath.ExternalFunctionObject):
    """An extension function for the transformation engine providing the implementation for on_invoke."""
    def __init__(self, function_signature, fn_on_invoke):
        super().__init__(function_signature)
        self.fn_on_invoke = fn_on_invoke

    def on_invoke(self, args: List[xpath.Sequence], session: xpath.Session, called_by: xpath.Instruction) -> xpath.Sequence:
        # Note: args contains the current arguments and can be None for 0 arity functions
        return self.fn_on_invoke(args, session, called_by)

def get_native_extension_functions():
    """A sample function to return a collection of xpath.ExternalFunctionObjects."""
    def my_raise(args, session, calling_instruction):
        raise RuntimeError(str(args[0][0]))
    return [
        ExtensionFunction(
            "Q{my-ext}python-version()",
            lambda args, session, calling_instruction: xpath.Sequence.create_from_item(xpath.AtomicItem.create_from_string(str(sys.version_info), session))
        ),
        ExtensionFunction(
            "Q{my-ext}os-version()",
            lambda args, session, calling_instruction: xpath.Sequence.create_from_item(xpath.AtomicItem.create_from_string(str(platform.platform()), session))
        ),
        ExtensionFunction(
            "Q{my-ext}integer-to-double($arg1 as xs:integer?) as xs:double? (: this function will do implicit conversions:)",
            lambda args, session, calling_instruction: args[0]
        ),
        ExtensionFunction(
            "Q{my-ext}error($arg1 as xs:string)(: this function will do implicit conversions:)",
            my_raise
        )
    ]

def print_sequence(items: xpath.Sequence):
    """print the size of the sequence and the contained items type and string value to the console"""
    s = f"xpath.Sequence[{len(items)}]("
    s += ", ".join(f"{i.type_name()}('{str(i)}')" for i in items)
    s += ")"
    print(s)

def serialize_and_print_sequence(items: xpath.Sequence, p: xpath.SerializationParams):
    """serialize_and_print_sequence"""
    ret, log = items.Serialize(p)
    if ret is None:
        raise RuntimeError("Serialization failed.", log_to_str(log))
    print(ret)

def serialize_with_method(items: xpath.Sequence, m: xpath.SerializationMethod, session: xpath.Session):
    p = xpath.SerializationParams(session)
    p.Indent = True
    p.Method = m
    serialize_and_print_sequence(items, p)

def log_to_str(log: xml.ErrorLog):
    """join all errors into a single string"""
    return ", ".join(str(e) for e in log.errors)

def load_xml_from_string(xml_string: str):
    """Parse the string to create an xml document."""
    xml_instance, log = xml.Instance.create_from_buffer(xml_string.encode())
    if xml_instance is None:
        raise RuntimeError("Failed to parse xml string.", log_to_str(log))
    return xml_instance

def print_results(result_list: xpath.ResultList, delivery_format: xpath.DeliveryFormat):
    """Iterate over results and print them."""
    print("Printing result list:")
    for r in result_list:
        # a Result object consists of a Sequence, SerializationParams and a Uri.
        if r.uri is not None and r.uri.Length > 0:
            print("Result for Uri: " + r.uri)
        if  delivery_format == xpath.DeliveryFormat.SERIALIZED:
            print_sequence(r.value)
        else:
            serialize_and_print_sequence(r.value, r.serialization_params)

def set_param_double(runtime_options: xslt.RuntimeOptions, session: xpath.Session, local_name: str, v):
    params = runtime_options.stylesheet_params
    if params is None:
        params = xpath.MapItem(session)
    var_name = xpath.AtomicItem.create_from_QName(xsd.QName(local_name), session)
    var_value = xpath.AtomicItem.create_from_double(v, session)
    params.set_value(var_name, xpath.Sequence.create_from_item(var_value))
    # Always re-assign the value of a transient object!
    runtime_options.stylesheet_params = params

def main():
    print("Starting XSLT 3.0 demo + extension functions with native callback to python.")
    #create the engine session/data-store
    session = xpath.Session()
    compile_options = xslt.CompileOptions(session)
    compile_options.external_functions, log = xpath.ExternalFunctions.create(
        session,
        *get_native_extension_functions())

    if compile_options.external_functions is None:
        raise RuntimeError("Error creating the extension function library", log_to_str(log))

    xslt_doc = load_xml_from_string(XSLT_SRC_STRING)
    xslt_node = xpath.NodeItem.create_from_informationItem(xslt_doc.document, session)
    expr, log = xslt.Stylesheet.compile(
        xslt.StylesheetProvider.create_from_node(xslt_node),
        compile_options)

    if expr is None:
        raise RuntimeError("Error compiling the xslt stylesheet.", log_to_str(log))
    runtime_options = xslt.RuntimeOptions(session)
    xml_doc = load_xml_from_string(XMLDOC_SRC_STRING)
    runtime_options.initial_match_selection = xpath.Sequence.create_from_item(
        xpath.NodeItem.create_from_informationItem(xml_doc.document, session))
    set_param_double(runtime_options, session, "param1", 2.99792458e8)

    runtime_options.delivery_format = xpath.DeliveryFormat.SERIALIZED

    result_list, log = expr.execute(runtime_options)
    if result_list is None:
        raise RuntimeError("Failed to execute the xslt stylesheet.", log_to_str(log))

    print_results(result_list, runtime_options.delivery_format)
    print("Transformation successfully finished.")

if __name__ == '__main__':
    main()
