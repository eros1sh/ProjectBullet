using HtmlAgilityPack;
using RuriLib.Attributes;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace RuriLib.Blocks.Functions.HtmlFormFunctions
{
    [BlockCategory("HTML Forms", "Blocks for HTML form parsing", "#ef5350")]
    public static class Methods
    {
        [Block("Extracts all form fields (input, select, textarea) from the specified form as a dictionary")]
        public static Dictionary<string, string> HtmlFormExtract(BotData data, string html, int formIndex = 0)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var forms = doc.DocumentNode.SelectNodes("//form");
            var result = new Dictionary<string, string>();

            if (forms == null || formIndex >= forms.Count)
            {
                data.Logger.LogHeader();
                data.Logger.Log($"No form found at index {formIndex}", LogColors.YellowGreen);
                return result;
            }

            var form = forms[formIndex];

            // Extract input fields
            var inputs = form.SelectNodes(".//input");
            if (inputs != null)
            {
                foreach (var input in inputs)
                {
                    var name = input.GetAttributeValue("name", null);
                    if (name != null)
                    {
                        var value = WebUtility.HtmlDecode(input.GetAttributeValue("value", string.Empty));
                        result[name] = value;
                    }
                }
            }

            // Extract select fields
            var selects = form.SelectNodes(".//select");
            if (selects != null)
            {
                foreach (var select in selects)
                {
                    var name = select.GetAttributeValue("name", null);
                    if (name != null)
                    {
                        var selectedOption = select.SelectSingleNode(".//option[@selected]")
                            ?? select.SelectSingleNode(".//option");
                        var value = selectedOption != null
                            ? WebUtility.HtmlDecode(selectedOption.GetAttributeValue("value", selectedOption.InnerText))
                            : string.Empty;
                        result[name] = value;
                    }
                }
            }

            // Extract textarea fields
            var textareas = form.SelectNodes(".//textarea");
            if (textareas != null)
            {
                foreach (var textarea in textareas)
                {
                    var name = textarea.GetAttributeValue("name", null);
                    if (name != null)
                    {
                        var value = WebUtility.HtmlDecode(textarea.InnerText);
                        result[name] = value;
                    }
                }
            }

            data.Logger.LogHeader();
            data.Logger.Log($"Extracted {result.Count} fields from form at index {formIndex}:", LogColors.YellowGreen);
            foreach (var kvp in result)
            {
                data.Logger.Log($"  {kvp.Key} = {kvp.Value}", LogColors.YellowGreen);
            }

            return result;
        }

        [Block("Gets the action attribute of the specified form")]
        public static string HtmlFormGetAction(BotData data, string html, int formIndex = 0)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var forms = doc.DocumentNode.SelectNodes("//form");
            var result = string.Empty;

            if (forms != null && formIndex < forms.Count)
            {
                result = forms[formIndex].GetAttributeValue("action", string.Empty);
            }

            data.Logger.LogHeader();
            data.Logger.Log($"Form action: {result}", LogColors.YellowGreen);

            return result;
        }

        [Block("Gets the method attribute of the specified form (defaults to GET)")]
        public static string HtmlFormGetMethod(BotData data, string html, int formIndex = 0)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var forms = doc.DocumentNode.SelectNodes("//form");
            var result = "GET";

            if (forms != null && formIndex < forms.Count)
            {
                result = forms[formIndex].GetAttributeValue("method", "GET").ToUpperInvariant();
            }

            data.Logger.LogHeader();
            data.Logger.Log($"Form method: {result}", LogColors.YellowGreen);

            return result;
        }

        [Block("Gets a list of all field names in the specified form")]
        public static List<string> HtmlFormGetFields(BotData data, string html, int formIndex = 0)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var forms = doc.DocumentNode.SelectNodes("//form");
            var result = new List<string>();

            if (forms != null && formIndex < forms.Count)
            {
                var form = forms[formIndex];

                var inputs = form.SelectNodes(".//input[@name]");
                if (inputs != null)
                    result.AddRange(inputs.Select(i => i.GetAttributeValue("name", string.Empty)));

                var selects = form.SelectNodes(".//select[@name]");
                if (selects != null)
                    result.AddRange(selects.Select(s => s.GetAttributeValue("name", string.Empty)));

                var textareas = form.SelectNodes(".//textarea[@name]");
                if (textareas != null)
                    result.AddRange(textareas.Select(t => t.GetAttributeValue("name", string.Empty)));
            }

            data.Logger.LogHeader();
            data.Logger.Log($"Found {result.Count} fields in form at index {formIndex}:", LogColors.YellowGreen);
            foreach (var field in result)
            {
                data.Logger.Log($"  {field}", LogColors.YellowGreen);
            }

            return result;
        }

        [Block("Sets a value in the form data dictionary and returns the updated dictionary")]
        public static Dictionary<string, string> HtmlFormSetValue(BotData data,
            [Variable] Dictionary<string, string> formData, string fieldName, string value)
        {
            var result = new Dictionary<string, string>(formData);
            result[fieldName] = value;

            data.Logger.LogHeader();
            data.Logger.Log($"Set form field '{fieldName}' = '{value}'", LogColors.YellowGreen);

            return result;
        }
    }
}
