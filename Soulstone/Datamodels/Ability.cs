using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soulstone.Datamodels
{
    internal class Ability
    {
        public int id;
        public string abilityName;
        public string abilityDescription;
        public Skill linkedSkill;
        public string linkedAttribute;
        public int abilityModifier;

        public string AbilityName { get => abilityName; set => abilityName = value; }
        public string AbilityDescription { get => abilityDescription; set => abilityDescription = value; }
        internal Skill LinkedSkill { get => linkedSkill; set => linkedSkill = value; }
        public string LinkedAttribute { get => linkedAttribute; set => linkedAttribute = value; }
        public int AbilityModifier { get => abilityModifier; set => abilityModifier = value; }
        public int Id { get => id; set => id = value; }

        public Ability()
        { }


    }
}
