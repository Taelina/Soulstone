using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Soulstone.Datamodels
{
    public class Skill
    {
        public int id;
        public string skillName = string.Empty;
        public string skillDescription = string.Empty;
        public string linkedAttribute = string.Empty;
        public int skillModifier;
        public string SkillName { get => skillName; set => skillName = value; }
        public string SkillDescription { get => skillDescription; set => skillDescription = value; }
        public string LinkedAttribute { get => linkedAttribute; set => linkedAttribute = value; }
        public int SkillModifier { get => skillModifier; set => skillModifier = value; }
        public int Id { get => id; set => id = value; }

        public Skill()
        { }
    }
}
