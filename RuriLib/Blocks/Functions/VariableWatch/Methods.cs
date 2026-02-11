using RuriLib.Attributes;
using RuriLib.Legacy.LS;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using RuriLib.Models.Variables;
using System.Linq;
using System.Text;

namespace RuriLib.Blocks.Functions.VariableWatchFunctions
{
    [BlockCategory("Variable Watch", "Blocks for debugging and inspecting variables", "#78909c")]
    public static class Methods
    {
        [Block("Enumerates all variables and logs their name, type, and value")]
        public static string WatchVariables(BotData data)
        {
            data.Logger.LogHeader();

            var variablesList = data.TryGetObject<VariablesList>("legacyVariables");

            if (variablesList == null || variablesList.Variables.Count == 0)
            {
                data.Logger.Log("No variables found", LogColors.YellowGreen);
                return "Variables: 0";
            }

            var sb = new StringBuilder();
            var count = 0;

            foreach (var variable in variablesList.Variables)
            {
                count++;
                var value = variable.AsString();
                data.Logger.Log($"[{variable.Type}] {variable.Name} = {value}", LogColors.YellowGreen);
                sb.AppendLine($"{variable.Name} ({variable.Type}) = {value}");
            }

            var summary = $"Variables: {count}";
            data.Logger.Log(summary, LogColors.YellowGreen);
            return summary;
        }

        [Block("Finds a variable by name and logs its name, type, and value")]
        public static string WatchVariable(BotData data, string name)
        {
            data.Logger.LogHeader();

            var variablesList = data.TryGetObject<VariablesList>("legacyVariables");
            RuriLib.Models.Variables.Variable variable = null;

            if (variablesList != null)
            {
                variable = variablesList.Variables.FirstOrDefault(v => v.Name == name);
            }

            if (variable == null)
            {
                data.Logger.Log($"Variable '{name}' NOT FOUND", LogColors.YellowGreen);
                return "NOT FOUND";
            }

            var value = variable.AsString();
            data.Logger.Log($"[{variable.Type}] {variable.Name} = {value}", LogColors.YellowGreen);
            return value;
        }

        [Block("Logs a custom debug message")]
        public static void WatchLog(BotData data, string message)
        {
            data.Logger.LogHeader();
            data.Logger.Log($"[DEBUG] {message}", "#ff9800");
        }
    }
}
