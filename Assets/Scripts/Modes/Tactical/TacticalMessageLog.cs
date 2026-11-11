// Modified for the VoreWar mercenary-company conversion.
// 2026-11-11: Resolve vore log lines from editable method/context JSON and persist each rendered line.
// 2026-11-08: Show native species weapon names in campaign battle logs while retaining generic shop names.
// 2026-10-30: Replace typed combat prose with neutral functional feedback; remove Human dummy context (spec 04B1).
// Upstream narrative contributions credited to Tatltuae are retained in this notice.
// See CHANGES.md for the fork change record and License.txt for GPL-3.0 terms.
// 2026-11-03: Implement four active stats and agreed combat rules (spec 29).
// 2026-11-04: Persist and label final opportunity-attack results in the combat log (spec 30).
using OdinSerializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class TacticalMessageLog
{
    [OdinSerialize]
    List<EventLog> events;

    public bool ShowOdds = false;
    public bool ShowHealing = true;
    public bool ShowSpells = true;
    public bool ShowMisses = true;
    public bool ShowWeaponCombat = true;
    public bool ShowInformational = true;
    public bool ShowPureFluff = true;

    public bool SimpleText = false;


    public TacticalMessageLog()
    {
        events = new List<EventLog>();
    }

    internal class EventLog
    {
        [OdinSerialize]
        internal MessageLogEvent Type;
        [OdinSerialize]
        internal float Odds;
        [OdinSerialize]
        internal Unit Target;
        [OdinSerialize]
        internal Unit Unit;
        [OdinSerialize]
        internal Unit Prey;
        [OdinSerialize]
        internal PreyLocation preyLocation;
        [OdinSerialize]
        internal PreyLocation oldLocation;
        [OdinSerialize]
        internal Weapon Weapon;
        [OdinSerialize]
        internal int Damage;
        [OdinSerialize]
        internal int Bonus;
        [OdinSerialize]
        internal int RebirthType;
        [OdinSerialize]
        internal string Message;
        [OdinSerialize]
        internal string Extra;
        [OdinSerialize] internal string WeaponResult;
        [OdinSerialize] internal bool Opportunity;
        [OdinSerialize] internal string NarrativeText;
    }




    class SpellLog : EventLog
    {
        [OdinSerialize]
        internal SpellTypes SpellType;
    }

    internal enum MessageLogEvent
    {
        Hit,
        Miss,
        Devour,
        Unbirth,
        CockVore,
        BreastVore,
        BellyRub,
        BreastRub,
        BallMassage,
        Feed,
        Birth,
        TransferFail,
        TransferSuccess,
        KissTransfer,
        Resist,
        Kill,
        Digest,
        Absorb,
        Escape,
        Freed,
        Regurgitated,
        Heal,
        NewTurn,
        LowHealth,
        FeedCum,
        RandomDigestion = 25, //Done because of a removed category
        Miscellaneous,
        PartialEscape,
        TailVore,
        AnalVore,
        Dazzle,
        Block,
        SpellHit,
        SpellMiss,
        SpellKill,
        CurseExpires,
        DiminishmentExpires,
        TailRub,
        Suckle,
        SuckleFail,
        VoreStealFail,
        VoreStealSuccess,
        GreatEscapeKeep,
        GreatEscapeFlee,
        ManualRegurgitation,
        BladderVore,
    }

    public void RefreshListing()
    {
        StringBuilder sb = new StringBuilder();
        var validEvents = events.Where(s => EventValid(s));
        List<EventLog> last200;

        if (validEvents.Count() > 200)
        {
            last200 = validEvents.ToList().GetRange(validEvents.Count() - 200, 200);
        }
        else
            last200 = validEvents.ToList();

        foreach (EventLog action in last200)
        {
            sb.AppendLine(EventDescription(action));
        }
        State.GameManager.TacticalMode.LogUI.Text.text = sb.ToString();
        State.GameManager.TacticalMode.LogUI.Text.transform.Translate(new Vector3(0, 30000, 0));
    }

    internal string DebugDump()
    {
        StringBuilder sb = new StringBuilder();

        foreach (EventLog action in events)
        {
            sb.AppendLine(EventDescription(action));
        }
        return sb.ToString();
    }

    bool EventValid(EventLog test)
    {
        switch (test.Type)
        {
            case MessageLogEvent.Heal:
                if (ShowHealing == false) return false; break;
            case MessageLogEvent.Miss:
            case MessageLogEvent.Block:
                if (ShowMisses == false || ShowWeaponCombat == false) return false; break;
            case MessageLogEvent.Resist:
            case MessageLogEvent.Dazzle:
            case MessageLogEvent.SpellMiss:
                if (ShowMisses == false || ShowSpells == false) return false; break;
            case MessageLogEvent.SpellHit:
                if (ShowSpells == false) return false; break;
            case MessageLogEvent.LowHealth:
            case MessageLogEvent.Absorb:
            case MessageLogEvent.NewTurn:
                if (ShowInformational == false) return false; break;
            case MessageLogEvent.RandomDigestion:
            case MessageLogEvent.GreatEscapeKeep:
                if (ShowPureFluff == false) return false; break;
            case MessageLogEvent.Hit:
                if (ShowWeaponCombat == false) return false; break;

        }
        return true;
    }

    public void Clear()
    {
        events.Clear();
        State.GameManager.TacticalMode.LogUI.Text.text = "";
    }



    void UpdateListing()
    {
        if (events.Count > 4000)
            events.RemoveRange(0, 400);
        if (State.GameManager.TacticalMode.turboMode)
            return;
        if (EventValid(events.Last()) == false)
            return;
        State.GameManager.TacticalMode.LogUI.Text.text += EventDescription(events.Last()) + "\n";
        if (State.GameManager.TacticalMode.LogUI.Text.text.Length > 10000)
        {
            State.GameManager.TacticalMode.LogUI.Text.text = State.GameManager.TacticalMode.LogUI.Text.text.Substring(1000);
        }
        State.GameManager.TacticalMode.TacticalLogUpdated = true;
    }

    private string EventDescription(EventLog action)
    {
        if (action == null)
            return "Battle event unavailable.";
        string narrativeText;
        if (InBattleVoreNarratives.TryGetNarrative(action, out narrativeText))
            return CompleteDescription(action, narrativeText);
        string actor = StoredLogTexts.UnitName(action.Unit);
        string target = StoredLogTexts.UnitName(action.Target);
        string weapon = action.Weapon == null ? "unarmed attack" : action.Unit != null && action.Unit.CompanyLoadout
            ? LogUtilities.GetWeaponTrueName(action.Weapon, action.Unit) : action.Weapon.Name;
        string message;
        switch (action.Type)
        {
            case MessageLogEvent.Hit:
                message = action.WeaponResult == "graze" ? $"{actor} grazed {target} with {weapon} for {action.Damage} damage." :
                    action.WeaponResult == "critical" ? $"{actor} critically hit {target} with {weapon} for {action.Damage} damage." :
                    $"{actor} hit {target} with {weapon} for {action.Damage} damage.";
                break;
            case MessageLogEvent.Miss:
                message = $"{actor} missed {target} with {weapon}.";
                break;
            case MessageLogEvent.Devour:
                message = StoredLogTexts.Format(StoredLogTexts.MessageTypes.SwallowMessages, action);
                break;
            case MessageLogEvent.Unbirth:
                message = StoredLogTexts.Format(StoredLogTexts.MessageTypes.UnbirthMessages, action);
                break;
            case MessageLogEvent.CockVore:
                message = StoredLogTexts.Format(StoredLogTexts.MessageTypes.CockVoreMessages, action);
                break;
            case MessageLogEvent.BreastVore:
                message = StoredLogTexts.Format(StoredLogTexts.MessageTypes.BreastVoreMessages, action);
                break;
            case MessageLogEvent.BellyRub:
                message = StoredLogTexts.Format(StoredLogTexts.MessageTypes.BellyRubMessages, action);
                break;
            case MessageLogEvent.BreastRub:
                message = StoredLogTexts.Format(StoredLogTexts.MessageTypes.BreastRubMessages, action);
                break;
            case MessageLogEvent.BallMassage:
                message = StoredLogTexts.Format(StoredLogTexts.MessageTypes.BallMassageMessages, action);
                break;
            case MessageLogEvent.Feed:
                message = StoredLogTexts.Describe(action, "Feed");
                break;
            case MessageLogEvent.Birth:
                message = StoredLogTexts.Format(StoredLogTexts.MessageTypes.RebirthMessages, action);
                break;
            case MessageLogEvent.TransferFail:
                message = StoredLogTexts.Describe(action, "Transfer failed");
                break;
            case MessageLogEvent.TransferSuccess:
                message = StoredLogTexts.Format(StoredLogTexts.MessageTypes.TransferMessages, action);
                break;
            case MessageLogEvent.KissTransfer:
                message = StoredLogTexts.Format(StoredLogTexts.MessageTypes.KissTransferMessages, action);
                break;
            case MessageLogEvent.Resist:
                message = StoredLogTexts.Describe(action, "Resisted");
                break;
            case MessageLogEvent.Kill:
                message = $"{actor} defeated {target} with {weapon}.";
                break;
            case MessageLogEvent.Digest:
                message = StoredLogTexts.Format(StoredLogTexts.MessageTypes.DigestionDeathMessages, action);
                break;
            case MessageLogEvent.Absorb:
                message = StoredLogTexts.Format(StoredLogTexts.MessageTypes.AbsorptionMessages, action);
                break;
            case MessageLogEvent.Escape:
                message = StoredLogTexts.Describe(action, "Escape");
                break;
            case MessageLogEvent.Freed:
                message = StoredLogTexts.Describe(action, "Released");
                break;
            case MessageLogEvent.Regurgitated:
                message = StoredLogTexts.Describe(action, "Regurgitated");
                break;
            case MessageLogEvent.Heal:
                message = $"{actor} recovered {action.Damage} HP" + (action.Bonus == 0 ? "." : $" and gained {action.Bonus} experience.");
                break;
            case MessageLogEvent.NewTurn:
                message = action.Message ?? "Turn update.";
                break;
            case MessageLogEvent.LowHealth:
                message = $"{target} is near digestion by {actor}.";
                break;
            case MessageLogEvent.FeedCum:
                message = StoredLogTexts.Format(StoredLogTexts.MessageTypes.CumFeedMessages, action);
                break;
            case MessageLogEvent.RandomDigestion:
                message = StoredLogTexts.Format(StoredLogTexts.MessageTypes.RandomDigestionMessages, action);
                break;
            case MessageLogEvent.Miscellaneous:
                message = action.Message ?? "Battle update.";
                break;
            case MessageLogEvent.PartialEscape:
                message = StoredLogTexts.Describe(action, "Partial escape");
                break;
            case MessageLogEvent.TailVore:
                message = StoredLogTexts.Format(StoredLogTexts.MessageTypes.TailVoreMessages, action);
                break;
            case MessageLogEvent.AnalVore:
                message = StoredLogTexts.Format(StoredLogTexts.MessageTypes.AnalVoreMessages, action);
                break;
            case MessageLogEvent.Dazzle:
                message = StoredLogTexts.Describe(action, "Dazzle");
                break;
            case MessageLogEvent.Block:
                message = $"{target} blocked {actor}; {action.Damage} damage received.";
                break;
            case MessageLogEvent.SpellHit:
                message = $"{actor} affected {target} with {SpellName(action)}" + (action.Damage == 0 ? "." : $" for {action.Damage} damage.");
                break;
            case MessageLogEvent.SpellMiss:
                message = $"{actor} failed to affect {target} with {SpellName(action)}.";
                break;
            case MessageLogEvent.SpellKill:
                message = $"{actor} defeated {target} with {SpellName(action)}.";
                break;
            case MessageLogEvent.CurseExpires:
                message = StoredLogTexts.Describe(action, "Curse expired");
                break;
            case MessageLogEvent.DiminishmentExpires:
                message = StoredLogTexts.Describe(action, "Diminishment expired");
                break;
            case MessageLogEvent.TailRub:
                message = StoredLogTexts.Format(StoredLogTexts.MessageTypes.TailRubMessages, action);
                break;
            case MessageLogEvent.Suckle:
                message = StoredLogTexts.Format(StoredLogTexts.MessageTypes.BreastFeedMessages, action);
                break;
            case MessageLogEvent.SuckleFail:
                message = StoredLogTexts.Describe(action, "Support failed (Suckle)");
                break;
            case MessageLogEvent.VoreStealFail:
                message = StoredLogTexts.Describe(action, "Transfer failed (VoreSteal)");
                break;
            case MessageLogEvent.VoreStealSuccess:
                message = StoredLogTexts.Format(StoredLogTexts.MessageTypes.VoreStealMessages, action);
                break;
            case MessageLogEvent.GreatEscapeKeep:
                message = StoredLogTexts.Format(StoredLogTexts.MessageTypes.GreatEscapeKeep, action);
                break;
            case MessageLogEvent.GreatEscapeFlee:
                message = StoredLogTexts.Format(StoredLogTexts.MessageTypes.GreatEscapeFlee, action);
                break;
            case MessageLogEvent.ManualRegurgitation:
                message = StoredLogTexts.Describe(action, "Regurgitated");
                break;
            case MessageLogEvent.BladderVore:
                message = StoredLogTexts.Format(StoredLogTexts.MessageTypes.BladderMessages, action);
                break;
            default:
                message = StoredLogTexts.Describe(action, "Unrecognized battle event");
                break;
        }
        return CompleteDescription(action, message);
    }

    private string CompleteDescription(EventLog action, string message)
    {
        if (ShowOdds && action.Odds > 0)
            message += $" ({Math.Round(action.Odds * 100f, 2)}% success)";
        return action.Opportunity ? "Opportunity attack: " + message : message;
    }

    private string SpellName(EventLog action)
    {
        SpellLog spell = action as SpellLog;
        Spell definition;
        if (spell != null && SpellList.SpellDict.TryGetValue(spell.SpellType, out definition) && definition != null)
            return definition.Name;
        return "Unknown spell";
    }

    public void RegisterHit(Unit Attacker, Unit Defender, Weapon weapon, int damage, float odds, string result = null)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.Hit,
            Opportunity = OpportunityAttacks.Resolving,
            Unit = Attacker,
            Damage = damage,
            Target = Defender,
            Weapon = weapon,
            WeaponResult = result,
            Odds = odds
        });
        UpdateListing();
    }

    public void RegisterMiss(Unit Attacker, Unit Defender, Weapon weapon, float odds)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.Miss,
            Opportunity = OpportunityAttacks.Resolving,
            Unit = Attacker,
            Target = Defender,
            Weapon = weapon,
            Odds = odds
        });
        UpdateListing();
    }

    public void RegisterVore(Unit predator, Unit prey, float odds)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.Devour,
            Unit = predator,
            Target = prey,
            Odds = odds,
            preyLocation = PreyLocation.stomach,
        });
        UpdateListing();
    }

    public void RegisterUnbirth(Unit predator, Unit prey, float odds)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.Unbirth,
            Unit = predator,
            Target = prey,
            Odds = odds,
            preyLocation = PreyLocation.womb,
        });
        UpdateListing();
    }

    public void RegisterCockVore(Unit predator, Unit prey, float odds)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.CockVore,
            Unit = predator,
            Target = prey,
            Odds = odds,
            preyLocation = PreyLocation.balls,
        });
        UpdateListing();
    }

    public void RegisterBreastVore(Unit predator, Unit prey, float odds)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.BreastVore,
            Unit = predator,
            Target = prey,
            Odds = odds,
            preyLocation = PreyLocation.breasts,
        });
        UpdateListing();
    }

    public void RegisterTailVore(Unit predator, Unit prey, float odds)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.TailVore,
            Unit = predator,
            Target = prey,
            Odds = odds,
            preyLocation = PreyLocation.tail,
        });
        UpdateListing();
    }

    public void RegisterAnalVore(Unit predator, Unit prey, float odds)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.AnalVore,
            Unit = predator,
            Target = prey,
            Odds = odds,
            preyLocation = PreyLocation.anal,
        });
        UpdateListing();
    }

    public void RegisterBladderVore(Unit predator, Unit prey, float odds)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.BladderVore,
            Unit = predator,
            Target = prey,
            Odds = odds,
            preyLocation = PreyLocation.bladder,
        });
        UpdateListing();
    }

    public void RegisterBellyRub(Unit rubber, Unit target, Unit prey, PreyLocation preyLoc, float odds)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.BellyRub,
            Unit = rubber,
            Target = target,
            Prey = prey,
            Odds = odds,
            preyLocation = preyLoc,
        });
        UpdateListing();
    }

    public void RegisterBreastRub(Unit rubber, Unit target, Unit prey, float odds)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.BreastRub,
            Unit = rubber,
            Target = target,
            Prey = prey,
            Odds = odds,
            preyLocation = PreyLocation.breasts,
        });
        UpdateListing();
    }

    public void RegisterTailRub(Unit rubber, Unit target, Unit prey, float odds)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.TailRub,
            Unit = rubber,
            Target = target,
            Prey = prey,
            Odds = odds,
            preyLocation = PreyLocation.tail,
        });
        UpdateListing();
    }

    public void RegisterBallMassage(Unit rubber, Unit target, Unit prey, float odds)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.BallMassage,
            Unit = rubber,
            Target = target,
            Prey = prey,
            Odds = odds
        });
        UpdateListing();
    }

    public void RegisterFeed(Unit predator, Unit target, Unit prey, float odds)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.Feed,
            Unit = predator,
            Target = target,
            Prey = prey,
            Odds = odds
        });
        UpdateListing();
    }

    public void RegisterCumFeed(Unit predator, Unit target, Unit prey, float odds)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.FeedCum,
            Unit = predator,
            Target = target,
            Prey = prey,
            Odds = odds
        });
        UpdateListing();
    }

    public void RegisterSuckle(Unit user, Unit target, PreyLocation location, float odds)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.Suckle,
            Unit = user,
            Target = target,
            preyLocation = location,
            Odds = odds
        });
        UpdateListing();
    }

    public void RegisterSuckleFail(Unit user, Unit target, PreyLocation location, float odds)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.SuckleFail,
            Unit = user,
            Target = target,
            preyLocation = location,
            Odds = odds
        });
        UpdateListing();
    }

    public void RegisterBirth(Unit predator, Unit prey, float odds, PreyLocation location, int rebirthType)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.Birth,
            Unit = predator,
            Target = prey,
            preyLocation = location,
            RebirthType = rebirthType,//1 = NormalRebirth|2 = NormalConvert|3 = DigestRebirth|4 = DigestConvert
            Odds = odds
        });
        UpdateListing();
    }

    public void RegisterTransferSuccess(Unit donor, Unit recipient, Unit donation, float odds, PreyLocation loc)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.TransferSuccess,
            Unit = donor,
            Target = recipient,
            Prey = donation,
            Odds = odds,
            preyLocation = loc,
        });
        UpdateListing();
    }

    public void RegisterKissTransfer(Unit donor, Unit recipient, Unit donation, float odds, PreyLocation loc)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.KissTransfer,
            Unit = donor,
            Target = recipient,
            Prey = donation,
            Odds = odds,
            preyLocation = loc,
        });
        UpdateListing();
    }

    public void RegisterTransferFail(Unit predator, Unit prey, float odds, PreyLocation loc)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.TransferFail,
            Unit = predator,
            Target = prey,
            Odds = odds,
            preyLocation = loc,
        });
        UpdateListing();
    }

    public void RegisterVoreStealSuccess(Unit donor, Unit recipient, Unit donation, float odds, PreyLocation loc, PreyLocation oldLoc)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.VoreStealSuccess,
            Unit = recipient,
            Target = donor,
            Prey = donation,
            Odds = odds,
            preyLocation = loc,
            oldLocation = oldLoc,
        });
        UpdateListing();
    }

    public void RegisterVoreStealFail(Unit donor, Unit recipient, Unit donation, PreyLocation oldLoc)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.VoreStealFail,
            Unit = recipient,
            Target = donor,
            Prey = donation,
            oldLocation = oldLoc,
        });
        UpdateListing();
    }

    public void RegisterResist(Unit predator, Unit prey, PreyLocation loc, float odds)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.Resist,
            Unit = predator,
            Target = prey,
            Odds = odds,
            preyLocation = loc
        });
        UpdateListing();
    }

    public void RegisterDazzle(Unit attacker, Unit target, float odds)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.Dazzle,
            Unit = attacker,
            Target = target,
            Odds = odds
        });
        UpdateListing();
    }
    public void RegisterBlock(Unit Attacker, Unit Defender, Weapon weapon, int damage, float odds)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.Block,
            Opportunity = OpportunityAttacks.Resolving,
            Unit = Attacker,
            Damage = damage,
            Target = Defender,
            Weapon = weapon,
            Odds = odds
        });
        UpdateListing();
    }

    public void RegisterSpellHit(Unit attacker, Unit target, SpellTypes type, int damage, float odds)
    {
        events.Add(new SpellLog
        {
            Type = MessageLogEvent.SpellHit,
            Unit = attacker,
            Target = target,
            Damage = damage,
            SpellType = type,
            Odds = odds
        });
        UpdateListing();
    }

    public void RegisterSpellMiss(Unit attacker, Unit target, SpellTypes type, float odds)
    {
        events.Add(new SpellLog
        {
            Type = MessageLogEvent.SpellMiss,
            Unit = attacker,
            Target = target,
            SpellType = type,
            Odds = odds
        });
        UpdateListing();
    }

    public void RegisterSpellKill(Unit attacker, Unit target, SpellTypes type)
    {
        events.Add(new SpellLog
        {
            Type = MessageLogEvent.SpellKill,
            Unit = attacker,
            Target = target,
            SpellType = type
        });
        UpdateListing();
    }


    public void RegisterKill(Unit Attacker, Unit Defender, Weapon weapon)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.Kill,
            Unit = Attacker,
            Target = Defender,
            Weapon = weapon
        });
        UpdateListing();
    }

    public void RegisterDigest(Unit predator, Unit prey, PreyLocation loc)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.Digest,
            Unit = predator,
            Target = prey,
            preyLocation = loc,
        });
        UpdateListing();
    }

    public void RegisterAbsorb(Unit predator, Unit prey, PreyLocation loc)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.Absorb,
            Unit = predator,
            Target = prey,
            preyLocation = loc,
        });
        UpdateListing();
    }

    public void RegisterPartialEscape(Unit predator, Unit prey, PreyLocation loc)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.PartialEscape,
            Unit = predator,
            Target = prey,
            preyLocation = loc,
        });
        UpdateListing();
    }

    public void RegisterEscape(Unit predator, Unit prey, PreyLocation loc)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.Escape,
            Unit = predator,
            Target = prey,
            preyLocation = loc,
        });
        UpdateListing();
    }

    public void RegisterFreed(Unit predator, Unit prey, PreyLocation loc)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.Freed,
            Unit = predator,
            Target = prey,
            preyLocation = loc,
        });
        UpdateListing();
    }

    public void RegisterRegurgitated(Unit predator, Unit prey, PreyLocation loc)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.Regurgitated,
            Unit = predator,
            Target = prey,
            preyLocation = loc,
        });
        UpdateListing();
    }

    public void RegisterHeal(Unit unit, int[] amount, string type = "absorb", string extra = "none")
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.Heal,
            Unit = unit,
            Damage = amount[0],
            Bonus = amount[1],
            Message = type,
            Extra = extra
        });
        UpdateListing();
    }

    public void RegisterNearDigestion(Unit predator, Unit prey, PreyLocation loc)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.LowHealth,
            Unit = predator,
            Target = prey,
            preyLocation = loc,
        });
        UpdateListing();
    }

    public void RegisterCurseExpiration(Unit predator, Unit prey, PreyLocation loc)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.CurseExpires,
            Unit = predator,
            Target = prey,
            preyLocation = loc,
        });
        UpdateListing();
    }

    public void RegisterDiminishmentExpiration(Unit predator, Unit prey, PreyLocation loc)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.DiminishmentExpires,
            Unit = predator,
            Target = prey,
            preyLocation = loc,
        });
        UpdateListing();
    }

    public void LogDigestionRandom(Unit predator, Unit prey, PreyLocation loc)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.RandomDigestion,
            Unit = predator,
            Target = prey,
            preyLocation = loc,
        });
        UpdateListing();
    }

    public void LogGreatEscapeKeep(Unit predator, Unit prey, PreyLocation loc)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.GreatEscapeKeep,
            Unit = predator,
            Target = prey,
            preyLocation = loc,
        });
        UpdateListing();
    }

    public void LogGreatEscapeFlee(Unit predator, Unit prey, PreyLocation loc)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.GreatEscapeFlee,
            Unit = predator,
            Target = prey,
            preyLocation = loc,
        });
        UpdateListing();
    }

    public void RegisterNewTurn(string name, int amount)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.NewTurn,
            Message = $"Turn {amount} - {name}"
        });
        UpdateListing();
    }

    public void RegisterMiscellaneous(string str)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.Miscellaneous,
            Message = str,
        });
        UpdateListing();
    }

    public void RegisterRegurgitate(Unit predator, Unit prey, PreyLocation loc)
    {
        events.Add(new EventLog
        {
            Type = MessageLogEvent.ManualRegurgitation,
            Unit = predator,
            Target = prey,
            preyLocation = loc,
        });
        UpdateListing();
    }

}
