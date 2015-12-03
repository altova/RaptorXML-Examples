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

import os, urllib.request, urllib.parse
from altova import xml, xsd, xbrl

# This script downloads all remote parts of the schema as a whole / discoverable taxonomy set.
# The target directory for the downloaded documents can be specified with the script-param target.
# If no target is specified the documents are stored in the subfolder /output in job.output_dir.
# If any document was downloaded, a catalog containing uri mappings for each downloaded
# document is created. It is stored in the target directory and called catalog.xml.
#
# Example: raptorxml xsd --script=build_remote_docs_catalog.py http://www.w3.org/MarkUp/Forms/2007/XForms-11-Schema.xsd


# string templates for catalog ####################################################################
g_CatalogTemplate = """<?xml version='1.0' encoding='UTF-8'?>
<catalog xmlns='urn:oasis:names:tc:entity:xmlns:xml:catalog'
         xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
         xsi:schemaLocation='urn:oasis:names:tc:entity:xmlns:xml:catalog Catalog.xsd'
>
  %(mappings)s
</catalog>
"""
g_uriMappingTemplate = """<uri name="%(source)s" uri="%(target)s"/>"""


# helper functions ################################################################################
def writeDoc( path, content, mode = "wb" ):
    dir, file = os.path.split( path )
    if not os.path.exists( dir ):
        os.makedirs( dir )
    f = open( path, mode )
    f.write( content )
    f.close()


def downloadDoc( url, target ):
    content = urllib.request.urlopen( url ).read()
    writeDoc( target, content, "wb" )


def createCatalog( uriMappings, catalogPath ):
    catalogDir = os.path.dirname( catalogPath )
    lines = []
    for source, target in uriMappings.items():
        target = os.path.relpath( target, catalogDir )
        lines.append( g_uriMappingTemplate %{ "source": source, "target": target } )
    catalogContent = g_CatalogTemplate %{ "mappings": "\n  ".join( lines ) }
    writeDoc( catalogPath, catalogContent, "w" )


def createUniqueFileName( targetDir, urlParts ):
    path = urlParts.path[ 1: ] if urlParts.path.startswith( "/" ) else urlParts.path
    targetFileName = os.path.join( targetDir, urlParts.netloc, path )
    head, tail = os.path.split( targetFileName )
    i = 1
    while os.path.exists( targetFileName ):
        nextTail = "%d_%s" %( i, tail )
        i += 1
        targetFileName = os.path.join( head, nextTail )
    return targetFileName


def download_docs( docs, targetDir ):
    uriMappings = {}
    for doc in docs:
        urlParts = urllib.parse.urlparse( doc.uri )
        if urlParts.scheme != "file":
            # only download remote documents
            targetFileName = createUniqueFileName( targetDir, urlParts )
            uriMappings[ doc.uri ] = targetFileName
            downloadDoc( doc.uri, targetFileName )
    if uriMappings:
        createCatalog( uriMappings, createUniqueFileName( targetDir, urllib.parse.urlparse( "catalog.xml" ) ) )


def download_dts( dts, targetDir ):
    if dts is None:
        print( "Error executing script: dts must not be None!" )
    else:
        download_docs( dts.documents, targetDir )


def download_schema( schema, targetDir ):
    if schema is None:
        print( "Error executing script: schema must not be None!" )
    else:
        download_docs( schema.documents, targetDir )


def getTargetDir( job ):
    return os.path.abspath( os.path.join( job.output_dir, job.script_params.get( "target", "./output" ) ) )


# Entry Points ####################################################################################

# Entry Point for valxsd (xsd)
def on_xsd_finished( job, schema ):
    download_schema( schema, getTargetDir( job ) )


# Entry Point for valxml-withxsd (xsi)
def on_xsi_finished( job, instance ):
    if instance is None:
        print( "Error executing script: instance must not be None!" )
    else:
        download_schema( instance.schema, getTargetDir( job ) )


# Entry Point for valxbrltaxonomy (dts)
def on_dts_finished( job, dts ):
    if dts is None:
        print( "Error executing script: dts must not be None!" )
    else:
        download_dts( dts, getTargetDir( job ) )


# Entry Point for valxbrl (xbrl)
def on_xbrl_finished( job, instance ):
    if instance is None:
        print( "Error executing script: instance must not be None!" )
    else:
        download_dts( instance.dts, getTargetDir( job ) )
