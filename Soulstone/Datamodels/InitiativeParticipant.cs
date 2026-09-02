using System;

namespace Soulstone.Datamodels
{
    public class InitiativeParticipant
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public int InitiativeValue { get; set; } = 0;
        public int BonusModifier { get; set; } = 0;
        public bool IsCurrentCharacter { get; set; } = false;
        public string Notes { get; set; } = string.Empty;

        public InitiativeParticipant() { }

        public InitiativeParticipant(string name, int initiativeValue, int bonusModifier = 0, bool isCurrentCharacter = false, string notes = "")
        {
            Id = Guid.NewGuid().ToString();
            Name = name;
            InitiativeValue = initiativeValue;
            BonusModifier = bonusModifier;
            IsCurrentCharacter = isCurrentCharacter;
            Notes = notes;
        }
    }
}
