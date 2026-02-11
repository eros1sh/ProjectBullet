using RuriLib.Attributes;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace RuriLib.Blocks.Functions.XmlFunctions
{
    [BlockCategory("XML Functions", "Blocks for XML parsing", "#26a69a")]
    public static class Methods
    {
        [Block("Parses XML and returns the value of the first element matching the XPath expression")]
        public static string XmlParse(BotData data, string xml, string xpath)
        {
            var doc = XDocument.Parse(xml);
            var element = doc.XPathSelectElement(xpath);
            var result = element?.Value ?? element?.ToString() ?? string.Empty;

            data.Logger.LogHeader();
            data.Logger.Log($"XPath '{xpath}' result: {result}", LogColors.YellowGreen);

            return result;
        }

        [Block("Parses XML and returns the values of all elements matching the XPath expression")]
        public static List<string> XmlParseAll(BotData data, string xml, string xpath)
        {
            var doc = XDocument.Parse(xml);
            var results = doc.XPathSelectElements(xpath).Select(e => e.Value).ToList();

            data.Logger.LogHeader();
            data.Logger.Log($"XPath '{xpath}' returned {results.Count} results:", LogColors.YellowGreen);
            foreach (var r in results)
            {
                data.Logger.Log($"  {r}", LogColors.YellowGreen);
            }

            return results;
        }

        [Block("Gets the value of a specific attribute from the first XML element matching the XPath expression")]
        public static string XmlGetAttribute(BotData data, string xml, string xpath, string attributeName)
        {
            var doc = XDocument.Parse(xml);
            var element = doc.XPathSelectElement(xpath);
            var result = element?.Attribute(attributeName)?.Value ?? string.Empty;

            data.Logger.LogHeader();
            data.Logger.Log($"XPath '{xpath}' attribute '{attributeName}': {result}", LogColors.YellowGreen);

            return result;
        }

        [Block("Gets the inner text of the first XML element matching the XPath expression")]
        public static string XmlGetInnerText(BotData data, string xml, string xpath)
        {
            var doc = XDocument.Parse(xml);
            var element = doc.XPathSelectElement(xpath);
            var result = element?.Value ?? string.Empty;

            data.Logger.LogHeader();
            data.Logger.Log($"XPath '{xpath}' inner text: {result}", LogColors.YellowGreen);

            return result;
        }

        [Block("Counts the number of XML elements matching the XPath expression")]
        public static int XmlCount(BotData data, string xml, string xpath)
        {
            var doc = XDocument.Parse(xml);
            var count = doc.XPathSelectElements(xpath).Count();

            data.Logger.LogHeader();
            data.Logger.Log($"XPath '{xpath}' matched {count} elements", LogColors.YellowGreen);

            return count;
        }

        [Block("Gets the names of child elements of the first XML element matching the XPath expression")]
        public static List<string> XmlGetElementNames(BotData data, string xml, string xpath)
        {
            var doc = XDocument.Parse(xml);
            var element = doc.XPathSelectElement(xpath);
            var names = element?.Elements().Select(e => e.Name.LocalName).ToList() ?? new List<string>();

            data.Logger.LogHeader();
            data.Logger.Log($"XPath '{xpath}' child element names ({names.Count}):", LogColors.YellowGreen);
            foreach (var name in names)
            {
                data.Logger.Log($"  {name}", LogColors.YellowGreen);
            }

            return names;
        }
    }
}
