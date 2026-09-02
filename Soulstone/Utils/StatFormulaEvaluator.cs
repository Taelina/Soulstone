using Soulstone.Datamodels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Soulstone.Utils
{
    internal static class StatFormulaEvaluator
    {
        private static readonly Dictionary<string, string> AttributeAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            { "STR", "Strength" },
            { "DEX", "Dexterity" },
            { "CON", "Constitution" },
            { "INT", "Intelligence" },
            { "WIS", "Wisdom" },
            { "CHA", "Charisma" },
            { "AGI", "Agility" },
            { "VIT", "Vitality" },
            { "PER", "Perception" },
            { "WIL", "Willpower" },
            { "END", "Endurance" },
            { "HP", "Health" },
            { "MP", "Mana" },
            { "SP", "Stamina" }
        };

        private static readonly HashSet<string> KnownFunctions = new(StringComparer.OrdinalIgnoreCase)
        {
            "min", "max", "clamp", "floor", "ceil", "ceiling", "round", "abs", "sqrt", "mod"
        };

        public static double Evaluate(
            string formula,
            CharacterSheet? sheet = null,
            DiceSystem? diceSystem = null,
            IDictionary<string, double>? customVariables = null)
        {
            if (string.IsNullOrWhiteSpace(formula))
                return 0;

            var tokens = Tokenize(formula);
            if (tokens.Count == 0)
                return 0;

            var parser = new Parser(tokens, sheet, diceSystem, customVariables);
            return parser.Parse();
        }

        public static int EvaluateToInt(
            string formula,
            CharacterSheet? sheet = null,
            DiceSystem? diceSystem = null,
            int defaultValue = 0,
            IDictionary<string, double>? customVariables = null)
        {
            if (string.IsNullOrWhiteSpace(formula))
                return defaultValue;

            try
            {
                double result = Evaluate(formula, sheet, diceSystem, customVariables);
                if (double.IsNaN(result) || double.IsInfinity(result))
                    return defaultValue;

                return (int)Math.Round(result);
            }
            catch
            {
                return defaultValue;
            }
        }

        public static bool TryEvaluate(
            string formula,
            CharacterSheet? sheet,
            DiceSystem? diceSystem,
            out double result,
            out string? errorMessage,
            IDictionary<string, double>? customVariables = null)
        {
            result = 0;
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(formula))
            {
                errorMessage = "Formula is empty.";
                return false;
            }

            try
            {
                var tokens = Tokenize(formula);
                var parser = new Parser(tokens, sheet, diceSystem, customVariables);
                result = parser.Parse();

                if (double.IsNaN(result) || double.IsInfinity(result))
                {
                    errorMessage = "Formula evaluated to an invalid numeric value (NaN or Infinity).";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        public static List<string> ExtractVariables(string formula)
        {
            var vars = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(formula))
                return vars.ToList();

            try
            {
                var tokens = Tokenize(formula);
                for (int i = 0; i < tokens.Count; i++)
                {
                    var t = tokens[i];
                    if (t.Type == TokenType.Identifier && !KnownFunctions.Contains(t.Text))
                    {
                        // Check if it's followed by '(' -> function call
                        if (i + 1 < tokens.Count && tokens[i + 1].Type == TokenType.LParen)
                            continue;

                        vars.Add(t.Text);
                    }
                }
            }
            catch
            {
                // In case of tokenization issues, fallback regex
                var matches = Regex.Matches(formula, @"\b[a-zA-Z_][a-zA-Z0-9_]*\b");
                foreach (Match m in matches)
                {
                    if (!KnownFunctions.Contains(m.Value) && !Regex.IsMatch(m.Value, @"^\d*d\d+$", RegexOptions.IgnoreCase))
                    {
                        vars.Add(m.Value);
                    }
                }
            }

            return vars.ToList();
        }

        public static double ResolveStatValue(
            string statName,
            CharacterSheet? sheet,
            DiceSystem? diceSystem = null,
            IDictionary<string, double>? customVariables = null)
        {
            if (string.IsNullOrWhiteSpace(statName))
                return 0;

            string cleanName = statName.Trim(' ', '[', ']', '\'', '"');

            if (customVariables != null && customVariables.TryGetValue(cleanName, out double customVal))
            {
                return customVal;
            }

            if (sheet == null)
                return 0;

            // Character level
            if (cleanName.Equals("Level", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("CharacterLevel", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("Lvl", StringComparison.OrdinalIgnoreCase))
            {
                return sheet.CharacterLevel;
            }

            // Character Attributes
            if (sheet.CharacterAttributes != null)
            {
                var attrKey = FindMatchingKey(sheet.CharacterAttributes.Keys, cleanName);
                if (attrKey != null)
                {
                    return sheet.GetEffectiveAttributeValue(attrKey);
                }

                // Check by Attribute.Name
                var attrEntry = sheet.CharacterAttributes.FirstOrDefault(kv =>
                    string.Equals(kv.Value.Name, cleanName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Value.Name.Replace(" ", ""), cleanName.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(attrEntry.Key))
                {
                    return sheet.GetEffectiveAttributeValue(attrEntry.Key);
                }
            }

            // Character Skills
            if (sheet.CharacterSkills != null)
            {
                var skillKey = FindMatchingKey(sheet.CharacterSkills.Keys, cleanName);
                if (skillKey != null)
                {
                    return sheet.GetEffectiveSkillTotal(skillKey, diceSystem);
                }

                var skillEntry = sheet.CharacterSkills.FirstOrDefault(kv =>
                    string.Equals(kv.Value.SkillName, cleanName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Value.SkillName.Replace(" ", ""), cleanName.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(skillEntry.Key))
                {
                    return sheet.GetEffectiveSkillTotal(skillEntry.Key, diceSystem);
                }
            }

            // Character Abilities
            if (sheet.CharacterAbilities != null)
            {
                var abilityKey = FindMatchingKey(sheet.CharacterAbilities.Keys, cleanName);
                if (abilityKey != null)
                {
                    return sheet.GetEffectiveAbilityModifier(abilityKey);
                }

                var abilityEntry = sheet.CharacterAbilities.FirstOrDefault(kv =>
                    string.Equals(kv.Value.AbilityName, cleanName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Value.AbilityName.Replace(" ", ""), cleanName.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(abilityEntry.Key))
                {
                    return sheet.GetEffectiveAbilityModifier(abilityEntry.Key);
                }
            }

            // Character Resources
            if (sheet.CharacterResources != null)
            {
                var resKey = FindMatchingKey(sheet.CharacterResources.Keys, cleanName);
                if (resKey != null)
                {
                    var res = sheet.CharacterResources[resKey];
                    return res.MaxValue > 0 ? res.MaxValue : res.CurrentValue;
                }
            }

            // Attribute Aliases (STR -> Strength, etc.)
            if (AttributeAliases.TryGetValue(cleanName, out var canonicalName))
            {
                return ResolveStatValue(canonicalName, sheet, diceSystem, customVariables);
            }

            return 0;
        }

        private static string? FindMatchingKey(IEnumerable<string> keys, string target)
        {
            foreach (var k in keys)
            {
                if (string.Equals(k, target, StringComparison.OrdinalIgnoreCase))
                    return k;
            }

            string targetNoSpaces = target.Replace(" ", "").Replace("_", "");
            foreach (var k in keys)
            {
                string keyNoSpaces = k.Replace(" ", "").Replace("_", "");
                if (string.Equals(keyNoSpaces, targetNoSpaces, StringComparison.OrdinalIgnoreCase))
                    return k;
            }

            return null;
        }

        private enum TokenType
        {
            Number,
            Identifier,
            Dice,
            Plus,
            Minus,
            Multiply,
            Divide,
            Modulo,
            Power,
            LParen,
            RParen,
            Comma,
            End
        }

        private class Token
        {
            public TokenType Type { get; set; }
            public string Text { get; set; } = string.Empty;
            public double NumberValue { get; set; }
            public int DiceCount { get; set; }
            public int DiceSides { get; set; }

            public override string ToString() => $"{Type}: {Text}";
        }

        private static List<Token> Tokenize(string input)
        {
            var tokens = new List<Token>();
            int i = 0;
            int len = input.Length;

            while (i < len)
            {
                char c = input[i];

                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }

                if (c == '+') { tokens.Add(new Token { Type = TokenType.Plus, Text = "+" }); i++; }
                else if (c == '-') { tokens.Add(new Token { Type = TokenType.Minus, Text = "-" }); i++; }
                else if (c == '*') { tokens.Add(new Token { Type = TokenType.Multiply, Text = "*" }); i++; }
                else if (c == '/') { tokens.Add(new Token { Type = TokenType.Divide, Text = "/" }); i++; }
                else if (c == '%') { tokens.Add(new Token { Type = TokenType.Modulo, Text = "%" }); i++; }
                else if (c == '^') { tokens.Add(new Token { Type = TokenType.Power, Text = "^" }); i++; }
                else if (c == '(') { tokens.Add(new Token { Type = TokenType.LParen, Text = "(" }); i++; }
                else if (c == ')') { tokens.Add(new Token { Type = TokenType.RParen, Text = ")" }); i++; }
                else if (c == ',') { tokens.Add(new Token { Type = TokenType.Comma, Text = "," }); i++; }
                else if (c == '[' || c == '\'' || c == '"')
                {
                    char closeChar = c == '[' ? ']' : c;
                    int start = ++i;
                    while (i < len && input[i] != closeChar) i++;
                    string id = input.Substring(start, i - start);
                    if (i < len) i++; // skip close char
                    tokens.Add(new Token { Type = TokenType.Identifier, Text = id });
                }
                else if (char.IsDigit(c) || (c == '.' && i + 1 < len && char.IsDigit(input[i + 1])))
                {
                    int start = i;
                    bool hasDot = c == '.';
                    i++;
                    while (i < len && (char.IsDigit(input[i]) || (!hasDot && input[i] == '.')))
                    {
                        if (input[i] == '.') hasDot = true;
                        i++;
                    }

                    // Check if it's a dice roll like 2d6 or 1d20
                    if (i < len && (input[i] == 'd' || input[i] == 'D') && i + 1 < len && char.IsDigit(input[i + 1]))
                    {
                        string countStr = input.Substring(start, i - start);
                        int count = int.TryParse(countStr, out int cnt) ? cnt : 1;
                        i++; // skip 'd'
                        int sideStart = i;
                        while (i < len && char.IsDigit(input[i])) i++;
                        string sidesStr = input.Substring(sideStart, i - sideStart);
                        int sides = int.TryParse(sidesStr, out int s) ? s : 6;

                        tokens.Add(new Token
                        {
                            Type = TokenType.Dice,
                            Text = $"{count}d{sides}",
                            DiceCount = count,
                            DiceSides = sides
                        });
                    }
                    else
                    {
                        string numStr = input.Substring(start, i - start);
                        double val = double.Parse(numStr, CultureInfo.InvariantCulture);
                        tokens.Add(new Token { Type = TokenType.Number, Text = numStr, NumberValue = val });
                    }
                }
                else if (c == 'd' || c == 'D')
                {
                    // Check if standalone d20, d6 etc.
                    if (i + 1 < len && char.IsDigit(input[i + 1]) && (tokens.Count == 0 || tokens[^1].Type == TokenType.Plus || tokens[^1].Type == TokenType.Minus || tokens[^1].Type == TokenType.Multiply || tokens[^1].Type == TokenType.Divide || tokens[^1].Type == TokenType.LParen || tokens[^1].Type == TokenType.Comma))
                    {
                        i++; // skip 'd'
                        int sideStart = i;
                        while (i < len && char.IsDigit(input[i])) i++;
                        string sidesStr = input.Substring(sideStart, i - sideStart);
                        int sides = int.TryParse(sidesStr, out int s) ? s : 6;

                        tokens.Add(new Token
                        {
                            Type = TokenType.Dice,
                            Text = $"1d{sides}",
                            DiceCount = 1,
                            DiceSides = sides
                        });
                    }
                    else
                    {
                        int start = i;
                        while (i < len && (char.IsLetterOrDigit(input[i]) || input[i] == '_')) i++;
                        string id = input.Substring(start, i - start);
                        tokens.Add(new Token { Type = TokenType.Identifier, Text = id });
                    }
                }
                else if (char.IsLetter(c) || c == '_')
                {
                    int start = i;
                    while (i < len && (char.IsLetterOrDigit(input[i]) || input[i] == '_')) i++;
                    string id = input.Substring(start, i - start);
                    tokens.Add(new Token { Type = TokenType.Identifier, Text = id });
                }
                else
                {
                    throw new FormatException($"Unexpected character '{c}' at position {i} in formula: {input}");
                }
            }

            tokens.Add(new Token { Type = TokenType.End, Text = string.Empty });
            return tokens;
        }

        private class Parser
        {
            private readonly List<Token> tokens;
            private readonly CharacterSheet? sheet;
            private readonly DiceSystem? diceSystem;
            private readonly IDictionary<string, double>? customVariables;
            private int pos = 0;
            private static readonly Random Rng = new();

            public Parser(
                List<Token> tokens,
                CharacterSheet? sheet,
                DiceSystem? diceSystem,
                IDictionary<string, double>? customVariables)
            {
                this.tokens = tokens;
                this.sheet = sheet;
                this.diceSystem = diceSystem;
                this.customVariables = customVariables;
            }

            private Token Current => pos < tokens.Count ? tokens[pos] : tokens[^1];

            private Token Consume(TokenType expected)
            {
                var token = Current;
                if (token.Type != expected)
                {
                    throw new FormatException($"Expected token '{expected}' but found '{token.Type}' ('{token.Text}') at position {pos}.");
                }
                pos++;
                return token;
            }

            public double Parse()
            {
                double result = ParseAdditive();
                if (Current.Type != TokenType.End)
                {
                    throw new FormatException($"Unexpected token '{Current.Text}' after expression end.");
                }
                return result;
            }

            private double ParseAdditive()
            {
                double left = ParseMultiplicative();

                while (Current.Type == TokenType.Plus || Current.Type == TokenType.Minus)
                {
                    var op = Current.Type;
                    pos++;
                    double right = ParseMultiplicative();

                    if (op == TokenType.Plus)
                        left += right;
                    else
                        left -= right;
                }

                return left;
            }

            private double ParseMultiplicative()
            {
                double left = ParsePower();

                while (Current.Type == TokenType.Multiply || Current.Type == TokenType.Divide || Current.Type == TokenType.Modulo)
                {
                    var op = Current.Type;
                    pos++;
                    double right = ParsePower();

                    if (op == TokenType.Multiply)
                    {
                        left *= right;
                    }
                    else if (op == TokenType.Divide)
                    {
                        if (Math.Abs(right) < double.Epsilon)
                            throw new DivideByZeroException("Division by zero in formula evaluation.");
                        left /= right;
                    }
                    else
                    {
                        if (Math.Abs(right) < double.Epsilon)
                            throw new DivideByZeroException("Modulo by zero in formula evaluation.");
                        left %= right;
                    }
                }

                return left;
            }

            private double ParsePower()
            {
                double left = ParseUnary();

                if (Current.Type == TokenType.Power)
                {
                    pos++;
                    double right = ParsePower(); // right-associative
                    return Math.Pow(left, right);
                }

                return left;
            }

            private double ParseUnary()
            {
                if (Current.Type == TokenType.Plus)
                {
                    pos++;
                    return ParseUnary();
                }

                if (Current.Type == TokenType.Minus)
                {
                    pos++;
                    return -ParseUnary();
                }

                return ParsePrimary();
            }

            private double ParsePrimary()
            {
                var token = Current;

                if (token.Type == TokenType.Number)
                {
                    pos++;
                    return token.NumberValue;
                }

                if (token.Type == TokenType.Dice)
                {
                    pos++;
                    int sum = 0;
                    for (int i = 0; i < token.DiceCount; i++)
                    {
                        sum += Rng.Next(1, Math.Max(1, token.DiceSides) + 1);
                    }
                    return sum;
                }

                if (token.Type == TokenType.LParen)
                {
                    pos++;
                    double value = ParseAdditive();
                    Consume(TokenType.RParen);
                    return value;
                }

                if (token.Type == TokenType.Identifier)
                {
                    string id = token.Text;
                    pos++;

                    // Check if function call
                    if (Current.Type == TokenType.LParen)
                    {
                        pos++;
                        var args = new List<double>();
                        if (Current.Type != TokenType.RParen)
                        {
                            args.Add(ParseAdditive());
                            while (Current.Type == TokenType.Comma)
                            {
                                pos++;
                                args.Add(ParseAdditive());
                            }
                        }
                        Consume(TokenType.RParen);
                        return EvaluateFunction(id, args);
                    }

                    // Stat or variable resolution
                    return ResolveStatValue(id, sheet, diceSystem, customVariables);
                }

                throw new FormatException($"Unexpected token '{token.Text}' of type '{token.Type}' in formula.");
            }

            private double EvaluateFunction(string funcName, List<double> args)
            {
                string name = funcName.ToLowerInvariant();
                switch (name)
                {
                    case "min":
                        if (args.Count == 0) throw new ArgumentException("min() requires at least one argument.");
                        return args.Min();

                    case "max":
                        if (args.Count == 0) throw new ArgumentException("max() requires at least one argument.");
                        return args.Max();

                    case "clamp":
                        if (args.Count != 3) throw new ArgumentException("clamp() requires 3 arguments: clamp(value, min, max).");
                        return Math.Clamp(args[0], args[1], args[2]);

                    case "floor":
                        if (args.Count != 1) throw new ArgumentException("floor() requires 1 argument: floor(value).");
                        return Math.Floor(args[0]);

                    case "ceil":
                    case "ceiling":
                        if (args.Count != 1) throw new ArgumentException("ceil() requires 1 argument: ceil(value).");
                        return Math.Ceiling(args[0]);

                    case "round":
                        if (args.Count == 1) return Math.Round(args[0]);
                        if (args.Count == 2) return Math.Round(args[0], (int)args[1]);
                        throw new ArgumentException("round() requires 1 or 2 arguments.");

                    case "abs":
                        if (args.Count != 1) throw new ArgumentException("abs() requires 1 argument: abs(value).");
                        return Math.Abs(args[0]);

                    case "sqrt":
                        if (args.Count != 1) throw new ArgumentException("sqrt() requires 1 argument: sqrt(value).");
                        if (args[0] < 0) throw new ArgumentException("sqrt() argument cannot be negative.");
                        return Math.Sqrt(args[0]);

                    case "mod":
                        if (args.Count != 2) throw new ArgumentException("mod() requires 2 arguments: mod(a, b).");
                        if (Math.Abs(args[1]) < double.Epsilon) throw new DivideByZeroException("mod() divisor cannot be zero.");
                        return args[0] % args[1];

                    default:
                        throw new NotSupportedException($"Unknown function '{funcName}()'.");
                }
            }
        }
    }
}
