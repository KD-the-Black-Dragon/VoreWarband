// Modified for the VoreWar mercenary-company conversion.
// 2026-11-08: Explain the eight-member campaign battle deployment limit (balance follow-up item 2).
// 2026-11-08: Update equipment guidance for renamed armor and tiered weapon progression.
// 2026-11-05: Describe upkeep-first payday, insolvency and prospective wage changes (spec 38 phase B).
// 2026-11-05: Update cleared-road/farm, new daily allowance and 10/20/30 HP recovery information (spec 36).
// 2026-11-05: Separate faster general HP recovery from longer calendar-day injuries.
// See CHANGES.md and License.txt for GPL-3.0 terms.
// Modified for the VoreWar mercenary-company conversion.
// 2026-11-04: Apply approved injury tiers, base-STR HP, healing ceilings and recovery rules (spec 33).
// 2026-11-04: Implement exclusive baseline trait tiers, flat combat modifiers and categorized Help (spec 32).
// 2026-11-02: Replace retired empire guidance with company gameplay, save boundaries and retained references; correct trait enumeration (spec 12).
// 2026-11-02: Restore vore terminology in existing help without rewriting its deferred content (Options feedback).
// 2026-11-03: Describe named saves, exact battle resumption and Ironman recovery (spec 19).
// 2026-11-03: Match saving help to the renamed Load Game button (spec 20).
// See CHANGES.md for the fork change record and License.txt for GPL-3.0 terms.
// 2026-11-03: Implement four active stats and agreed combat rules (spec 29).
// Modified for the VoreWar mercenary-company conversion.
// 2026-11-04: Show the QuickLearner name in Help (spec 31).
// See CHANGES.md and License.txt for GPL-3.0 terms.
using System;
using System.Linq;
using System.Text;

static class HelpText
{
    public static string GameBasics = @"Mercenary campaign

Lead one company through a finite world generated for each new campaign. There is no conquest victory or final objective: keep the company alive, take work and develop its members.

Create your leader, then enter the starting town. Your initial gold comfortably hires about two inexperienced mercenaries; choose among the available names, species and backstories. The leader draws no wages and must join every battle.

Town services offer recruitment, equipment, contracts and treatment. Return to the world map between errands. The company roster is available outside battle; active members fight and reserves stay out. Every battle gives a final deployment choice.

Pure Tactical is a separate single battle with no campaign progress. Only curated species can generate. Global species preferences stop new individuals from appearing while preserving existing characters and offers.

Values are provisional. Use the other help chapters for travel, wages, injuries and supported saving.";

    public static string StrategyMode = @"World map and days

Your company is the only moving force. Click adjacent map tiles to travel; grass/desert currently cost one action and forest/hills two. Roads connect the six towns and clear their routes to grassland, costing one action per tile. Fields surround towns beside the roads. Nearby terrain is revealed as you explore. Towns are static, and accepted contracts mark their exact target even under fog.

Each campaign turn is one day. New companies have six actions per day; existing companies retain their saved allowance. At zero actions you cannot pay for further movement; End day advances time and refills actions. Visiting a service and taking individual transactions are separate concepts; most service action costs currently remain zero.

Rest advances the chosen number of days, anywhere. Resting in town improves recovery. Every skipped day still processes wages, contract deadlines, pool refreshes, healing and healthy-member sparring. Required notices interrupt rest; deal with them and explicitly resume or cancel the remaining plan.

Tactical battles take no campaign days. Moving to a contract target opens final deployment before the battle starts.";

    public static string RecruitMode = @"Recruitment, roster and wages

Each town has a small separate recruit pool that refreshes over time. Higher company reputation allows more and stronger candidates. Species talents affect roles, but any sapient recruit can develop in any direction. Appearance is fixed; ordinary members may be nicknamed. The leader's creation choices remain fixed.

There is no fixed company roster cap. Each campaign battle allows up to eight active members; reserves stay out, and the leader always deploys. Wages are the main growth constraint and include rations/upkeep in the fiction. Active and reserve members both draw wages, including when injured. Mark reserves from Company or final deployment.

Wages are paid in advance every seven days, first due seven days after campaign start. Midweek hiring costs a prorated advance through the next payday, plus any old debt. Quoted weekly wages rise with level. Before choosing a level-up, review the next weekly wage and company total. The increase starts at the next regular payday, with no midweek top-up.

Company upkeep is charged first on payday, even with only the leader. It starts at 40 gold and rises by 10 each week without a cap. Available gold pays this bill first. Any shortfall becomes upkeep debt. Use Pay Upkeep on the leader in Company to clear the full balance before the next payday. An unpaid upkeep balance at that deadline ends the campaign for critical insolvency, even if gold is available but has not been used to pay it. Warnings interrupt multi-day rest.

If the full payday cannot be covered, everyone owed gains personal debt. You decide whom to pay, in full. Threatened members get their stated grace day. An actual departure gets a separate notice. Former members retain their level, injuries, equipment and unpaid debt, and may offer their services in a town again. Rehiring must settle their old debt.";

