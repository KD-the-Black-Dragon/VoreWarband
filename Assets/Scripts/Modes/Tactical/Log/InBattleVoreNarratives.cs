// Added by the VoreWar mercenary-company conversion fork.
// 2026-11-11: Load and validate editable, context- and method-specific in-battle vore narrative catalogs.
// See CHANGES.md for the fork change record and License.txt for GPL-3.0 terms.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine;

internal static class InBattleVoreNarratives
{
    const string NarrativeFile = "Narrative/InBattleVoreNarratives.json";
    const string SynonymFile = "Narrative/NarrativeSynonyms.json";
    static readonly string[] ContextIds = { "companyPredator", "enemySapientPredator", "enemyAnimalPredator" };
    static readonly string[] PhysicalMethods = { "oral", "womb", "cock", "breast", "tail", "anal", "bladder" };
    static readonly string[] SharedStages =
    {
        "action", "nearDigestion", "digestionProgress", "digestionComplete", "absorptionComplete",
        "escape", "freed", "regurgitated", "partialEscape", "greatEscapeKeep", "greatEscapeFlee",
        "manualRegurgulation", "curseExpires", "diminishmentExpires", "bellyRub", "birth", "resist"
    };
    static readonly Dictionary<string, string[]> RequiredStages = BuildRequiredStages();
    static readonly Regex TokenPattern = new Regex("\\{([^{}]+)\\}", RegexOptions.Compiled);
    static readonly System.Random TextRandom = new System.Random();
    static readonly object RandomLock = new object();

    public sealed class NarrativeDocument
    {
        public int version;
        public Dictionary<string, Dictionary<string, Dictionary<string, string[]>>> contexts;
    }

    public sealed class SynonymDocument
    {
        public int version;
        public Dictionary<string, string[]> synonyms;
    }

    static NarrativeDocument narrative;
    static Dictionary<string, string[]> synonyms;
    static readonly HashSet<string> InvalidEntries = new HashSet<string>(StringComparer.Ordinal);
    static bool loaded;

    static Dictionary<string, string[]> BuildRequiredStages()
    {
        var stages = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (string method in PhysicalMethods)
        {
            var methodStages = new List<string>(SharedStages);
            if (method == "breast") methodStages.AddRange(new[] { "breastRub", "suckle", "suckleFail" });
            if (method == "tail") methodStages.Add("tailRub");
            if (method == "cock") methodStages.AddRange(new[] { "ballMassage", "suckle", "suckleFail" });
            if (method == "cock") methodStages.Add("transferFail");
            if (method == "oral" || method == "womb") methodStages.AddRange(new[] { "transferSuccess", "kissTransfer", "voreStealSuccess" });
            if (method != "anal") methodStages.Add("voreStealFail");
            stages.Add(method, methodStages.ToArray());
        }
        stages.Add("breastFeeding", new[] { "feed" });
        stages.Add("fluidFeeding", new[] { "feedCum" });
        return stages;
    }

    internal static void Load()
    {
        if (loaded) return;
        loaded = true;
        var errors = new List<string>();
        narrative = Read<NarrativeDocument>(NarrativeFile, "in-battle vore narrative", errors);
        if (narrative == null) narrative = new NarrativeDocument();
        SynonymDocument synonymDocument = Read<SynonymDocument>(SynonymFile, "narrative synonym", errors);
        synonyms = synonymDocument == null ? null : synonymDocument.synonyms;
        if (synonyms == null) synonyms = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (narrative.version != 1) errors.Add("Narrative file version must be 1 (found " + narrative.version + ").");
        if (synonymDocument == null || synonymDocument.version != 1)
            errors.Add("Synonym file version must be 1 (found " + (synonymDocument == null ? 0 : synonymDocument.version) + ").");
        if (narrative.contexts == null) narrative.contexts = new Dictionary<string, Dictionary<string, Dictionary<string, string[]>>>(StringComparer.Ordinal);
        if (synonyms.Count == 0) errors.Add("Synonym file contains no synonym groups.");
        Validate(errors);
        if (errors.Count > 0)
            Debug.LogWarning("In-battle vore narrative catalog needs attention. Missing or invalid entries are listed below; the game will show keyed technical fallbacks for them until corrected:\n - " + string.Join("\n - ", errors.Distinct().ToArray()));
    }

