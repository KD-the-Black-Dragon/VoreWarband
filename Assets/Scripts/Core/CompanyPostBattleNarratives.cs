// Added by the VoreWar mercenary-company conversion fork.
// 2026-11-10: Add character-focused aftermath prose for post-battle trait events (spec 40D narrative follow-up).
// 2026-11-10: Revise assistant-written passages to use the curator's plainer, conversational voice.
// 2026-11-10: Remove explicit references to the hidden event conditions from aftermath prose.
// 2026-11-10: Use curator-authored text for the remaining events and approved lighter text for one escape.
// See CHANGES.md for the fork's change record and License.txt for GPL-3.0 terms.
using System.Collections.Generic;

internal static class CompanyPostBattleNarratives
{
    sealed class Template
    {
        internal readonly string Opening, Positive, Negative;
        internal Template(string opening, string positive, string negative)
        { Opening = opening; Positive = positive; Negative = negative; }
    }

    static Template T(string opening, string positive, string negative)
    { return new Template(opening, positive, negative); }

    static readonly Dictionary<string, Template> Templates = new Dictionary<string, Template>
    {
        // Curator-authored passages are preserved verbatim apart from dynamic pronoun placeholders; the Grudge passage is below.
        { "digested the contract boss", T(
            "After digesting such a powerful foe, [name] isn't soon going to let anyone forget about it.",
            "In fact, [he] seems invigorated and empowered beyond what digesting another sapient anthro would normally provide.",
            "It's starting to get on everyone's nerves. The other mercenaries are questioning why [he] was granted such an honor.") },
        { "escaped or was freed from vore repeatedly", T(
            "It's lost on neither [Name] nor your company that [he] was swallowed multiple times and yet somehow managed to evade being digested.",
            "[Name] is a remarkably good sport about it and suggests that the more often [he]'s swallowed, the more experience [he] gets dealing with it.",
            "[Name] doesn't seem to find it nearly as amusing as everyone else does. This might be a liability in the future.") },
        { "suffered three or more new injuries", T(
            "[Name] will be long recovering from the beating [he] took from the enemy.",
            "[He]'s surprisingly game about it, however, and is already pestering you about when [he]'ll be allowed to fight again.",
            "[He] has become noticeably apprehensive about the next battle, and even the prospect of another engagement seems to trouble [him].") },
        { "had the highest ranged hit rate", T(
            "[Name] got plenty of practice in with long-range weapons.",
            "The focused experience is paying off, and [his] performance is improving significantly.",
            "It seems to have been a fluke, however. [He] is not able to reproduce the performance during training or sparring.") },

        { "digested an unusually large share of the enemy force", T(
            "[Name]'s stomach worked overtime in the last battle. [His] paunch is heavy with the enormous banquet.",
            "It seems [he]'s developing quite a talent for making opponents disappear forever.",
            "[He] may have pushed [him]self beyond [his] limits, if the way [he] keeps wincing as [he] massages [his] belly is any guide.") },
        { "the company lost more than half of its deployed fighters", T(
            "With so many of [his] comrades having just been digested, [Name] is beginning to appreciate just how unpredictable this line of work can be.",
            "[He] has decided that [he] must be uncommonly talented to have avoided becoming lunch, and is rather pleased with this discovery.",
            "[He] is increasingly convinced that [his] survival was a fluke, and worries that [he] won't be so fortunate next time.") },
        { "was near an enemy when they completed multiple digestions", T(
            "Multiple mercenaries were lost in the last battle, and [Name] had the unfortunate experience of watching [his] comrades get digested right in front of [him], despite [his] best efforts to intervene.",
            "[He] promises you that, next time, [he]'ll make sure the enemy won't have time to finish its meal.",
            "[He] can't help but wonder whether [his] comrades would be any more successful at rescuing [him] if their positions were reversed.") },
        { "was the last active company member standing and survived", T(
            "The company owes its latest victory to [Name], who somehow managed to prevail after everyone else had been taken out of the fight.",
            "[He] has begun referring to the engagement as [his] finest hour, and is quite certain that there will be many more to come.",
            "[He] is increasingly convinced that [his] survival was a fluke, and worries that [he] won't be so fortunate next time.") },
        { "survived damage exceeding their normal maximum HP without being downed", T(
            "It's a wonder [Name] was still alive by the end of the battle. Between the enemy's enthusiasm for beating [him] senseless and the magical assistance keeping [him] upright, [he] endured quite an ordeal.",
            "The other mercenaries have started questioning whether [Name] actually enjoys getting hit. Judging by [his] cheerful response, they may be onto something.",
            "Unfortunately, magical protection does little to make the blows themselves any less painful. [Name] seems to have learned this lesson rather thoroughly.") },
        { "survived more incoming opportunity attacks than ordinary attacks", T(
            "Whatever [Name] was trying to accomplish with all that running around, the enemy certainly seemed appreciative of the opportunities to hit [him].",
            "There's something to be said for a mercenary who refuses to be intimidated by a wall of hostile weapons. [Name] seems to have taken this philosophy to heart.",
            "Many of the attacks landed on sensitive, undefended areas, and [Name] is still feeling it.") },
        { "personally downed most of the enemy force", T(
            "[Name] was responsible for multiple key knockdowns of enemy fighters. Although some mercenaries have complained of \"killstealing\", you remind them that battles are a team effort.",
            "Apparently, [he] has developed a knack for recognizing exactly when and how to finish an opponent. [He] is eager to demonstrate that [his] success was no accident.",
            "The experience may have done more for [his] ego than [his] actual abilities. [He] has become rather careless, apparently expecting every opponent to be as easily defeated as the last.") },
        { "was near an enemy when they completed a digestion", T(
            "The company has a hole in its ranks, and [Name] had the unfortunate experience of watching one of [his] comrades get digested right in front of [him], despite [his] best efforts to intervene.",
            "[He] promises you that, next time, [he]'ll make sure the enemy won't have time to finish its meal.",
            "[He] can't help but wonder whether [his] comrades would be any more successful at rescuing [him] if their positions were reversed.") },
        { "was reduced to 0 HP and survived", T(
            "There are worse ways to end up out of action than being downed, although [Name] seems hard-pressed to think of any at the moment.",
            "Apparently, [Name] considers the experience proof of [his] durability. [He] is already looking forward to demonstrating it again.",
            "In any case, [he] will be long recovering from this day, if [he] ever truly does at all.") },
        { "suffered one or two new injuries", T(
            "One particularly unfortunate hit has left [Name] with an injury that refuses to be ignored, especially whenever [he] tries to move normally.",
            "[He] has resolved to make a full recovery, and seems almost offended by the suggestion that anything could slow [him] down.",
            "What initially seemed like a straightforward injury has proven rather more troublesome. It's apparent that [he] won't recover from this so easily.") },
        { "escaped or was freed from vore once", T(
            "It's lost on neither [Name] nor your company that [he] was swallowed and yet managed to evade being digested.",
            "[Name] seems to take the experience in stride and thinks [he] may handle it better if it happens again.",
            "[Name] doesn't seem to find the experience as amusing as everyone else does.") },
        { "dealt the most melee damage", T(
            "[Name] distinguished [him]self in melee combat today.",
            "There's every reason to expect the strong performance to continue.",
            "Unfortunately, success is getting to [his] head, and [he]'s neglecting [his] regular training.") },
        { "dealt the most ranged damage", T(
            "[Name] distinguished [him]self in ranged combat today.",
            "There's every reason to expect the strong performance to continue.",
            "Unfortunately, success is getting to [his] head, and [he]'s neglecting [his] regular training.") },
        { "dealt the most magic damage", T(
            "[Name] distinguished [him]self in use of offensive magic today.",
            "There's every reason to expect the strong performance to continue.",
            "Unfortunately, success is getting to [his] head, and [he]'s neglecting [his] regular training.") },
        { "spent the most mana", T(
            "[Name] earned [his] paycheck today, given all the spells [he] cast.",
            "There's every reason to expect the strong performance to continue.",
            "The exertion seems to have left [him] with some burnout, hopefully not permanent.") },
        { "had the lowest melee hit rate", T(
            "[Name] missed a lot of swings today, contributing little to the battle.",
            "[He] promises to put in extra training time so as to not repeat such an embarrassing performance.",
            "Melee combat is simply not among [his] talents right now.") },
        { "had the highest melee hit rate", T(
            "[Name] distinguished [him]self in melee combat today.",
            "There's every reason to expect the strong performance to continue.",
            "Unfortunately, success is getting to [his] head, and [he]'s neglecting [his] regular training.") },
        { "had the lowest ranged hit rate", T(
            "[Name] sent a lot of ranged shots wide today, contributing little to the battle.",
            "[He] promises to put in extra training time so as to not repeat such an embarrassing performance.",
            "Archery is simply not among [his] talents right now.") },
        { "had the highest ranged critical rate", T(
            "[Name] demonstrated an uncanny ability to land devastating attacks.",
            "[He]'s gotten remarkably good at targeting weak points.",
            "It seems to have been a fluke, lucky shots in the heat of the moment.") },
        { "had the highest melee critical rate", T(
            "[Name] demonstrated an uncanny ability to land devastating attacks.",
            "[He]'s gotten remarkably good at targeting weak points.",
            "It seems to have been a fluke, lucky shots in the heat of the moment.") }
    };

