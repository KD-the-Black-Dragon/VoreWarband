// Added by the VoreWar mercenary-company conversion fork.
// 2026-11-09: Pass the optional enemy flag explicitly in synthetic reflected character creation.
// 2026-11-05: Validate approved injury rules, probabilities and persistence with isolated native fixtures (spec 38 phase D).
// See CHANGES.md and License.txt for GPL-3.0 terms. Existing attribution remains applicable.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using OdinSerializer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Synthetic saves are retained under Logs. No desktop input or personal saves are used.
[InitializeOnLoad]
public static class InjuryValidationAudit
{
    const string Key = "InjuryValidationAudit";
    const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static readonly string[] Parts = { "Head", "Eyes", "Arm", "Hand", "Chest", "Abdomen", "Leg", "Knee" };
    static readonly string[] Grades = { "Bruised", "Battered", "Mangled" };
    static readonly string[] ProtectionSlots = { "Helmet", "Helmet", "Arms", "Arms", "Torso", "Torso", "Legs", "Legs" };
    static readonly Stat[] Stats = { Stat.Strength, Stat.Agility, Stat.Intelligence, Stat.Voracity };
    static IEnumerator work;
    static double started;
    static GameManager gm;
    static object view, state;
    static Actor_Unit a, b;
    static string reserveId, casualtyId;
    static int reserveHealth;
    static Report report;
    [Serializable] public sealed class Report
    {
        public bool complete;
        public string directory, error;
        public List<string> checks = new List<string>();
        public List<Sample> samples = new List<Sample>();
    }
    [Serializable] public sealed class Sample
    {
        public string rule;
        public int seed, trials, observed;
        public double expected, tolerance;
        public List<string> choices = new List<string>();
    }
    sealed class ControlledRandom : System.Random
    {
        readonly Queue<double> rolls;
        readonly Queue<int> choices;
        public ControlledRandom(double[] rolls, int[] choices)
        { this.rolls = new Queue<double>(rolls); this.choices = new Queue<int>(choices); }
        public override double NextDouble()
        { if (rolls.Count == 0) throw new InvalidOperationException("Unexpected probability roll"); return rolls.Dequeue(); }
        public override int Next(int maxValue)
        {
            if (choices.Count == 0) throw new InvalidOperationException("Unexpected selection roll");
            int choice = choices.Dequeue();
            if (choice < 0 || choice >= maxValue) throw new InvalidOperationException("Invalid controlled selection");
            return choice;
        }
    }
    static InjuryValidationAudit() { EditorApplication.update += Update; }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/base.unity");
        SessionState.SetBool(Key, true); EditorApplication.isPlaying = true;
    }
    static void Update()
    {
        int exit = SessionState.GetInt(Key + ".exit", 0);
        if (exit != 0 && !EditorApplication.isPlayingOrWillChangePlaymode)
        { SessionState.EraseInt(Key + ".exit"); EditorApplication.Exit(exit - 1); return; }
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying) return;
        try
        {
            if (work == null) { started = EditorApplication.timeSinceStartup; work = Review(); }
            if (EditorApplication.timeSinceStartup - started > 300) throw new TimeoutException("Injury audit timed out");
            if (!work.MoveNext()) Finish(null);
        }
        catch (Exception ex) { Finish(ex); }
    }
    static void Finish(Exception ex)
    {
        SessionState.SetBool(Key, false);
        if (report != null) { report.complete = ex == null; report.error = ex == null ? null : ex.ToString(); Write(); }
        SessionState.SetInt(Key + ".exit", ex == null ? 1 : 2); EditorApplication.isPlaying = false;
    }
    static Type T(string name) => typeof(GameManager).Assembly.GetType(name, true);
    static object G(object target, string name)
    {
        var type = target as Type ?? target.GetType(); var field = type.GetField(name, Flags);
        return field != null ? field.GetValue(target is Type ? null : target) : type.GetProperty(name, Flags).GetValue(target is Type ? null : target, null);
    }
    static void S(object target, string name, object value)
    {
        var type = target as Type ?? target.GetType(); var field = type.GetField(name, Flags);
        if (field != null) field.SetValue(target is Type ? null : target, value);
        else type.GetProperty(name, Flags).SetValue(target is Type ? null : target, value, null);
    }
    static object C(object target, string name, params object[] args)
    {
        var type = target as Type ?? target.GetType();
        return type.GetMethods(Flags).First(m => m.Name == name && m.GetParameters().Length == args.Length)
            .Invoke(target is Type ? null : target, args);
    }
    static object E(string type, string name) => Enum.Parse(T(type), name);
    static IList Injuries(Unit unit) => (IList)G(unit, "Injuries");
    static Unit U(object character) => (Unit)G(character, "Unit");
    static IEnumerable<object> Characters(object campaign) => ((IEnumerable)G(campaign, "Characters")).Cast<object>();
    static void Check(bool value, string description)
    { if (!value) throw new InvalidOperationException(description); report.checks.Add(description); }
    static void Near(double actual, double expected, string description)
    { Check(Math.Abs(actual - expected) < .0001, description + " = " + actual); }
    static void Write() => File.WriteAllText(Path.Combine(report.directory, "report.json"), JsonUtility.ToJson(report, true));
    static void Rolls(double[] rolls, params int[] choices)
    { var random = new Rand(1); S(random, "rand", new ControlledRandom(rolls, choices)); State.Rand = random; }
    static void Reset(Actor_Unit actor)
    {
        C(typeof(TraitCurationAudit), "Reset", actor);
        S(actor.Unit, "BattleActive", false); S(actor.Unit, "Type", UnitType.Soldier);
        S(actor.Unit, "PermanentInjuryImmune", false); S(actor.Unit, "Digested", false);
        C(G(actor.Unit, "TempBoosts"), "ResetAll");
        S(actor.Unit, "Items", new Item[9]); S(actor.Unit, "CompanyLoadout", true);
        foreach (var stat in Stats) actor.Unit.SetStatBase(stat, 100);
        S(actor.Unit, "TemporaryHealth", 0); S(actor.Unit, "Downed", false);
        actor.Unit.Health = actor.Unit.MaxHealth; C(actor.Unit,"SetBarrier",0); C(actor.Unit,"RestoreManaPct",1f);
        S(actor.Unit, "BattleActive", true); actor.UpdateBestWeapons();
    }
    static object Add(Unit unit, string part, string grade, double roll = .9)
    {
        Rolls(new[] { roll }); C(T("CompanyInjuries"), "Add", unit, E("CompanyBodyPart", part), E("CompanyInjuryGrade", grade));
        return Injuries(unit)[Injuries(unit).Count - 1];
    }
    static void Old(Unit unit, string part, string kind = "BodyPart")
    { C(T("CompanyInjuries"), "AddOld", unit, E("CompanyBodyPart", part), E("CompanyComplication", kind)); }
    static void Health(Unit unit, int health)
    { S(unit, "BattleActive", false); S(unit, "TemporaryHealth", 0); unit.Health = health; S(unit, "BattleActive", true); }
    static IEnumerator Review()
    {
        for (int frame = 0; frame < 12; frame++) yield return null;
        report = new Report { directory = Path.GetFullPath("Logs/injury38d-" + Guid.NewGuid().ToString("N")) };
        Directory.CreateDirectory(report.directory); File.WriteAllText("Logs/injury38d-latest.txt", report.directory);
        S(T("CompanyCampaignSave"), "DiagnosticDirectory", report.directory);
        gm = UnityEngine.Object.FindObjectOfType<GameManager>(); Check(gm != null, "Native scene initialized"); gm.enabled = false;
        view = G(gm.Start_Mode, "CompanySaveView");
        state = C(T("CompanyCampaign"), "Generate", 38101, false);
        var leader = C(T("CompanyEconomy"), "Create", state, Race.Lizards, 2, true, 1, false);
        C(T("CompanyEconomy"), "Begin", state, leader); C(view, "Install", state);
        var town = ((IEnumerable)G(state, "Towns")).Cast<object>().First(t => (int)G(t, "Tile") == (int)G(state, "Position"));
        S(state,"Gold",1000); // Fixture funding for distinct survivor, casualty and reserve roles, not an economy sample.
        foreach (var person in ((IEnumerable)C(T("CompanyEconomy"), "Offers", state, town)).Cast<object>().Take(3).ToArray())
            C(T("CompanyEconomy"), "Hire", state, person, town);
        var reserve=((IList)G(state,"Members")).Cast<object>().Last();
        reserveId=(string)G(reserve,"CharacterId"); S(reserve,"Reserve",true);
        var reserveUnit=U(Characters(state).Single(c=>(string)G(c,"Id")==reserveId));
        reserveHealth=reserveUnit.Health; Add(reserveUnit,"Arm","Bruised"); State.Rand=new Rand(38100);
        var contract = ((IList)G(state, "Contracts"))[0];
        C(T("CompanyContracts"), "Accept", state, contract); C(T("CompanyBattleSession"), "Prepare", state, contract);
        S(G(state, "Battle"), "PositionCommitted", true); C(view, "LaunchBattle");
        foreach (var camera in Resources.FindObjectsOfTypeAll<Camera>()) camera.enabled = false;
        foreach (var renderer in Resources.FindObjectsOfTypeAll<UnityEngine.Tilemaps.TilemapRenderer>()) renderer.enabled = false;
        var actors = ((IList)G(gm.TacticalMode, "units")).Cast<Actor_Unit>().ToArray();
        a = actors.First(x => x.Unit.Side == 0 && x.Unit.Type != UnitType.Leader); b = actors.First(x => x.Unit.Side == 1);
        casualtyId=(string)G(Characters(state).Single(c=>ReferenceEquals(U(c),actors.First(x=>x.Unit.Side==0 && x.Unit.Type!=UnitType.Leader && x!=a).Unit)),"Id");
        a.SetPos(new Vec2i(4,4)); b.SetPos(new Vec2i(5,4));
        S(gm.TacticalMode, "turboMode", true);
        var log = G(gm.TacticalMode, "Log"); S(gm.TacticalMode, "Log", null);
        try
        {
            PartMatrix(); ConditionsAndHealing(); RecoveryBoundaries(); ArmorAndHitBoundaries(); DailyBoundaries(); CampaignRecoveryAndSparring(); LegacyValidation();
            Write(); yield return null;
            Distributions(); Write(); yield return null;
        }
        finally { S(gm.TacticalMode, "Log", log); State.Rand = new Rand(38199); }
        CombatEntryPoints(); PersistenceAndSettlement(); Write();
        SingleBattleIsolation(); Write();
        yield return null;
    }
    static void PartMatrix()
    {
        var injuries = T("CompanyInjuries");
        foreach (string part in Parts) for (int grade = 0; grade < Grades.Length; grade++)
        {
            Reset(a); Reset(b);
            float melee = b.GetAttackChance(a, false), ranged = b.GetAttackChance(a, true), vore = b.GetDevourChance(a);
            var spell = G(T("SpellList"), "Fireball");
            float magic = (float)C(b, "GetMagicChance", a, spell, 0f, Stat.Intelligence);
            int meleeDamage = a.WeaponDamageAgainstTarget(b, false), rangedDamage = a.WeaponDamageAgainstTarget(b, true);
            int ap = a.CurrentMaxMovement(), hp = a.Unit.MaxHealth;
            var injury = Add(a.Unit, part, Grades[grade]);
            string label = Grades[grade] + " " + part;
            Check((string)C(injuries, "Name", injury) == label, label + " name");
            Check((int)G(injury, "DaysRemaining") == new[] {5,14,28}[grade], label + " calendar duration");
            foreach (var stat in Stats)
            {
                string affected = stat == Stat.Strength ? "Arm" : stat == Stat.Agility ? "Leg" : stat == Stat.Intelligence ? "Head" : "Abdomen";
                Check(a.Unit.GetStat(stat) == (part == affected ? 100 - 20 * (grade + 1) : 100), label + " " + stat);
            }
            if (part == "Eyes")
            {
                double penalty = (grade + 1) * .10;
                Near(b.GetAttackChance(a, false), Math.Max(0, melee - penalty), label + " melee odds");
                Near(b.GetAttackChance(a, true), Math.Max(0, ranged - penalty), label + " ranged odds");
                Near(b.GetDevourChance(a), Math.Max(0, vore - penalty), label + " vore odds");
                Near((float)C(b, "GetMagicChance", a, spell, 0f, Stat.Intelligence), Math.Max(0, magic - penalty), label + " spell odds");
            }
            if (part == "Hand")
            {
                int penalty = new[] {1,2,4}[grade];
                Check(a.WeaponDamageAgainstTarget(b,false) == Math.Max(1,meleeDamage-penalty), label + " melee damage");
                Check(a.WeaponDamageAgainstTarget(b,true) == Math.Max(1,rangedDamage-penalty), label + " ranged damage");
                Check((int)C(b,"ApplySpellTraitDamage",a,25) == 25-penalty, label + " spell damage");
            }
            if (part == "Chest") Check((int)G(a.Unit,"HealingCeiling") == (int)Math.Floor(hp*(1-(grade+1)*.1)+.0001), label + " healing ceiling");
            if (part == "Knee") Check(a.CurrentMaxMovement() == Math.Max(1,ap-grade-1), label + " AP minimum");
            Check(a.Unit.MaxHealth == hp && a.Unit.Health == hp, label + " preserves normal/current HP");
        }
        foreach (string part in Parts)
        {
            Reset(a);
            for (int tier = 1; tier <= 4; tier++)
            {
                Old(a.Unit,part); var injury = Injuries(a.Unit).Cast<object>().Single(); int applied = Math.Min(3,tier);
                Check((int)G(injury,"Tier") == applied, part + " old tier/cap " + tier);
                string name = new[] {"Frequent Headaches","Blurred Vision","Stiff Arm","Weak Grip","Shortened Breath","Poor Digestion","Leg Pains","Weak Knees"}[Array.IndexOf(Parts,part)];
                Check((string)C(injuries,"Name",injury)==name+" "+new[]{"I","II","III"}[applied-1],part+" old injury name "+tier);
                if (new[] {"Head","Arm","Abdomen","Leg"}.Contains(part))
                {
                    Stat stat = part=="Head"?Stat.Intelligence:part=="Arm"?Stat.Strength:part=="Leg"?Stat.Agility:Stat.Voracity;
                    Check(a.Unit.GetStat(stat) == 100-new[]{10,15,20}[applied-1], part + " old stat penalty " + tier);
                }
                if (part=="Eyes") Near((float)C(injuries,"AccuracyPenalty",a.Unit), applied*.05, "Old eye accuracy " + tier);
                if (part=="Hand") Check((int)C(injuries,"DamagePenalty",a.Unit)==applied,"Old hand damage " + tier);
                if (part=="Chest") Near((float)C(injuries,"HealingCeilingFraction",a.Unit),1-applied*.05,"Old chest ceiling " + tier);
                if (part=="Knee") Check(a.CurrentMaxMovement()>=1,"Old knee AP minimum " + tier);
            }
        }
        foreach (string kind in new[] {"PoorFocus","DulledInstincts","SluggishRecovery","SensitiveStomach"})
        {
            Reset(a); int mana=(int)G(a.Unit,"MaxMana");
            for(int tier=1;tier<=4;tier++)
            {
                Old(a.Unit,"Head",kind); int applied=Math.Min(3,tier);
                Check(Injuries(a.Unit).Count==1 && (int)G(Injuries(a.Unit)[0],"Tier")==applied,kind+" single capped family "+tier);
                if(kind=="PoorFocus") Check((int)G(a.Unit,"MaxMana")==Mathf.FloorToInt(mana*(1-applied*.05f)+.0001f) && (int)G(a.Unit,"Mana")<=(int)G(a.Unit,"MaxMana"),kind+" mana/clamp "+tier);
                if(kind=="DulledInstincts") Near((float)C(injuries,"CriticalPenalty",a.Unit),applied*.05,kind+" penalty "+tier);
                if(kind=="SensitiveStomach") Near((float)C(injuries,"EscapeBonus",a.Unit),applied*.05,kind+" escape bonus "+tier);
                if(kind=="SluggishRecovery") Check((int)C(injuries,"RecoveryPenalty",a.Unit)==applied,kind+" recovery penalty "+tier);
            }
        }
    }
    static void ConditionsAndHealing()
    {
        Reset(a); int hp=a.Unit.MaxHealth;
        foreach(int health in new[]{hp,226,225,224,151,150,149,76,75,74,1})
        {
            Health(a.Unit,health); int tier=health<75?3:health<150?2:health<225?1:0;
            Check((int)C(T("CompanyInjuries"),"DamageTier",a.Unit)==tier,"Attrition boundary HP "+health);
            foreach(var stat in Stats) Check(a.Unit.GetStat(stat)==100-10*tier,"Attrition "+health+" "+stat);
            Check(a.Unit.MaxHealth==hp,"Attrition HP denominator "+health);
        }
        Reset(a);Health(a.Unit,135); a.Unit.Heal(1000);
        Check((int)G(a.Unit,"LastingHealth")==135 && (int)G(a.Unit,"TemporaryHealth")==165 && a.Unit.GetStat(Stat.Strength)==100,"Temporary healing removes attrition without restoring lasting HP");
        C(a.Unit,"FinishCombat");Check(a.Unit.Health==135 && a.Unit.GetStat(Stat.Strength)==80,"Battle end restores lasting-HP attrition");
        Reset(a);Health(a.Unit,135);C(a.Unit,"HealLasting",105);
        Check(a.Unit.Health==240 && (int)C(T("CompanyInjuries"),"DamageTier",a.Unit)==0,"Lasting vore healing crosses damage threshold");
        Reset(a);Add(a.Unit,"Chest","Bruised");Old(a.Unit,"Chest");
        Check((int)G(a.Unit,"HealingCeiling")==255 && a.Unit.Health==hp && a.Unit.MaxHealth==hp,"Combined chest injuries retain existing HP and 85 percent ceiling");
        Health(a.Unit,150);C(a.Unit,"HealLasting",1000);Check(a.Unit.Health==255,"Lasting healing stops at additive chest ceiling");
        Health(a.Unit,150);a.Unit.Heal(1000);Check(a.Unit.Health==255 && a.Unit.MaxHealth==hp,"Temporary healing respects ceiling and normal denominator");
        Reset(a);Add(a.Unit,"Arm","Bruised");Old(a.Unit,"Arm");Health(a.Unit,135);
        Check(a.Unit.GetStat(Stat.Strength)==50 && a.Unit.MaxHealth==hp,"Old, temporary and attrition stat penalties add");
        Reset(a);a.Unit.AddTrait(Traits.Fit);Check(a.Unit.MaxHealth==hp,"Combat STR bonus does not alter HP capacity");
        Reset(a);a.Unit.AddTrait(Traits.Hearty);Check(a.Unit.MaxHealth==hp+a.Unit.Level,"Explicit HP trait changes capacity");
        Reset(a);Add(a.Unit,"Hand","Mangled");Old(a.Unit,"Hand");Old(a.Unit,"Hand");Old(a.Unit,"Hand");
        Check((int)C(T("CompanyInjuries"),"DamagePenalty",a.Unit)==7,"Temporary and old hand damage penalties add");
        Reset(a);Add(a.Unit,"Knee","Mangled");for(int i=0;i<3;i++)Old(a.Unit,"Knee");Check(a.CurrentMaxMovement()==1,"Stacked knee injuries retain one AP");
        Reset(a);Health(a.Unit,1);C(a.Unit,"FinishCombat");Check(a.Unit.Health==45,"Post-combat baseline is fifteen percent");
        Reset(a);Add(a.Unit,"Arm","Mangled");C(a.Unit,"FinishCombat");Check(a.Unit.Health==hp && a.Unit.GetStat(Stat.Strength)==40,"Full HP does not erase a specific injury");
        Reset(a);
        for(int i=0;i<4;i++) Add(a.Unit,"Chest","Mangled");
        Check((int)G(a.Unit,"HealingCeiling")==1 && a.Unit.Health==hp && a.Unit.MaxHealth==hp,"Extreme additive chest penalties keep one-HP healing floor without removing existing HP");
        Health(a.Unit,0); C(a.Unit,"FinishCombat");
        Check(a.Unit.Health==1,"Post-combat baseline respects the one-HP chest ceiling");
        Race priorRace=a.Unit.Race;
        try
        {
            foreach(var race in new[]{Race.Avians,Race.Lizards,Race.Gnolls,Race.SpitterSlugs})
                for(int grade=0;grade<3;grade++)
                {
                    Reset(a); S(a.Unit,"Race",race); Add(a.Unit,"Knee",Grades[grade]);
                    int allowance=race==Race.Gnolls?3:2;
                    Check(a.CurrentMaxMovement()==Math.Max(1,allowance-grade-1),race+" "+Grades[grade]+" Knee respects fixed AP/minimum");
                }
        }
        finally { S(a.Unit,"Race",priorRace); }
    }
    static void RecoveryBoundaries()
    {
        var injuries=T("CompanyInjuries");
        for(int grade=0;grade<3;grade++) foreach(bool treated in new[]{false,true})
        {
            double risk=grade==0?0:grade==1?(treated?.17:.33):(treated?.33:.66);
            foreach(double roll in new[]{0,Math.Max(0,risk-.000001),risk,.999999}.Distinct())
            {
                Reset(a);var injury=Add(a.Unit,"Eyes",Grades[grade],roll);string id=(string)G(injury,"Id");
                if(treated)C(injuries,"TreatInjuries",a.Unit);
                Check((double)G(injury,"RecoveryRoll")==roll && (string)G(injury,"Id")==id,"Treatment preserves occurrence identity/roll "+Grades[grade]+" "+treated+" "+roll);
                Rolls(new double[0]);C(injuries,"Recover",a.Unit,100);
                Check(Injuries(a.Unit).Count==(roll<risk?1:0),"Recovery risk boundary "+Grades[grade]+" "+treated+" "+roll);
                if(roll<risk)Check(G(Injuries(a.Unit)[0],"Grade").ToString()=="Old" && G(Injuries(a.Unit)[0],"Part").ToString()=="Eyes","Complication matches recovered body part");
            }
        }
        foreach(bool leaderType in new[]{false,true})
        {
            Reset(a);S(a.Unit,"PermanentInjuryImmune",!leaderType);if(leaderType)S(a.Unit,"Type",UnitType.Leader);
            Add(a.Unit,"Arm","Mangled",0);C(injuries,"Recover",a.Unit,100);Old(a.Unit,"Head");
            Health(a.Unit,1);Rolls(new double[0]);C(injuries,"DailyComplication",a.Unit);
            Check(Injuries(a.Unit).Count==0,"Leader immunity from recovery, direct and daily complications "+leaderType);
        }
        Reset(a);var single=Add(a.Unit,"Arm","Battered",.2);S(single,"DaysRemaining",1);C(injuries,"TreatInjuries",a.Unit);
        Check((int)G(single,"DaysRemaining")==1 && (bool)G(single,"Treated"),"Last-day treatment retains one recovery day and reduces risk");
        C(injuries,"TreatInjuries",a.Unit);Check((double)G(single,"RecoveryRoll")==.2 && (int)G(single,"DaysRemaining")==1,"Repeated treatment cannot shorten or reroll again");
    }
    static void ArmorAndHitBoundaries()
    {
        var injuries=T("CompanyInjuries");
        for(int part=0;part<Parts.Length;part++)
        {
            Reset(a);string slot=ProtectionSlots[part];
            var definition=((IEnumerable)G(T("CompanyEquipment"),"Catalog")).Cast<object>().First(d=>G(d,"Slot").ToString()==slot && (int)G(d,"ArmorClass")>0);
            a.Unit.SetItem((Item)C(T("CompanyEquipment"),"Create",G(definition,"Id")),(int)G(definition,"Slot"));
            double risk=(double)C(injuries,"RiskMultiplier",a.Unit,E("CompanyBodyPart",Parts[part]));
            Check(risk<1,"Armor covers "+Parts[part]+" through "+slot);
            Near(risk,1.0/(1+(int)G(definition,"Protection")),"Armor injury-risk protection formula for "+Parts[part]);
            for(int other=0;other<Parts.Length;other++)
                if(ProtectionSlots[other]!=slot)
                    Near((double)C(injuries,"RiskMultiplier",a.Unit,E("CompanyBodyPart",Parts[other])),1,"Armor does not protect unrelated "+Parts[other]);
            double threshold=.5*risk;
            Rolls(new[]{threshold},(int)E("CompanyBodyPart",Parts[part]));C(injuries,"RecordDamage",a.Unit,150);Check(Injuries(a.Unit).Count==0,"Protected injury chance excludes exact threshold "+Parts[part]);
            Rolls(new[]{threshold-.000001,.7},(int)E("CompanyBodyPart",Parts[part]));C(injuries,"RecordDamage",a.Unit,150);Check(Injuries(a.Unit).Count==1,"Protected injury chance includes below threshold "+Parts[part]);
        }
        foreach(int damage in new[]{1,29})
        {Reset(a);Rolls(new double[0]);C(a.Unit,"ApplyCombatDamage",damage,true);Check(Injuries(a.Unit).Count==0,"Sub-threshold lasting hit "+damage);}
        foreach(int damage in new[]{30,74,75,149,150,300})
        {
            Reset(a);Rolls(new[]{0.0,.6},(int)E("CompanyBodyPart","Arm"));C(a.Unit,"ApplyCombatDamage",damage,true);
            Check(Injuries(a.Unit).Count==1 && G(Injuries(a.Unit)[0],"Grade").ToString()==(damage<75?"Bruised":damage<150?"Battered":"Mangled"),"Lasting hit threshold/severity "+damage);
        }
        Reset(a);Rolls(new double[0]);C(a.Unit,"ApplyCombatDamage",150,false);Check(Injuries(a.Unit).Count==0 && a.Unit.Health==150,"Graze suppresses injury while contributing attrition");
        Reset(a);Health(a.Unit,100);a.Unit.Heal(200);Rolls(new double[0]);C(a.Unit,"ApplyCombatDamage",200,true);
        Check(a.Unit.Health==100 && (int)G(a.Unit,"LastingHealth")==100 && Injuries(a.Unit).Count==0,"Temporary-only damage never checks a body injury");
        Reset(a);S(a.Unit,"Type",UnitType.Summon);Rolls(new double[0]);C(a.Unit,"ApplyCombatDamage",150,true);Check(Injuries(a.Unit).Count==0,"Summon cannot gain persistent injury");
    }
    static void DailyBoundaries()
    {
        var injuries=T("CompanyInjuries");
        foreach(int tier in new[]{0,1,2,3})
        {
            int health=tier==0?300:tier==1?200:tier==2?100:50;double risk=tier<2?0:tier==2?.05:.10;
            foreach(double roll in new[]{0,Math.Max(0,risk-.000001),risk,.999999}.Distinct())
            {
                Reset(a);Health(a.Unit,health);Rolls(tier<2?new double[0]:new[]{roll},roll<risk?new[]{0}:new int[0]);
                C(injuries,"DailyComplication",a.Unit);
                Check(Injuries(a.Unit).Count==(roll<risk?1:0),"Daily risk boundary tier "+tier+" roll "+roll);
            }
        }
        for(int choice=0;choice<11;choice++)
        {
            Reset(a);Health(a.Unit,50);Rolls(new[]{0.0},choice);C(injuries,"DailyComplication",a.Unit);
            var old=Injuries(a.Unit).Cast<object>().Single();
            string expected=choice<7?new[]{"Head","Eyes","Arm","Hand","Chest","Abdomen","Leg"}[choice]:new[]{"PoorFocus","DulledInstincts","SluggishRecovery","SensitiveStomach"}[choice-7];
            Check((choice<7?G(old,"Part"):G(old,"Complication")).ToString()==expected,"Daily complication selection "+choice+" "+expected);
        }
        foreach(double rate in new[]{.10,.20,.30})
        {
            Reset(a);S(a.Unit,"BattleActive",false);a.Unit.Health=150;var injury=Add(a.Unit,"Arm","Mangled");
            Rolls(new[]{.999});C(T("CompanyDailyRecovery"),"Recover",a.Unit,rate,1);
            Check(a.Unit.Health==150+(int)Math.Ceiling(300*rate) && (int)G(injury,"DaysRemaining")==27,"Daily HP and calendar injury recovery "+rate);
        }
        Reset(a);S(a.Unit,"BattleActive",false);a.Unit.Health=1;for(int i=0;i<3;i++)Old(a.Unit,"Head","SluggishRecovery");
        Rolls(new[]{.999});C(T("CompanyDailyRecovery"),"Recover",a.Unit,0.0,1);Check(a.Unit.Health==2,"Sluggish recovery retains minimum one HP");
    }
    static void SampleCheck(Sample sample)
    {
        sample.tolerance=6*Math.Sqrt(sample.trials*sample.expected*(1-sample.expected))+6;
        report.samples.Add(sample);
        bool withinRange = sample.expected == 0 ? sample.observed == 0 :
            Math.Abs(sample.observed-sample.trials*sample.expected)<=sample.tolerance;
        Check(withinRange,"Seeded frequency "+sample.rule+" "+sample.observed+"/"+sample.trials);
    }
    static void CombatEntryPoints()
    {
        Reset(a); Reset(b);
        a.Unit.SetStatBase(Stat.Strength,200); Health(a.Unit,a.Unit.MaxHealth);
        b.Unit.SetStatBase(Stat.Strength,1); Health(b.Unit,b.Unit.MaxHealth);
        a.Movement=2;
        var draws=new[] {.999,0.0}.Concat(Enumerable.Repeat(0.0,50)).ToArray();
        Rolls(draws,Enumerable.Repeat(0,50).ToArray());
        Check(b.GetAttackChance(a,false)>=.51f,"Native graze fixture has eligible accuracy");
        bool hit=a.Attack(b,false);
        string log=(string)C(G(gm.TacticalMode,"Log"),"DebugDump");
        Check(hit && log.IndexOf("graze",StringComparison.OrdinalIgnoreCase)>=0 && Injuries(b.Unit).Count==0,
            "Actual weapon graze records final result without inflicting body injury");
        Reset(a); Reset(b); Rolls(Enumerable.Repeat(0.0,50).ToArray(),Enumerable.Repeat(0,50).ToArray());
        C(b,"DefendDamageSpell",G(T("SpellList"),"Fireball"),a,100);
        Check(Injuries(b.Unit).Count==1 && b.Unit.Health<b.Unit.MaxHealth,"Actual damaging spell can inflict a body injury from lasting HP loss");
        State.Rand=new Rand(38300);
    }
    static void CampaignRecoveryAndSparring()
    {
        var fresh=C(T("CompanyCampaign"),"Generate",38191,false);
        var leader=C(T("CompanyEconomy"),"Create",fresh,Race.Lizards,2,true,1,false);
        C(T("CompanyEconomy"),"Begin",fresh,leader);
        var town=((IEnumerable)G(fresh,"Towns")).Cast<object>().First(t=>(int)G(t,"Tile")== (int)G(fresh,"Position"));
        foreach(var candidate in ((IEnumerable)C(T("CompanyEconomy"),"Offers",fresh,town)).Cast<object>().Take(2).ToArray())
            C(T("CompanyEconomy"),"Hire",fresh,candidate,town);
        var members=((IList)G(fresh,"Members")).Cast<object>().ToArray();
        var people=members.Select(m=>Characters(fresh).Single(c=>(string)G(c,"Id")== (string)G(m,"CharacterId"))).ToArray();
        var units=people.Select(U).ToArray();
        S(members[1],"Reserve",true); Old(units[1],"Chest");
        units[1].Health=(int)G(units[1],"HealingCeiling");
        var temporary=Add(units[2],"Arm","Mangled",.4);
        int wage=(int)C(T("CompanyEconomy"),"Wage",fresh,people[2]);
        float[] before=units.Select(u=>(float)G(u,"experience")).ToArray();
        C(Activator.CreateInstance(T("CompanySparring"),true),"Apply",fresh);
        Check((float)G(units[0],"experience")>before[0],"Healthy active leader spars");
        Check((float)G(units[1],"experience")>before[1],"Healthy reserve at chest ceiling with only old injury spars");
        Check((float)G(units[2],"experience")==before[2],"Full-HP mercenary with body injury cannot spar");
        Check((int)C(T("CompanyEconomy"),"Wage",fresh,people[2])==wage,"Full-HP injured mercenary retains normal wage");
        Health(units[0],Math.Max(1,units[0].MaxHealth/2));units[0].Heal(10000);
        before=units.Select(u=>(float)G(u,"experience")).ToArray();
        C(Activator.CreateInstance(T("CompanySparring"),true),"Apply",fresh);
        Check((float)G(units[0],"experience")==before[0],"Temporary full HP alone does not qualify for sparring");
        S(fresh,"Gold",1000); var other=Add(units[2],"Eyes","Battered",.2); Old(units[2],"Chest");
        int oldCount=Injuries(units[2]).Count;
        C(T("CompanyInjuries"),"Treat",fresh,G(people[2],"Id"));
        Check((int)G(fresh,"Gold")==990 && (int)G(temporary,"DaysRemaining")==26 && (int)G(other,"DaysRemaining")==12,"Town treatment charges once per member and treats both wounds");
        Check(Injuries(units[2]).Count==oldCount && (double)G(temporary,"RecoveryRoll")==.4 && (double)G(other,"RecoveryRoll")==.2,"Town treatment preserves old injuries and both stored rolls");
        int cash=(int)G(fresh,"Gold");bool rejected=false;
        try { C(T("CompanyInjuries"),"Treat",fresh,G(people[2],"Id")); } catch(TargetInvocationException ex) { rejected=ex.InnerException is InvalidOperationException; }
        Check(rejected && (int)G(fresh,"Gold")==cash,"Already-treated member cannot be charged again");
        S(units[0],"BattleActive",false);S(units[0],"TemporaryHealth",0);units[0].Health=units[0].MaxHealth;
        S(fresh,"ElapsedDays",6); State.Rand=new Rand(38192);
        int totalWages=people.Sum(p=>(int)C(T("CompanyEconomy"),"Wage",fresh,p));
        int upkeep=(int)C(T("CompanyEconomy"),"Upkeep",fresh);
        C(G(view,"clock"),"EndDay",fresh,true);
        Check((int)G(fresh,"Gold")==cash-upkeep-totalWages,"Payday includes injured and reserve wages after upkeep");
        Check((int)G(temporary,"DaysRemaining")==25 && (int)G(other,"DaysRemaining")==11,"Town-rest day advances body injury by one calendar day");
    }
    static void LegacyValidation()
    {
        var injuries=T("CompanyInjuries");
        foreach(string part in Parts)
        {
            Reset(a);
            for(int tier=0;tier<3;tier++) Old(a.Unit,part);
            var restored=SerializationUtility.DeserializeValue<Unit>(SerializationUtility.SerializeValue(a.Unit,DataFormat.Binary),DataFormat.Binary);
            C(injuries,"Validate",restored);
            Check((bool)C(injuries,"Same",a.Unit,restored),"Serialized old tier/identity survives for "+part);
        }
        Reset(a);
        for(int i=0;i<2;i++)
        {
            var legacy=Activator.CreateInstance(T("CompanyInjury"),true);
            S(legacy,"Grade",E("CompanyInjuryGrade","Old"));
            S(legacy,"Part",E("CompanyBodyPart","Arm"));
            S(legacy,"Tier",0); Injuries(a.Unit).Add(legacy);
        }
        Rolls(new double[0]); C(injuries,"Validate",a.Unit);
        var first=Injuries(a.Unit).Cast<object>().Single();
        double roll=(double)G(first,"RecoveryRoll");
        string id=(string)G(first,"Id");
        Check((int)G(first,"Tier")==2 && roll>=0 && roll<1,"Legacy duplicate old records normalize and merge without random draws");
        var copy=SerializationUtility.DeserializeValue<Unit>(SerializationUtility.SerializeValue(a.Unit,DataFormat.Binary),DataFormat.Binary);
        C(injuries,"Validate",copy);
        Check((double)G(Injuries(copy)[0],"RecoveryRoll")==roll && (string)G(Injuries(copy)[0],"Id")==id,"Legacy ID-derived roll remains stable after reload");
        foreach(double invalid in new[]{double.NaN,double.PositiveInfinity,1.0})
        {
            S(Injuries(copy)[0],"RecoveryRoll",invalid);
            bool rejected=false;
            try { C(injuries,"Validate",copy); }
            catch(TargetInvocationException ex) { rejected=ex.InnerException is InvalidOperationException; }
            Check(rejected,"Invalid recovery roll rejected "+invalid);
        }
    }
    static void Distributions()
    {
        const int trials = 10000;
        var injuries = T("CompanyInjuries");
        for (int grade = 0; grade < 3; grade++) foreach (bool treated in new[] {false,true})
        {
            Reset(a);
            int seed = 38110 + grade*2 + (treated ? 1 : 0);
            State.Rand = new Rand(seed);
            var sample = new Sample
            {
                rule = "Recovery " + Grades[grade] + " treated=" + treated, seed = seed, trials = trials,
                expected = grade == 0 ? 0 : grade == 1 ? (treated ? .17 : .33) : (treated ? .33 : .66)
            };
            for (int i = 0; i < trials; i++)
            {
                Injuries(a.Unit).Clear();
                C(injuries,"Add",a.Unit,E("CompanyBodyPart","Arm"),E("CompanyInjuryGrade",Grades[grade]));
                if (treated) C(injuries,"TreatInjuries",a.Unit);
                C(injuries,"Recover",a.Unit,100);
                if (Injuries(a.Unit).Count > 0) sample.observed++;
            }
            SampleCheck(sample);
        }
        foreach(int tier in new[]{2,3})
        {
            Reset(a);Health(a.Unit,tier==2?100:50);int seed=38120+tier;State.Rand=new Rand(seed);
            var sample=new Sample{rule="Daily complications tier "+tier,seed=seed,trials=trials*2,expected=tier==2?.05:.10};
            var choices=new Dictionary<string,int>();
            for(int i=0;i<sample.trials;i++)
            {
                Injuries(a.Unit).Clear();C(injuries,"DailyComplication",a.Unit);
                if(Injuries(a.Unit).Count==0)continue;sample.observed++;var old=Injuries(a.Unit)[0];
                string key=G(old,"Complication").ToString()=="BodyPart"?G(old,"Part").ToString():G(old,"Complication").ToString();
                if(!choices.ContainsKey(key))choices[key]=0;choices[key]++;
            }
            sample.choices=choices.OrderBy(p=>p.Key).Select(p=>p.Key+"="+p.Value).ToList();SampleCheck(sample);
            Check(choices.Count==11 && !choices.ContainsKey("Knee"),"Seeded daily pool has all eleven families and no knees "+tier);
            foreach(var choice in choices)
                Check(Math.Abs(choice.Value-sample.observed/11.0)<=6*Math.Sqrt(sample.observed*(1/11.0)*(10/11.0))+6,
                    "Seeded uniform daily family "+choice.Key+" tier "+tier);
        }
        foreach(int damage in new[]{30,75,150,270,300})
        {
            Reset(a);int seed=38200+damage;State.Rand=new Rand(seed);
            var sample=new Sample{rule="Body hit lasting damage "+damage,seed=seed,trials=trials,expected=Math.Min(.9,damage/300.0)};
            for(int i=0;i<trials;i++)
            {
                Injuries(a.Unit).Clear();
                C(injuries,"RecordDamage",a.Unit,damage);
                if(Injuries(a.Unit).Count>0) sample.observed++;
            }
            SampleCheck(sample);
        }
        Reset(a); a.Unit.SetItem((Item)C(T("CompanyEquipment"),"Create","armor.helmet"),0);
        State.Rand=new Rand(38501);
        double protectedRisk=(double)C(injuries,"RiskMultiplier",a.Unit,E("CompanyBodyPart","Head"));
        var armored=new Sample{rule="Half-HP hit with helmet on two of eight parts",seed=38501,trials=trials,expected=.5*(6+2*protectedRisk)/8};
        for(int i=0;i<trials;i++)
        {
            Injuries(a.Unit).Clear(); C(injuries,"RecordDamage",a.Unit,150);
            if(Injuries(a.Unit).Count>0) armored.observed++;
        }
        SampleCheck(armored);
        Reset(a); Race priorRace=a.Unit.Race;
        try
        {
            S(a.Unit,"Race",Race.SpitterSlugs); State.Rand=new Rand(38502);
            var animal=new Sample{rule="Animal half-HP hit",seed=38502,trials=trials,expected=.5};
            var parts=new HashSet<string>();
            for(int i=0;i<trials;i++)
            {
                Injuries(a.Unit).Clear(); C(injuries,"RecordDamage",a.Unit,150);
                if(Injuries(a.Unit).Count==0) continue;
                animal.observed++; parts.Add(G(Injuries(a.Unit)[0],"Part").ToString());
            }
            SampleCheck(animal);
            Check(parts.SetEquals(new[]{"Head","Chest"}),"Animal injury selection remains head/chest only");
        }
        finally { S(a.Unit,"Race",priorRace); }
    }
    static void PersistenceAndSettlement()
    {
        Reset(a);Reset(b);Health(a.Unit,135);a.Unit.Heal(105);Add(a.Unit,"Eyes","Battered",.2);
        C(T("CompanyInjuries"),"TreatInjuries",a.Unit);Old(a.Unit,"Arm");
        a.Unit.ApplyStatusEffect(StatusEffectType.Frozen,1,3);
        var session=G(gm,"CompanyBattle");State.Rand=new Rand(38301);C(session,"Capture");
        byte[] bytes=(byte[])C(T("CompanyCampaignSave"),"Encode",state);
        File.WriteAllBytes(Path.Combine(report.directory,"injured-battle.bin"),bytes);
        var copy=C(T("CompanyCampaignSave"),"Decode",bytes);C(T("CompanyBattleSnapshot"),"RequirePreserved",state,copy);
        var character=Characters(state).Single(c=>ReferenceEquals(U(c),a.Unit));string id=(string)G(character,"Id");
        var restored=U(Characters(copy).Single(c=>(string)G(c,"Id")==id));
        Check((bool)C(T("CompanyInjuries"),"Same",a.Unit,restored) && restored.Health==240 && (int)G(restored,"TemporaryHealth")==105,"Battle save preserves treated injury, old tier and temporary HP");
        C(session,"Autosave");
        var autoEntry=((IEnumerable)C(T("CompanySaveSlots"),"List")).Cast<object>().Single(entry=>
            (string)G(G(entry,"State"),"Id")== (string)G(state,"Id") && (int)G(G(entry,"State"),"SaveKind")==2);
        var autoState=G(autoEntry,"State");
        C(T("CompanyCampaignSave"),"RequireHealthPreserved",state,autoState);
        Check((bool)C(T("CompanyInjuries"),"Same",a.Unit,U(Characters(autoState).Single(c=>(string)G(c,"Id")==id))),
            "Native battle autosave reload retains the actual injured state");
        string path=(string)C(T("CompanySaveSlots"),"Manual",state,"Injured battle");
        C(view,"LoadCheckpoint",path);state=G(view,"Campaign");session=G(gm,"CompanyBattle");
        a=((IList)G(gm.TacticalMode,"units")).Cast<Actor_Unit>().Single(actor=>ReferenceEquals(actor.Unit,U(Characters(state).Single(c=>(string)G(c,"Id")==id))));
        Check(a.Unit.Health==240 && (int)G(a.Unit,"TemporaryHealth")==105 && (bool)G(a.Unit,"BattleActive"),"Actual battle resume restores total/temporary HP and active flag");
        Check((double)G(Injuries(a.Unit)[0],"RecoveryRoll")==.2 && (bool)G(Injuries(a.Unit)[0],"Treated"),"Actual battle resume retains treatment and occurrence roll");
        var casualty=U(Characters(state).Single(c=>(string)G(c,"Id")==casualtyId));
        var casualtyGear=casualty.Items.Where(i=>i!=null).Select(i=>(string)G(i,"CompanyInstanceId")).ToArray();
        S(casualty,"Digested",true);
        foreach(var enemy in ((IList)G(gm.TacticalMode,"units")).Cast<Actor_Unit>().Where(x=>x.Unit.Side==1))enemy.Fled=true;
        State.Rand=new Rand(38302);C(gm.TacticalMode,"VictoryCheck");
        var result=G(view,"returnedCampaign");Check(result!=null,"Native battle settlement queues canonical campaign");
        var survivor=U(Characters(result).Single(c=>(string)G(c,"Id")==id));
        Check(((IList)G(survivor,"StatusEffects")).Count==0 && !(bool)G(survivor,"BattleActive") && (int)G(survivor,"TemporaryHealth")==0,"Battle settlement clears temporary statuses and HP");
        Check(Injuries(survivor).Count==2 && (double)G(Injuries(survivor)[0],"RecoveryRoll")==.2,"Settlement retains actual injuries and saved recovery roll");
        Check(survivor.Health==135,"Settled survivor loses temporary HP but retains lasting HP");
        var reserve=U(Characters(result).Single(c=>(string)G(c,"Id")==reserveId));
        Check(reserve.Health==reserveHealth && Injuries(reserve).Count==1 && (int)G(Injuries(reserve)[0],"DaysRemaining")==5 && !(bool)G(reserve,"BattleActive"),"Reserve health and injury calendar remain untouched by battle");
        Check(!((IList)G(result,"Members")).Cast<object>().Any(m=>(string)G(m,"CharacterId")==casualtyId) &&
            !((IList)G(result,"Inventory")).Cast<Item>().Any(i=>casualtyGear.Contains((string)G(i,"CompanyInstanceId"))),"Digested mercenary loses worn gear through native settlement");
        var map=C(T("CompanyCampaignSave"),"Decode",C(T("CompanyCampaignSave"),"Encode",result));
        C(T("CompanyCampaignSave"),"RequireHealthPreserved",result,map);File.WriteAllBytes(Path.Combine(report.directory,"injured-map.bin"),(byte[])C(T("CompanyCampaignSave"),"Encode",map));
        Check((bool)C(T("CompanyInjuries"),"Same",survivor,U(Characters(map).Single(c=>(string)G(c,"Id")==id))),"Map save retains recovered battle state and injury identity");
        var mapUnit=U(Characters(map).Single(c=>(string)G(c,"Id")==id));
        C(T("CompanyInjuries"),"Recover",survivor,100);C(T("CompanyInjuries"),"Recover",mapUnit,100);
        Check(Injuries(survivor).Count==1 && Injuries(mapUnit).Count==1,"Stored .2 recovery roll causes no treated eye complication before or after reload");
        C(view,"Update");
        var mapEntry=((IEnumerable)C(T("CompanySaveSlots"),"List")).Cast<object>().Single(entry=>
            (string)G(G(entry,"State"),"Id")== (string)G(result,"Id") && (int)G(G(entry,"State"),"SaveKind")==1);
        C(T("CompanyCampaignSave"),"RequireHealthPreserved",result,G(mapEntry,"State"));
        Check(!(bool)G(G(mapEntry,"State"),"InBattle"),"Post-battle map autosave reload preserves settled campaign");
    }
    static void SingleBattleIsolation()
    {
        int saveFiles=Directory.GetFiles(report.directory,"*",SearchOption.AllDirectories).Length;
        gm.Start_Mode.CreateTactical();var setup=gm.Start_Mode.CreateTacticalGame;
        setup.Attacker.UnitCount.text="1";setup.Defender.UnitCount.text="1";
        setup.Attacker.Level.text="1";setup.Defender.Level.text="1";
        setup.Attacker.MaxSpellLevel.text="1";setup.Defender.MaxSpellLevel.text="1";
        setup.Attacker.AIPlayer.isOn=false;setup.Defender.AIPlayer.isOn=false;
        setup.AutoScale.isOn=false;setup.SizeX.text="16";setup.SizeY.text="16";
        State.Rand=new Rand(38401);setup.CreateTactical();
        var actors=((IList)G(gm.TacticalMode,"units")).Cast<Actor_Unit>().ToArray();
        Check(actors.Length==2 && actors.All(actor=>Injuries(actor.Unit).Count==0),"Single Battle starts fresh uninjured characters");
        var unit=actors.First(actor=>actor.Unit.Side==0).Unit;
        Add(unit,"Arm","Mangled",.1);State.Rand=new Rand(38402);
        unit.ApplyStatusEffect(StatusEffectType.Petrify,1,2);
        for(int turn=0;turn<3;turn++)C(unit,"TickStatusEffects");
        Check(C(unit,"GetStatusEffect",StatusEffectType.Petrify)==null && Injuries(unit).Count==1,"Petrify expires without changing persistent body injuries");
        unit.ApplyStatusEffect(StatusEffectType.Frozen,1,3);unit.Heal(1000);
        foreach(var enemy in actors.Where(actor=>actor.Unit.Side!=0))enemy.Fled=true;
        C(gm.TacticalMode,"VictoryCheck");
        Check(((IList)G(unit,"StatusEffects")).Count==0 && (int)G(unit,"TemporaryHealth")==0,"Single Battle settlement clears temporary effects and HP");
        State.Rand=new Rand(38403);setup.CreateTactical();
        var next=((IList)G(gm.TacticalMode,"units")).Cast<Actor_Unit>().ToArray();
        Check(next.All(actor=>!ReferenceEquals(actor.Unit,unit) && Injuries(actor.Unit).Count==0 && ((IList)G(actor.Unit,"StatusEffects")).Count==0),"Next Single Battle inherits no prior people, injuries or statuses");
        Check(Directory.GetFiles(report.directory,"*",SearchOption.AllDirectories).Length==saveFiles,"Single Battle does not create or change company save files");
    }
}