    static T Read<T>(string relativePath, string label, List<string> errors) where T : class
    {
        string path = Path.Combine(Application.streamingAssetsPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        try
        {
            if (!File.Exists(path))
            {
                errors.Add("Missing " + label + " file: " + relativePath);
                return null;
            }
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
        }
        catch (Exception exception)
        {
            errors.Add("Could not read " + label + " file " + relativePath + ": " + exception.Message);
            return null;
        }
    }

    static void Validate(List<string> errors)
    {
        foreach (string context in ContextIds)
        {
            Dictionary<string, Dictionary<string, string[]>> contextData;
            if (!narrative.contexts.TryGetValue(context, out contextData) || contextData == null)
            {
                foreach (var method in RequiredStages)
                    foreach (string stage in method.Value)
                        errors.Add(context + "/" + method.Key + "/" + stage + " is missing.");
                continue;
            }

            foreach (var method in RequiredStages)
            {
                Dictionary<string, string[]> methodData;
                if (!contextData.TryGetValue(method.Key, out methodData) || methodData == null)
                {
                    foreach (string stage in method.Value)
                        errors.Add(context + "/" + method.Key + "/" + stage + " is missing.");
                    continue;
                }
                foreach (string stage in method.Value)
                {
                    string[] lines;
                    string key = Key(context, method.Key, stage);
                    if (!methodData.TryGetValue(stage, out lines) || lines == null || !lines.Any(s => !string.IsNullOrWhiteSpace(s)))
                    {
                        errors.Add(key + " has no narrative lines.");
                        continue;
                    }
                    foreach (string line in lines.Where(s => !string.IsNullOrWhiteSpace(s)))
                    {
                        foreach (Match match in TokenPattern.Matches(NormalizeLegacyTokens(line)))
                        {
                            string token = match.Groups[1].Value;
                            if (!IsKnownToken(token))
                            {
                                InvalidEntries.Add(key);
                                errors.Add(key + " uses unknown placeholder {" + token + "}.");
                            }
                        }
                    }
                }
            }
        }

        foreach (var group in synonyms)
            if (group.Value == null || group.Value.Length == 0 || group.Value.Any(string.IsNullOrWhiteSpace))
                errors.Add("Synonym group " + group.Key + " must contain one or more non-empty values.");
    }

    static bool IsKnownToken(string token)
    {
        string lower = token.ToLowerInvariant();
        string[] synonymValues;
        if (synonyms.TryGetValue(lower, out synonymValues) && synonymValues != null && synonymValues.Any(s => !string.IsNullOrWhiteSpace(s))) return true;
        if (lower == "name" || lower == "he" || lower == "him" || lower == "his" || lower == "himself") return true;
        string[] roles = { "actor", "target", "prey", "contained", "donor", "recipient", "predator" };
        string[] parts = lower.Split('.');
        if (parts.Length == 1) return roles.Contains(parts[0]);
        return parts.Length == 2 && roles.Contains(parts[0]) && (parts[1] == "name" || parts[1] == "display" || parts[1] == "articlename" ||
            parts[1] == "he" || parts[1] == "subject" || parts[1] == "him" || parts[1] == "object" ||
            parts[1] == "his" || parts[1] == "possessive" || parts[1] == "reflexive" || parts[1] == "himself");
    }

    internal static bool TryGetNarrative(TacticalMessageLog.EventLog action, out string text)
    {
        text = null;
        if (action == null) return false;
        string method, stage;
        if (!TryGetMethodAndStage(action, out method, out stage)) return false;
        Load();

        string context = GetContext(action.Unit);
        string key = Key(context, method, stage);
        if (action.NarrativeText == null)
            action.NarrativeText = BuildText(key, action);
        text = action.NarrativeText;
        return true;
    }

    static string BuildText(string key, TacticalMessageLog.EventLog action)
    {
        string[] parts = key.Split('/');
        string[] lines = null;
        Dictionary<string, Dictionary<string, string[]>> contextData;
        Dictionary<string, string[]> methodData;
        if (narrative != null && narrative.contexts != null &&
            narrative.contexts.TryGetValue(parts[0], out contextData) && contextData != null &&
            contextData.TryGetValue(parts[1], out methodData) && methodData != null)
            methodData.TryGetValue(parts[2], out lines);
        if (InvalidEntries.Contains(key) || lines == null || lines.Length == 0)
            return "[Missing vore narrative: " + key + "]";
        var choices = lines.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        if (choices.Length == 0) return "[Missing vore narrative: " + key + "]";
        int index;
        lock (RandomLock) index = TextRandom.Next(choices.Length);
        return Expand(choices[index], action);
    }

    static string Expand(string template, TacticalMessageLog.EventLog action)
    {
        string normalized = NormalizeLegacyTokens(template);
        return TokenPattern.Replace(normalized, match =>
        {
            string token = match.Groups[1].Value;
            string value;
            if (TryResolveToken(token, action, out value))
                return char.IsUpper(token[0]) ? Capitalize(value) : value;
            string[] values;
            if (synonyms.TryGetValue(token.ToLowerInvariant(), out values) && values != null)
            {
                values = values.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                if (values.Length == 0) return "[Missing narrative token: " + token + "]";
                int index;
                lock (RandomLock) index = TextRandom.Next(values.Length);
                return char.IsUpper(token[0]) ? Capitalize(values[index]) : values[index];
            }
            return "[Missing narrative token: " + token + "]";
        });
    }

    static string NormalizeLegacyTokens(string text)
    {
        return text.Replace("[Name]", "{Name}").Replace("[name]", "{name}")
            .Replace("[He]", "{He}").Replace("[he]", "{he}").Replace("[him]", "{him}")
            .Replace("[His]", "{His}").Replace("[his]", "{his}").Replace("[himself]", "{himself}");
    }

    static bool TryResolveToken(string token, TacticalMessageLog.EventLog action, out string value)
    {
        value = null;
        string lower = token.ToLowerInvariant();
        if (lower == "name" || lower == "he" || lower == "him" || lower == "his" || lower == "himself")
        {
            value = UnitToken(action == null ? null : action.Unit, lower);
            return true;
        }
        string[] parts = lower.Split('.');
        if (parts.Length == 1 && new[] { "actor", "target", "prey", "contained", "donor", "recipient", "predator" }.Contains(lower))
        {
            Unit roleUnit = RoleUnit(action, lower);
            value = roleUnit == null ? "" : ArticleName(roleUnit);
            return true;
        }
        if (parts.Length != 2) return false;
        Unit unit = RoleUnit(action, parts[0]);
        if (unit == null) { value = ""; return true; }
        value = UnitToken(unit, parts[1]);
        return value != null;
    }

    static Unit RoleUnit(TacticalMessageLog.EventLog action, string role)
    {
        if (action == null) return null;
        if (role == "actor" || role == "predator") return action.Unit;
        if (role == "donor")
            return action.Type == TacticalMessageLog.MessageLogEvent.VoreStealSuccess || action.Type == TacticalMessageLog.MessageLogEvent.VoreStealFail ? action.Target : action.Unit;
        if (role == "recipient")
            return action.Type == TacticalMessageLog.MessageLogEvent.VoreStealSuccess || action.Type == TacticalMessageLog.MessageLogEvent.VoreStealFail ? action.Unit : action.Target;
        if (role == "target") return action.Target;
        if (role == "prey") return action.Prey ?? action.Target;
        if (role == "contained") return action.Prey;
        return null;
    }

    static string UnitToken(Unit unit, string token)
    {
        if (token == "name") return StoredLogTexts.UnitName(unit);
        if (token == "display" || token == "articlename") return ArticleName(unit);
        if (token == "he" || token == "subject") return Pronoun(unit, 0, "they");
        if (token == "him" || token == "object") return Pronoun(unit, 1, "them");
        if (token == "his" || token == "possessive") return Pronoun(unit, 2, "their");
        if (token == "himself" || token == "reflexive") return Pronoun(unit, 4, "themself");
        return null;
    }

    static string Pronoun(Unit unit, int index, string fallback)
    {
        if (unit == null) return fallback;
        try { return unit.GetPronoun(index); }
        catch { return fallback; }
    }

    static string ArticleName(Unit unit)
    {
        string name = StoredLogTexts.UnitName(unit);
        if (unit == null || !IsGenericIdentifier(unit, name) || name.StartsWith("the ", StringComparison.OrdinalIgnoreCase)) return name;
        return "the " + name;
    }

    static bool IsGenericIdentifier(Unit unit, string name)
    {
        string species = SpeciesPolicy.DisplayName(unit.Race);
        if (string.Equals(name, species, StringComparison.OrdinalIgnoreCase)) return true;
        string prefix = species + " ";
        if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
        string archetype = name.Substring(prefix.Length);
        return archetype == "Warrior" || archetype == "Archer" || archetype == "Mage" ||
            archetype == "Warrior (Boss)" || archetype == "Archer (Boss)" || archetype == "Mage (Boss)";
    }

    static string GetContext(Unit predator)
    {
        GameManager manager = State.GameManager;
        if (manager != null && manager.PureTactical) return SpeciesContext(predator);
        if (manager != null && manager.CompanyBattle != null && predator != null)
        {
            CompanyCampaign campaign = manager.CompanyBattle.Campaign;
            if (campaign != null && campaign.Members != null && campaign.Characters != null)
                foreach (CompanyMember member in campaign.Members)
                {
                    CompanyCharacter character = campaign.Characters.FirstOrDefault(c => c.Id == member.CharacterId);
                    if (character != null && character.Unit == predator) return "companyPredator";
                }
            if (predator.Side == 0) return "companyPredator";
        }
        return SpeciesContext(predator);
    }

    static string SpeciesContext(Unit unit)
    {
        if (unit != null)
        {
            bool sapient = SpeciesPolicy.IsCurated(unit.Race) ? SpeciesPolicy.GetKind(unit.Race) == SpeciesKind.Sapient : !unit.HasTrait(Traits.Feral);
            return sapient ? "enemySapientPredator" : "enemyAnimalPredator";
        }
        return "enemySapientPredator";
    }

    static bool TryGetMethodAndStage(TacticalMessageLog.EventLog action, out string method, out string stage)
    {
        method = MethodForLocation(action.preyLocation);
        stage = null;
        switch (action.Type)
        {
            case TacticalMessageLog.MessageLogEvent.Devour: method = "oral"; stage = "action"; break;
            case TacticalMessageLog.MessageLogEvent.Unbirth: method = "womb"; stage = "action"; break;
            case TacticalMessageLog.MessageLogEvent.CockVore: method = "cock"; stage = "action"; break;
            case TacticalMessageLog.MessageLogEvent.BreastVore: method = "breast"; stage = "action"; break;
            case TacticalMessageLog.MessageLogEvent.TailVore: method = "tail"; stage = "action"; break;
            case TacticalMessageLog.MessageLogEvent.AnalVore: method = "anal"; stage = "action"; break;
            case TacticalMessageLog.MessageLogEvent.BladderVore: method = "bladder"; stage = "action"; break;
            case TacticalMessageLog.MessageLogEvent.BellyRub: stage = "bellyRub"; break;
            case TacticalMessageLog.MessageLogEvent.BreastRub: method = "breast"; stage = "breastRub"; break;
            case TacticalMessageLog.MessageLogEvent.TailRub: method = "tail"; stage = "tailRub"; break;
            case TacticalMessageLog.MessageLogEvent.BallMassage: method = "cock"; stage = "ballMassage"; break;
            case TacticalMessageLog.MessageLogEvent.Feed: method = "breastFeeding"; stage = "feed"; break;
            case TacticalMessageLog.MessageLogEvent.FeedCum: method = "fluidFeeding"; stage = "feedCum"; break;
            case TacticalMessageLog.MessageLogEvent.Suckle: stage = "suckle"; break;
            case TacticalMessageLog.MessageLogEvent.SuckleFail: stage = "suckleFail"; break;
            case TacticalMessageLog.MessageLogEvent.Birth: stage = "birth"; break;
            case TacticalMessageLog.MessageLogEvent.TransferFail: stage = "transferFail"; break;
            case TacticalMessageLog.MessageLogEvent.TransferSuccess: stage = "transferSuccess"; break;
            case TacticalMessageLog.MessageLogEvent.KissTransfer: stage = "kissTransfer"; break;
            case TacticalMessageLog.MessageLogEvent.VoreStealFail: method = MethodForLocation(action.oldLocation); stage = "voreStealFail"; break;
            case TacticalMessageLog.MessageLogEvent.VoreStealSuccess: stage = "voreStealSuccess"; break;
            case TacticalMessageLog.MessageLogEvent.Resist: stage = "resist"; break;
            case TacticalMessageLog.MessageLogEvent.Digest: stage = "digestionComplete"; break;
            case TacticalMessageLog.MessageLogEvent.Absorb: stage = "absorptionComplete"; break;
            case TacticalMessageLog.MessageLogEvent.Escape: stage = "escape"; break;
            case TacticalMessageLog.MessageLogEvent.Freed: stage = "freed"; break;
            case TacticalMessageLog.MessageLogEvent.Regurgitated: stage = "regurgitated"; break;
            case TacticalMessageLog.MessageLogEvent.LowHealth: stage = "nearDigestion"; break;
            case TacticalMessageLog.MessageLogEvent.RandomDigestion: stage = "digestionProgress"; break;
            case TacticalMessageLog.MessageLogEvent.PartialEscape: stage = "partialEscape"; break;
            case TacticalMessageLog.MessageLogEvent.GreatEscapeKeep: stage = "greatEscapeKeep"; break;
            case TacticalMessageLog.MessageLogEvent.GreatEscapeFlee: stage = "greatEscapeFlee"; break;
            case TacticalMessageLog.MessageLogEvent.ManualRegurgitation: stage = "manualRegurgulation"; break;
            case TacticalMessageLog.MessageLogEvent.CurseExpires: stage = "curseExpires"; break;
            case TacticalMessageLog.MessageLogEvent.DiminishmentExpires: stage = "diminishmentExpires"; break;
            default: return false;
        }
        return true;
    }

    static string MethodForLocation(PreyLocation location)
    {
        switch (location)
        {
            case PreyLocation.stomach:
            case PreyLocation.stomach2: return "oral";
            case PreyLocation.womb: return "womb";
            case PreyLocation.balls: return "cock";
            case PreyLocation.breasts:
            case PreyLocation.leftBreast:
            case PreyLocation.rightBreast: return "breast";
            case PreyLocation.tail: return "tail";
            case PreyLocation.anal: return "anal";
            case PreyLocation.bladder: return "bladder";
            default: return "oral";
        }
    }

    static string Key(string context, string method, string stage)
    { return context + "/" + method + "/" + stage; }

    static string Capitalize(string text)
    { return string.IsNullOrEmpty(text) ? text : text.Substring(0, 1).ToUpperInvariant() + text.Substring(1); }
}