    internal static string Build(Unit unit, string occurrence, bool positive, bool removal, Traits before, Traits after)
    {
        Template template;
        if (!Templates.TryGetValue(occurrence, out template))
            template = T("[Name] has been thinking about the battle since it ended.",
                "[He] seems ready to put what [he] learned to use.",
                "[He] seems less confident about the next fight.");
        string narrative = Format(unit, template.Opening + "\n" + (positive ? template.Positive : template.Negative));
        return narrative + "\n\n" + MechanicalNotice(unit, removal, before, after);
    }

    internal static string BuildGrudge(Unit unit, Traits before, Traits after)
    {
        const string opening = "While nobody would complain about it openly, the other mercenaries are obviously displeased that you digested such a large number of enemy combatants, thus denying them the opportunity.";
        const string response = "[Name], especially, has been increasingly terse with you.";
        return Format(unit, opening + "\n" + response) + "\n\n" + MechanicalNotice(unit, false, before, after);
    }

    static string MechanicalNotice(Unit unit, bool removal, Traits before, Traits after)
    {
        string name = unit.Name ?? "The mercenary";
        if (removal) return name + " has lost the trait " + TraitsMethods.DisplayName(before) + ".";
        if (before != default(Traits))
            return name + "'s trait " + TraitsMethods.DisplayName(before) + " has changed to " + TraitsMethods.DisplayName(after) + ".";
        return name + " has gained the trait " + TraitsMethods.DisplayName(after) + ".";
    }

    static string Format(Unit unit, string text)
    {
        string name = unit.Name ?? "The mercenary";
        string he = SafePronoun(unit, 0, "they");
        string him = SafePronoun(unit, 1, "them");
        string his = SafePronoun(unit, 2, "their");
        string himself = SafePronoun(unit, 4, "themself");
        return text.Replace("[Name]", name).Replace("[name]", name)
            .Replace("[He]", Capitalize(he)).Replace("[His]", Capitalize(his))
            .Replace("[he]", he).Replace("[him]", him).Replace("[his]", his).Replace("[himself]", himself);
    }

    static string SafePronoun(Unit unit, int index, string fallback)
    {
        try { return unit.GetPronoun(index); }
        catch { return fallback; }
    }

    static string Capitalize(string value)
    { return string.IsNullOrEmpty(value) ? value : value.Substring(0, 1).ToUpperInvariant() + value.Substring(1); }
}