    public static string TacticalMode = @"Tactical battle

Control your company against the AI using the existing combat actions. Melee and ranged equipment are separate: a unit with both automatically uses the melee weapon for melee attacks. Without one, it attacks unarmed.

At zero HP a permanent character is downed and cannot take any action for the rest of the battle. Fear can also down someone above zero HP. Downed units are not ordinary direct-attack targets; collateral damage does not kill them. They remain vulnerable to vore. An empty predator can swallow a downed target regardless of capacity; otherwise normal capacity applies. Overcapacity does not damage the predator. Magical summons disappear instead of becoming downed.

Reach the appropriate edge and use Retreat when the existing retreat rules permit it. Downed allies do not silently retreat or become assumed casualties: if opponents can still act, the battle continues. You may use the existing instant simulation to finish it.

Downed or contained units do not by themselves finish a battle. Both sides unable to act, or the generous 200-turn limit, cause a stalemate: remaining units are released and count as fled. Enemy flight counts as contract defeat, even on a draw. Enemies cannot leave while carrying a living captive.

After battle, read the results, then handle any leader revival. Campaign rewards settle automatically.";

    public static string Stats = @"Character stats

Strength (STR): melee accuracy/damage, maximum HP and digestion resistance.
Agility (AGI): ranged accuracy/damage, physical evasion and vore evasion.
Intelligence (INT): magic accuracy/damage/effects, mana, magic resistance, escape while swallowed and critical chance.
Voracity (VOR): swallow chance, capacity, digestion speed and keeping prey from escaping.

Base AP is fixed by species specialty: melee/vore 3, ranged/magic 2. AGI does not grant AP. Moving uses AP; most attacks and abilities use all remaining AP.

A missed weapon attack with at least 51% accuracy gets one silent retry at that same percentage for a graze. A graze uses reduced damage; a successful hit can become a critical. Only the final result is shown.

Injuries and equipment still modify combat. Head/arm/leg/abdomen injuries reduce INT/STR/AGI/VOR. Eye injuries reduce all four attack accuracies, hand injuries reduce weapon/spell damage, chest injuries limit healing and knee injuries reduce AP (minimum 1). Leadership remains retired.";

    public static string Items = @"Company equipment

Nine slots: Helmet, Tunic, Greaves, Bracers, Accessory, Melee weapon, Ranged weapon and two spells.

Buy gear at a town merchant, then equip it through Company > Manage. Equipment can change outside battle. The company inventory owns unequipped items and members' equipped items; sell spare unequipped gear at a merchant. Dismissed or departing members keep their gear.

An HP-granting item changes current and maximum HP by the same amount when equipped/removed. If removing one would leave its wearer without positive HP, recover first.

Armor also reduces injury risk for its covered body part. Helmets protect Head/Eyes, tunics Chest/Abdomen, greaves Leg/Knee and bracers Arm/Hand. There is no armor appearance system in this version.

Digested mercenaries' equipped gear is lost with them. The initial contract reward is gold only. Pure Tactical keeps its original equipment setup.";

    public static string Vore = @"Vore, HP and company fate

The retained vore/digestion mechanics remain central to combat. Health, stats, size and capacity affect attempts against standing units. Downed units have guaranteed vore odds when the attempt otherwise meets its capacity/range requirements.

A downed or contained character is still alive. Permanent characters are only removed by actual completed digestion; collateral damage cannot permanently kill a downed member. The battle continues to resolve those fates rather than inventing off-screen losses.

Grey temporary HP absorbs damage first and disappears after battle. Retained spells/traits normally grant temporary healing. Retained vore recovery raises lasting HP and survives battle end. Surviving critically hurt characters return with a modest HP baseline, currently 15%, and keep their injuries.

Digested non-leaders are permanently lost and recorded in the memorial. A digested leader can use the optional unique revival service if the company still has survivors: results/rewards first, then a nearest-town cutscene, fee 20 gold per leader level, full health and no remaining injuries. No campaign day passes. If gold is insufficient it falls to zero; revival creates no extra debt. With revival disabled, leader digestion ends the campaign immediately. Losing the whole company also ends it.";

    public static string Experience = @"Experience and level-ups

Surviving deployed starters qualify for level 2 after completing a contract. Choose their improvement through Company > Manage > Level up; the existing level-up choices are preserved.

Current cumulative XP thresholds without trait modifiers are 20, 56, 121, 238, 448 and 826 for levels 2 through 7. Requirements rise progressively faster. Trait learning modifiers still apply.

Contract completion awards participants challenge-scaled XP; reserves receive no participation award. Actions against weaker enemies give veterans much less XP. Repeating easy contracts is progressively less useful for development and their small payouts may not cover veteran wages.

All healthy current members gain a little daily sparring XP, active or reserve. Healthy means lasting HP at its healing ceiling with no recovering temporary injury after daily recovery; old injuries alone do not prevent sparring. Sparring does not pick a level-up improvement for you. Wages rise with actual level.

These values are provisional and may be tuned after integrated playtesting.";

