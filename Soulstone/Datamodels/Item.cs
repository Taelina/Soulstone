using Dalamud.Game.Text;
using Soulstone.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Soulstone.Datamodels
{
    public struct FormulaResult
    {
        public bool Success { get; set; }
        public int Total { get; set; }
        public string Details { get; set; }
        public string Formula { get; set; }
    }

    public class ItemUseResult
    {
        public bool Success { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int? FormulaResult { get; set; }
        public int RemainingQuantity { get; set; }
    }

    [JsonDerivedType(typeof(Item), typeDiscriminator: "Item")]
    [JsonDerivedType(typeof(GearItem), typeDiscriminator: "GearItem")]
    public class Item
    {
        public string id = Guid.NewGuid().ToString();
        public string name = string.Empty;
        public string description = string.Empty;
        public string effect = string.Empty;
        public string itemType = "Miscellaneous";
        public int quantity = 1;
        public int maxStack = 99;
        public string imageUrl = string.Empty;
        public float weight = 0.0f;
        public string rarity = "Common";
        public bool isUsable = false;
        public string useFormula = string.Empty;
        public Dictionary<string, string> customProperties = new();

        public string Id { get => id; set => id = value; }
        public string Name { get => name; set => name = value; }
        public string Description { get => description; set => description = value; }
        public string Effect { get => effect; set => effect = value; }
        public string ItemType { get => itemType; set => itemType = value; }
        public int Quantity { get => quantity; set => quantity = value; }
        public int MaxStack { get => maxStack; set => maxStack = value; }
        public string ImageUrl { get => imageUrl; set => imageUrl = value; }
        public float Weight { get => weight; set => weight = value; }
        public string Rarity { get => rarity; set => rarity = value; }
        public bool IsUsable { get => isUsable; set => isUsable = value; }
        public string UseFormula { get => useFormula; set => useFormula = value; }
        public Dictionary<string, string> CustomProperties { get => customProperties; set => customProperties = value; }

        public Item()
        {
            customProperties = new Dictionary<string, string>();
        }

        public Item(string name, string description = "", string effect = "", string itemType = "Miscellaneous", int quantity = 1, string imageUrl = "", bool isUsable = false, string useFormula = "")
        {
            this.id = Guid.NewGuid().ToString();
            this.name = name;
            this.description = description;
            this.effect = effect;
            this.itemType = itemType;
            this.quantity = quantity;
            this.imageUrl = imageUrl;
            this.isUsable = isUsable;
            this.useFormula = useFormula;
            this.customProperties = new Dictionary<string, string>();
        }

        public virtual Item Clone()
        {
            var clone = new Item
            {
                Id = Guid.NewGuid().ToString(),
                Name = this.Name,
                Description = this.Description,
                Effect = this.Effect,
                ItemType = this.ItemType,
                Quantity = this.Quantity,
                MaxStack = this.MaxStack,
                ImageUrl = this.ImageUrl,
                Weight = this.Weight,
                Rarity = this.Rarity,
                IsUsable = this.IsUsable,
                UseFormula = this.UseFormula,
                CustomProperties = new Dictionary<string, string>(this.CustomProperties)
            };
            return clone;
        }

        public static FormulaResult EvaluateUseFormula(string formula)
        {
            if (string.IsNullOrWhiteSpace(formula))
            {
                return new FormulaResult { Success = false, Total = 0, Details = string.Empty, Formula = string.Empty };
            }

            var clean = formula.Trim();

            if (int.TryParse(clean, out int flatVal))
            {
                return new FormulaResult
                {
                    Success = true,
                    Total = flatVal,
                    Details = $"{flatVal}",
                    Formula = clean
                };
            }

            var regex = new Regex(@"(?<sign>[+-]?)\s*(?:(?<count>\d+)?d(?<sides>\d+)|(?<flat>\d+))", RegexOptions.IgnoreCase);
            var matches = regex.Matches(clean);

            if (matches.Count > 0)
            {
                int total = 0;
                var detailsParts = new List<string>();
                var rand = new Random();

                foreach (Match match in matches)
                {
                    if (match.Length == 0) continue;
                    var signStr = match.Groups["sign"].Value;
                    int sign = signStr == "-" ? -1 : 1;

                    if (match.Groups["sides"].Success)
                    {
                        int count = match.Groups["count"].Success && int.TryParse(match.Groups["count"].Value, out int c) && c > 0 ? c : 1;
                        int sides = int.TryParse(match.Groups["sides"].Value, out int s) && s > 0 ? s : 6;

                        var rolls = new List<int>();
                        int diceSum = 0;
                        for (int i = 0; i < count; i++)
                        {
                            int r = rand.Next(1, sides + 1);
                            rolls.Add(r);
                            diceSum += r;
                        }

                        total += sign * diceSum;
                        string signPrefix = sign < 0 ? "-" : (detailsParts.Count > 0 ? "+" : "");
                        string rollsStr = rolls.Count > 1 ? $"[{string.Join(", ", rolls)}]" : rolls[0].ToString();
                        detailsParts.Add($"{signPrefix}{rollsStr}");
                    }
                    else if (match.Groups["flat"].Success && int.TryParse(match.Groups["flat"].Value, out int flat))
                    {
                        total += sign * flat;
                        string signPrefix = sign < 0 ? "-" : (detailsParts.Count > 0 ? "+" : "");
                        detailsParts.Add($"{signPrefix}{flat}");
                    }
                }

                if (detailsParts.Count > 0)
                {
                    string detailStr = $"{clean} -> {string.Join(" ", detailsParts)} = {total}";
                    return new FormulaResult
                    {
                        Success = true,
                        Total = total,
                        Details = detailStr,
                        Formula = clean
                    };
                }
            }

            return new FormulaResult { Success = false, Total = 0, Details = clean, Formula = clean };
        }

        internal ItemUseResult Use(CharacterSheet? sheet = null)
        {
            if (!IsUsable)
            {
                return new ItemUseResult
                {
                    Success = false,
                    ItemName = Name,
                    Message = "Item is not usable."
                };
            }

            if (Quantity <= 0)
            {
                return new ItemUseResult
                {
                    Success = false,
                    ItemName = Name,
                    Message = "Item quantity is 0."
                };
            }

            Quantity--;
            if (Quantity <= 0 && sheet != null)
            {
                sheet.RemoveItem(Id);
            }

            string resultText;
            int? formulaTotal = null;

            if (!string.IsNullOrWhiteSpace(UseFormula))
            {
                var eval = EvaluateUseFormula(UseFormula);
                if (eval.Success)
                {
                    formulaTotal = eval.Total;
                    resultText = !string.IsNullOrWhiteSpace(Effect)
                        ? $"Used {Name}: {eval.Details} ({Effect})"
                        : $"Used {Name}: {eval.Details}";
                }
                else
                {
                    resultText = !string.IsNullOrWhiteSpace(Effect)
                        ? $"Used {Name} ({UseFormula}): {Effect}"
                        : $"Used {Name} ({UseFormula})";
                }
            }
            else if (!string.IsNullOrWhiteSpace(Effect))
            {
                resultText = $"Used {Name}: {Effect}";
            }
            else
            {
                resultText = $"Used {Name}";
            }

            try
            {
                XivChatEntry chatEntry = new XivChatEntry
                {
                    Message = resultText,
                    Type = XivChatType.Echo
                };
                Messages.SendMessage(chatEntry);
            }
            catch
            {
                Plugin.Log?.Information(resultText);
            }

            return new ItemUseResult
            {
                Success = true,
                ItemName = Name,
                Message = resultText,
                FormulaResult = formulaTotal,
                RemainingQuantity = Quantity
            };
        }

        public string ToJson()
        {
            try
            {
                return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, $"Error serializing item '{Name}' to JSON");
                return "{}";
            }
        }

        public static Item? FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JsonSerializer.Deserialize<Item>(json);
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, "Error deserializing item from JSON");
                return null;
            }
        }

        public static List<Item> ImportFromJson(string json)
        {
            var list = new List<Item>();
            if (string.IsNullOrWhiteSpace(json)) return list;

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            var trimmed = json.Trim();

            try
            {
                if (trimmed.StartsWith("["))
                {
                    var arrayItems = JsonSerializer.Deserialize<List<Item>>(trimmed, options);
                    if (arrayItems != null)
                    {
                        list.AddRange(arrayItems.Where(i => i != null));
                    }
                }
                else if (trimmed.StartsWith("{"))
                {
                    using var doc = JsonDocument.Parse(trimmed);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("characterInventory", out var invProp) && invProp.ValueKind == JsonValueKind.Array)
                    {
                        var inner = JsonSerializer.Deserialize<List<Item>>(invProp.GetRawText(), options);
                        if (inner != null) list.AddRange(inner.Where(i => i != null));
                    }
                    else if (root.TryGetProperty("inventory", out var invProp2) && invProp2.ValueKind == JsonValueKind.Array)
                    {
                        var inner = JsonSerializer.Deserialize<List<Item>>(invProp2.GetRawText(), options);
                        if (inner != null) list.AddRange(inner.Where(i => i != null));
                    }
                    else if (root.TryGetProperty("items", out var itemsProp) && itemsProp.ValueKind == JsonValueKind.Array)
                    {
                        var inner = JsonSerializer.Deserialize<List<Item>>(itemsProp.GetRawText(), options);
                        if (inner != null) list.AddRange(inner.Where(i => i != null));
                    }
                    else
                    {
                        var single = JsonSerializer.Deserialize<Item>(trimmed, options);
                        if (single != null && !string.IsNullOrWhiteSpace(single.Name))
                        {
                            list.Add(single);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, $"Error parsing item json: {ex.Message}");
            }

            foreach (var item in list)
            {
                if (string.IsNullOrWhiteSpace(item.Id))
                {
                    item.Id = Guid.NewGuid().ToString();
                }
                if (string.IsNullOrWhiteSpace(item.Name))
                {
                    item.Name = "Imported Item";
                }
                if (item.Quantity < 1) item.Quantity = 1;
                if (item.MaxStack < 1) item.MaxStack = 99;
                if (string.IsNullOrWhiteSpace(item.ItemType)) item.ItemType = "Miscellaneous";
                if (string.IsNullOrWhiteSpace(item.Rarity)) item.Rarity = "Common";
                item.CustomProperties ??= new Dictionary<string, string>();
            }

            return list;
        }

        public static bool TryImportFromJson(string json, out List<Item> items, out string error)
        {
            items = new List<Item>();
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "Input text is empty.";
                return false;
            }

            try
            {
                items = ImportFromJson(json);
                if (items.Count == 0)
                {
                    error = "No valid items found in JSON.";
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, $"Exception in TryImportFromJson: {ex.Message}");
                error = ex.Message;
                return false;
            }
        }
    }
}
