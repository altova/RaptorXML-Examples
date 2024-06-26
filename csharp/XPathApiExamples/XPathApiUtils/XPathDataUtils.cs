// Copyright 2024 Altova GmbH
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using XPath = Altova.RaptorXml.XPath;
using Err = Altova.RaptorXml.ErrorLog;
using Xsd = Altova.RaptorXml.Xsd;

namespace XPathDataUtils
{
	// extracted from the GettingStartedWithVariables
	public class Print
	{
		public static void PrintErrors(string msg, Err log)
		{
			//print the Altova.RaptorXml.ErrorLog to the console
			System.Console.WriteLine(msg);
			foreach (var error in log)
			{
				System.Console.WriteLine(error.Text);
			}
		}
		public static void PrintSequence(XPath.Sequence res)
		{
			//print XPath.Sequence
			System.Console.WriteLine(String.Format("There are {0} items in the sequence.", res.Count));
			foreach (var item in res)
			{
				System.Console.WriteLine(String.Format("ItemType {0} with value {1}", item.GetTypeName(), item.ToString()));
			}
		}
	}
}