    public static string Controls = @"Controls

Company map: click adjacent tiles to move, use Enter town for services, Company for roster/gear and End day to advance time. Rest buttons support chosen days. Contracts lists work and exact targets. Save / Load opens named company saves and autosaves.

Tactical defaults:1 melee attack,2 ranged attack,3 vore; right click for available actions. Use the movement mode/buttons and existing key bindings to select destinations. N selects a unit that can act; Enter ends the tactical turn. WASD/arrows or dragging move the camera, and the mouse wheel zooms. Bindings can differ in Options.

Use the existing AI action/instant-simulation controls to speed combat. Escape opens/closes the ordinary tactical menu; Pause lets you inspect an AI turn.

F5 creates a Quicksave in a normal company battle; F9 opens Save / Load. Escape > Save / Load Game opens the same browser. Save during your own turn when ready to act. See Saving and Ironman for autosave and recovery rules.";

    public static string Teams = @"Injuries and recovery

Normal maximum HP comes from base STR and explicit HP modifiers. Combat STR bonuses and penalties never change it.

Below 75% / 50% / 25% of normal maximum HP, Light / Moderate / Severe Damage reduces all four combat stats by 10% / 20% / 30%. Temporary HP counts immediately. The condition returns if temporary HP expires and lasting HP is still low.

Grazes never cause body-part injuries. Other damage must remove lasting HP equal to at least 10% of normal maximum HP before it can cause one. Larger hits carry greater risk. Armor reduces injury risk for its covered parts. Neither 1 HP nor downing guarantees an injury. New injuries appear in the battle log and as tactical popup text.

Bruised / Battered / Mangled injuries take 5 / 14 / 28 calendar days before treatment. Head, Arm, Abdomen and Leg reduce INT, STR, VOR and AGI by 20% / 40% / 60%. Eyes reduce melee, ranged, magic and vore accuracy by 10 / 20 / 30 percentage points. Hands reduce weapon and spell damage by 1 / 2 / 4. Knees reduce AP by 1 / 2 / 3, never below 1.

Chest injuries limit healing to 90% / 80% / 70% of normal maximum HP. Existing HP is not removed. The normal maximum remains the denominator for damage conditions. Shortened Breath adds another 5% / 10% / 15% reduction to that healing ceiling.

Bruised injuries leave no permanent complication. Battered injuries have a 33% chance, reduced to 17% if treated; Mangled injuries have a 66% chance, reduced to 33% if treated. The hidden roll is saved when the injury occurs; treatment changes its threshold, not its roll. Old injury families advance I to II to III and cap at III. They cannot be cured. The leader is immune to permanent complications.

Old Head/Arm/Abdomen/Leg injuries reduce their associated stat by 10% / 15% / 20%. Blurred Vision reduces all four attack accuracies by 5 / 10 / 15 percentage points; Weak Grip reduces weapon/spell damage by 1 / 2 / 3; Weak Knees reduce AP by 1 / 2 / 3 (minimum 1). Temporary and old penalties add together.

Each campaign day begins recovery with a 5% complication chance for Moderate Damage, or 10% for Severe Damage. Complications can affect Head, Eyes, Arm, Hand, Chest, Abdomen or Leg, or cause Poor Focus, Dulled Instincts, Sluggish Recovery or Sensitive Stomach. Knees are excluded from this daily pool. Poor Focus reduces maximum mana by 5% / 10% / 15%; Dulled Instincts reduce critical chance by 5 / 10 / 15 percentage points. Sluggish Recovery removes 1 / 2 / 3 daily HP recovery, with a minimum of 1. Sensitive Stomach adds 5 / 10 / 15 percentage points to swallowed enemies' escape chances.

Every calendar day advances injury recovery by one day, whether traveling or resting. Daily HP recovery is 10% normally, 20% while resting in the field, 30% while resting in town (rounded up, before Sluggish Recovery). Surviving contract targets recover 10% daily. Mana recovers 60% daily. Healthy members can spar at their lasting healing ceiling with no recovering injuries.

Town treatment costs gold per injured member, shortens untreated temporary injuries and lowers their complication risk. At least one recovery day remains. Treatment does not instantly refill HP. Inspect Company > Manage > Injuries / recovery for current penalties and recovery.";

