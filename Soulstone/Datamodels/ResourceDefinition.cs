using System;

namespace Soulstone.Datamodels
{
    public class ResourceDefinition
    {
        public string name = string.Empty;
        public string description = string.Empty;
        public int defaultMax = 100;
        public int defaultCurrent = 100;
        public string colorHex = "#2ecc71";
        public bool isRequired = false;
        public string formula = string.Empty;

        public string Name { get => name; set => name = value; }
        public string Description { get => description; set => description = value; }
        public int DefaultMax { get => defaultMax; set => defaultMax = value; }
        public int DefaultCurrent { get => defaultCurrent; set => defaultCurrent = value; }
        public string ColorHex { get => colorHex; set => colorHex = value; }
        public bool IsRequired { get => isRequired; set => isRequired = value; }
        public string Formula { get => formula; set => formula = value; }

        public ResourceDefinition() { }

        public ResourceDefinition(string name, int defaultMax = 100, int defaultCurrent = 100, string colorHex = "#2ecc71", string description = "", bool isRequired = false, string formula = "")
        {
            this.name = name;
            this.defaultMax = defaultMax;
            this.defaultCurrent = defaultCurrent;
            this.colorHex = colorHex;
            this.description = description;
            this.isRequired = isRequired;
            this.formula = formula;
        }

        public ResourceDefinition Clone()
        {
            return new ResourceDefinition
            {
                Name = this.Name,
                Description = this.Description,
                DefaultMax = this.DefaultMax,
                DefaultCurrent = this.DefaultCurrent,
                ColorHex = this.ColorHex,
                IsRequired = this.IsRequired,
                Formula = this.Formula
            };
        }
    }
}
