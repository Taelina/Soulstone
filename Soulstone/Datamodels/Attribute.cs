using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Soulstone.Datamodels
{
    public class Attribute
    {
        [JsonInclude]
        public string Name = "";
        [JsonInclude]
        public string Description = "";
        [JsonInclude]
        public int Value = 0;
        [JsonInclude]
        public int TempBonus = 0;
        [JsonInclude]
        public int PermBonus = 0;
        [JsonInclude]
        public int EpicBonus = 0;

        [JsonConstructor]
        public Attribute()
        {
        }

        public Attribute(string name, int value, string description = "")
        {
            Name = name;
            Value = value;
            Description = description;
            TempBonus = 0;
            PermBonus = 0;
            EpicBonus = 0;
        }

        public Attribute Clone()
        {
            return new Attribute(Name, Value, Description)
            {
                TempBonus = this.TempBonus,
                PermBonus = this.PermBonus,
                EpicBonus = this.EpicBonus
            };
        }

        public int TotalValue => Value + TempBonus + PermBonus;
    }
}
