using ProjectBullet.Core.Services;
using ProjectBullet.Avalonia.Services;
using RuriLib.Models.Blocks;
using RuriLib.Models.Blocks.Custom;
using RuriLib.Models.Blocks.Settings;
using RuriLib.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectBullet.Avalonia.Utils
{
    public static class Suggestions
    {
        public static IEnumerable<string> GetInputVariableSuggestions(BlockSetting setting)
        {
            var debuggerVM = SP.GetService<ViewModelsService>().Debugger;
            var rlSettings = SP.GetService<RuriLibSettingsService>();
            var configService = SP.GetService<ConfigService>();

            var suggestions = new List<string> {
            "data.SOURCE", "data.ERROR", "data.ADDRESS",
            "data.HEADERS[\"name\"]", "data.COOKIES[\"name\"]",
            "data.STATUS", "data.RESPONSECODE", "data.RAWSOURCE", "data.Line.Data" };

            var wordlistTypeName = debuggerVM.WordlistType;
            var wordlistType = rlSettings.Environment.WordlistTypes.First(w => w.Name == wordlistTypeName);
            foreach (var slice in wordlistType.Slices.Concat(wordlistType.SlicesAlias).Reverse())
            {
                suggestions.Insert(0, $"input.{slice}");
            }

            var stack = configService.SelectedConfig.Stack;

            foreach (var block in stack)
            {
                // If it's the current block, stop here (we don't want to add variables from this or the next blocks)
                if (block.Settings.Any(s => s.Value == setting))
                {
                    break;
                }

                foreach (var variable in GetOutputVariables(block).Reverse())
                {
                    if (!string.IsNullOrWhiteSpace(variable) && !suggestions.Contains(variable))
                    {
                        suggestions.Insert(0, variable);
                    }
                }
            }

            return suggestions;
        }

        private static IEnumerable<string> GetOutputVariables(BlockInstance block)
            => block switch
            {
                AutoBlockInstance x => x.Descriptor.ReturnType == null ? Array.Empty<string>() : new string[] { x.OutputVariable },
                ParseBlockInstance x => new string[] { x.OutputVariable },
                ScriptBlockInstance x => x.OutputVariables.Select(v => v.Name),
                _ => Array.Empty<string>()
            };

        public static IEnumerable<string> Filter(IEnumerable<string> source, string query)
        {
            if (string.IsNullOrEmpty(query))
                return source;

            return source.Where(s => s.Contains(query, StringComparison.OrdinalIgnoreCase));
        }
    }
}
