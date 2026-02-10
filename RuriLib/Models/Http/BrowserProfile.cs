using System.Collections.Generic;

namespace RuriLib.Models.Http
{
    public class BrowserProfile
    {
        public string Name { get; set; }
        public string UserAgent { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new();
    }

    public enum BrowserProfileName
    {
        None,
        Chrome120,
        Firefox121,
        Safari17,
        Edge120,
        Random
    }
}
