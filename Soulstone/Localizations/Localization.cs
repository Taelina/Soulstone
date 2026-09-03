using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soulstone.Localizations
{
    public enum Language
    {
        Français = 0,
        English = 1
    }
    internal class Localization
    {
        private Language language;
        public Language Language { get => language; set => language = value; }
        public Dictionary<string, string> LocalizedStrings { get => localizedStrings; set => localizedStrings = value; }

        private Dictionary<string, string> localizedStrings = new();

        public Localization() { }
        
    }
}
