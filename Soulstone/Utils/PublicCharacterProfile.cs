using Soulstone.Datamodels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Soulstone.Utils;

// Preserve the existing profile JSON shape while explicitly excluding private gameplay data.
internal sealed record PublicCharacterProfile(Dictionary<string, JsonElement> Fields)
{
    private static readonly string[] VisibleFields =
    {
        "CharacterFullName", "CharacterNickName", "CharacterRace", "CharacterSubRace", "CharacterJob",
        "CharacterSex", "CharacterGender", "CharacterPronouns", "CharacterAge", "CharacterHeight",
        "CharacterWeight", "CharacterBuild", "CharacterEyeColor", "CharacterHairColor", "CharacterSkinTone",
        "CharacterScars", "CharacterTattoos", "CharacterDistinctiveFeatures", "CharacterHomeland",
        "CharacterOrigin", "CharacterAffiliation", "CharacterOccupation", "CharacterReputation",
        "CharacterBackground", "CharacterNotes", "CharacterInfo", "PlayerAvailability", "PlayerTimezone",
        "PlayerNotes", "CharacterQuickLook1", "CharacterQuickLook2", "CharacterQuickLook3",
        "CharacterQuickLook4", "CharacterQuickLook5", "CharacterFamily", "CharacterFriends", "CharacterEnnemies",
        "CharacterPictureUrl", "LinkedDiceSystem"
    };

    public static PublicCharacterProfile FromSheet(CharacterSheet sheet)
    {
        var source = JsonSerializer.SerializeToElement(sheet);
        var fields = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var name in VisibleFields)
        {
            var visibilityKey = name == "LinkedDiceSystem" ? "CharacterLinkedSystem" : name;
            if (sheet.IsFieldHidden(visibilityKey))
                continue;
            if (name == "CharacterPictureUrl" &&
                (!Uri.TryCreate(sheet.CharacterPictureUrl, UriKind.Absolute, out var uri) ||
                 (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
                continue;
            fields[name] = source.GetProperty(name).Clone();
        }
        var resources = sheet.CharacterResources?.Where(pair =>
            !sheet.IsFieldHidden(pair.Key) && !sheet.IsFieldHidden($"Resource_{pair.Key}"))
            .ToDictionary(pair => pair.Key, pair => new CharacterResource
            {
                Name = pair.Value.Name,
                CurrentValue = pair.Value.CurrentValue,
                MaxValue = pair.Value.TotalMaxValue,
                ResourceType = pair.Value.ResourceType,
                ShowInGroup = pair.Value.ShowInGroup,
                IsRollable = pair.Value.IsRollable
            }, StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, CharacterResource>();
        fields["CharacterResources"] = JsonSerializer.SerializeToElement(resources);
        fields["HiddenFields"] = JsonSerializer.SerializeToElement(sheet.HiddenFields ?? new List<string>());
        return new PublicCharacterProfile(fields);
    }

    public string ToJson() => JsonSerializer.Serialize(Fields);
}
