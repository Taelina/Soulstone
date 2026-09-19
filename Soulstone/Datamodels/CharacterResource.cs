using System;

namespace Soulstone.Datamodels
{
    public class CharacterResource
    {
        public string name = string.Empty;
        public int currentValue = 0;
        public int maxValue = 0;
        public int tempBonus = 0;
        public string formula = string.Empty;
        public ResourceType resourceType = ResourceType.Bar;
        public bool isRollable = true;
        public bool showInGroup = true;

        public string Name { get => name; set => name = value; }
        public int CurrentValue { get => currentValue; set => currentValue = value; }
        public int MaxValue { get => maxValue; set => maxValue = value; }
        public int TempBonus { get => tempBonus; set => tempBonus = value; }
        public string Formula { get => formula; set => formula = value; }
        public ResourceType ResourceType { get => resourceType; set => resourceType = value; }
        public bool IsRollable { get => isRollable; set => isRollable = value; }
        public bool ShowInGroup { get => showInGroup; set => showInGroup = value; }
        public int TotalMaxValue => MaxValue + TempBonus;

        public CharacterResource() { }

        public CharacterResource(string name, int currentValue, int maxValue, int tempBonus = 0, string formula = "", ResourceType resourceType = ResourceType.Bar, bool isRollable = true, bool showInGroup = true)
        {
            this.name = name;
            this.currentValue = currentValue;
            this.maxValue = maxValue;
            this.tempBonus = tempBonus;
            this.formula = formula;
            this.resourceType = resourceType;
            this.isRollable = isRollable;
            this.showInGroup = showInGroup;
        }

        public CharacterResource Clone()
        {
            return new CharacterResource
            {
                Name = this.Name,
                CurrentValue = this.CurrentValue,
                MaxValue = this.MaxValue,
                TempBonus = this.TempBonus,
                Formula = this.Formula,
                ResourceType = this.ResourceType,
                IsRollable = this.IsRollable,
                ShowInGroup = this.ShowInGroup
            };
        }
    }
}
