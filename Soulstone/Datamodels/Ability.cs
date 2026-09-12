using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soulstone.Datamodels
{
    public class Ability
    {
        public int id;
        public string abilityName = string.Empty;
        public string abilityDescription = string.Empty;
        public Skill? linkedSkill;
        public string linkedAttribute = string.Empty;
        public int abilityModifier;

        public string AbilityName { get => abilityName; set => abilityName = value; }
        public string AbilityDescription { get => abilityDescription; set => abilityDescription = value; }
        public Skill? LinkedSkill { get => linkedSkill; set => linkedSkill = value; }
        public string LinkedAttribute { get => linkedAttribute; set => linkedAttribute = value; }
        public int AbilityModifier { get => abilityModifier; set => abilityModifier = value; }
        public int Id { get => id; set => id = value; }

        public Ability()
        { }

        public Ability(string name, int modifier = 0, string linkedAttribute = "", Skill? linkedSkill = null, string description = "", int id = 0)
        {
            this.abilityName = name;
            this.abilityModifier = modifier;
            this.linkedAttribute = linkedAttribute;
            this.linkedSkill = linkedSkill;
            this.abilityDescription = description;
            this.id = id;
        }

        public Ability Clone()
        {
            return new Ability
            {
                id = this.id,
                abilityName = this.abilityName,
                abilityDescription = this.abilityDescription,
                linkedSkill = this.linkedSkill?.Clone(),
                linkedAttribute = this.linkedAttribute,
                abilityModifier = this.abilityModifier
            };
        }


    }
}
