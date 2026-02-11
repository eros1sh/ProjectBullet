using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using RuriLib.Attributes;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using System.Collections.Generic;
using System.IO;

namespace RuriLib.Blocks.Functions.PythonScriptFunctions
{
    [BlockCategory("Python Script", "Blocks for executing Python scripts", "#306998")]
    public static class Methods
    {
        [Block("Executes a Python script and returns the standard output")]
        public static string PythonExecute(BotData data, string script)
        {
            data.Logger.LogHeader();

            var engine = Python.CreateEngine();
            var scope = engine.CreateScope();

            var ms = new MemoryStream();
            var sw = new StreamWriter(ms);
            engine.Runtime.IO.SetOutput(ms, sw);

            engine.Execute(script, scope);

            sw.Flush();
            ms.Position = 0;
            var result = new StreamReader(ms).ReadToEnd();

            data.Logger.Log($"Executed Python script, output length: {result.Length}", LogColors.YellowGreen);
            data.Logger.Log(result, LogColors.YellowGreen);
            return result;
        }

        [Block("Executes a Python script with input variables and returns the result variable")]
        public static string PythonExecuteWithVars(BotData data, string script,
            [Variable] Dictionary<string, string> variables)
        {
            data.Logger.LogHeader();

            var engine = Python.CreateEngine();
            var scope = engine.CreateScope();

            // Set input variables into the scope
            if (variables != null)
            {
                foreach (var kvp in variables)
                {
                    scope.SetVariable(kvp.Key, kvp.Value);
                }
            }

            var ms = new MemoryStream();
            var sw = new StreamWriter(ms);
            engine.Runtime.IO.SetOutput(ms, sw);

            engine.Execute(script, scope);

            sw.Flush();
            ms.Position = 0;
            var output = new StreamReader(ms).ReadToEnd();

            // Try to get a "result" variable from the scope
            string result;
            if (scope.TryGetVariable<object>("result", out var resultVar) && resultVar != null)
            {
                result = resultVar.ToString();
            }
            else
            {
                result = output;
            }

            data.Logger.Log($"Executed Python script with {variables?.Count ?? 0} input variables", LogColors.YellowGreen);
            data.Logger.Log($"Result: {result}", LogColors.YellowGreen);
            return result;
        }

        [Block("Evaluates a Python expression and returns the result as a string")]
        public static string PythonEval(BotData data, string expression)
        {
            data.Logger.LogHeader();

            var engine = Python.CreateEngine();
            var result = engine.Execute<object>(expression);
            var resultString = result?.ToString() ?? string.Empty;

            data.Logger.Log($"Evaluated expression: {expression}", LogColors.YellowGreen);
            data.Logger.Log($"Result: {resultString}", LogColors.YellowGreen);
            return resultString;
        }
    }
}
