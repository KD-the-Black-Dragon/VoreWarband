// 2026-11-06: Isolate and record phase 40C spellbook regression checks.
// 2026-11-06: Keep background-save regression evidence separate (save responsiveness follow-up).
// 2026-11-06: Keep phase-40B grouped regression evidence separate from earlier reviews.
// 2026-11-06: Keep the 40A grouped regression result separate from accepted earlier evidence (spec 40A).
// Added by the VoreWar mercenary-company conversion fork.
// 2026-11-05: Preserve separate grouped regression evidence for injury validation phase D.
// 2026-11-05: Preserve separate grouped regression evidence for display/input phase C.
// 2026-11-05: Preserve separate grouped regression evidence for economy phase B.
// 2026-11-05: Check global Flight retirement and grounded species metadata.
// 2026-11-05: Validate calibrated evasion and keep phase-A regression evidence separate (spec 38).
// 2026-11-05: Measure actual village road journeys and two-exit circuit topology (spec 37).
// 2026-11-05: Check six-zone compact maps, cleared roads, farm art and existing-road save upgrades (spec 36).
// 2026-11-05: Check empty-predator/downed-prey capacity and removal of overcapacity self-damage.
// 2026-11-05: Validate larger-world generation, road connectivity/fog/geometry and old-save geography.
// 2026-11-04: Check healthy power tiers, bounded encounter scaling and fixed-rating generation (spec 34 slice 3).
// 2026-11-04: Check direct-hit armor, evasion, previews, gear persistence and merchant categories (spec 34 slice 4).
// 2026-11-04: Validate tiered stock, gear quotes, starter budget and trade/save invariants (spec 34 slice 5).
// 2026-11-04: Exercise developer gate, isolated native lab and fixture/recovery lifecycle (spec 34 slice 2).
// 2026-11-04: Validate caster-aware targeting and non-mutating spell previews (spec 34 slice 1).
// 2026-11-04: Authorized grouped trait/UI runtime checks using isolated synthetic company data (spec 31).
// 2026-11-04: Extend the grouped review to baseline tiers and approved injuries/recovery (specs 32–33).
// See CHANGES.md and License.txt for GPL-3.0 terms; inherited notices remain applicable.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Run in a separate batch editor. All saves remain under Logs; deletion is intercepted.
[InitializeOnLoad]
public static class TraitCurationAudit
{
    const string Key = "TraitCurationAudit";
    const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    static IEnumerator steps;
    static double started;
    static readonly List<string> checks = new List<string>();
    [Serializable] public class Report { public bool passed; public string error, directory; public List<string> checks; }
    static string directory;
    static TraitCurationAudit() { EditorApplication.update += Update; }
    public static void Run()
    {
        if (Environment.GetCommandLineArgs().Contains("-spellReview"))
            Environment.SetEnvironmentVariable("VOREWAR_AUTHORING_USERDATA", Path.GetFullPath("Logs/spells40c-grouped-client"));
        else if (Environment.GetCommandLineArgs().Contains("-saveReview"))
            Environment.SetEnvironmentVariable("VOREWAR_AUTHORING_USERDATA", Path.GetFullPath("Logs/save41-grouped-client"));
        EditorSceneManager.OpenScene("Assets/base.unity");
        SessionState.SetBool(Key, true);
        EditorApplication.isPlaying = true;
    }
    static void Update()
    {
        int exit = SessionState.GetInt(Key + ".exit", 0);
        if (exit != 0 && !EditorApplication.isPlayingOrWillChangePlaymode)
        { SessionState.EraseInt(Key + ".exit"); EditorApplication.Exit(exit - 1); return; }
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying) return;
        try
        {
            if (steps == null) { started = EditorApplication.timeSinceStartup; steps = Check(); }
            if (EditorApplication.timeSinceStartup - started > 180) throw new TimeoutException("Save runtime checks timed out.");
            if (!steps.MoveNext()) Finish(null);
        }
        catch (Exception ex) { Finish(ex); }
    }
    static void Finish(Exception ex)
    {
        SessionState.SetBool(Key, false);
        var report = new Report { passed = ex == null, error = ex == null ? null : ex.ToString(), directory = directory, checks = checks };
        string resultPath = Environment.GetCommandLineArgs().Contains("-spellReview") ? "Logs/spells40c-grouped-result.json" :
            Environment.GetCommandLineArgs().Contains("-saveReview") ? "Logs/save41-regression-result.json" :
            Environment.GetCommandLineArgs().Contains("-travelReview") ? "Logs/travel40b-regression-result.json" :
            Environment.GetCommandLineArgs().Contains("-balanceAuthoringReview") ? "Logs/balance40a-regression-result.json" :
            Environment.GetCommandLineArgs().Contains("-injuryReview") ? "Logs/injury38d-runtime-result.json" :
            Environment.GetCommandLineArgs().Contains("-displayReview") ? "Logs/display38c-runtime-result.json" :
            Environment.GetCommandLineArgs().Contains("-economyReview") ? "Logs/economy38b-runtime-result.json" :
            Environment.GetCommandLineArgs().Contains("-flightReview") ? "Logs/flight39-runtime-result.json" :
            Environment.GetCommandLineArgs().Contains("-calibrationReview") ? "Logs/calibration38-runtime-result.json" :
            "Logs/roads37-runtime-result.json";
        File.WriteAllText(resultPath, JsonUtility.ToJson(report, true));
        Debug.Log("Trait curation audit: " + (ex == null ? "PASS" : ex.ToString()));
        SessionState.SetInt(Key + ".exit", ex == null ? 1 : 2);
        EditorApplication.isPlaying = false;
    }
    static Type Type(string name) { return typeof(GameManager).Assembly.GetType(name, true); }
    static object Get(object obj, string name)
    {
        var t = obj as Type ?? obj.GetType(); var f = t.GetField(name, Flags);
        return f != null ? f.GetValue(obj is Type ? null : obj) : t.GetProperty(name, Flags).GetValue(obj is Type ? null : obj, null);
    }
    static void Set(object obj, string name, object value)
    {
        var t = obj as Type ?? obj.GetType(); var f = t.GetField(name, Flags);
        if (f != null) f.SetValue(obj is Type ? null : obj, value); else t.GetProperty(name, Flags).SetValue(obj is Type ? null : obj, value, null);
    }
    static object Call(object obj, string name, params object[] args)
    {
        var t = obj as Type ?? obj.GetType();
        return t.GetMethods(Flags).First(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(obj is Type ? null : obj, args);
    }
    static void Expect(bool ok, string text) { if (!ok) throw new Exception(text); checks.Add(text); }
    static bool Rejects(Action action) { try { action(); return false; } catch { return true; } }
    static IList List(object obj, string field) { return (IList)Get(obj, field); }
    static object Character(object state, string id) { return List(state, "Characters").Cast<object>().Single(c => (string)Get(c, "Id") == id); }
    static IEnumerator Check()
    {
        for (int frame = 0; frame < 12; frame++) yield return null;
        var gm = UnityEngine.Object.FindObjectOfType<GameManager>();
        Expect(gm != null, "Base scene initialized");
        directory = Path.GetFullPath("Logs/progression34e-preserved-" + Guid.NewGuid().ToString("N"));
        Set(Type("CompanyCampaignSave"), "DiagnosticDirectory", directory);
        Directory.CreateDirectory(directory);
        if(Environment.GetCommandLineArgs().Contains("-worldOnly"))
        {
            gm.enabled=false;
            CheckWorld(gm,Get(gm.Start_Mode,"CompanySaveView"));yield break;
        }
        var state = Call(Type("CompanyCampaign"), "Generate", 173, false);
        var leader = Call(Type("CompanyEconomy"), "Create", state, Race.Lizards, 2, true, 1);
        Call(Type("CompanyEconomy"), "Begin", state, leader);
        var view = Get(gm.Start_Mode, "CompanySaveView");
        Call(view, "Install", state);
        var contract = List(state, "Contracts")[0];
        Call(Type("CompanyContracts"), "Accept", state, contract);
        Call(Type("CompanyBattleSession"), "Prepare", state, contract);
        Set(Get(state, "Battle"), "PositionCommitted", true);
        Call(view, "LaunchBattle");
        gm.enabled = false;
        // Unity 2018's null graphics device crashes rendering populated tilemaps.
        // This audit checks gameplay/properties, not pixels; prevent headless drawing.
        foreach (var camera in Resources.FindObjectsOfTypeAll<Camera>()) camera.enabled = false;
        foreach (var renderer in Resources.FindObjectsOfTypeAll<UnityEngine.Tilemaps.TilemapRenderer>()) renderer.enabled = false;
        var data = Call(gm.TacticalMode, "Export");
        var actors = List(data, "units").Cast<Actor_Unit>().ToList();
        var a = actors.First(x => x.Unit.Side == (int)Get(data,"attackerSide"));
        var b = actors.First(x => x.Unit.Side != a.Unit.Side);
        a.SetPos(new Vec2i(4,4)); b.SetPos(new Vec2i(5,4));
        if(Environment.GetCommandLineArgs().Contains("-capacityOnly"))
        { CheckCapacity(a,b); yield break; }
        CheckCapacity(a,b);a.SetPos(new Vec2i(4,4));b.SetPos(new Vec2i(5,4));
        var traits = Type("TraitsMethods");
        foreach (Traits t in Enum.GetValues(typeof(Traits)))
            if ((bool)Call(traits,"IsRetired",t))
            {
                ((List<Traits>)Get(a.Unit,"Tags")).Add(t);
                Expect(!a.Unit.HasTrait(t), "Retired trait blocked: " + t);
            }
        Reset(a); Reset(b);
        string help = (string)Call(Type("HelpText"), "Traits");
        Expect((bool)Call(traits, "IsRetired", Traits.Flight) && Call(Type("TraitList"), "GetTrait", Traits.Flight) == null,
            "Flight is unavailable as an active trait");
        Expect(!help.Contains("\nFlight - "), "Help omits retired Flight");
        foreach (var field in Type("RaceParameters").GetFields(Flags))
        {
            if (field.FieldType != Type("RaceTraits")) continue;
            var metadata = field.GetValue(null);
            // Some inactive legacy fields have no prototype. Check every defined baseline.
            if (metadata == null) continue;
            var racialTraits = ((IEnumerable)Get(metadata, "RacialTraits")).Cast<Traits>();
            Expect(!racialTraits.Contains(Traits.Flight), "Species baseline excludes Flight: " + field.Name);
        }
        foreach (Race race in (Race[])Call(Type("SpeciesPolicy"), "GetCuratedSpecies"))
            Expect(Call(Type("RaceParameters"), "GetRaceTraits", race) != null,
                "Curated species has baseline metadata: " + race);
        foreach (var pair in new[] {new[]{"AdaptiveTactics","QuickLearner"},new[]{"DoubleAttack","DoubleShot"},new[]{"AccuteDodge","Dodger I"},new[]{"Defenseless","EasyPrey I"},new[]{"DyingStrike","Vindication"},new[]{"AdeptLearner","Talented"},new[]{"MadScience","MadMage"},new[]{"PackDexterity","PackAgility"}})
            Expect(help.Contains(pair[1]+" - ") && !help.Contains("\n"+pair[0]+" - "), "Help display alias: "+pair[1]);
        CheckBaselineAndInjuries(a, b);
        CheckSpellPreviews(a,b,gm);
        CheckPower(a,b);
        CheckEquipment(a,b);
        if (Environment.GetCommandLineArgs().Contains("-spellReview")) CheckSpellProgression((CompanyCampaign)state, a, b, gm);
        Expect(!help.Contains("\nLightFrame - ") && !help.Contains("\nBlessingOfNature - "), "Help omits retired entries");
        foreach (var test in new[] {new[]{Traits.DoubleAttack},new[]{Traits.Maul},new[]{Traits.FastCaster}})
        {
            Reset(a); ((List<Traits>)Get(a.Unit,"Tags")).Add(test[0]); Call(a.Unit,"InitializeTraits");
            bool spell = test[0] == Traits.FastCaster;
            bool ranged = test[0] == Traits.DoubleAttack;
            a.Movement=2;
            if(spell) Call(a,"SpendSpellAP"); else Call(a,"SpendWeaponAP",ranged,1,false);
            Expect(a.Movement==1,"Standing first action leaves one AP: "+test[0]);
            if(spell) Call(a,"SpendSpellAP"); else Call(a,"SpendWeaponAP",ranged,1,false);
            Expect(a.Movement==0,"Second action spends final AP: "+test[0]);
            Set(a,"WeaponAttackThisTurn",false); Set(a,"SpellCastThisTurn",false); Set(a,"MovedThisTurn",true);a.Movement=2;
            if(spell) Call(a,"SpendSpellAP"); else Call(a,"SpendWeaponAP",ranged,1,false);
            Expect(a.Movement==0,"Moved actor gets no extra action: "+test[0]);
        }
        Reset(a); ((List<Traits>)Get(a.Unit,"Tags")).Add(Traits.DoubleAttack);Call(a.Unit,"InitializeTraits");a.Movement=2;Call(a,"SpendWeaponAP",false,1,false);
        Expect(a.Movement==0,"DoubleShot does not enhance melee");
        Reset(a); ((List<Traits>)Get(a.Unit,"Tags")).Add(Traits.Maul);Call(a.Unit,"InitializeTraits");a.Movement=2;Call(a,"SpendWeaponAP",true,1,false);
        Expect(a.Movement==0,"Maul does not enhance ranged");
        Reset(a); Reset(b);
        float baseChance=b.GetAttackChance(a,false);
        ((List<Traits>)Get(b.Unit,"Tags")).Add(Traits.AccuteDodge);Call(b.Unit,"InitializeTraits");
        Expect(Math.Abs(b.GetAttackChance(a,false)-Math.Max(0,baseChance-.10f))<.0001f,"Dodger subtracts ten percentage points");
        Reset(b);float vore=b.GetDevourChance(a);
        ((List<Traits>)Get(b.Unit,"Tags")).Add(Traits.Defenseless);Call(b.Unit,"InitializeTraits");
        Expect(Math.Abs(b.GetDevourChance(a)-Math.Min(1,vore+.10f))<.0001f,"EasyPrey I adds ten percentage points");
        Reset(b); b.Unit.SetStatBase(Stat.Intelligence,50);float unDazzled=b.GetAttackChance(a,false);
        ((List<Traits>)Get(b.Unit,"Tags")).Add(Traits.Dazzle);Call(b.Unit,"InitializeTraits");
        Expect(Math.Abs(b.GetAttackChance(a,false)-Math.Max(0,unDazzled-.10f))<.0001f,"Dazzle uses INT accuracy reduction");
        Reset(a);Reset(b);((List<Traits>)Get(a.Unit,"Tags")).Add(Traits.KeenEye);Call(a.Unit,"InitializeTraits");
        foreach(float wanted in new[]{.50f,.51001f})
        {
            Set(Get(b.Unit,"TraitBoosts"),"FlatHitReduction",1f);
            float raw=b.GetAttackChance(a,false);
            Set(Get(b.Unit,"TraitBoosts"),"FlatHitReduction",wanted/raw);
            float actual=b.GetAttackChance(a,false);
            int misses=0,grazes=0;
            for(int i=0;i<300;i++)
            {
                object[] args={b,false,0f};
                int result=(int)a.GetType().GetMethod("ResolveWeaponHit",Flags).Invoke(a,args);
                if(result==0)misses++; if(result==3)grazes++;
            }
            Expect(wanted>.5f ? misses==0 && grazes>0 : grazes==0 && misses>0,"KeenEye threshold sample at "+actual);
        }
        Reset(a);((List<Traits>)Get(a.Unit,"Tags")).Add(Traits.AdaptiveTactics);Call(a.Unit,"InitializeTraits");
        Expect(Math.Abs((float)Get(Get(a.Unit,"TraitBoosts"),"ExpGain")-1.1f)<.0001f,"QuickLearner grants ten percent XP");
        Reset(a);Set(a,"TurnsSlacking",0);
        ((List<Traits>)Get(a.Unit,"Tags")).Add(Traits.Juggernaut);Call(a.Unit,"InitializeTraits");
        Call(a,"RestoreMP");
        Expect(a.Movement>0 && (bool)Call(Type("OpportunityAttacks"),"CanAct",a),"Juggernaut active turn remains actionable");
        Call(a,"RestoreMP");
        Expect(a.Movement==0 && !(bool)Call(Type("OpportunityAttacks"),"CanAct",a),"Juggernaut skipped turn cannot act or react");
        Call(a,"RestoreMP");
        Expect(a.Movement>0 && (bool)Call(Type("OpportunityAttacks"),"CanAct",a),"Juggernaut acts again after skipped turn");
        Reset(a);Set(a,"MovedThisTurn",true);Set(a,"WeaponAttackThisTurn",true);Set(a,"SpellCastThisTurn",true);Set(a,"SkippedBySlacking",true);
        var bytes=OdinSerializer.SerializationUtility.SerializeValue(a,OdinSerializer.DataFormat.Binary);
        var restored=OdinSerializer.SerializationUtility.DeserializeValue<Actor_Unit>(bytes,OdinSerializer.DataFormat.Binary);
        Expect((bool)Get(restored,"MovedThisTurn") && (bool)Get(restored,"WeaponAttackThisTurn") &&
            (bool)Get(restored,"SpellCastThisTurn") && (bool)Get(restored,"SkippedBySlacking"),"New turn eligibility flags survive actor serialization");
        Reset(a);Reset(b);
        ((List<Traits>)Get(a.Unit,"Tags")).Add(Traits.ArcaneMagistrate);Call(a.Unit,"InitializeTraits");
        b.Unit.Health=1;
        var fireball=Get(Type("SpellList"),"Fireball");
        bool resist=(bool)Get(fireball,"Resistable");Set(fireball,"Resistable",false);
        try { Call(b,"DefendDamageSpell",fireball,a,10000); }
        finally { Set(fireball,"Resistable",resist); }
        var focus=Call(a.Unit,"GetStatusEffect",StatusEffectType.Focus);
        Expect(b.Surrendered && focus!=null && (int)Get(focus,"Duration")==5,"ArcaneMagistrate gets full bonus on newly downing a target");
        Reset(a);Reset(b);
        ((List<Traits>)Get(b.Unit,"Tags")).Add(Traits.DyingStrike);Call(b.Unit,"InitializeTraits");
        Set(Get(a.Unit,"TraitBoosts"),"CapacityMult",10000f);
        Set(Get(Get(a.Unit,"TraitBoosts"),"Outgoing"),"DigestionRate",1000000f);
        b.Unit.Health=1;Set(b.Unit,"Downed",true);a.Movement=2;
        bool swallowed=(bool)Call(a.PredatorComponent,"Devour",b,0f);
        Expect(swallowed,"Synthetic Vindication target swallowed through native vore action");
        Call(a.PredatorComponent,"Digest","");
        string battleLog=(string)Call(Get(gm.TacticalMode,"Log"),"DebugDump");
        Expect(battleLog.Contains("Vindication struck"),"Vindication executes on completed digestion");
        Reset(a);
        ((List<Traits>)Get(a.Unit,"Tags")).Add(Traits.EvasiveBattler);Call(a.Unit,"InitializeTraits");
        Set(gm.TacticalMode,"currentTurn",4);a.Movement=2;
        gm.TacticalMode.SelectedUnit=a;
        Call(gm.TacticalMode,"EvasiveRetreat");
        Expect(a.Fled,"EvasiveBattler can retreat from the interior on turn four");
        gm.StatScreen.VictoryType.text="Defenders victorious";
        gm.StatScreen.Open(false);
        Expect(gm.StatScreen.VictoryType.fontSize==48 && gm.StatScreen.AttackerTitle.fontSize==32 && gm.StatScreen.AttackerText.fontSize==24,"Outcome font hierarchy configured");
        Expect(gm.StatScreen.AttackerText.lineSpacing>1 && gm.StatScreen.transform.Find("Outcome divider")!=null,"Outcome line spacing and divider configured");
        foreach(var step in CheckDeveloperLab(gm))yield return step;
        CheckMerchant(gm,view);
    }
    static void CheckEquipment(Actor_Unit a,Actor_Unit b)
    {
        var equipment=Type("CompanyEquipment");
        for(int damage=0;damage<=16;damage++)for(int armor=0;armor<=12;armor++)for(int barrier=0;barrier<=5;barrier++)
        {
            double expected=Enumerable.Range(0,armor+1).Average(roll=>(double)Math.Max(0,Math.Max(1,damage-roll)-barrier));
            double actual=(double)Call(equipment,"ExpectedAfterArmor",damage,armor,barrier);
            if(Math.Abs(expected-actual)>.000001)throw new Exception("Armor expectation mismatch: "+damage+"/"+armor+"/"+barrier);
        }
        Expect(true,"Armor expectation matches exhaustive integer rolls, floors and barriers (1326 cases)");
        var savedItems=b.Unit.Items.ToArray();bool oldLoadout=(bool)Get(b.Unit,"CompanyLoadout");var rng=(int[])Call(State.Rand,"Capture");
        try
        {
            Reset(a);Reset(b);Set(b.Unit,"CompanyLoadout",true);
            if(b.Unit.Items.Length<9)Set(b.Unit,"Items",new Item[9]);
            for(int i=0;i<4;i++)b.Unit.SetItem(null,i,false);
            float melee=b.GetAttackChance(a,false),ranged=b.GetAttackChance(a,true),vore=b.GetDevourChance(a);
            var fire=Get(Type("SpellList"),"Fireball");float magic=(float)Call(b,"GetMagicChance",a,fire,0f,Stat.Intelligence);
            var bolt=Get(Type("SpellList"),"PowerBolt");float guaranteed=(float)Call(b,"GetMagicChance",a,bolt,0f,Stat.Intelligence);
            b.Unit.SetItem((Item)Call(equipment,"Create","armor.legs"),2,false);
            // Leg armor also preserves its old STR bonus; attacker's accuracy stats are unchanged.
            Expect(Math.Abs(b.GetAttackChance(a,false)-Math.Max(0,melee-.06f))<.0001f,"Gear evasion subtracts six melee accuracy points");
            Expect(Math.Abs(b.GetAttackChance(a,true)-Math.Max(0,ranged-.06f))<.0001f,"Gear evasion subtracts six ranged accuracy points");
            Expect(Math.Abs((float)Call(b,"GetMagicChance",a,fire,0f,Stat.Intelligence)-Math.Max(0,magic-.06f))<.0001f,"Gear evasion reduces resistible spell accuracy");
            Expect(Math.Abs(b.GetDevourChance(a)-vore)<.0001f,"Gear evasion does not change vore accuracy");
            Expect((float)Call(b,"GetMagicChance",a,bolt,0f,Stat.Intelligence)==guaranteed,"Gear evasion does not affect guaranteed-hit spells");
            b.Unit.SetItem((Item)Call(equipment,"Create","armor.helmet"),0,false);
            b.Unit.SetItem((Item)Call(equipment,"Create","armor.torso"),1,false);
            b.Unit.SetItem((Item)Call(equipment,"Create","armor.arms"),3,false);
            Expect((int)Call(equipment,"ArmorClass",b.Unit)==4,"Armor class adds across its correct slots");
            var rolls=new HashSet<int>();for(int i=0;i<256;i++)rolls.Add(10-(int)Call(equipment,"Deflect",b.Unit,10));
            Expect(rolls.SetEquals(new[]{0,1,2,3,4}),"AC4 rolls zero through four deflection");
            Expect((int)Call(equipment,"Deflect",b.Unit,1)==1,"Armor cannot reduce a damaging hit below one");
            int hp=b.Unit.Health;b.Damage(10,false,true,DamageTypes.Generic,true,false);
            Expect(hp-b.Unit.Health>=6 && hp-b.Unit.Health<=10,"Native physical hit uses armor once");
            Reset(b);hp=b.Unit.Health;b.Damage(10,true,true,DamageTypes.Fire,true,false);
            Expect(hp-b.Unit.Health>=6 && hp-b.Unit.Health<=10,"Native magic hit uses the same armor");
            Reset(b);hp=b.Unit.Health;bool resist=(bool)Get(fire,"Resistable");Set(fire,"Resistable",false);
            try {Call(b,"DefendDamageSpell",fire,a,10);}
            finally {Set(fire,"Resistable",resist);}
            Expect(hp-b.Unit.Health>=6 && hp-b.Unit.Health<=10,"Full damaging-spell pipeline applies armor exactly once");
            Reset(b);hp=b.Unit.Health;b.Damage(10,false,true,DamageTypes.Poison,false,false);
            Expect(hp-b.Unit.Health==10,"Ongoing poison damage bypasses armor");
            Reset(b);hp=b.Unit.Health;Call(b,"SubtractHealth",10,false);
            Expect(hp-b.Unit.Health==10,"Direct digestion/attrition health loss bypasses armor");
            Reset(b);Set(b.Unit,"Barrier",8);Set(b.Unit,"TemporaryHealth",5);int lasting=(int)Get(b.Unit,"LastingHealth");
            b.Damage(10,false,true,DamageTypes.Generic,true,false);
            Expect((int)Get(b.Unit,"LastingHealth")==lasting && (int)Get(b.Unit,"TemporaryHealth")>=3,"Armor precedes barrier and temporary HP");
            Reset(b);Set(b.Unit,"Barrier",0);
            int preview=(int)Call(b,"PreviewHitDamage",10);var before=(int[])Call(State.Rand,"Capture");int mana=(int)Get(b.Unit,"Mana");
            for(int i=0;i<30;i++){Call(b,"PreviewHitDamage",10);Call(b,"PreviewSpellDamage",a,10,DamageTypes.Generic);}
            Expect(preview==8 && mana==(int)Get(b.Unit,"Mana") && before.SequenceEqual((int[])Call(State.Rand,"Capture")),"Weapon/spell armor previews preserve RNG and resources");
            b.Unit.AddTrait(Traits.ManaBarrier);Call(b.Unit,"RestoreManaPct",1f);mana=(int)Get(b.Unit,"Mana");
            preview=(int)Call(b,"PreviewHitDamage",10);hp=b.Unit.Health;b.Damage(10,false,true,DamageTypes.Generic,true,false);
            Expect(mana>(int)Get(b.Unit,"Mana") && preview>=1 && preview<=hp-b.Unit.Health+4,"Mana defense resolves before armor");
            var bytes=OdinSerializer.SerializationUtility.SerializeValue(b.Unit,OdinSerializer.DataFormat.Binary);
            var restored=OdinSerializer.SerializationUtility.DeserializeValue<Unit>(bytes,OdinSerializer.DataFormat.Binary);
            Expect((int)Call(equipment,"ArmorClass",restored)==4 && Math.Abs((float)Call(equipment,"Evasion",restored)-.06f)<.0001f,"Item identities preserve armor/evasion after serialization");
            Set(b.Unit,"CompanyLoadout",false);Set(b.Unit,"Items",new[]{(Item)Call(equipment,"Create","armor.helmet"),null});
            Expect((int)Call(equipment,"ArmorClass",b.Unit)==1,"Original Single Battle two-slot armor remains supported");
        }
        finally {Set(b.Unit,"Items",savedItems);Set(b.Unit,"CompanyLoadout",oldLoadout);Reset(a);Reset(b);State.Rand=(Rand)Call(typeof(Rand),"Resume",(object)rng);}
    }
    static void CheckMerchant(GameManager gm,object view)
    {
        var state=Call(Type("CompanyCampaign"),"Generate",918,false);
        var leader=Call(Type("CompanyEconomy"),"Create",state,Race.Gnolls,0,true,1);Call(Type("CompanyEconomy"),"Begin",state,leader);
        Call(view,"Install",state);var ui=Get(view,"management");Call(ui,"Shop",0);
        var page=(GameObject)Get(ui,"Page");
        foreach(var label in new[]{"All","Helmet","Torso Armor","Leg Armor","Arm Armor","Accessory","Melee Weapon","Ranged Weapon","Spells"})
            Expect(page.GetComponentsInChildren<UnityEngine.UI.Button>().Any(b=>b.GetComponentInChildren<UnityEngine.UI.Text>().text==label),"Merchant category visible: "+label);
        foreach(int category in Enumerable.Range(0,8))
        {
            Set(ui,"shopCategory",category);Call(ui,"Shop",0);
            var text=string.Join("\n",page.GetComponentsInChildren<UnityEngine.UI.Text>().Select(t=>t.text).ToArray());
            var defs=((IEnumerable)Call(Type("CompanyEquipment"),"Available",state)).Cast<object>().Where(d=>(int)Get(d,"Slot")==category).ToArray();
            Expect(defs.All(d=>text.Contains((string)Get(d,"Name"))),"Merchant filter shows matching slot: "+category);
        }
        var inventory=List(state,"Inventory");var armor=(Item)Call(Type("CompanyEquipment"),"Create","armor.torso");inventory.Add(armor);
        Call(Type("CompanyEquipment"),"Equip",state,leader,armor,1);
        Call(ui,"Slot",(string)Get(leader,"Id"),1,0);
        Expect(page.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t=>t.text.StartsWith("Equipped:") && t.text.Contains("AC +2")),"Loadout picker compares equipped armor mechanics");
        var encoded=(byte[])Call(Type("CompanyCampaignSave"),"Encode",state);var restored=Call(Type("CompanyCampaignSave"),"Decode",encoded);
        var character=List(restored,"Characters").Cast<object>().First(c=>(string)Get(c,"Id")== (string)Get(leader,"Id"));
        var unit=(Unit)Get(character,"Unit");
        Expect((int)Call(Type("CompanyEquipment"),"ArmorClass",unit)==2 && (string)Get(unit.Items[1],"CompanyInstanceId")== (string)Get(armor,"CompanyInstanceId"),"Campaign gear identity survives save/reload without re-equipping");
        CheckGearProgression(state,ui,page);
    }
    [Serializable] public class GearRow { public string id,name,slot,summary; public int tier,reputation,cost; }
    [Serializable] public class GearTable { public int startingGold; public List<GearRow> items=new List<GearRow>(); }
    static void CheckGearProgression(object state,object ui,GameObject page)
    {
        var equipment=Type("CompanyEquipment");var economy=Type("CompanyEconomy");
        var catalog=((IEnumerable)Get(equipment,"Catalog")).Cast<object>().ToArray();
        Expect(catalog.Select(d=>(string)Get(d,"Id")).Distinct().Count()==catalog.Length,"Progression catalog IDs are unique");
        var table=new GearTable{startingGold=(int)Get(economy,"StartingGold")};
        foreach(var d in catalog.Where(d=>!(bool)Get(d,"EnemyOnly")))
        {
            int tier=(int)Get(d,"Tier");string id=(string)Get(d,"Id");var item=(Item)Call(equipment,"Create",id);
            Expect(tier>=1 && tier<=3 && (int)Get(d,"Cost")==item.Cost,"Tier and price are valid: "+id);
            Expect((bool)Call(equipment,"CanPlace",item,(int)Get(d,"Slot")),"Tiered item fits its slot: "+id);
            table.items.Add(new GearRow{id=id,name=item.Name,slot=Get(d,"Slot").ToString(),tier=tier,cost=item.Cost,
                reputation=(int)Call(equipment,"RequiredReputation",tier),summary=(string)Call(equipment,"Summary",item)});
        }
        File.WriteAllText("Logs/progression34e-catalog.json",JsonUtility.ToJson(table,true));
        foreach(int rep in new[]{0,9,10,24,25,100})
        {
            Set(state,"Reputation",rep);var before=(uint)Get(state,"RandomState");
            var stock=((IEnumerable)Call(equipment,"Available",state)).Cast<object>().ToArray();
            int tier=(int)Call(equipment,"UnlockedTier",rep);
            Expect(stock.Length>0 && stock.All(d=>(int)Get(d,"Tier")<=tier) && stock.All(d=>!(bool)Get(d,"EnemyOnly")),"Whole-tier stock boundary at reputation "+rep);
            Expect(catalog.Where(d=>!(bool)Get(d,"EnemyOnly") && (int)Get(d,"Tier")<=tier).All(stock.Contains),"Lower-tier stock remains available: "+rep);
            Expect(before==(uint)Get(state,"RandomState"),"Stock inspection preserves campaign RNG: "+rep);
        }
        Set(state,"Gold",100000);Set(state,"Reputation",0);int gold=(int)Get(state,"Gold"),count=List(state,"Inventory").Count;
        Expect(Rejects(()=>Call(equipment,"Buy",state,"weapon.mace.3")) && (int)Get(state,"Gold")==gold && List(state,"Inventory").Count==count,"Direct purchases cannot bypass reputation or mutate a failed purchase");
        Set(state,"Reputation",25);Call(equipment,"Buy",state,"weapon.mace.3");Call(equipment,"Buy",state,"weapon.mace.3");
        var bought=List(state,"Inventory").Cast<Item>().Where(i=>(string)Get(i,"CompanyDefinitionId")=="weapon.mace.3").ToArray();
        Expect(bought.Length==2 && !ReferenceEquals(bought[0],bought[1]) && (string)Get(bought[0],"CompanyInstanceId")!=(string)Get(bought[1],"CompanyInstanceId"),"Unlimited stock creates distinct owned items");
        int position=(int)Get(state,"Position");var stockHere=((IEnumerable)Call(equipment,"Available",state)).Cast<object>().Select(d=>(string)Get(d,"Id")).ToArray();
        var otherTown=List(state,"Towns").Cast<object>().First(t=>(int)Get(t,"Tile")!=position);Set(state,"Position",Get(otherTown,"Tile"));
        Expect(stockHere.SequenceEqual(((IEnumerable)Call(equipment,"Available",state)).Cast<object>().Select(d=>(string)Get(d,"Id"))),"Every town offers the same eligible stock");Set(state,"Position",position);
        foreach(int tier in new[]{1,2,3})foreach(int slot in Enumerable.Range(0,4))
        {
            var armor=catalog.Where(d=>(int)Get(d,"Tier")==tier && (int)Get(d,"Slot")==slot).ToArray();
            Expect(armor.Any(d=>(int)Get(d,"ArmorClass")>0) && armor.Any(d=>(float)Get(d,"Evasion")>0) && armor.All(d=>(int)Get(d,"ArmorClass")>=0 && (float)Get(d,"Evasion")>=0),"AC/evasion alternatives without penalties: tier "+tier+" slot "+slot);
        }
        foreach (int tier in new[] { 1, 2, 3 })
        {
            var light = catalog.Where(d => (int)Get(d, "Tier") == tier && (int)Get(d, "Slot") < 4 &&
                (float)Get(d, "Evasion") > 0).ToArray();
            double evasion = light.Sum(d => (double)(float)Get(d, "Evasion"));
            Expect(light.Length == 4 && Math.Abs(evasion - new[] { .16, .28, .40 }[tier - 1]) < .00001,
                "Calibrated full light armor evasion at tier " + tier);
        }
        foreach(int rep in new[]{0,10,25})
        {
            Set(state,"Reputation",rep);Set(ui,"shopCategory",5);Call(ui,"Shop",0);
            string labels=string.Join("\n",page.GetComponentsInChildren<UnityEngine.UI.Text>().Select(t=>t.text).ToArray());
            Expect(labels.Contains("Next tier:") || labels.Contains("All tiers available"),"Merchant explains tier availability: "+rep);
            Expect(rep>=25 || !labels.Contains("Masterwork mace"),"Merchant hides locked equipment: "+rep);
        }
        Set(state,"Reputation",0);Set(ui,"shopCategory",5);Call(ui,"Shop",1000);
        Expect(page.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t=>t.text.StartsWith("Mace —")),"Merchant clamps stale page after reputation loss");
        var starter=Call(Type("CompanyCampaign"),"Generate",456,false);
        var leader=Call(economy,"Create",starter,Race.Lizards,2,true,1);Call(economy,"Begin",starter,leader);
        var candidates=List(starter,"Characters").Cast<object>().Where(c=>Get(c,"Employment").ToString()=="Candidate").ToArray();
        var prices=candidates.Select(c=>(int)Call(economy,"HirePrice",starter,c)).OrderBy(x=>x).ToArray();
        int initial=(int)Get(starter,"Gold");
        Expect(prices.Length>=3 && prices.Reverse().Take(2).Sum()<=initial && prices.Take(3).Sum()>initial,"Starting funds cover any two recruits but not three");
        var c1=candidates[0];var town=List(starter,"Towns").Cast<object>().First(t=>(int)Get(t,"Tile")== (int)Get(starter,"Position"));
        Set(starter,"ElapsedDays",5);Set(c1,"Debt",73);
        int gear=(int)Call(equipment,"GearPrice",Get(c1,"Unit")),advance=(int)Call(economy,"AdvanceWages",starter,c1),quote=(int)Call(economy,"HirePrice",starter,c1);
        Expect(quote==gear+advance+73 && advance==(int)Math.Ceiling((int)Call(economy,"Wage",starter,c1)*2/7.0),"Hire quote includes exact gear, prorated advance and full debt");
        Set(starter,"Gold",10000);Call(economy,"Hire",starter,c1,town);
        Expect((int)Get(starter,"Gold")==10000-quote && (int)Get(c1,"Debt")==0,"Hiring charges the quote once and clears debt");
        var recruit=(Unit)Get(c1,"Unit");var items=recruit.Items.ToArray();
        for(int slot=0;slot<items.Length;slot++)if(items[slot]!=null){Call(equipment,"Equip",starter,c1,null,slot);Call(equipment,"Sell",starter,items[slot]);}
        Expect((int)Get(starter,"Gold")<10000-advance-73,"Stripping and reselling recruit gear cannot make a profit");
        var owned=(Item)Call(equipment,"Create","weapon.mace.3");List(starter,"Inventory").Add(owned);Call(equipment,"Equip",starter,c1,owned,5);Call(economy,"Leave",starter,c1);
        Set(c1,"Debt",21);Set(c1,"TownId",Get(town,"Id"));
        Expect((int)Call(economy,"HirePrice",starter,c1)==(int)Call(equipment,"Value",owned)+(int)Call(economy,"AdvanceWages",starter,c1)+21,"Former mercenary quote includes retained gear and debt");
        Set(starter,"Reputation",0);var payload=(byte[])Call(Type("CompanyCampaignSave"),"Encode",starter);var restored=Call(Type("CompanyCampaignSave"),"Decode",payload);
        var former=List(restored,"Characters").Cast<object>().Single(c=>(string)Get(c,"Id")== (string)Get(c1,"Id"));
        Expect((string)Get(((Unit)Get(former,"Unit")).Items[5],"CompanyInstanceId")== (string)Get(owned,"CompanyInstanceId"),"Owned high-tier gear survives save/reload below its shop reputation requirement");
        Expect((int)Get(restored,"Gold")== (int)Get(starter,"Gold"),"Save/reload does not grant the new starting treasury to existing campaigns");
    }
    static object E(string type, string value) { return Enum.Parse(Type(type), value); }
    static void Injury(Actor_Unit actor, string part, string grade, double roll = .9, bool treated = false)
    {
        Call(Type("CompanyInjuries"), "Add", actor.Unit, E("CompanyBodyPart", part), E("CompanyInjuryGrade", grade));
        var injury = List(actor.Unit, "Injuries").Cast<object>().Last();
        Set(injury, "RecoveryRoll", roll); Set(injury, "Treated", treated);
    }
    static void Old(Actor_Unit actor, string part, string kind = "BodyPart")
    { Call(Type("CompanyInjuries"), "AddOld", actor.Unit, E("CompanyBodyPart", part), E("CompanyComplication", kind)); }
    static void CheckBaselineAndInjuries(Actor_Unit a, Actor_Unit b)
    {
        var tiers = Type("TieredTraits"); var injuries = Type("CompanyInjuries");
        var families = ((IEnumerable)Get(tiers, "Families")).Cast<object>().ToArray();
        Expect(families.Length == 28, "All 28 baseline families defined");
        foreach (var family in families)
        {
            var variants = (Traits[])Get(family, "Tiers"); var effect = Get(family, "Effect");
            string effectName = effect.ToString(), name = (string)Get(family, "Name");
            int sign = (int)Get(family, "Sign");
            for (int i = 0; i < 3; i++)
            {
                Reset(a); a.Unit.AddTrait(variants[i]);
                float expected = effectName == "CriticalChance" ? new[] { .05f, .10f, .15f }[i] :
                    effectName.Contains("Accuracy") ? new[] { .10f, .20f, .30f }[i] : new[] { 1f, 2f, 4f }[i];
                if (effectName == "MaxHealth" || effectName == "MaxMana") expected *= a.Unit.Level;
                float actual = (float)Call(tiers, "Amount", a.Unit, effect);
                Expect(Math.Abs(actual - expected * sign) < .0001f, name + " tier " + (i + 1) + " modifier");
                string label = (string)Call(Type("TraitsMethods"), "DisplayName", variants[i]);
                var parse = Type("TraitsMethods").GetMethod("TryParse", Flags); var args = new object[] { label, default(Traits) };
                Expect((bool)parse.Invoke(null, args) && (Traits)args[1] == variants[i], name + " tier " + (i + 1) + " hover label resolves");
            }
            Reset(a); a.Unit.AddTrait(variants[0]); a.Unit.AddTrait(variants[1]); a.Unit.AddTrait(variants[2]); a.Unit.AddTrait(variants[0]);
            Expect(!a.Unit.HasTrait(variants[0]) && !a.Unit.HasTrait(variants[1]) && a.Unit.HasTrait(variants[2]), name + " upgrades exclusively without downgrade");
        }
        Reset(a); a.Unit.AddTrait(Traits.PreciseIII); a.Unit.AddTrait(Traits.ClumsyII);
        Expect(!a.Unit.HasTrait(Traits.PreciseIII) && a.Unit.HasTrait(Traits.ClumsyII), "Opposite accuracy family replaces prior trait");
        Reset(a); Call(a.Unit, "AddSharedTrait", Traits.Deadeye); a.Unit.AddTrait(Traits.DeadeyeII);
        Expect(a.Unit.HasTrait(Traits.DeadeyeII) && !a.Unit.HasTrait(Traits.Deadeye), "Shared tier upgrades do not stack");
        Reset(a); ((List<Traits>)Get(a.Unit,"Tags")).Add(Traits.MetalBody); ((List<Traits>)Get(a.Unit,"Tags")).Add(Traits.AwkwardShape); Call(a.Unit,"InitializeTraits");
        Expect(List(a.Unit,"GetTraits").Cast<Traits>().Count(t => t == Traits.AwkwardShape) == 1 &&
            Math.Abs((float)Get(Get(Get(a.Unit,"TraitBoosts"),"Outgoing"),"Nutrition") - 1) < .0001f, "Legacy HardPrey folds into one tier without nutrition drawback");
        Reset(a); Reset(b);
        Set(b.Unit,"Type",UnitType.Soldier); Set(b.Unit,"PermanentInjuryImmune",false);
        int hp=b.Unit.MaxHealth, str=b.Unit.GetStat(Stat.Strength);
        b.Unit.AddTrait(Traits.Fit);
        Expect(b.Unit.MaxHealth==hp && b.Unit.GetStat(Stat.Strength)>str, "Modified STR never changes normal maximum HP");
        Reset(b); hp=b.Unit.MaxHealth;
        foreach(var test in new[]{new[]{.80f,0f},new[]{.70f,1f},new[]{.45f,2f},new[]{.20f,3f}})
        {
            Set(b.Unit,"BattleActive",false);b.Unit.Health=(int)(hp*test[0]);
            Expect((int)Call(injuries,"DamageTier",b.Unit)==(int)test[1], "Accumulated damage tier at "+test[0]);
            Expect(b.Unit.MaxHealth==hp && b.Unit.GetStat(Stat.Strength)==Mathf.RoundToInt(40*(1-.1f*test[1])), "Damage penalty changes combat stats, not HP denominator");
        }
        b.Unit.Health=(int)(hp*.45f); Set(b.Unit,"BattleActive",true); Set(b.Unit,"TemporaryHealth",0);
        b.Unit.Heal(10000);
        Expect((int)Call(injuries,"DamageTier",b.Unit)==0, "Temporary healing immediately removes accumulated damage penalty");
        Call(b.Unit,"FinishCombat");
        Expect((int)Call(injuries,"DamageTier",b.Unit)==2, "Expiring temporary HP restores appropriate damage penalty");
        Reset(b);Set(b.Unit,"BattleActive",false);hp=b.Unit.MaxHealth;b.Unit.Health=hp;
        Injury(b,"Chest","Bruised"); Old(b,"Chest");
        Expect((int)Get(b.Unit,"HealingCeiling")==Mathf.FloorToInt(hp*.85f+.0001f) && b.Unit.Health==hp && b.Unit.MaxHealth==hp, "Chest and old injury add a healing ceiling without losing current HP");
        Call(b.Unit,"HealLasting",10000);
        Expect(b.Unit.Health==hp, "Healing above ceiling never reduces existing HP");
        b.Unit.Health=hp/2;Call(b.Unit,"HealLasting",10000);
        Expect(b.Unit.Health==(int)Get(b.Unit,"HealingCeiling"), "Lasting healing respects combined ceiling");
        foreach(string part in new[]{"Head","Eyes","Arm","Hand","Chest","Abdomen","Leg","Knee"})
        {
            Reset(b);for(int i=0;i<4;i++)Old(b,part);
            var old=List(b.Unit,"Injuries").Cast<object>().Single();
            Expect((int)Get(old,"Tier")==3, "Old injury caps at III: "+part);
        }
        foreach(string kind in new[]{"PoorFocus","DulledInstincts","SluggishRecovery","SensitiveStomach"})
        {
            Reset(b); for(int i=0;i<4;i++)Old(b,"Head",kind);
            Expect(List(b.Unit,"Injuries").Count==1 && (int)Get(List(b.Unit,"Injuries")[0],"Tier")==3,"Systemic complication caps at III: "+kind);
        }
        foreach(var test in new[]{new[]{"Bruised","0","no"},new[]{"Battered","0.25","yes"},new[]{"Battered","0.25","no","treated"},new[]{"Mangled","0.5","yes"},new[]{"Mangled","0.5","no","treated"}})
        {
            Reset(b);Injury(b,"Arm",test[0],double.Parse(test[1],System.Globalization.CultureInfo.InvariantCulture),test.Length>3);
            Call(injuries,"Recover",b.Unit,100);
            Expect((List(b.Unit,"Injuries").Count>0)==(test[2]=="yes"),"Stored complication roll and treatment: "+string.Join("/",test));
        }
        Reset(b);Injury(b,"Eyes","Battered",.2,true);
        var bytes=OdinSerializer.SerializationUtility.SerializeValue(b.Unit,OdinSerializer.DataFormat.Binary);
        var restored=OdinSerializer.SerializationUtility.DeserializeValue<Unit>(bytes,OdinSerializer.DataFormat.Binary);
        Expect((bool)Call(injuries,"Same",b.Unit,restored),"Injury grade, tier, treatment and hidden roll survive serialization");
        Reset(b);for(int i=0;i<200;i++)Call(injuries,"RecordDamage",b.Unit,1);
        Expect(List(b.Unit,"Injuries").Count==0,"Sub-threshold hits cannot cause body-part injuries");
        Reset(b);Call(b.Unit,"ApplyCombatDamage",b.Unit.MaxHealth/2,false);
        Expect(List(b.Unit,"Injuries").Count==0,"Graze-exempt damage never rolls a body-part injury");
        Reset(b);Set(b.Unit,"TemporaryHealth",b.Unit.MaxHealth/2);Call(b.Unit,"ApplyCombatDamage",b.Unit.MaxHealth/3,true);
        Expect(List(b.Unit,"Injuries").Count==0,"Temporary HP alone shields against body-part injury rolls");
        Reset(b);Old(b,"Knee");Old(b,"Knee");Old(b,"Knee");
        Expect(b.CurrentMaxMovement()>=1,"Knee penalties cannot remove the final AP");
        Reset(b);int mana=(int)Get(b.Unit,"MaxMana");Old(b,"Head","PoorFocus");
        Expect((int)Get(b.Unit,"MaxMana")==Mathf.FloorToInt(mana*.95f),"Poor Focus reduces maximum mana by percentage: " + mana + " -> " + Get(b.Unit,"MaxMana"));
        Reset(b);Old(b,"Head","SluggishRecovery");Old(b,"Head","SluggishRecovery");Old(b,"Head","SluggishRecovery");
        Set(b.Unit,"BattleActive",false);b.Unit.Health=1;
        Call(Type("CompanyDailyRecovery"),"Recover",b.Unit,0.0,1);
        Expect(b.Unit.Health>=2,"Sluggish Recovery retains at least one daily HP recovery");
        Expect(Math.Abs((double)Get(injuries,"ModerateDailyRisk")-.05)<.00001 && Math.Abs((double)Get(injuries,"SevereDailyRisk")-.10)<.00001,"Approved daily complication chances are 5% and 10%");
        Reset(a);Reset(b);
        string[] parts={"Head","Eyes","Arm","Hand","Chest","Abdomen","Leg","Knee"};
        string[] grades={"Bruised","Battered","Mangled"};
        for(int grade=0;grade<3;grade++) foreach(string part in parts)
        {
            Reset(b); Injury(b,part,grades[grade]);
            if(part=="Head" || part=="Arm" || part=="Abdomen" || part=="Leg")
            {
                Stat stat=part=="Head"?Stat.Intelligence:part=="Arm"?Stat.Strength:part=="Leg"?Stat.Agility:Stat.Voracity;
                int baseline=part=="Head"?20:part=="Arm"?40:part=="Leg"?30:20;
                Expect(b.Unit.GetStat(stat)==Mathf.RoundToInt(baseline*(1-new[]{.2f,.4f,.6f}[grade])),grades[grade]+" "+part+" combat stat penalty");
            }
            if(part=="Eyes") Expect(Math.Abs((float)Call(injuries,"AccuracyPenalty",b.Unit)-(grade+1)*.10f)<.0001f,grades[grade]+" Eyes accuracy penalty");
            if(part=="Hand") Expect((int)Call(injuries,"DamagePenalty",b.Unit)==new[]{1,2,4}[grade],grades[grade]+" Hand damage penalty");
            if(part=="Knee") Expect((int)Call(injuries,"MovementPenalty",b.Unit)==grade+1,grades[grade]+" Knee AP penalty");
            if(part=="Chest") Expect(Math.Abs((float)Call(injuries,"HealingCeilingFraction",b.Unit)-(1-(grade+1)*.10f))<.0001f,grades[grade]+" Chest healing ceiling");
        }
        Reset(b);int originalHP=b.Unit.MaxHealth,originalHealth=b.Unit.Health;
        var torso=(Item)Call(Type("CompanyEquipment"),"Create","armor.torso");
        var prior=(Item[])Get(b.Unit,"Items"); var priorCopy=prior.ToArray(); bool oldLoadout=(bool)Get(b.Unit,"CompanyLoadout");
        try
        {
            if(prior.Length<9) Set(b.Unit,"Items",new Item[9]);
            Set(b.Unit,"CompanyLoadout",true);
            b.Unit.SetItem(torso,1,false);
            Expect(b.Unit.MaxHealth==originalHP && b.Unit.Health==originalHealth,"STR equipment does not grant or heal maximum HP");
            b.Unit.SetItem(null,1,false);
            var equipped=(Item[])Get(b.Unit,"Items");
            foreach(var mapping in new[]{new[]{"Head","Helmet"},new[]{"Eyes","Helmet"},new[]{"Arm","Arms"},new[]{"Hand","Arms"},new[]{"Chest","Torso"},new[]{"Abdomen","Torso"},new[]{"Leg","Legs"},new[]{"Knee","Legs"}})
            {
                Array.Clear(equipped,0,equipped.Length);
                var defs=((IEnumerable)Get(Type("CompanyEquipment"),"Catalog")).Cast<object>();
                var armor=defs.First(d=>Get(d,"Slot").ToString()==mapping[1]);
                int slot=(int)Convert.ChangeType(Get(armor,"Slot"),typeof(int));
                equipped[slot]=(Item)Call(Type("CompanyEquipment"),"Create",(string)Get(armor,"Id"));
                Expect((double)Call(injuries,"RiskMultiplier",b.Unit,E("CompanyBodyPart",mapping[0]))<1,"Armor protects "+mapping[0]);
            }
        }
        finally { Set(b.Unit,"Items",priorCopy);Set(b.Unit,"CompanyLoadout",oldLoadout); }
        Reset(a);Reset(b);
        float melee=b.GetAttackChance(a,false),ranged=b.GetAttackChance(a,true),vore=b.GetDevourChance(a);
        Injury(a,"Eyes","Bruised");
        Expect(Math.Abs(b.GetAttackChance(a,false)-Math.Max(0,melee-.1f))<.0001f && Math.Abs(b.GetAttackChance(a,true)-Math.Max(0,ranged-.1f))<.0001f &&
            Math.Abs(b.GetDevourChance(a)-Math.Max(0,vore-.1f))<.0001f,"Eye injury reduces final melee/ranged/vore odds by flat points");
        Reset(a);int spellBase=(int)Call(b,"ApplySpellTraitDamage",a,10);Injury(a,"Hand","Mangled");
        Expect((int)Call(b,"ApplySpellTraitDamage",a,10)==spellBase-4,"Hand injury reduces damaging spells too");
        Reset(a);Reset(b);b.Unit.AddTrait(Traits.ResilientIII);
        Expect(a.WeaponDamageAgainstTarget(b,false,.01f)==1,"A heavily reduced graze still deals at least one weapon damage");
        Reset(a);Reset(b);
        var bolt=Get(Type("SpellList"),"Fireball");
        float magicBefore=(float)Call(b,"GetMagicChance",a,bolt,0f,Stat.Intelligence);
        Injury(a,"Eyes","Battered");
        Expect(Math.Abs((float)Call(b,"GetMagicChance",a,bolt,0f,Stat.Intelligence)-Math.Max(0,magicBefore-.2f))<.0001f,"Eye injury applies flat penalty to magic accuracy");
        Reset(a);Reset(b);
        b.Unit.AddTrait(Traits.HotBlooded);int healthBefore=b.Unit.Health;
        Call(b,"Damage",1,true,true,DamageTypes.Fire,true,true);
        Expect(b.Unit.Health==healthBefore-1,"Successful spell hit retains minimum one after elemental resistance");
        Reset(a);Reset(b);
        var preyList=Activator.CreateInstance(typeof(List<>).MakeGenericType(Type("Prey")));
        var prey=Activator.CreateInstance(Type("Prey"),new object[]{b,a,preyList});
        Call(prey,"UpdateEscapeRate");float escapeBefore=(float)Get(prey,"EscapeRate");
        Set(a.Unit,"Type",UnitType.Soldier);Set(a.Unit,"PermanentInjuryImmune",false);
        try
        {
            for(int i=0;i<3;i++)Old(a,"Abdomen","SensitiveStomach");
            Call(prey,"UpdateEscapeRate");
            Expect(Math.Abs((float)Get(prey,"EscapeRate")-Math.Min(1,escapeBefore+.15f))<.0001f,"Sensitive Stomach III adds fifteen final escape percentage points");
            b.Unit.AddTrait(Traits.Submissive);Call(prey,"UpdateEscapeRate");
            Expect((float)Get(prey,"EscapeRate")==0,"Sensitive Stomach does not override Submissive escape prohibition");
        }
        finally { Set(a.Unit,"Type",UnitType.Leader);Set(a.Unit,"PermanentInjuryImmune",true);Set(b,"SelfPrey",null); }
        var originalStats=Get(b.Unit,"Stats");
        try { Set(b.Unit,"Stats",null);Expect(b.Unit.MaxHealth==1,"Incomplete preview units retain safe maximum HP getter"); }
        finally { Set(b.Unit,"Stats",originalStats); }
        Reset(a);Old(a,"Head");
        Expect(List(a.Unit,"Injuries").Count==0,"Company leader cannot receive permanent old injuries");
        Reset(a);Reset(b);
    }
    static void CheckSpellPreviews(Actor_Unit a, Actor_Unit b, GameManager gm)
    {
        var fire=Get(Type("SpellList"),"Fireball");bool resist=(bool)Get(fire,"Resistable");
        try
        {
            Set(fire,"Resistable",false);
            for(int scenario=0;scenario<10;scenario++)
            {
                Reset(a);Reset(b);Set(b.Unit,"Mana",60);Set(b.Unit,"Barrier",0);
                if(scenario==1) { Set(Get(Get(a.Unit,"TraitBoosts"),"Outgoing"),"MagicDamage",1.7f);Set(Get(Get(b.Unit,"TraitBoosts"),"Incoming"),"MagicDamage",.6f); }
                if(scenario==2)b.Unit.AddTrait(Traits.HotBlooded);
                if(scenario==3){a.Unit.AddTrait(Traits.PowerCasterIII);b.Unit.AddTrait(Traits.MagicResistanceII);Injury(a,"Hand","Battered");}
                if(scenario==4)b.Unit.AddTrait(Traits.ManaBarrier);
                if(scenario==5){a.Unit.AddTrait(Traits.ManaBurn);Set(b.Unit,"Mana",0);}
                if(scenario==6){a.Unit.AddTrait(Traits.ManaBurn);b.Unit.AddTrait(Traits.ManaBarrier);}
                if(scenario==7)Set(b.Unit,"Barrier",100);
                if(scenario==8){b.Unit.AddTrait(Traits.ManaBarrier);Call(b.Unit,"ApplyStatusEffect",StatusEffectType.SpellForce,1f,20,null,null);}
                if(scenario==9)b.Unit.AddTrait(Traits.Crystaline);
                int hp=b.Unit.Health,mana=(int)Get(b.Unit,"Mana"),barrier=(int)Get(b.Unit,"Barrier");
                float cached=(float)Get(Get(Get(b.Unit,"TraitBoosts"),"Incoming"),"MagicDamage");
                var randomBefore=(int[])Call(State.Rand,"Capture");
                int preview=0;
                for(int n=0;n<20;n++)preview=(int)Call(b,"PreviewSpellDamage",a,24,DamageTypes.Fire);
                Expect(randomBefore.SequenceEqual((int[])Call(State.Rand,"Capture")),"Preview does not consume random draws: "+scenario);
                Expect(hp==b.Unit.Health && mana==(int)Get(b.Unit,"Mana") && barrier==(int)Get(b.Unit,"Barrier") &&
                    cached==(float)Get(Get(Get(b.Unit,"TraitBoosts"),"Incoming"),"MagicDamage"),"Preview preserves resources and cached resistance: "+scenario);
                Expect((bool)Call(b,"DefendDamageSpell",fire,a,24),"Controlled spell hit: "+scenario);
                Expect(hp-b.Unit.Health==preview,"Preview matches actual HP loss: "+scenario);
            }
        }
        finally {Set(fire,"Resistable",resist);}
        Reset(a);Reset(b);a.Unit.SetStatBase(Stat.Intelligence,80);b.Unit.SetStatBase(Stat.Intelligence,10);
        Set(b,"InSight",true);Set(b,"Targetable",true);
        var ai=Activator.CreateInstance(Type("StandardTacticalAI"),new object[]{new List<Actor_Unit>{a,b},Get(gm.TacticalMode,"tiles"),a.Unit.Side,false});
        var ice=Get(Type("SpellList"),"Icicle");
        float expected=(float)Call(b,"GetMagicChance",a,ice,0f,Stat.Intelligence);
        var candidates=((IEnumerable)Call(ai,"GetListOfPotentialSpellTargets",a,ice,a.Position)).Cast<object>().ToList();
        Expect(candidates.Any(c=>ReferenceEquals(Get(c,"actor"),b) && Math.Abs((float)Get(c,"chance")-expected)<.0001f),"AI spell scores actual caster versus target with asymmetric INT");
        var bolt=Get(Type("SpellList"),"PowerBolt");
        candidates=((IEnumerable)Call(ai,"GetListOfPotentialSpellTargets",a,bolt,a.Position)).Cast<object>().ToList();
        Expect(candidates.Any(c=>ReferenceEquals(Get(c,"actor"),b) && (float)Get(c,"chance")==1f),"AI non-resistible spells use guaranteed hit chance");
        Reset(a);Reset(b);
    }
    static void CheckSpellProgression(CompanyCampaign campaign, Actor_Unit actor, Actor_Unit enemy, GameManager gm)
    {
        int reputation = campaign.Reputation;
        try
        {
            campaign.Reputation = 0;
            var first = CompanyEquipment.Available(campaign).Where(d => d.Slot == CompanySlot.Spell1).Select(d => d.Id).ToArray();
            Expect(first.Contains("spell.bolt") && first.Contains("spell.mending") && !first.Contains("spell.cinder"), "Starter spellbook tier provides bolt and temporary healing");
            campaign.Reputation = 10;
            var second = CompanyEquipment.Available(campaign).Where(d => d.Slot == CompanySlot.Spell1).Select(d => d.Id).ToArray();
            Expect(second.Contains("spell.cinder") && second.Contains("spell.predation") && second.Contains("spell.poison") && !second.Contains("spell.summon"), "Reputation unlocks second-tier utility and hazard books");
            campaign.Reputation = 25;
            var final = CompanyEquipment.Available(campaign).Where(d => d.Slot == CompanySlot.Spell1).ToArray();
            Expect(final.Any(d => d.Id == "spell.summon") && final.Any(d => d.Id == "spell.gatemaw") && final.Any(d => d.Id == "spell.trance"), "High tier unlocks summon, Gate Maw and control");
            foreach (var definition in final)
            {
                var book = (SpellBook)CompanyEquipment.Create(definition.Id);
                Expect(SpellList.SpellDict.ContainsKey(book.ContainedSpell) && !SpellList.SpellDict[book.ContainedSpell].IsRetired &&
                    SpellList.SpellDict[book.ContainedSpell].ManaCost > 0 && CompanyEquipment.Value(book) == definition.Cost,
                    "Book has a valid active spell and matching price: " + definition.Id);
            }
            var recruit = CompanyEconomy.Create(campaign, Race.Lizards, 2, false, 6);
            Expect(recruit.Unit.Items[(int)CompanySlot.Spell2] is SpellBook && CompanyEquipment.GearPrice(recruit.Unit) >=
                CompanyEquipment.Value(recruit.Unit.Items[(int)CompanySlot.Spell2]), "Experienced mage's generated book is included in equipment cost");
            var unit = recruit.Unit;
            Expect((int)Call(typeof(SpellList), "SummonExperience", unit) > unit.Experience / 2,
                "Experienced summoners pass on accumulated progression rather than only current-level XP");
            unit.SetItem(CompanyEquipment.Create("spell.mending"), (int)CompanySlot.Spell1);
            unit.SetItem(CompanyEquipment.Create("spell.mending"), (int)CompanySlot.Spell2);
            unit.UpdateSpells();
            Expect(unit.UseableSpells.Count(spell => spell.SpellType == SpellTypes.Mending) == 1, "Duplicate spell sources yield one usable action");
            bool active = actor.Unit.BattleActive;
            actor.Unit.Health = Math.Max(1, actor.Unit.MaxHealth / 2); actor.Unit.BeginCombat();
            int lasting = actor.Unit.LastingHealth;
            actor.Unit.ApplyStatusEffect(StatusEffectType.Mending, 12, 2);
            actor.Unit.TickStatusEffects();
            Expect(actor.Unit.LastingHealth == lasting && actor.Unit.TemporaryHealth > 0, "Mending adds only temporary battle health");
            actor.Unit.FinishCombat();
            Expect(actor.Unit.Health == lasting && actor.Unit.TemporaryHealth == 0, "Mending health disappears after battle");
            actor.Unit.BattleActive = active; Reset(actor);
            unit.SetStatBase(Stat.Intelligence, 30); unit.RestoreManaPct(1);
            unit.SetItem(CompanyEquipment.Create("spell.bolt"), (int)CompanySlot.Spell1);
            unit.SetItem(null, (int)CompanySlot.Spell2); unit.UpdateSpells();
            double before = CompanyPower.Healthy(unit);
            unit.SetItem(CompanyEquipment.Create("spell.gatemaw"), (int)CompanySlot.Spell2); unit.UpdateSpells();
            Expect(CompanyPower.Healthy(unit) > before, "Enemy power considers an added usable high-tier spell");
        }
        finally { campaign.Reputation = reputation; }
        var effects = gm.TacticalMode.ActiveEffects;
        var savedEffects = new Dictionary<Vec2, TileEffect>(effects);
        var originalPositions = gm.TacticalMode.Export().units.ToDictionary(unit => unit, unit => unit.Position);
        var originalRandom = State.Rand.Capture();
        try
        {
            effects.Clear();
            foreach (var unit in originalPositions.Keys) unit.SetPos(new Vec2i(1, 1));
            actor.SetPos(new Vec2i(4, 4)); enemy.SetPos(new Vec2i(1, 1));
            Reset(actor); Reset(enemy); actor.Unit.RestoreManaPct(1); actor.Movement = 3;
            int mana = actor.Unit.Mana;
            var center = new Vec2i(6, 6);
            Expect(SpellList.CinderField.TryCast(actor, center) && actor.Unit.Mana == mana - SpellList.CinderField.ManaCost,
                "Cinder Field uses the native casting and mana rules");
            Expect(effects.Count == 9 && effects.Values.All(effect => effect.Type == TileEffectType.Fire && effect.OwnerSide == actor.Unit.Side && effect.RemainingDuration == 3),
                "Cinder Field creates owned, three-turn ground fire");
            var encoded = OdinSerializer.SerializationUtility.SerializeValue(gm.TacticalMode.Export(), OdinSerializer.DataFormat.Binary);
            var restored = OdinSerializer.SerializationUtility.DeserializeValue<TacticalData>(encoded, OdinSerializer.DataFormat.Binary);
            Expect(restored.activeEffects.Count == 9 && restored.activeEffects.Values.All(effect => effect.OwnerSide == actor.Unit.Side),
                "Hazard ownership and duration survive tactical serialization");
            actor.SetPos(center); actor.Movement = 3;
            var ai = new StandardTacticalAI(new List<Actor_Unit> { actor, enemy }, (TacticalTileType[,])Get(gm.TacticalMode, "tiles"), actor.Unit.Side, false);
            var escape = typeof(TacticalAI).GetMethod("TryLeaveHazard", BindingFlags.Instance | BindingFlags.NonPublic);
            Expect((bool)escape.Invoke(ai, new object[] { actor }), "AI plans an escape from an active fire field");
            var route = (AIPlottedPath)Get(ai, "path");
            Expect(route.HazardEscape && route.Path.Count > 0 && !TacticalPathfinder.IsBurning(route.Path.Last().X, route.Path.Last().Y),
                "AI escape ends on a non-burning tile");
            var escapeAllowed = typeof(TacticalAI).GetMethod("HazardEscapeAllowed", BindingFlags.Instance | BindingFlags.NonPublic);
            int movement = actor.Movement; actor.Movement = 0;
            Expect(!(bool)escapeAllowed.Invoke(ai, new object[] { actor, route.Path }), "AI rejects a hazard escape it cannot afford");
            actor.Movement = movement;
            Reset(enemy); enemy.SetPos(center);
            int actorHealth = actor.Unit.Health, enemyHealth = enemy.Unit.Health;
            int injuries = actor.Unit.Injuries.Count;
            int enemyInjuries = enemy.Unit.Injuries.Count;
            gm.TacticalMode.ProcessTileEffects();
            Expect(actor.Unit.Health < actorHealth && enemy.Unit.Health < enemyHealth && actor.Unit.Injuries.Count == injuries &&
                enemy.Unit.Injuries.Count == enemyInjuries && effects.Values.All(effect => effect.RemainingDuration == 2),
                "Ongoing fire affects all occupants without hit-based injuries and advances its duration");
            gm.TacticalMode.ProcessTileEffects(); gm.TacticalMode.ProcessTileEffects();
            Expect(effects.Count == 0, "Ground fire expires after its duration");
        }
        finally
        {
            effects.Clear(); foreach (var entry in savedEffects) effects.Add(entry.Key, entry.Value);
            foreach (var entry in originalPositions) entry.Key.SetPos(entry.Value);
            State.Rand = Rand.Resume(originalRandom); Reset(actor); Reset(enemy);
        }
    }

    static IEnumerable<object> CheckDeveloperLab(GameManager gm)
    {
        Set(gm,"CompanyBattle",null);gm.StatScreen.gameObject.SetActive(false);gm.SwitchToMainMenu();
        Expect(gm.Start_Mode.UI.transform.Find("Developer Battle Lab")!=null,"Main menu contains Battle Lab next to Race Editor");
        gm.Menu.OpenBattleLab();var prompt=(InputBox)Get(gm.Menu,"raceEditorPrompt");
        Expect(prompt!=null,"Developer lab requests password");prompt.NoClicked();yield return null;
        Expect(!(bool)Get(Type("DeveloperBattleLab"),"IsOpen"),"Cancelling developer gate keeps lab closed");
        gm.Menu.OpenBattleLab();prompt=(InputBox)Get(gm.Menu,"raceEditorPrompt");prompt.InputField.text="wrong";prompt.YesClicked();yield return null;
        Expect(!(bool)Get(Type("DeveloperBattleLab"),"IsOpen"),"Incorrect developer password keeps lab closed");
        // The error message uses the normal modal; close it through its UI button.
        foreach(var message in Resources.FindObjectsOfTypeAll<MessageBox>()) if(message.gameObject.activeInHierarchy)message.gameObject.SetActive(false);
        Set(gm,"ActiveInput",false);
        var world=State.World;var config=Get(Type("Config"),"World");var random=State.Rand;var rng=(int[])Call(random,"Capture");
        int files=Directory.GetFiles(directory,"*",SearchOption.AllDirectories).Length;
        gm.Menu.OpenBattleLab();prompt=(InputBox)Get(gm.Menu,"raceEditorPrompt");prompt.InputField.text="214214214";prompt.YesClicked();yield return null;
        var lab=gm.GetComponent(Type("DeveloperBattleLab"));
        Expect(lab!=null && (bool)Get(Type("DeveloperBattleLab"),"IsOpen"),"Correct shared password opens native battle lab");
        try
        {
            Expect(gm.PureTactical && Get(gm,"CompanyBattle")==null,"Lab uses independent Single Battle without campaign session");
            var fighters=(Actor_Unit[])Get(lab,"Fighters");Expect(fighters.Length==2,"Lab creates two native combatants");
            for(int page=0;page<6;page++){Set(lab,"tab",page);Call(lab,"Draw");Expect(Get(lab,"page")!=null,"Lab page renders controls: "+page);}
            int round=(int)Get(gm.TacticalMode,"currentTurn");Call(lab,"AdvanceTurn");
            Expect((int)Get(gm.TacticalMode,"currentTurn")==round+1,"Lab advances the native round counter");
            Expect(((Actor_Unit[])Get(lab,"Fighters")).All(f=>f.Movement>=1),"Lab explicitly advances a native combat turn");

            Set(lab,"selected",0);
            foreach(var actor in fighters){Call(actor.Unit,"ClearLabTraits");actor.Unit.SetStatBase(Stat.Strength,100);Call(actor.Unit,"SetLabResources",actor.Unit.MaxHealth,0,60);}
            fighters[0].Unit.AddTrait(Traits.PreciseIII);fighters[0].Unit.AddTrait(Traits.DeadeyeIII);fighters[1].Unit.SetStatBase(Stat.Agility,1);
            int before=fighters[1].Unit.Health;Call(lab,"PrepareAction",false);fighters[0].Attack(fighters[1],false);
            Expect(fighters[1].Unit.Health<before,"Lab native melee action damages its target");
            Call(lab,"AdvanceTurn");before=fighters[1].Unit.Health;Call(lab,"PrepareAction",true);fighters[0].Attack(fighters[1],true);
            Expect(fighters[1].Unit.Health<before,"Lab native ranged action damages its target");
            Call(lab,"AdvanceTurn");before=fighters[1].Unit.Health;Call(lab,"PrepareAction",true);Call(fighters[0],"CastOffensiveSpell",Get(Type("SpellList"),"PowerBolt"),fighters[1],null);
            Expect(fighters[1].Unit.Health<before,"Lab native spell action damages its target");
            Set(lab,"tab",6);Call(lab,"Draw");Expect(((GameObject)Get(lab,"page")).transform.Find("Log viewport")!=null,"Lab full log has a scrollable viewport");
            Set(lab,"selected",1);var u=fighters[1].Unit;
            Call(u,"SetLabResources",u.MaxHealth/2,0,0);Injury(fighters[1],"Eyes","Battered",.25);
            double roll=(double)Get(List(u,"Injuries")[0],"RecoveryRoll");Call(lab,"StoreFixture");
            Call(u,"SetLabResources",u.MaxHealth,0,0);List(u,"Injuries").Clear();
            Call(lab,"RestoreFixture");fighters=(Actor_Unit[])Get(lab,"Fighters");u=fighters[1].Unit;
            Expect(u.Health==u.MaxHealth/2 && List(u,"Injuries").Count==1 && (double)Get(List(u,"Injuries")[0],"RecoveryRoll")==roll,"In-memory fixture restores HP and saved injury roll");
            Call(Type("CompanyInjuries"),"TreatInjuries",u);
            Expect((bool)Get(List(u,"Injuries")[0],"Treated") && (double)Get(List(u,"Injuries")[0],"RecoveryRoll")==roll,"Lab treatment uses native rules without reroll");
            int hp=u.Health;Call(lab,"RecoveryDay",0);Expect(u.Health>hp,"Lab recovery day uses production daily healing");
            Call(lab,"ResetScenario",97531);var first=((Actor_Unit[])Get(lab,"Fighters")).Select(f=>f.Unit.GetStatBase(Stat.Strength)).ToArray();
            Call(lab,"ResetScenario",97531);Expect(first.SequenceEqual(((Actor_Unit[])Get(lab,"Fighters")).Select(f=>f.Unit.GetStatBase(Stat.Strength))),"Seeded scenarios reproduce generated stats");
            Expect(Directory.GetFiles(directory,"*",SearchOption.AllDirectories).Length==files,"Lab scenarios and fixtures do not write save files");
        }
        finally {Call(lab,"Close");}
        Expect(ReferenceEquals(State.World,world) && ReferenceEquals(Get(Type("Config"),"World"),config) && ReferenceEquals(State.Rand,random) && rng.SequenceEqual((int[])Call(random,"Capture")),"Closing lab restores previous world/settings/random state exactly");
        Expect(gm.CurrentScene==gm.Start_Mode && !(bool)Get(Type("DeveloperBattleLab"),"IsOpen") && !(bool)Get(gm.Menu,"HasRaceEditorAccess"),"Lab closes to main menu and relocks developer access");
    }
    static void CheckPower(Actor_Unit a,Actor_Unit b)
    {
        var power=Type("CompanyPower");
        Reset(a);Reset(b);double healthy=(double)Call(power,"Healthy",a.Unit);
        int hp=a.Unit.Health;var rng=(int[])Call(State.Rand,"Capture");
        for(int i=0;i<10;i++)Call(power,"Evaluate",a.Unit);
        Expect(a.Unit.Health==hp && rng.SequenceEqual((int[])Call(State.Rand,"Capture")),"Power inspection preserves HP and RNG");
        Set(a.Unit,"BattleActive",false);a.Unit.Health=Math.Max(1,a.Unit.MaxHealth/5);Injury(a,"Arm","Mangled");
        Expect(Math.Abs((double)Call(power,"Healthy",a.Unit)-healthy)<.00001,"Healthy power ignores current HP and injuries");
        Expect((double)Call(power,"Current",a.Unit,Call(power,"Evaluate",a.Unit))<healthy,"Diagnostic current power reflects wounds");
        foreach(var test in new[]{new[]{Traits.Precise,Traits.PreciseII,Traits.PreciseIII},new[]{Traits.StrongMelee,Traits.StrongMeleeII,Traits.StrongMeleeIII},
            new[]{Traits.Hearty,Traits.HeartyII,Traits.HeartyIII},new[]{Traits.AccuteDodge,Traits.DodgerII,Traits.DodgerIII},new[]{Traits.Defenseless,Traits.EasyPreyII,Traits.EasyPreyIII}})
        {
            double previous=0;for(int i=0;i<3;i++)
            {
                Reset(a);a.Unit.AddTrait(test[i]);double value=(double)Call(power,"Healthy",a.Unit);
                if(i>0)Expect(test[0]==Traits.Defenseless?value<previous:value>previous,"Power distinguishes tiers: "+test[i]);
                previous=value;
            }
        }
        Reset(a);double without=(double)Call(power,"Healthy",a.Unit);a.Unit.AddTrait(Traits.WeakBonesIII);
        Expect((double)Call(power,"Healthy",a.Unit)<without,"Damage vulnerability lowers power");
        var originalItems=a.Unit.Items.ToArray();
        try
        {
            a.Unit.SetItem((Item)Call(Type("CompanyEquipment"),"Create","weapon.mace"),5,false);
            a.Unit.SetItem((Item)Call(Type("CompanyEquipment"),"Create","weapon.bow"),6,false);
            a.Unit.SetItem((Item)Call(Type("CompanyEquipment"),"Create","spell.bolt"),7,false);
            a.Unit.UpdateSpells();
            Action resistibleSpell=()=>{var spells=(IList)Get(a.Unit,"UseableSpells");spells.Clear();spells.Add(Get(Type("SpellList"),"Fireball"));};
            foreach(var family in (IEnumerable)Get(Type("TieredTraits"),"Families"))
            {
                var tiers=(Traits[])Get(family,"Tiers");int sign=(int)Get(family,"Sign");
                if(Get(family,"Effect").ToString().EndsWith("Received"))sign=-sign;
                Reset(a);resistibleSpell();double baseline=(double)Call(power,"Healthy",a.Unit),previous=baseline;
                foreach(var trait in tiers)
                {
                    Reset(a);a.Unit.AddTrait(trait);resistibleSpell();double score=(double)Call(power,"Healthy",a.Unit);
                    // Damage/critical floors can make the last two penalty tiers equal.
                    Expect((score-previous)*sign>=-.011,"Power tier direction: "+trait);previous=score;
                }
                Expect((previous-baseline)*sign>.01,"Power prices equipped family: "+Get(family,"Name"));
            }
        }
        finally {for(int slot=0;slot<originalItems.Length;slot++)a.Unit.SetItem(originalItems[slot],slot,false);a.Unit.UpdateSpells();}
        foreach(double fraction in new[]{1.0,.8,.6,.2})
        {
            Reset(a);Reset(b);Set(a.Unit,"BattleActive",false);Set(b.Unit,"BattleActive",false);
            a.Unit.Health=(int)Math.Round(a.Unit.MaxHealth*fraction);b.Unit.Health=(int)Math.Round(b.Unit.MaxHealth*fraction);
            double actual=(double)Call(power,"HealthMultiplier",new List<Unit>{a.Unit,b.Unit});
            double expected=1-.5*Math.Min(.4,1-(a.Unit.Health+b.Unit.Health)/(double)(a.Unit.MaxHealth+b.Unit.MaxHealth));
            Expect(Math.Abs(actual-expected)<.00001 && actual>=.8 && actual<=1,"Capped health scaling at fraction "+fraction);
        }
        Reset(a);Set(a.Unit,"BattleActive",false);a.Unit.Health=1;Set(a.Unit,"TemporaryHealth",a.Unit.MaxHealth-1);a.Unit.Health=a.Unit.MaxHealth;
        Expect((double)Call(power,"HealthMultiplier",new List<Unit>{a.Unit})==.8,"Temporary HP cannot inflate travel encounter health");
        Reset(a);Reset(b);
        Expect((double)Call(power,"FormationMultiplier",30)==1.2,"Swarm premium is bounded");
        Expect((double)Call(power,"RecruitCeiling",100)>(double)Call(power,"RecruitCeiling",0),"Reputation continuously raises recruit power ceiling");
        for(int seed=71;seed<76;seed++)
        {
            var state=Call(Type("CompanyCampaign"),"Generate",seed,false);
            var leader=Call(Type("CompanyEconomy"),"Create",state,Race.Lizards,2,true,1);Call(Type("CompanyEconomy"),"Begin",state,leader);
            var offers=List(state,"Contracts").Cast<object>().ToArray();Expect(offers.Length>=2,"Starter contracts fill fixed power band: seed "+seed);
            foreach(var c in offers)
            {
                int rating=(int)Get(c,"Rating");double budget=(double)Call(Type("CompanyContracts"),"Budget",rating);double score=(double)Get(c,"Power");
                Expect(score>=budget*.92-.01 && score<=budget*1.08+.01,"Generated force meets rating band: "+seed+"/"+rating);
            }
            var candidates=List(state,"Characters").Cast<object>().Where(c=>Get(c,"Employment").ToString()=="Candidate").ToArray();
            Expect(candidates.Length>=2 && candidates.All(c=>(double)Call(power,"Healthy",Get(c,"Unit"))<=(double)Call(power,"RecruitCeiling",0)),"Starting recruitment respects power ceiling: "+seed);
            int extraRating=new[]{2,5,10,15,20}[seed-71];
            var force=((IEnumerable)Call(Type("CompanyContracts"),"Generate",state,extraRating)).Cast<object>().Select(c=>(Unit)Get(c,"Unit")).ToArray();
            double forceScore=(double)Call(power,"Force",(object)force),fixedBudget=(double)Call(Type("CompanyContracts"),"Budget",extraRating);
            Expect(forceScore>=fixedBudget*.92-.01 && forceScore<=fixedBudget*1.08+.01,"Higher fixed rating fills power band: "+extraRating);
        }
        Reset(a);Reset(b);
    }
    static void CheckCapacity(Actor_Unit a,Actor_Unit b)
    {
        Reset(a);Reset(b);var pred=a.PredatorComponent;
        Set(Get(a.Unit,"TraitBoosts"),"CapacityMult",.01f);
        Expect(!(bool)Call(pred,"HasCapacityFor",b),"Empty predator cannot bypass capacity for standing prey");
        Set(b.Unit,"Downed",true);
        Expect((bool)Call(pred,"HasCapacityFor",b),"Empty predator can fit oversized downed prey");
        Expect((bool)Call(pred,"Devour",b,0f) && Get(b,"SelfPrey")!=null,"Native vore swallows oversized downed prey");
        Expect((int)Get(pred,"PreyCount")==1 && (float)Call(pred,"FreeCap")<0,"Oversized prey occupies actual capacity");
        Expect(!(bool)Call(pred,"HasCapacityFor",b),"Occupied predator cannot use the downed capacity exception again");
        var toggles=(IDictionary)Get(Get(Type("Config"),"World"),"Toggles");var old=toggles["OverfeedingDamage"];
        try
        {
            toggles["OverfeedingDamage"]=true;int hp=a.Unit.Health;
            for(int i=0;i<6;i++)Call(pred,"Digest","");
            Expect(Get(b,"SelfPrey")!=null && a.Unit.Health==hp,"Legacy overfeeding setting cannot inflict overcapacity self-damage");
        }
        finally {toggles["OverfeedingDamage"]=old;}
        Call(pred,"FreePrey",Get(b,"SelfPrey"),true);
        Reset(a);Reset(b);
        Expect((bool)Call(pred,"HasCapacityFor",b),"Ordinary fitting prey remains eligible under normal capacity");
    }
    [Serializable] sealed class WorldSample
    { public int seed,roads,minimumTownDistance;public double milliseconds,matchingNeighbors;public int[] connectionLengths,nearestRoadDistances; }
    [Serializable] sealed class WorldSamples { public List<WorldSample> worlds=new List<WorldSample>(); }
    [Serializable] sealed class WorldPreview { public int width,height;public int[] terrain,towns;public bool[] roads,farms; }
    static int NearestRoadTown(object state,int origin,int[] towns,bool[] roads,Type generation)
    {
        var distance=Enumerable.Repeat(-1,roads.Length).ToArray();var queue=new Queue<int>();queue.Enqueue(origin);distance[origin]=0;
        while(queue.Count>0)
        {
            int tile=queue.Dequeue();
            foreach(int next in (IEnumerable)Call(generation,"Neighbors",state,tile))if(roads[next] && distance[next]<0)
            {
                distance[next]=distance[tile]+1;
                if(towns.Contains(next))return distance[next];
                queue.Enqueue(next);
            }
        }
        throw new Exception("No other town is reachable by road.");
    }
    static void CheckWorld(GameManager gm,object view)
    {
        var results=new WorldSamples();var generation=Type("CompanyWorldGeneration");object sample=null;
        for(int seed=1;seed<=40;seed++)
        {
            var watch=System.Diagnostics.Stopwatch.StartNew();var state=Call(Type("CompanyCampaign"),"Generate",seed,false);double elapsed=watch.Elapsed.TotalMilliseconds;
            Expect((int)Get(state,"Width")==120 && (int)Get(state,"Height")==80 && List(state,"Towns").Count==6,"Large region retains six towns, seed "+seed);
            Expect((int)Get(state,"DailyActions")==6 && (int)Get(state,"Actions")==6,"New campaigns use a compact six-action day, seed "+seed);
            Call(generation,"ValidateNetwork",state);
            var terrain=(int[])Get(state,"Terrain");var roads=(bool[])Get(state,"Roads");
            var lengths=((IEnumerable)Call(generation,"RoadLegLengths",state)).Cast<int>().ToArray();
            Expect(lengths.Length==6 && lengths.All(n=>n>=25 && n<=35),"Six road connections each require25-35actual moves, seed "+seed);
            int min=int.MaxValue;var towns=List(state,"Towns").Cast<object>().Select(t=>(int)Get(t,"Tile")).ToArray();
            foreach(int a in towns)foreach(int b in towns)if(a!=b)min=Math.Min(min,(int)Call(Type("CompanyContracts"),"Distance",state,a,b));
            var nearest=towns.Select(t=>NearestRoadTown(state,t,towns,roads,generation)).ToArray();
            Expect(nearest.All(n=>n>=25 && n<=35) && towns.All(t=>((IEnumerable)Call(generation,"Neighbors",state,t)).Cast<int>().Count(n=>roads[n])==2),"Each town has two physical exits and a nearby town within25-35road moves, seed "+seed);
            Expect(min>=24 && terrain.Distinct().Count()==4,"Compact spaced towns and all retained terrain types, seed "+seed);
            var zones=towns.Select(t=>(t%120)/40+3*((t/120)/40)).ToArray();
            Expect(zones.Distinct().Count()==6 && zones[0]==4 && (int)Get(state,"Position")==towns[0],"One town per zone and bottom-center start, seed "+seed);
            var farms=(bool[])Get(state,"Farms");var expectedFarms=new HashSet<int>();
            foreach(int town in towns)for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){int x=town%120+dx,y=town/120+dy;if(x>=0 && x<120 && y>=0 && y<80){int t=y*120+x;if(!roads[t] && !towns.Contains(t))expectedFarms.Add(t);}}
            Expect(Enumerable.Range(0,farms.Length).Where(t=>farms[t]).SequenceEqual(expectedFarms.OrderBy(t=>t)),"Only non-road town neighbors are farmed, seed "+seed);
            Expect(Enumerable.Range(0,roads.Length).Where(t=>roads[t] || farms[t]).All(t=>terrain[t]==0 && (int)Call(state,"MoveCost",t)==1),"Roads and fields are actual one-action grassland, seed "+seed);
            int same=0,total=0;
            for(int t=0;t<terrain.Length;t++)foreach(int n in (IEnumerable)Call(generation,"Neighbors",state,t))if(n>t){total++;if(terrain[t]==terrain[n])same++;}
            double coherence=same/(double)total;Expect(coherence>.8,"Terrain forms coherent patches, seed "+seed);
            var repeated=Call(Type("CompanyCampaign"),"Generate",seed,false);
            Expect(terrain.SequenceEqual((int[])Get(repeated,"Terrain")) && roads.SequenceEqual((bool[])Get(repeated,"Roads")) && towns.SequenceEqual(List(repeated,"Towns").Cast<object>().Select(t=>(int)Get(t,"Tile"))),"Seed reproduces geography and roads, seed "+seed);
            var copy=Call(Type("CompanyCampaignSave"),"Decode",Call(Type("CompanyCampaignSave"),"Encode",state));
            Expect(roads.SequenceEqual((bool[])Get(copy,"Roads")) && farms.SequenceEqual((bool[])Get(copy,"Farms")) && terrain.SequenceEqual((int[])Get(copy,"Terrain")),"Roads and terrain survive save roundtrip, seed "+seed);
            results.worlds.Add(new WorldSample{seed=seed,roads=roads.Count(r=>r),minimumTownDistance=min,milliseconds=elapsed,matchingNeighbors=coherence,connectionLengths=lengths,nearestRoadDistances=nearest});sample=state;
        }
        File.WriteAllText("Logs/roads37-worlds.json",JsonUtility.ToJson(results,true));
        File.WriteAllText("Logs/roads37-region-preview.json",JsonUtility.ToJson(new WorldPreview{width=(int)Get(sample,"Width"),height=(int)Get(sample,"Height"),terrain=(int[])Get(sample,"Terrain"),roads=(bool[])Get(sample,"Roads"),farms=(bool[])Get(sample,"Farms"),towns=List(sample,"Towns").Cast<object>().Select(t=>(int)Get(t,"Tile")).ToArray()}));
        string oldPath="Logs/balance34f-533e071f082f41b991f52c0cb7112a11/101-starter-mixed-r1/initial-company.bin";
        var legacy=Call(Type("CompanyCampaignSave"),"Decode",File.ReadAllBytes(oldPath));
        Expect((int)Get(legacy,"Width")==80 && (int)Get(legacy,"Height")==60 && Get(legacy,"Roads")==null,"Existing campaign keeps its old dimensions and no invented roads");
        var oldTerrain=((int[])Get(legacy,"Terrain")).ToArray();var oldTowns=List(legacy,"Towns").Cast<object>().Select(t=>(int)Get(t,"Tile")).ToArray();
        var previous=JsonUtility.FromJson<WorldPreview>(File.ReadAllText("Logs/followup35-region-preview.json"));
        var upgrade=Call(Type("CompanyCampaign"),"Generate",72,false);
        Set(upgrade,"Width",previous.width);Set(upgrade,"Height",previous.height);Set(upgrade,"Terrain",previous.terrain.ToArray());Set(upgrade,"Roads",previous.roads.ToArray());Set(upgrade,"Farms",null);Set(upgrade,"Discovered",new bool[previous.terrain.Length]);
        for(int i=0;i<previous.towns.Length;i++)Set(List(upgrade,"Towns")[i],"Tile",previous.towns[i]);
        Set(upgrade,"Position",previous.towns[0]);Set(upgrade,"DailyActions",10);Set(upgrade,"Actions",4);
        var upgraded=Call(Type("CompanyCampaignSave"),"Decode",Call(Type("CompanyCampaignSave"),"Encode",upgrade));
        var upgradedTerrain=(int[])Get(upgraded,"Terrain");var upgradedFarms=(bool[])Get(upgraded,"Farms");
        Expect((int)Get(upgraded,"Width")==160 && (int)Get(upgraded,"Height")==120 && List(upgraded,"Towns").Cast<object>().Select(t=>(int)Get(t,"Tile")).SequenceEqual(previous.towns),"Existing road campaign keeps dimensions and towns");
        Expect(Enumerable.Range(0,previous.roads.Length).All(t=>previous.roads[t]?upgradedTerrain[t]==0:upgradedFarms[t] || upgradedTerrain[t]==previous.terrain[t]),"Existing rough road tiles clear without changing unrelated terrain");
        Expect((int)Get(upgraded,"DailyActions")==10 && (int)Get(upgraded,"Actions")==4,"Existing campaign action allowance and partial-day progress are preserved");
        Call(view,"Open",gm.Start_Mode);Call(view,"Install",legacy);
        Expect(oldTerrain.SequenceEqual((int[])Get(legacy,"Terrain")) && oldTowns.SequenceEqual(List(legacy,"Towns").Cast<object>().Select(t=>(int)Get(t,"Tile"))),"Opening legacy geography does not regenerate towns or terrain");
        var leader=Call(Type("CompanyEconomy"),"Create",sample,Race.Avians,1,true,1);Call(Type("CompanyEconomy"),"Begin",sample,leader);Call(view,"Install",sample);
        Canvas.ForceUpdateCanvases();Call(view,"RefreshMapTiles",true);
        var graphics=((Array)Get(view,"roadGraphics")).Cast<object>().ToArray();var indices=(int[])Get(view,"tileIndices");
        Expect(graphics.Length==348 && ((Array)Get(view,"tiles")).Length==348,"Large map uses a bounded348-tile display pool");
        var discovery=(bool[])Get(sample,"Discovered");
        for(int i=0;i<graphics.Length;i++)if(indices[i]>=0)
            Expect((int)Get(graphics[i],"connections")== (discovery[indices[i]]?(int)Call(generation,"RoadMask",sample,indices[i]):0),"Road overlay obeys tile fog "+i);
        int visibleFarms=0;var farmTiles=(bool[])Get(sample,"Farms");var buttons=(Array)Get(view,"tiles");
        for(int i=0;i<indices.Length;i++)if(indices[i]>=0 && discovery[indices[i]] && farmTiles[indices[i]])
        {
            int tile=indices[i];int art=(int)Call(Type("StrategicTileInfo"),"GetTileType",StrategicTileType.field,tile%120,tile/120);
            var sprite=((UnityEngine.UI.Button)buttons.GetValue(i)).GetComponent<UnityEngine.UI.Image>().sprite;
            Expect(sprite!=null && sprite==((Sprite[])Get(gm.StrategyMode,"TileSprites"))[art],"Farm tile uses original strategic field artwork");visibleFarms++;
        }
        Expect(visibleFarms>0,"Starting town shows farming tiles inside revealed terrain");
        var graphic=graphics[0];
        for(int mask=0;mask<16;mask++)using(var mesh=new UnityEngine.UI.VertexHelper())
        {Call(graphic,"SetConnections",mask);Call(graphic,"OnPopulateMesh",mesh);Expect(mask==0?mesh.currentVertCount==0:mesh.currentVertCount>0 && mesh.currentVertCount<=40,"Road geometry mask "+mask);}
        Expect(graphics.All(g=>!(bool)Get(g,"raycastTarget")),"Road overlays do not intercept movement or dragging");
        var content=(RectTransform)Get(view,"mapContent");content.anchoredPosition=new Vector2(-2000,1000);Call(view,"RefreshMapTiles",true);
        Expect(((int[])Get(view,"tileIndices")).Where(t=>t>=0).All(t=>t<120*80),"Dragging a large map keeps pooled tile indices in bounds");
        Call(view,"CenterMap");
        int origin=(int)Get(sample,"Position");int next=((IEnumerable)Call(generation,"Neighbors",sample,origin)).Cast<int>().First();
        int expected=((int[])Get(sample,"Terrain"))[next]==1 || ((int[])Get(sample,"Terrain"))[next]==2?2:1;
        int actions=(int)Get(sample,"Actions");Expect((bool)Call(sample,"Move",next) && (int)Get(sample,"Actions")==actions-expected,"Road-era movement retains actual terrain costs and permits adjacent off-road travel");
    }
    static void Reset(Actor_Unit actor)
    {
        ((List<Traits>)Get(actor.Unit,"Tags")).Clear();
        ((List<Traits>)Get(actor.Unit,"PermanentTraits")).Clear();
        ((IList)Get(actor.Unit,"StatusEffects")).Clear();
        List(actor.Unit,"Injuries").Clear();
        foreach(string field in new[]{"SharedTraits","PersistentSharedTraits","ActiveConditionalTraits"})
        { var list=Get(actor.Unit,field) as IList; if(list!=null)list.Clear(); }
        Set(actor.Unit,"BattleActive",true);
        actor.Unit.SetStatBase(Stat.Voracity,20);
        Set(actor.Unit,"TemporaryHealth",0);
        actor.Unit.SetStatBase(Stat.Strength,40);
        actor.Unit.SetStatBase(Stat.Agility,30);
        actor.Unit.SetStatBase(Stat.Intelligence,20);
        Set(actor.Unit,"Downed",false);
        Set(actor,"WeaponAttackThisTurn",false);Set(actor,"SpellCastThisTurn",false);Set(actor,"MovedThisTurn",false);
        Set(actor,"SkippedBySlacking",false);Set(actor,"TurnsSlacking",0);
        Call(actor.Unit,"InitializeTraits");actor.Unit.Health=actor.Unit.MaxHealth;actor.Movement=2;
    }
}