    public static string Diplomacy = @"Contracts and reputation

Accept work in a discovered town. Each town has a limited local offer pool that refreshes slowly. Several contracts can be active with equal priority. The current mission type is rout the enemy force.

Ratings are numeric fixed enemy-power budgets, independent of your company size. Rosters are generated from units, levels, traits and equipment within a tight power range. Inspect the generated enemy roster before committing. Taking a contract marks the exact target tile even under fog.

Defeat the target before its generous deadline, currently 30 days after acceptance. Actual digested or fleeing enemies count as defeated; casualties do not respawn on a retry. A surviving target remains available until the deadline, recovering over time.

Completion automatically grants the quoted gold and reputation; no return trip is required. Abandoning and expiry have the same difficulty-scaled reputation penalty and remove the map target. Reputation is a visible company-wide number, changes only through contract outcomes and improves access to contracts/recruits. It does not decay just for waiting.";

    public static string Saving = @"Saving, recovery and Ironman

Normal companies can create several named saves. Use Save / Load on the map, Load Game on the main menu, or Save / Load Game in the battle menu. Select or hover over a save to see company progress. Saving with an existing name replaces that slot; Delete removes that slot and its backups after confirmation. Old empire saves are not supported.

Map autosaves capture the beginning of each day and committed company changes. Battle autosaves capture the start of each side's turn after turn effects settle, before actions begin. Normal mode can also save during its own tactical turn while ready to act. Loading restores that battlefield, including spent AP, HP, containment and turn progress; it does not restart deployment, advance campaign time or repeat rewards.

Each slot keeps its latest verified save and three older recovery copies. Backups are used automatically when a newer copy is unreadable. Closing a battle from the menu saves the current state when ready to act; during AI processing the last turn autosave is retained.

Ironman is chosen at creation and cannot be toggled later. It allows autosaves only and resumes the latest valid save, falling back to a backup if needed. When the company ends, its saves are invalidated and removed. Deleting an Ironman entry removes that company and all its backups. This is not protection against external file editing.

If a map autosave fails, use Retry save before continuing. A battle autosave failure pauses combat and reports the problem. Previously verified saves remain available. Single Battle has no persistent company to save, but can open the browser to load a campaign.";

    public static string Legal = @"Project and license

This is the VoreWar mercenary-company conversion fork. VoreWar continues darkevilme's Vore War; upstream project attribution remains applicable.

The covered derivative work remains under the applicable GNU GPL version 3 terms in License.txt. Modification dates and source paths are recorded in CHANGES.md. The program is provided without warranty under those terms.

Third-party assets and dependencies retain their own applicable notices and terms. Their inclusion is not a new claim of ownership by this fork. See the distributed source and accompanying notices for the full license texts.

This proof of concept has provisional tuning and documented limitations. Help describes the implemented campaign; further integrated testing and distribution review remain before a release.";

    public static string Traits()
    {
        var sb = new StringBuilder("Traits I, II and III are increasing tiers. Only one tier or opposing trait in a family can apply at a time. A trait's presence here does not guarantee it can appear on every species.\n\n");
        var traits = Enum.GetValues(typeof(Traits)).Cast<Traits>()
            .Where(t => !TraitsMethods.IsRetired(t) && (int)t < 1000).Select(TieredTraits.Canonical).Distinct().ToList();
        foreach (var category in new[] { "Combat Bonuses", "Combat Penalties", "Personal Development", "Vore and Digestion", "Special Abilities", "Species and Biology" })
        {
            sb.AppendLine("<b>" + category + "</b>");
            foreach (var trait in traits.Where(t => TieredTraits.Category(t) == category).OrderBy(TraitsMethods.DisplayName))
                sb.AppendLine(TraitsMethods.DisplayName(trait) + " - " + HoveringTooltip.GetTraitData(trait).Replace('\n', ' '));
            sb.AppendLine();
        }
        return sb.ToString();
    }
    public static string Spells()
    {
        var sb = new StringBuilder("Retained spells use mana. Company mana recovers daily; ordinary magical healing supplies temporary HP.\n\n");
        if (SpellList.SpellDict == null) return sb.ToString();
        foreach (var spell in SpellList.SpellDict.Values.Where(s => !s.IsRetired).OrderBy(s => s.Name))
            sb.AppendLine(spell.Name + "\n" + spell.Description + "\nRange: " + spell.Range.Min + "-" + spell.Range.Max +
                " | Mana: " + spell.ManaCost + "\nTargets: " + string.Join(", ", spell.AcceptibleTargets) + "\n");
        return sb.ToString();
    }
    internal static readonly string[] CompanyTitles = { "Getting started", "World map and days", "Contracts", "Roster and wages", "Equipment", "Battle", "HP and company fate", "Recovery", "Progression", "Saving and Ironman", "Controls", "Project and license" };
    internal static readonly string[] CompanyPages = { GameBasics, StrategyMode, Diplomacy, RecruitMode, Items, TacticalMode, Vore, Teams, Experience, Saving, Controls, Legal };
}
