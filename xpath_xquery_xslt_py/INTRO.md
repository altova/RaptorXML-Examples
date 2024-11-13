# Altova RaptorXML+XBRL Server API Tutorial for Python

## Overview

This tutorial provides a comprehensive guide to using the Altova RaptorXML+XBRL in Python for processing XML and JSON with XPath, XSLT, and XQuery. The guide is aimed at developers who want to automate XML transformations and queries using Python with the [Altova RaptorXML+XBRL Server API](https://www.altova.com/manual/en/raptorapi/pyapiv2/2.10.0/html/index.html).


## Table of Contents

1. [Prerequisites](#prerequisites)

2. [Getting Started](#getting-started)

    - Setting up the environment

    - Importing the necessary modules

3. [Basic XPath, XQuery, and XSLT Processing](#basic-xpath-xquery-and-xslt-processing)

    - [Basic Workflow](#basic-workflow)

    - [Error Handling](#error-handling)

    - [Processing a Sequence](#processing-a-sequence)

4. [Advanced Options: Compile and Runtime](#advanced-options-compile-and-runtime)

    - [Working with Namespaces](#working-with-namespaces)

    - [Creating Atomic Items](#creating-atomic-items)

    - [Creating Sequences](#creating-sequences)

    - [Using External Variables](#using-external-variables)

	- [Serialization](#serialization)

    - [Loading XML documents](#loading-xml-documents)
  
    - [Setting the input document](#setting-the-input-document)
  
5. [Advanced Topics](#advanced-topics)

    - [Writing Native Extension Functions in Python](#advanced-topics)

	  - [Function Signature](#function-signature)

	  - [The on_invoke Member Function](#the-on_invoke-member-function)

      - [Creating a Generic Extension Function Class](#creating-a-generic-extension-function-class)

6. [Complete Example](#complete-example)

7. [Additional Resources](#additional-resources)

8. [Conclusion](#conclusion)

9. [Additional Resources](#additional-resources)

## Prerequisites

* Altova RaptorXML+XBRL Server 2025 installed with a valid license (see the [manual](https://www.altova.com/manual/RaptorXML/raptorxmlxbrlserver/srvstpwin_licensing_registerapp.html) for license registration and activation).

* Altova RaptorXML+XBRL executable, which has a built-in CPython interpreter accessible with the command `raptorxmlxbrl.exe script example.py`

* Optional: Python 3.11 and raptorxml whl package installed.

## Getting Started

* Create a new Python file or open an existing one.

* Import the necessary modules from the `altova_api.v2` package:

```python
from altova_api.v2 import xml, xpath, xquery, xsd, xslt
```

## Basic XPath, XQuery, and XSLT Processing

### Basic Workflow

The typical workflow for using XPath, XQuery, and XSLT in Python with the RaptorXML API includes the following steps:

  1. Create a *Session* object: Initialize an xpath.Session to manage the lifetime of engine-specific resources.
  2. Compile Options: Configure the static context components using *CompileOptions*.
  3. Create the *Executable*: Compile your XPath, XQuery, or XSLT expression using the `compile()` method from the corresponding class.  
     > Note: There are slight differences in specifying the input for XPath, XQuery, and XSLT:
     >   - For XPath, the input is a string.
     >   - For XQuery, use *xquery.ExpressionProvider* created from text or a file location (`create_from_text`, `create_from_location`).
     >   - For XSLT, use *xslt.StylesheetProvider* created from text, file location, or an XML node (`create_from_text`, `create_from_location`, `create_from_node`).
  4. Runtime Options: Configure the dynamic context components (ex. initial context item, external variables, etc.) using *RuntimeOptions*.
  5. Execute the Expression: Run the compiled expression with the `execute()` method and retrieve the results.
     >  - XPath returns an `xpath.Sequence`.  
     >  - XQuery and XSLT return an `xpath.ResultList`, an iterable container of `xpath.Result` objects - this contains the `value` sequence, the output `uri` and the `serialization_params`.

  6. Handle the Results: Process and print the results, or handle errors.

Example:

```python
# Step 1: Create an XPath session
session = xpath.Session()

# Step 2: Create the compile options for static context components
compile_options = xpath.CompileOptions(session)

# Step 3: Compile an XPath expression
expr, log = xpath.Expression.compile("""parse-xml('<?xml version="1.0"?><root><element/></root>')/root/element""", compile_options)

# If there are syntax or static errors, the executable is None,
# and the log.errors contains the details.
if expr is None:
    print("Failed to compile expression:", log.errors)
	return

# Step 4: Create the runtime options for dynamic context components
runtime_options = xpath.RuntimeOptions(session)

# Step 5: Execute the expression
result, log = expr.execute(runtime_options)

# If there were runtime errors, the result is None,
# and the log.errors contains the details.
if result is None:
    print("Failed to execute the expression:", log.errors)
	return

# Step 6. Handle the Result:
for item in result:
	print(item)
```

### Error Handling

Always check if the returned expression or result is None and inspect the error logs. Common errors include syntax errors, missing or incorrect function and variable names, static type errors during compilation or execution errors due invalid XML, missing context, dynamic type errors, etc.

```py
if result is None:
    print(", ".join(str(e) for e in log.errors))
```

The log can also contain *warnings*. These might be helpful to identify subtle mistakes.

```py
if log is not None and log.has_warnings:
    print(", ".join(str(e) for e in log.warnings))
```

### Processing a Sequence

You can print the size of the sequence and each item's type and string value:
```py
def print_sequence(items: xpath.Sequence):
    # Note: This will not descend into xpath.ArrayItem(s) and xpath.MapItem(s).
    s = f"xpath.Sequence[{len(items)}]("
    s += ", ".join(f"{i.type_name()}('{str(i)}')" for i in items)
    s += ")"
    print(s)
```

## Advanced Options: Compile and Runtime

> **Important**: Due to the functional nature of the language, many internal data structures that wrap a sequence or item are immutable and when accessed via a field the python wrapped object behaves like a copy of the original - one could think of it as accessed by value - modifications would happen on this transient object. To avoid errors/unexpected results due modifying a transient value, it is recommended to re-assign these fields.


### Working with Namespaces

In XPath, XQuery, and XSLT, QNames are used for XML elements and attributes, function and variable names, type names. In expanded name matching, the prefix is only used to calculate the corresponding namespace-uri. You can define namespace mappings using `compile_options.statically_known_namespaces`. The `compile_options.use_standard_namespace_prefixes` property enables the use of default namespaces from the specifications. The statically known namespaces can't be set for XSLT instructions, the in-scope namespaces of the instructions XML node is used. Starting with version 3.0 the URIQualifiedName can be used to define the namespace-uri of an expanded name directly.

```py
MyNamespaceUri = "my-namespace-uri"
compile_options.statically_known_namespaces = {"prefix1": MyNamespaceUri}
str_expr = f"parse-xml('<a xmlns=\"{MyNamespaceUri}\"><b/><c xmlns=\"different-ns\"/></a>')//prefix1:*"

# The wildcard match with the BracedURILiteral could be used as well: Q{my-namespace-uri}*
expr, log = xpath.Expression.compile(str_expr, compile_options)
```

### Creating Atomic Items

Atomic items are created using the create_from_* class methods. You can pass native Python types or XML Schema types:

```py
xpath.AtomicItem.create_from_string("lorem ipsum", session)
xpath.AtomicItem.create_from_int(123, session)
xpath.AtomicItem.create_from_integer(xsd.Integer("123"), session)
```

### Creating Sequences

Sequences are similar to atomic items bound to a session object. Sequences can't contain 'gaps' (empty items). You can create an empty sequences as follows:  

```py
xpath.Sequence(session)
```

A singleton sequence contains exactly one item and can be created with the create_from_item function:

```py
xpath.Sequence.create_from_item(item)
```

### Using External Variables

External variables are set using xpath.MapItem. Names must be AtomiItems of type xs:QName:

```py
params = xpath.MapItem(session)
var_name = xpath.AtomicItem.create_from_QName(xsd.QName("param1"), session)
var_value = xpath.AtomicItem.create_from_double(3e8, session)
params.set_value(var_name, xpath.Sequence.create_from_item(var_value))
runtime_options.external_variables = params
```

Note: For XPath, all non-local variables are treated as external. In XQuery, external variables must be declared explicitly in the *Prolog*. Use the `xquery.CompileOptions.allow_undeclared_variables` property to relax this rule.

### Serialization

You can control the serialization of sequences via the *SerializationParams*, for the various output formats *XML*, *HTML*, *XHTML*, *JSON*, *ADAPTIVE*, *TEXT* as controlled by the *SerializationMethod*:

```py
def serialize_sequence(items: xpath.Sequence, params: xpath.SerializationParams):
    result, log = items.serialize(params)
    # Serialization can fail for various reasons so check for errors
    if result is None:
        raise RuntimeError("Serialization failed", log)
    print(result)

def serialize(items: xpath.Sequence, method: xpath.SerializationMethod, session: xpath.Session):
    params = xpath.SerializationParams(session)
    params.indent = True
    params.method = method
    serialize_sequence(items, params)
```

### Loading XML documents

You can load an XML document from a string or buffer:
```py
xml_instance, log = xml.Instance.create_from_buffer("""<?xml version="1.0" encoding="UTF-8"?><root><element/></root>""".encode())

if xml_instance is None:
    raise RuntimeError("Error loading document", log)
```

To load an XML document from a file:

```py
xml_instance, log = xml.Instance.create_from_url(file_name)
if xml_instance is None:
    raise RuntimeError("Error loading document", log)
```

Additionally to the previous methods, one can (re)use a document returned by a previous execution -ex. loaded by `fn:doc`, `fn:parse-xml`, created by node constructors in xquery or xslt, etc.

### Setting the input document

You can provide an input document for an expression by setting the *initial_context* in the RuntimeOptions:

```py
runtime_options.initial_context = xpath.NodeItem.create_from_information_item(xml_instance.document, session)
```

## Advanced Topics

### Writing Native Extension Functions in Python

You can extend XPath, XQuery, and XSLT execution with an external function library providing callback to native Python functions.

> The *xpath.ExternalFunctionObject* is the base class for these extension functions.  

> The *xpath.ExternalFunctions* represents a sealed collection of such extension function objects. It can be constructed with the `create()` class method and can be set on the compile_options.external_functions, it can be shared by multiple compiled expressions.


To create new native extension functions:
1. Inherit from the class *xpath.ExternalFunctionObject*:
   - initialize the [**`function signature`**](#function-signature) in the  constructor
   - implement the [**`on_invoke`**](#the-on_invoke-member-function) member function.
2. Use these objects in the `create()` method of the `xpath.ExternalFunctions`.
3. Set the new function library on the *compile_options.external_functions*.
 
 You can then compile and execute expressions referencing and invoking these extension functions - function lookup is performed based on the expanded-name and arity as mandated by the XPath/XQuery specifications.

```py
# Step 1. Inherit from the class *xpath.ExternalFunctionObject*:
class MyExtensionFunction(xpath.ExternalFunctionObject):
    def __init__(self):
		# Set the signature string in the base class constructor to specify:
		# the function *name*, *arity* and optionally the parameter and return types.
        super().__init__("Q{new-fn-namespace-uri}just-a-string($in as xs:string) as xs:string")

    def on_invoke(self, param_list, session, instruction):
        # Set a breakpoint in your debugger to inspect the invocation parameters
		s = ', '.join([str(param_list[0][0]), "lérem", "utolérem!"])
        return xpath.Sequence.create_from_item(xpath.AtomicItem.create_from_string(s, session))

# Step 2. Create a new function library
# optional parameter namespaces: dict   - when None(default), then use the built-in namespaces 
# optional parameter schema: xsd.Schema - when None(default), then use the built-in schema-types
fn_lib, log = xpath.ExternalFunctions.create(session, MyExtensionFunction())
if fn_lib is None:
    raise RuntimeError("error compiling external function library", log.errors)

# Step 3. set the new function library on the compile_options.
compile_options.external_functions = fn_lib

# The function can now be used in the expression. It is identified by the expanded name and arity.
# With a matching prefix mapping the function can be invoked using the form prefix1:local-name($arg1, $arg2, ...)
expr, log = xpath.Expression.compile("Q{new-fn-namespace-uri}just-a-string('érem')", compile_options)
```

#### **Function signature**

The signature must be passed to the base class constructor to specify the function *name*, *arity* (parameter count) and optionally the parameter and return types if a *SequenceType* different than *item()\** is desired.  
This string will be parsed and validated during the call to *xpath.ExternalFunctions.create()* and must have the form:

>  **[EQName](https://www.w3.org/TR/xquery-31/#doc-xquery31-EQName) "(" [ParamList](https://www.w3.org/TR/xquery-31/#prod-xquery31-ParamList)? ")" ("as" [SequenceType](https://www.w3.org/TR/xquery-31/#doc-xquery31-SequenceType))?**  

where the production rules are the same as the ones in the XPath/XQuery specification:  

```bnf
ParamList         ::= Param ("," Param)*
Param             ::= "$" EQName ("as" SequenceType)?
EQName            ::= QName | URIQualifiedName
QName             ::= NCName (":" NCName)?
URIQualifiedName  ::= "Q{" namespace-uri "}" NCName
```

 If the function name is specified with a prefix instead of a BracedURILiteral, then there must be a valid namespace mapping for that prefix in the namespaces parameter of the ExternalFunctions.create call. For the function names similar restrictions apply as for the XQuery 3.1 user defined functions (reserved function names, reserved namespaces).


#### **The on_invoke Member Function**

This is the member method that gets called during XPath, XQuery, Xslt evaluation when a function matching the name and arity provided by the signature in the constructor is invoked. This method must be implemented on the derived class and will propagate the returned xpath.Sequence to the executing expression. The number of sequences provided via the param_list will match the arity of the function and contain the parameter values for the actual call.  
Note: Both the param_list and the returned sequences are subject to the function conversion rules as defined in the W3C specifications  (optional node atomization, type promotion, etc.).

```py
def on_invoke(self, param_list: List[xpath.Sequence], session: xpath.Session, called_by: xpath.Instruction) -> xpath.Sequence:
	# args can be None for functions that don't take any parameters
    return xpath.Sequence(session)
```
> **Important:** The param_list sequences are not sanitized, they can contain function and extension items. One must not store any xpath function item or extension item for later use - neither directly nor indirectly (ex. in a map or array). Calling such a function outside of their original execution scope and context is undefined behavior!


#### **Creating a Generic Extension Function Class**

You can also generalize the extension function: The signature and the callback function (ex. lambda function) gets specified during object construction.

```py
class ExtensionFunction(xpath.ExternalFunctionObject):
    """An extension function for the transformation engine providing the implementation for on_invoke."""
    def __init__(self, function_signature, fn_on_invoke):
        super().__init__(function_signature)        
        self.fn_on_invoke = fn_on_invoke

    def on_invoke(self, args: List[xpath.Sequence], session: xpath.Session, called_by: xpath.Instruction):
        # Note: args contains the current arguments and can be None for 0 arity functions
        return self.fn_on_invoke(args, session, called_by)
```

Using the generalized extension function:

```py
fn_list = [
    ExternalFunction(
        "Q{my-ext-ns}python-version()",
        lambda param_list, session, called_by: xpath.Sequence.create_from_item(xpath.AtomicItem.create_from_string(str(sys.version_info), session))),

    ExternalFunction(
        "Q{my-ext-ns}os-version()",
        lambda param_list, session, called_by: xpath.Sequence.create_from_item(xpath.AtomicItem.create_from_string(str(platform.platform()), session)))]

compile_options.external_functions, log = xpath.ExternalFunctions.create(session, *fn_list)
# define a ns1->my-ext prefix namespace mapping 
compile_options.statically_known_namespaces = { "ns1": "my-ext-ns" }

expr, log = xpath.Expression.compile("ns1:python-version(), ns1:os-version()", compile_options)
result, log = expr.execute(xpath.RuntimeOptions(session))
```

## Complete Example

```py
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

```

---

## Conclusion

This tutorial has covered the basics of using the XPath, XQuery and XSLT engingines via the Altova RaptorXML+XBRL Server API with Python. With these fundamentals, you should be able to create robust XML, XSLT, and XQuery solutions in Python.


## Additional Resources

[https://www.w3.org/TR/xpath-31/]()  
[https://www.w3.org/TR/xquery-31/]()  
[https://www.w3.org/TR/xslt-30/]()  
[https://www.w3.org/TR/xpath-functions-31/]()  
[https://www.w3.org/TR/xslt-xquery-serialization-31/]()

[https://www.altova.com/raptorxml]()  
[https://www.altova.com/manual/en/raptorapi/pyapiv2/2.9.0/html/index.html]()  

---

Last modified: *17. September 2024*  
Copyright 2024 Altova.
