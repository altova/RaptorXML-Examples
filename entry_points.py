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

# This script contains a minimal stub implementation for each entry point function in RaptorXML Python API v2.
#
# Example invocation:
# raptorxml valxml-withxsd --streaming=false --script=entry_points.py
# ExpReport.xml

from altova import *

# This entry point will be called by RaptorXML after the XML instance
# validation job (valxml-withxsd) has finished


def on_xsi_finished(job, instance):
    # instance object will be None if XML validation was not successful
    print('on_xsi_finished() was called')

# This entry point will be called by RaptorXML after the DTD validation
# job (valdtd) has finished (since v2.1)


def on_dtd_finished(job, dtd):
    # dtd object will be None if DTD validation was not successful
    print('on_dtd_finished() was called')

# This entry point will be called by RaptorXML after the XML Schema
# validation job (valxsd) has finished


def on_xsd_finished(job, schema):
    # schema object will be None if XML Schema validation was not successful
    print('on_xsd_finished() was called')

# This entry point will be called by RaptorXML+XBRL after the XBRL DTS
# validation job (valdts) has finished


def on_dts_finished(job, schema):
    # dts object will be None if XBRL DTS validation was not successful
    print('on_dts_finished() was called')

# This entry point will be called by RaptorXML+XBRL after the XBRL
# instance validation job (valxbrl) has finished


def on_xbrl_finished(job, instance):
    # instance object will be None if XBRL 2.1 validation was not successful
    print('on_xbrl_finished() was called')
