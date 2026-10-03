using System;
using System.Collections.Generic;
using System.Linq;
using Soulstone.Datamodels;
using Soulstone.Managers;

namespace Soulstone.Utils;

internal static class UiLabels
{
    private static readonly string[] FeatCategories = ["General", "Combat", "Magic", "Passive", "Active", "Origin", "Racial", "Class", "Custom"];
    private static readonly string[] Rarities = ["Common", "Uncommon", "Rare", "Epic", "Legendary", "Artifact"];
    private static readonly string[] Slots = GearItem.StandardSlots.Concat(GearItem.StandardAugmentationSlots).Append("General").ToArray();

    public static string FeatCategory(string value) => BuiltIn(value, "FeatCategory", FeatCategories);

    public static string Rarity(string value) => BuiltIn(value, "Rarity", Rarities);

    public static string Slot(string value) => BuiltIn(value, "Slot", Slots);

    public static string SystemType(SystemType value) =>
        LocalizationManager.Instance.GetLocalizedString($"SystemType{value}");

    public static string BuffTarget(string value) => value.ToLowerInvariant() switch
    {
        "all" or "global" => LocalizationManager.Instance.GetLocalizedString("FilterAll"),
        "initiative" => LocalizationManager.Instance.GetLocalizedString("InitiativeTab"),
        _ => value
    };

    public static string BuffModifiers(Buff buff) => buff.StatModifiers == null
        ? string.Empty
        : string.Join(", ", buff.StatModifiers.Select(kv => $"{(kv.Value >= 0 ? "+" : "")}{kv.Value} {BuffTarget(kv.Key)}"));

    public static List<string> BuffTargets(CharacterSheet? sheet, DiceSystem? system)
    {
        var targets = new List<string> { "All", "Initiative" };
        if (sheet != null)
        {
            if (sheet.characterAttributes != null) targets.AddRange(sheet.characterAttributes.Keys);
            if (sheet.characterSkills != null) targets.AddRange(sheet.characterSkills.Keys);
            if (sheet.characterAbilities != null) targets.AddRange(sheet.characterAbilities.Keys);
            if (sheet.characterResources != null) targets.AddRange(sheet.characterResources.Values.Select(r => r.Name));
        }
        if (system != null)
        {
            if (system.SystemAttributes != null) targets.AddRange(system.SystemAttributes.Keys);
            if (system.SystemSkills != null) targets.AddRange(system.SystemSkills.Keys);
            if (system.SystemAbilities != null) targets.AddRange(system.SystemAbilities.Keys);
            if (system.SystemResources != null) targets.AddRange(system.SystemResources.Select(r => r.Name));
        }

        return targets.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static string ConnectionStatus(string value) => value switch
    {
        "Connecting" => LocalizationManager.Instance.GetLocalizedString("RelayConnecting"),
        "Connected" => LocalizationManager.Instance.GetLocalizedString("RelayConnected"),
        "Disconnected" => LocalizationManager.Instance.GetLocalizedString("RelayDisconnected"),
        "Connection failed" => LocalizationManager.Instance.GetLocalizedString("RelayConnectionFailed"),
        _ => value
    };

    private static string BuiltIn(string value, string prefix, IEnumerable<string> knownValues)
    {
        var canonical = knownValues.FirstOrDefault(v => string.Equals(v, value, StringComparison.OrdinalIgnoreCase));
        if (canonical == null)
            return value;

        var key = prefix + canonical;
        var label = LocalizationManager.Instance.GetLocalizedString(key);
        return label == key ? value : label;
    }
}
