// Added by this fork for the VoreWar mercenary-company conversion.
// 2026-11-09: Wait for asynchronous synthetic saves in current native auto-resolve measurements.
// 2026-11-09: Pass the optional enemy flag explicitly in synthetic reflected character creation.
// 2026-11-06: Compare synthetic spellbook and bow loadouts in native battles (spec 40C).
// 2026-11-05: Run connected native campaign economy histories and boundary checks (spec 38 phase B).
// 2026-11-05: Diagnose cleanup backtracking and replay exact companies after Flight retirement.
// 2026-11-05: Add exact mitigation tables and paired armor-only calibration batches (spec 38 phase A).
// 2026-11-05: Verify the requested 10/20/30 HP recovery rates (spec 36).
// 2026-11-05: Trace cleanup stalls and measure revised injury/HP recovery.
// 2026-11-04: Seeded native battle/economy measurements using preserved synthetic campaigns (spec 34 slice 6).
// 2026-11-04: Compare the actual auto-resolve path and save writes for the reported slowdown.
// See CHANGES.md and License.txt for GPL-3.0 terms; inherited notices remain applicable.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using OdinSerializer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Editor-only measurements use isolated synthetic fixtures and retain their files.
[InitializeOnLoad]
public static partial class CompanyBalanceAudit
{
    const string Key = "CompanyBalanceAudit";
    const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static IEnumerator work;
    static Report report;
    static GameManager gm;
    static CompanyCampaignView view;
    static object oldWorld, oldConfig;
    static Rand oldRandom;
    static UnityEngine.Random.State oldUnityRandom;
    static string reportName="report.json";
    static bool nativeTurbo;
    static bool calibration;
    static readonly Dictionary<string,object> oldPreferences = new Dictionary<string,object>();
    static readonly Dictionary<object, float> oldArmorEvasion = new Dictionary<object, float>();
    [Serializable] public sealed class UnitRow
    { public string id,race,stats,gear,weapons; public int side,level,hp,maxHp,injuries; public bool digested,fled; }
    [Serializable] public sealed class Recovery
    { public string mode; public int days,wagesPaid,debt,departures,newOldInjuries,treatment; public bool ready; }
    [Serializable] public sealed class Battle
    {
        public string scenario,folder,error,outcome;
        public int seed,rating,companyCount,enemyCount,rounds,steps,downedCompany,downedEnemies,digestedCompany,digestedEnemies,
            fledCompany,fledEnemies,permanentLosses,newInjuries,goldReward,revivalCost,gearValue,fixtureFunding,setupSpent;
        public int weaponHits,weaponMisses,spells,swallows,escapes,opportunities,enemyDamageToCompany,mostDamageToOneMember,lastActionRound,roundsAfterLastAction;
        public double companyPower,enemyPower,seconds;
        public int autoResolveSaveFiles;
        public long autoResolveSaveBytes;
        public int[] finalRandomState;
        public bool normalTurnSaved,normalSaveValid,resultSaved,resultSaveValid;
        public double resultSaveSeconds;
        public bool completed,paid,ended,leaderDigested;
        public List<UnitRow> initial = new List<UnitRow>(), final = new List<UnitRow>(), settled = new List<UnitRow>();
        public List<Recovery> recovery = new List<Recovery>();
    }
    [Serializable] public sealed class Economy
    { public string scenario,debtPolicy; public int seed,days,rewardEveryDays,rewardRating,goldEarned,wagesPaid,finalGold,debt,departures,notices; }
    [Serializable] public sealed class RecoverySample
    { public string rules,grade,mode; public bool treated; public int hpDays,injuryDays,readyDays; }
    [Serializable] public sealed class Report
    {
        public bool complete;
        public string directory,error;
        public List<Battle> battles = new List<Battle>();
        public List<Economy> economy = new List<Economy>();
        public List<string> assumptions = new List<string>();
        public List<RecoverySample> recoverySamples = new List<RecoverySample>();
        public string calibrationMath;
        public string calibrationVariant;
        public string enemyReference;
        public float[] lightArmorTotals = new float[3];
        public int calibrationChecks;
    }
    sealed class Scenario
    {
        public string name; public int level=1,leaderLevel=1,rating=1,gear,slugs;
        public int[] roles=new[]{0,1,2}; public float hp=1; public bool injury,road,light;
        public bool armorOnly;
        public int armorSlot = -1;
        public int enemyDamageBonus;
        public float enemyAccuracyBonus;
        public string spellReview;
    }
    static CompanyBalanceAudit() { EditorApplication.update += Update; }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/base.unity");
        SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    static void Update()
    {
        int exit=SessionState.GetInt(Key+".exit",0);
        if(exit!=0 && !EditorApplication.isPlayingOrWillChangePlaymode)
        {SessionState.EraseInt(Key+".exit");EditorApplication.Exit(exit-1);return;}
        if(!SessionState.GetBool(Key,false) || !EditorApplication.isPlaying)return;
        try {if(work==null)work=RunAll();if(!work.MoveNext())Finish(null);}
        catch(Exception ex){Finish(ex);}
    }
    static Type T(string name) {return typeof(GameManager).Assembly.GetType(name,true);}
    static object G(object obj,string name)
    {var type=obj as Type??obj.GetType();var f=type.GetField(name,Flags);return f!=null?f.GetValue(obj is Type?null:obj):type.GetProperty(name,Flags).GetValue(obj is Type?null:obj,null);}
    static void S(object obj,string name,object value)
    {var type=obj as Type??obj.GetType();var f=type.GetField(name,Flags);if(f!=null)f.SetValue(obj is Type?null:obj,value);else type.GetProperty(name,Flags).SetValue(obj is Type?null:obj,value,null);}
    static object C(object obj,string name,params object[] args)
    {var type=obj as Type??obj.GetType();return type.GetMethods(Flags).First(m=>m.Name==name && m.GetParameters().Length==args.Length).Invoke(obj is Type?null:obj,args);}
    static IList L(object obj,string name) {return (IList)G(obj,name);}
    static string Id(object obj) {return (string)G(obj,"Id");}
    static Unit U(object obj) {return (Unit)G(obj,"Unit");}
    static byte[] Encode(object state) {return (byte[])C(T("CompanyCampaignSave"),"Encode",state);}
    static object Clone(object state) {return C(T("CompanyCampaignSave"),"Decode",Encode(state));}
    static void Preference(string name,object value)
    {oldPreferences[name]=G(T("Config"),name);S(T("Config"),name,value);}
    static void WriteReport() {File.WriteAllText(Path.Combine(report.directory,reportName),JsonUtility.ToJson(report,true));}
    static void Finish(Exception ex)
    {
        SessionState.SetBool(Key,false);
        try
        {
            if(report!=null){report.complete=ex==null;report.error=ex==null?null:ex.ToString();WriteReport();}
            if (economicReport != null && report != null) WriteEconomy();
            if(ex!=null)UnityEngine.Debug.LogError("Balance driver failed: "+ex);
            foreach(var pair in oldPreferences)S(T("Config"),pair.Key,pair.Value);
            foreach (var pair in oldArmorEvasion) S(pair.Key, "Evasion", pair.Value);
            if(oldConfig!=null)S(T("Config"),"World",oldConfig);
            if(oldWorld!=null)S(typeof(State),"World",oldWorld);
            if(oldRandom!=null)State.Rand=oldRandom;
            UnityEngine.Random.state=oldUnityRandom;
        }
        finally {SessionState.SetInt(Key+".exit",ex==null?1:2);EditorApplication.isPlaying=false;}
    }
    static IEnumerator RunAll()
    {
        for(int i=0;i<12;i++)yield return null;
        gm=UnityEngine.Object.FindObjectOfType<GameManager>();gm.enabled=false;
        view=(CompanyCampaignView)G(gm.Start_Mode,"CompanySaveView");view.enabled=false;
        string analyze=StringArgument("-balanceAnalyze");
        if(analyze!=null)
        {
            string path=Path.GetFullPath(analyze),logs=Path.GetFullPath("Logs")+Path.DirectorySeparatorChar;
            if(!path.StartsWith(logs,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Analysis inputs must be preserved synthetic reports under Logs.");
            var loaded=JsonUtility.FromJson<Report>(File.ReadAllText(path));
            if(loaded==null || !Path.GetFullPath(loaded.directory).StartsWith(logs,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Analysis output must stay under project Logs.");
            report=loaded;reportName="enriched-report.json";
        }
        else report=new Report{directory=Path.GetFullPath("Logs/balance34f-"+Guid.NewGuid().ToString("N"))};
        Directory.CreateDirectory(report.directory);
        File.WriteAllText("Logs/balance34f-latest.txt",report.directory);
        S(T("CompanyCampaignSave"),"DiagnosticDirectory",report.directory);
        oldWorld=State.World;oldConfig=G(T("Config"),"World");oldRandom=State.Rand;oldUnityRandom=UnityEngine.Random.state;
        var copied=SerializationUtility.DeserializeValue<WorldConfig>(SerializationUtility.SerializeValue((WorldConfig)oldConfig,DataFormat.Binary),DataFormat.Binary);
        S(T("Config"),"World",copied);S(copied,"AutoScaleTactical",false);S(copied,"TacticalSizeX",16);S(copied,"TacticalSizeY",16);
        Preference("WatchAIBattles",true);Preference("SkipAIOnlyStats",false);Preference("AutoUseAI",false);
        nativeTurbo=Environment.GetCommandLineArgs().Contains("-balanceTurbo");
        calibration=Environment.GetCommandLineArgs().Contains("-balanceCalibration");
        if (Environment.GetCommandLineArgs().Contains("-balanceEconomy"))
        {
            var economy = EconomyCampaigns();
            while (economy.MoveNext()) yield return economy.Current;
            yield break;
        }
        if(Environment.GetCommandLineArgs().Contains("-balanceRecovery"))
        { RecoveryFixtures();WriteReport();yield break; }
        if(analyze!=null)
        {
            foreach(var row in report.battles.Where(b=>b.completed))Enrich(row);
            report.assumptions.Add("Derived log timing is the final hit/miss/spell/swallow/escape round; subsequent rounds can include movement, support actions, digestion or a stall. Settled snapshots come from the canonical post-result save, not detached pre-settlement actors.");
            WriteReport();yield break;
        }
        report.assumptions.AddRange(new[]{"Native AI controls both sides on seeded 16x16 grass maps; no inference about optimal human play.",
            "Paired contract fixtures share enemy and battle seeds. Roles/species are coupled: Gnoll melee, Avian ranged, Lizard magic.",
            "Premium fixtures receive explicit synthetic capital; their setup cost is reported, not treated as earned campaign money.",
            "Fresh contract fixtures use current production nominal rewards; frozen replay states preserve their original quotes. Road fixtures use production travel-event generation; fixed swarms are labelled stress cases.",
            nativeTurbo ? "Actual synchronous TurboMode is measured, including any production save writes. Normal turn saving is checked before simulation; recovery/economy branches are omitted." : "Stepped turbo runs use production autosave suppression. Native victory/settlement runs explicitly each step with the real company session installed.",
            "Camp/town/treatment recovery are cloned counterfactuals; relocation is free in this benchmark, not a gameplay rule. Travel/deadline assumptions are reported separately.",
            "Recovery repays affordable full individual debts in roster order; economy reports compare this explicit policy with ignoring debt. Notice dismissal does not alter game debt rules. All files are synthetic and retained."});
        var scenarios=new[]{
            new Scenario{name="starter-mixed-r1"},new Scenario{name="starter-melee-r1",roles=new[]{0,0,0}},
            new Scenario{name="starter-ranged-r1",roles=new[]{1,1,1}},new Scenario{name="starter-magic-r1",roles=new[]{2,2,2}},
            new Scenario{name="starter-mixed-r2",rating=2},
            new Scenario{name="veteran-heavy1-r4",level=5,leaderLevel=5,rating=4,gear=1},
            new Scenario{name="veteran-heavy2-r4",level=5,leaderLevel=5,rating=4,gear=2},
            new Scenario{name="veteran-heavy3-r4",level=5,leaderLevel=5,rating=4,gear=3},
            new Scenario{name="veteran-light2-r4",level=5,leaderLevel=5,rating=4,gear=2,light=true},
            new Scenario{name="healthy-mixed-r2",level=3,leaderLevel=3,rating=2},
            new Scenario{name="wounded-mixed-r2",level=3,leaderLevel=3,rating=2,hp=.45f,injury=true},
            new Scenario{name="road-starter",road=true},new Scenario{name="road-balanced",road=true,level=3,leaderLevel=3},
            new Scenario{name="road-strong-leader",road=true,leaderLevel=8},
            new Scenario{name="road-wounded",road=true,level=3,leaderLevel=3,hp=.60f},
            new Scenario{name="stress-five-slugs",road=true,slugs=5},new Scenario{name="stress-seven-slugs",road=true,slugs=7}};
        bool spellReview = Environment.GetCommandLineArgs().Contains("-balanceSpells40C");
        if (spellReview)
        {
            scenarios = new[] {
                new Scenario{name="spell-bolt",level=5,leaderLevel=5,rating=4,roles=new[]{2,2,2},spellReview="bolt"},
                new Scenario{name="spell-cinder",level=5,leaderLevel=5,rating=4,roles=new[]{2,2,2},spellReview="cinder"},
                new Scenario{name="spell-bow",level=5,leaderLevel=5,rating=4,roles=new[]{2,2,2},spellReview="bow"}
            };
            report.assumptions.Add("Exploratory paired level-5 Lizard companies: all retain Power Bolt; the second slot is empty, Cinder Field, or a basic bow respectively. Loadouts are synthetic, without purchase costs; fixed-rating enemies share generation seeds. Small samples are not a balance proof.");
        }
        if (calibration)
        {
            ConfigureCalibrationVariant();
            var catalog = ((IEnumerable)G(T("CompanyEquipment"), "Catalog")).Cast<object>().ToArray();
            for (int tier = 1; tier <= 3; tier++)
                report.lightArmorTotals[tier - 1] = catalog.Where(d => (int)G(d, "Tier") == tier &&
                    Convert.ToInt32(G(d, "Slot")) < 4).Sum(d => (float)G(d, "Evasion"));
            report.enemyReference = StringArgument("-balanceEnemyReference");
            if (report.enemyReference != null)
            {
                string logs = Path.GetFullPath("Logs") + Path.DirectorySeparatorChar;
                report.enemyReference = Path.GetFullPath(report.enemyReference);
                if (!report.enemyReference.StartsWith(logs, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Enemy reference must be preserved synthetic data under project Logs.");
                var reference = JsonUtility.FromJson<Report>(File.ReadAllText(Path.Combine(report.enemyReference, "report.json")));
                if (!reference.complete || reference.battles.Any(b => !b.completed || !string.IsNullOrEmpty(b.error)))
                    throw new ArgumentException("Enemy reference must be a completed error-free batch.");
                report.assumptions.Add("Enemy characters/loadouts are decoded from each reference initial campaign. Stress modifications are already present and are not applied again.");
            }
            scenarios = CalibrationScenarios().ToArray();
            CalibrationMath();
            report.assumptions.Add("Calibration changes armor only. All branches retain generated weapons, with starting armor cleared before comparison. Fixed contract forces are generated in a separate seeded campaign.");
            report.assumptions.Add("Calibration omits recovery/economy counterfactuals. Initial and settled canonical company snapshots and battle logs remain preserved for each case. Same starting seed does not imply identical later random draws after outcomes diverge.");
        }
        int firstSeed = IntArgument("-balanceSeedStart", calibration ? 1001 : 101);
        int seedCount = IntArgument("-balanceSeedCount", calibration ? 8 : 3);
        if (seedCount < 1 || seedCount > 100 || firstSeed < 0)
            throw new ArgumentOutOfRangeException("Calibration seed range is invalid.");
        var seeds = calibration ? Enumerable.Range(firstSeed, seedCount) : new[] { 101, 202, 303 }.AsEnumerable();
        string scenarioPrefix = StringArgument("-balanceScenarioPrefix");
        int limit=IntArgument("-balanceCount",int.MaxValue),done=0;
        foreach(int seed in seeds)foreach(var scenario in scenarios)
        {
            if(seed!=IntArgument("-balanceSeed",seed) || (StringArgument("-balanceScenario")!=null && scenario.name!=StringArgument("-balanceScenario")))continue;
            if (scenarioPrefix != null && !scenario.name.StartsWith(scenarioPrefix, StringComparison.Ordinal)) continue;
            if(done++>=limit)break;
            var row=new Battle{scenario=scenario.name,seed=seed,rating=scenario.road?0:scenario.rating};
            row.folder=Path.Combine(report.directory,seed+"-"+scenario.name);Directory.CreateDirectory(row.folder);report.battles.Add(row);WriteReport();
            var enumerator=BattleRun(scenario,seed,row);
            bool more=true;while(more)
            {
                try {more=enumerator.MoveNext();}
                catch(Exception ex){row.error=ex.ToString();more=false;}
                if(more)yield return enumerator.Current;
            }
            CleanupBattle();WriteReport();UnityEngine.Debug.Log("Balance case "+row.scenario+" seed "+seed+": "+(row.error??row.outcome));yield return null;
        }
        if(!nativeTurbo && !calibration && !spellReview)EconomyRuns();WriteReport();
        if (report.battles.Any(b => !b.completed || !string.IsNullOrEmpty(b.error)))
            throw new InvalidOperationException("One or more native cases failed. See the preserved case errors.");
    }
    static int IntArgument(string key,int fallback)
    {var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,key);return i>=0 && i+1<args.Length?int.Parse(args[i+1],CultureInfo.InvariantCulture):fallback;}
    static string StringArgument(string key)
    {var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,key);return i>=0 && i+1<args.Length?args[i+1]:null;}
    static object Fresh(int seed,int[] roles,int level,int leaderLevel,bool sponsored)
    {
        State.Rand=new Rand(seed);var state=C(T("CompanyCampaign"),"Generate",seed,false);
        var races=new[]{Race.Gnolls,Race.Avians,Race.Lizards};
        var leader=C(T("CompanyEconomy"),"Create",state,races[roles[0]],roles[0],true,leaderLevel,false);C(T("CompanyEconomy"),"Begin",state,leader);
        if(sponsored)S(state,"Gold",30000);
        var town=L(state,"Towns").Cast<object>().First(t=>(int)G(t,"Tile")==(int)G(state,"Position"));
        for(int i=1;i<roles.Length;i++)
        {
            var c=C(T("CompanyEconomy"),"Create",state,races[roles[i]],roles[i],false,level,false);S(c,"TownId",Id(town));L(state,"Characters").Add(c);C(T("CompanyEconomy"),"Hire",state,c,town);
        }
        return state;
    }
    static object[] Members(object state)
    {return L(state,"Members").Cast<object>().Select(m=>L(state,"Characters").Cast<object>().Single(c=>Id(c)==(string)G(m,"CharacterId"))).ToArray();}
    static void Kit(object state,object c,int role,int tier,bool light)
    {
        string[] keys={"helmet","torso","legs","arms"};
        string weapon=role==1?(tier==1?"weapon.bow":"weapon.bow."+tier):(tier==1?"weapon.mace":"weapon.mace."+tier);
        EquipBuy(state,c,weapon,role==1?6:5);
        for(int slot=0;slot<4;slot++)
        {
            string id=tier==1?light?(slot==2?"armor.legs":"armor."+keys[slot]+".light.1"):(slot==2?"armor.legs.heavy.1":"armor."+keys[slot]):"armor."+keys[slot]+(light?".light.":".heavy.")+tier;
            EquipBuy(state,c,id,slot);
        }
    }
    static void EquipBuy(object state,object c,string id,int slot)
    {
        if(U(c).Items[slot]!=null && (string)G(U(c).Items[slot],"CompanyDefinitionId")==id)return;
        C(T("CompanyEquipment"),"Buy",state,id);var item=(Item)L(state,"Inventory")[L(state,"Inventory").Count-1];C(T("CompanyEquipment"),"Equip",state,c,item,slot);
    }
    static IEnumerator BattleRun(Scenario scenario,int seed,Battle row)
    {
        bool sponsored=scenario.level>1 || scenario.leaderLevel>1 || scenario.gear>0;
        var state=Fresh(seed,scenario.roles,scenario.level,scenario.leaderLevel,sponsored);var company=Members(state);
        if (scenario.spellReview != null)
            foreach (var member in company)
            {
                var unit = U(member);
                var bolt = (Item)C(T("CompanyEquipment"), "Create", "spell.bolt");
                var extra = scenario.spellReview == "cinder" ? (Item)C(T("CompanyEquipment"), "Create", "spell.cinder") : null;
                var bow = scenario.spellReview == "bow" ? (Item)C(T("CompanyEquipment"), "Create", "weapon.bow") : null;
                L(state, "Inventory").Add(bolt);
                if (extra != null) L(state, "Inventory").Add(extra);
                if (bow != null) L(state, "Inventory").Add(bow);
                unit.SetItem(bolt, 7); unit.SetItem(extra, 8); unit.SetItem(bow, 6);
                unit.UpdateSpells();
            }
        row.fixtureFunding=sponsored?30000-(int)G(T("CompanyEconomy"),"StartingGold"):0;S(state,"Reputation",25);
        if (scenario.armorOnly)
            foreach (var member in company) CalibrationArmor(state, member, scenario);
        else if(scenario.gear>0)for(int i=0;i<company.Length;i++)Kit(state,company[i],scenario.roles[i],scenario.gear,scenario.light);
        S(state,"Reputation",0);
        for(int i=0;i<company.Length;i++)
        {
            U(company[i]).Health=Math.Max(1,(int)(U(company[i]).MaxHealth*scenario.hp));
            if(scenario.injury && i==1)C(T("CompanyInjuries"),"Add",U(company[i]),Enum.Parse(T("CompanyBodyPart"),"Eyes"),Enum.Parse(T("CompanyInjuryGrade"),"Battered"));
        }
        int initialInjuries=company.Sum(c=>L(U(c),"Injuries").Count);
        row.gearValue=company.Sum(c=>(int)C(T("CompanyEquipment"),"GearPrice",U(c)));row.setupSpent=(int)G(T("CompanyEconomy"),"StartingGold")+row.fixtureFunding-(int)G(state,"Gold");
        int home=(int)G(state,"Position"),tile=(int)G(L(state,"Contracts")[0],"Tile");
        if(scenario.road)
        {
            S(state,"Position",tile);
            for(int attempt=0;attempt<200;attempt++)
            {
                S(state,"RoadEvent",null);S(state,"Battle",null);S(state,"NextRoadEventDay",0);C(T("CompanyRoadEvents"),"AfterTravel",state,home);
                if(G(state,"RoadEvent")!=null && G(G(state,"RoadEvent"),"Kind").ToString()=="SpitterSlugs")break;
            }
            if(G(state,"Battle")==null)throw new Exception("Could not select a native slug encounter.");
            if(scenario.slugs>0)
            {
                var occurrence=G(state,"RoadEvent");var ids=(List<string>)G(occurrence,"EnemyIds");var characters=L(state,"Characters");
                foreach(var c in characters.Cast<object>().Where(c=>ids.Contains(Id(c))).ToArray())characters.Remove(c);
                ids.Clear();for(int i=0;i<scenario.slugs;i++){var c=C(T("CompanyContracts"),"Enemy",state,Race.SpitterSlugs,1);S(c,"Employment",Enum.Parse(T("CompanyEmployment"),"Unavailable"));characters.Add(c);ids.Add(Id(c));}
            }
        }
        else
        {
            object[] enemies;
            if (calibration && report.enemyReference != null)
                enemies = ReferenceEnemies(seed, scenario);
            else
            {
                var enemyState=C(T("CompanyCampaign"),"Generate",seed+7000,false);
                enemies=((IEnumerable)C(T("CompanyContracts"),"Generate",enemyState,scenario.rating)).Cast<object>().ToArray();
            }
            if (report.enemyReference == null && (scenario.enemyDamageBonus != 0 || scenario.enemyAccuracyBonus != 0))
                foreach (var enemy in enemies)
                    foreach (var weapon in U(enemy).Items.OfType<Weapon>())
                    {
                        S(weapon, "Damage", weapon.Damage + scenario.enemyDamageBonus);
                        S(weapon, "AccuracyModifier", weapon.AccuracyModifier + scenario.enemyAccuracyBonus);
                    }
            foreach(var c in enemies)L(state,"Characters").Add(c);
            var contract=Activator.CreateInstance(T("CompanyContract"));S(contract,"Id",Guid.NewGuid().ToString("N"));S(contract,"TownId",Id(L(state,"Towns")[0]));
            S(contract,"Rating",scenario.rating);S(contract,"Reward",C(T("CompanyContracts"),"Reward",scenario.rating,0));S(contract,"Tile",tile);S(contract,"Deadline",30);
            S(contract,"Status",Enum.Parse(T("CompanyContractStatus"),"Active"));S(contract,"EnemyIds",enemies.Select(Id).ToList());S(contract,"Power",C(T("CompanyPower"),"Force",(object)enemies.Select(U).ToArray()));
            L(state,"Contracts").Add(contract);C(T("CompanyBattleSession"),"Prepare",state,contract);S(G(state,"Battle"),"PositionCommitted",true);S(state,"Position",tile);
        }
        ((int[])G(state,"Terrain"))[tile]=0;S(G(state,"Battle"),"Seed",seed+9000);
        if (Environment.GetCommandLineArgs().Contains("-balanceReplayCompany"))
        {
            if (!calibration || report.enemyReference == null)
                throw new ArgumentException("Exact replay requires a validated calibration reference.");
            string original = Path.Combine(report.enemyReference, seed + "-" + scenario.name, "initial-company.bin");
            state = C(T("CompanyCampaignSave"), "Decode", File.ReadAllBytes(original));
            company = Members(state);
            initialInjuries = company.Sum(c => L(U(c), "Injuries").Count);
            row.gearValue = company.Sum(c => (int)C(T("CompanyEquipment"), "GearPrice", U(c)));
            var sourceReport = JsonUtility.FromJson<Report>(File.ReadAllText(Path.Combine(report.enemyReference, "report.json")));
            var sourceRow = sourceReport.battles.Single(b => b.seed == seed && b.scenario == scenario.name);
            row.fixtureFunding = sourceRow.fixtureFunding;
            row.setupSpent = sourceRow.setupSpent;
            if (report.battles.Count == 1)
                report.assumptions.Add("Exact company replay decodes the original initial snapshot for both sides, retaining stats, equipment, battle seed and campaign state. Current gameplay retirement rules apply to stored traits.");
        }
        C(view,"Install",state);var session=Activator.CreateInstance(T("CompanyBattleSession"),Flags,null,new object[]{view,state},null);
        File.WriteAllBytes(Path.Combine(row.folder,"initial-company.bin"),Encode(state));C(session,"Start");
        var mode=gm.TacticalMode;S(mode,"AIAttacker",true);S(mode,"AIDefender",true);S(mode,"IsPlayerTurn",false);S(mode,"turboMode",true);C(mode,"RefreshAIIfNecessary");
        foreach(var camera in Resources.FindObjectsOfTypeAll<Camera>())camera.enabled=false;
        foreach(var renderer in Resources.FindObjectsOfTypeAll<UnityEngine.Tilemaps.TilemapRenderer>())renderer.enabled=false;
        var actors=L(mode,"units").Cast<Actor_Unit>().ToArray();row.initial=actors.Select(Snapshot).ToList();row.companyCount=company.Length;row.enemyCount=actors.Count(a=>a.Unit.Side==1);
        if (Environment.GetCommandLineArgs().Contains("-balanceTrace"))
        {
            var points = new[] { new Vec2i(8,7), new Vec2i(7,8), new Vec2i(8,9), new Vec2i(7,5),
                new Vec2i(8,6), new Vec2i(9,7), new Vec2i(8,5), new Vec2i(4,5), new Vec2i(3,6) };
            File.WriteAllLines(Path.Combine(row.folder, "terrain-trace.txt"), points.Select(p => TerrainState(actors[0], p)).ToArray());
        }
        if (calibration && report.battles.Count == 1)
            VerifyNativeHitRolls(actors.First(a => a.Unit.Side == 0), actors.First(a => a.Unit.Side == 1));
        row.companyPower=(double)C(T("CompanyPower"),"Force",(object)company.Select(U).ToArray());row.enemyPower=(double)C(T("CompanyPower"),"Force",(object)actors.Where(a=>a.Unit.Side==1).Select(a=>a.Unit).ToArray());
        int gold=(int)G(state,"Gold");var downed=new HashSet<Unit>();var watch=Stopwatch.StartNew();var slice=Stopwatch.StartNew();
        var traced=new HashSet<string>();
        if(nativeTurbo)
        {
            // Exercise the player-facing synchronous loop, without suppressing its save calls.
            S(mode,"turboMode",false);
            string saves=(string)G(view,"SaveDirectory");
            int before=Directory.GetFiles(saves,"*.company").Length;
            C(mode,"SaveCompanyTurn");
            // Company saves are asynchronous; wait here in the audit, never in the gameplay path.
            while ((bool)G(T("CompanySaveWorker"), "Busy")) yield return null;
            row.normalTurnSaved=Directory.GetFiles(saves,"*.company").Length==before+1;
            if(!row.normalTurnSaved)throw new Exception("Normal battle turn did not autosave.");
            string normalPath=Directory.GetFiles(saves,"*.company").OrderByDescending(p=>p,StringComparer.Ordinal).First();
            var loaded=C(T("CompanyCampaignSave"),"LoadForResume",normalPath);
            row.normalSaveValid=(bool)G(loaded,"InBattle") && (int)G(G(G(loaded,"Battle"),"Tactical"),"currentTurn")== (int)G(mode,"currentTurn");
            if(!row.normalSaveValid)throw new Exception("Normal autosave was not a valid resumable battle.");
            var existing=new HashSet<string>(Directory.GetFiles(saves,"*.company"));
            watch.Restart();C(mode,"TurboMode");
            var writes=Directory.GetFiles(saves,"*.company").Where(p=>!existing.Contains(p)).ToArray();
            row.autoResolveSaveFiles=writes.Length;row.autoResolveSaveBytes=writes.Sum(p=>new FileInfo(p).Length);
            row.finalRandomState=(int[])C(State.Rand,"Capture");
            if(!(bool)G(mode,"battleFinished"))throw new Exception("Native auto-resolve did not complete.");
        }
        while(!nativeTurbo && !(bool)G(mode,"battleFinished"))
        {
            if(++row.steps>30000 || watch.Elapsed.TotalSeconds>45)throw new TimeoutException("Native battle step/time cap; not classified as a loss or draw.");
            foreach(var a in actors)if((bool)G(a.Unit,"Downed"))downed.Add(a.Unit);
            var reactions=G(mode,"Opportunities");
            if(!(bool)G(reactions,"Busy") && (bool)C(mode,"VictoryCheck"))break;
            if((bool)G(mode,"waitingForDialog"))throw new Exception("Native combat requested an interactive dialog.");
            if((bool)C(reactions,"Advance",0f))
            {
                if((bool)G(mode,"DirtyPack"))C(mode,"UpdateAreaTraits");
                var currentAI = G(mode, "currentAI");
                bool traceDecision = Environment.GetCommandLineArgs().Contains("-balanceTrace") &&
                    (int)G(mode, "currentTurn") == 50 && (int)G(currentAI, "AISide") == 0;
                string beforeDecision = traceDecision ? DecisionState(currentAI, actors) : null;
                bool ran = (bool)C(currentAI, "RunAI");
                if (traceDecision)
                    File.AppendAllLines(Path.Combine(row.folder, "decision-trace.txt"), new[]
                    { "Before " + beforeDecision, "After " + DecisionState(currentAI, actors) + " ran=" + ran });
                if(!ran)
                {
                    if(Environment.GetCommandLineArgs().Contains("-balanceTrace"))
                    {
                        int turn=(int)G(mode,"currentTurn");var ai=G(mode,"currentAI");
                        string key=turn+"/"+G(ai,"AISide");
                        if(new[]{20,25,50,199}.Contains(turn) && traced.Add(key))TraceCleanup(row,ai,actors,turn);
                    }
                    C(mode,"EndTurn");
                }
            }
            if(slice.ElapsedMilliseconds>40){slice.Restart();yield return null;}
        }
        row.seconds=watch.Elapsed.TotalSeconds;row.rounds=(int)G(mode,"currentTurn");row.final=actors.Select(Snapshot).ToList();
        row.downedCompany=downed.Count(u=>u.Side==0);row.downedEnemies=downed.Count(u=>u.Side==1);row.digestedCompany=actors.Count(a=>a.Unit.Side==0 && (bool)G(a.Unit,"Digested"));row.digestedEnemies=actors.Count(a=>a.Unit.Side==1 && (bool)G(a.Unit,"Digested"));
        row.fledCompany=actors.Count(a=>a.Unit.Side==0 && a.Fled);row.fledEnemies=actors.Count(a=>a.Unit.Side==1 && a.Fled);
        File.WriteAllBytes(Path.Combine(row.folder,"battle-log.bin"),SerializationUtility.SerializeValue(mode.Log,DataFormat.Binary));
        var events=L(mode.Log,"events").Cast<object>().ToArray();
        row.weaponHits=events.Count(e=>G(e,"Type").ToString()=="Hit");row.weaponMisses=events.Count(e=>G(e,"Type").ToString()=="Miss");row.spells=events.Count(e=>G(e,"Type").ToString()=="SpellHit");row.swallows=events.Count(e=>new[]{"Devour","Unbirth","CockVore","BreastVore"}.Contains(G(e,"Type").ToString()));row.escapes=events.Count(e=>G(e,"Type").ToString()=="Escape");row.opportunities=events.Count(e=>(bool)G(e,"Opportunity"));
        var incoming=events.Where(e=>(G(e,"Type").ToString()=="Hit" || G(e,"Type").ToString()=="SpellHit") && G(e,"Unit") is Unit && ((Unit)G(e,"Unit")).Side==1 && G(e,"Target") is Unit && ((Unit)G(e,"Target")).Side==0).ToArray();
        row.enemyDamageToCompany=incoming.Sum(e=>(int)G(e,"Damage"));row.mostDamageToOneMember=incoming.GroupBy(e=>G(e,"Target")).Select(g=>g.Sum(e=>(int)G(e,"Damage"))).DefaultIfEmpty(0).Max();
        var result=G(view,"returnedCampaign");if(result==null)throw new Exception("Native company settlement was not queued.");
        row.goldReward=(int)G(result,"Gold")-gold;row.ended=(bool)G(result,"Ended");row.permanentLosses=L(result,"Memorial").Count;
        row.paid=!scenario.road && L(result,"Contracts").Cast<object>().Any(c=>(string)G(c,"Id")== (string)G(G(state,"Battle"),"ContractId") && G(c,"Status").ToString()=="Completed");
        row.leaderDigested=(bool)G(U(L(result,"Characters").Cast<object>().Single(c=>Id(c)==(string)G(result,"LeaderId"))),"Digested");
        row.newInjuries=Math.Max(0,company.Sum(c=>L(U(c),"Injuries").Count)-initialInjuries);
        row.outcome=((string)G(G(result,"Battle"),"Summary")).StartsWith("Stalemate")?"stalemate":row.ended?"company-ended":row.paid?"contract-completed":scenario.road && row.fledEnemies+row.digestedEnemies==row.enemyCount?"road-cleared":"retreated-or-target-remains";
        if(nativeTurbo && !row.ended)
        {
            // Return the detached result to the campaign view before acknowledging it.
            // Result acknowledgement is not an autosave boundary; campaign day/player-turn starts are.
            C(view,"Update");
        }
        C(T("CompanyBattleSession"),"Acknowledge",result);
        if(!row.ended && G(G(result,"Battle"),"Phase").ToString()=="Revival"){int cash=(int)G(result,"Gold");C(T("CompanyBattleSession"),"Revive",result);row.revivalCost=cash-(int)G(result,"Gold");}
        File.WriteAllBytes(Path.Combine(row.folder,"settled-company.bin"),Encode(result));
        if(!nativeTurbo && !calibration && !row.ended)foreach(string recovery in new[]{"camp","town","town-treated"})row.recovery.Add(Recover(result,recovery,seed));
        row.completed=true;
        Enrich(row);
    }
    static UnitRow Snapshot(Actor_Unit actor)
    {return SnapshotUnit(actor.Unit,actor.Fled);}
    static string TerrainState(Actor_Unit actor, Vec2i position)
    {
        var utilities = T("TacticalUtilities");
        var tiles = (Array)G(utilities, "tiles");
        var blocked = (bool[,])G(utilities, "blockedTiles");
        object tile = tiles.GetValue(position.x, position.y);
        return position.x + "," + position.y + " terrain=" + tile +
            " blocked=" + (blocked != null && blocked[position.x, position.y]) +
            " walkable=" + C(T("TacticalTileInfo"), "CanWalkInto", tile, actor);
    }
    static string DecisionState(object ai, Actor_Unit[] actors)
    {
        var path = G(ai, "path");
        string order = "none";
        if (path != null)
        {
            var action = (Action)G(path, "Action");
            var actor = (Actor_Unit)G(path, "Actor");
            string nodes = string.Join(" ", L(path, "Path").Cast<object>().Select(n =>
                G(n, "X") + "," + G(n, "Y")).ToArray());
            order = actor.Unit.Name + "[" + nodes + "] action=" + (action == null ? "none" : action.Method.Name);
        }
        return "path=" + order + " found=" + G(ai, "foundPath") + " acted=" + G(ai, "didAction") + " | " +
            string.Join(" | ", actors.Where(a => a.Unit.Side == 0).Select(a =>
                a.Unit.Name + "@" + a.Position.x + "," + a.Position.y + " AP=" + a.Movement +
                " occupiedOpen=" + C(T("TacticalUtilities"), "OpenTile", a.Position.x, a.Position.y, a) +
                " overlapping=" + C(T("TacticalUtilities"), "TileContainsMoreThanOneUnit", a.Position.x, a.Position.y)).ToArray());
    }
    static void TraceCleanup(Battle row,object ai,Actor_Unit[] actors,int turn)
    {
        var lines=new List<string>{"Round "+turn+" side "+G(ai,"AISide")+" type="+ai.GetType().Name+
            " onlyDowned="+G(ai,"onlySurrenderedEnemies")+" lackPredators="+G(ai,"lackPredators")+
            " retreating="+G(ai,"retreating")+" foundPath="+G(ai,"foundPath")+" didAction="+G(ai,"didAction")};
        foreach(var a in actors)
        {
            var pred=G(a,"PredatorComponent");
            string prey=pred==null?"none":string.Join(",",((IEnumerable)C(ai,"GetListOfPotentialPrey",a,true,a.Position,3)).Cast<object>().Select(t=>((Actor_Unit)G(t,"actor")).Unit.Name).ToArray());
            lines.Add(a.Unit.Name+" side="+a.Unit.Side+" pos="+a.Position.x+","+a.Position.y+" HP="+a.Unit.Health+" downed="+G(a,"Surrendered")+" dead="+G(a.Unit,"IsDead")+" fled="+a.Fled+" targetable="+a.Targetable+" predator="+G(a.Unit,"Predator")+" bulk="+C(a,"Bulk",0)+" cap="+(pred==null?"none":C(pred,"TotalCapacity")+"/"+C(pred,"FreeCap"))+" prey="+prey);
            string statuses = string.Join(",", L(a.Unit, "StatusEffects").Cast<object>().Select(e =>
                G(e, "Type") + "(" + G(e, "Duration") + ")").ToArray());
            lines.Add("  AP=" + a.Movement + "/" + a.MaxMovement() + " canAct=" + C(T("OpportunityAttacks"), "CanAct", a) +
                " paralyzed=" + a.Paralyzed + " sinceParalysis=" + G(a, "TurnsSinceLastParalysis") +
                " skipped=" + G(a, "SkippedBySlacking") + " contained=" + (G(a, "SelfPrey") != null) +
                " flight=" + a.Unit.HasTrait(Traits.Flight) + " statuses=" + statuses);
        }
        File.AppendAllLines(Path.Combine(row.folder,"cleanup-trace.txt"),lines);
    }
    static UnitRow SnapshotUnit(Unit u,bool fled=false)
    {
        return new UnitRow{id=u.Name,race=u.Race.ToString(),side=u.Side,level=u.Level,hp=u.Health,maxHp=u.MaxHealth,
            injuries=L(u,"Injuries").Count,digested=(bool)G(u,"Digested"),fled=fled,
            stats=string.Join("/",new[]{Stat.Strength,Stat.Agility,Stat.Intelligence,Stat.Voracity}.Select(s=>u.GetStatBase(s).ToString()).ToArray()),
            weapons=string.Join("/",u.Items.OfType<Weapon>().Select(w=>w.Damage+":"+w.Range+":"+w.AccuracyModifier.ToString("R",CultureInfo.InvariantCulture)).ToArray()),
            gear=string.Join("/",u.Items.Where(i=>i!=null).Select(i=>(string)G(i,"CompanyDefinitionId")).ToArray())};
    }
    static void Enrich(Battle row)
    {
        string root=Path.GetFullPath(report.directory)+Path.DirectorySeparatorChar;
        if(!Path.GetFullPath(row.folder).StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Case data escaped its synthetic run directory.");
        var initial=C(T("CompanyCampaignSave"),"Decode",File.ReadAllBytes(Path.Combine(row.folder,"initial-company.bin")));
        var names=new HashSet<string>(Members(initial).Select(c=>U(c).Name));
        foreach(var snapshot in row.initial.Concat(row.final))snapshot.side=names.Contains(snapshot.id)?0:1;
        var log=SerializationUtility.DeserializeValue<TacticalMessageLog>(File.ReadAllBytes(Path.Combine(row.folder,"battle-log.bin")),DataFormat.Binary);
        int round=1,last=1;var types=new HashSet<string>{"Hit","Miss","SpellHit","SpellMiss","Devour","Unbirth","CockVore","Escape"};
        foreach(var e in L(log,"events"))
        {
            string type=G(e,"Type").ToString();
            if(type=="NewTurn")
            {var match=System.Text.RegularExpressions.Regex.Match((string)G(e,"Message"),@"^Turn (\d+) -");if(match.Success)round=int.Parse(match.Groups[1].Value,CultureInfo.InvariantCulture);}
            if(types.Contains(type))last=round;
        }
        row.lastActionRound=last;row.roundsAfterLastAction=Math.Max(0,row.rounds-last);
        var state=C(T("CompanyCampaignSave"),"Decode",File.ReadAllBytes(Path.Combine(row.folder,"settled-company.bin")));
        row.settled=Members(state).Select(c=>SnapshotUnit(U(c))).ToList();
    }
    static int OldCount(object state) {return Members(state).Sum(c=>L(U(c),"Injuries").Cast<object>().Count(i=>G(i,"Grade").ToString()=="Old"));}
    static void RecoveryFixtures()
    {
        for(int mode=0;mode<3;mode++)if(Math.Abs((double)C(T("CompanyDailyRecovery"),"Rate",mode!=0,mode==2)-new[]{.10,.20,.30}[mode])>.000001)throw new Exception("HP recovery rates must be 10/20/30 percent.");
        var source=Fresh(951,new[]{0,1,2},1,1,false);C(view,"Install",source);
        string[] grades={"Bruised","Battered","Mangled"};int[] oldDays={3,5,8},newDays={5,14,28};double[] health={.70,.45,.20};
        for(int grade=0;grade<3;grade++)for(int mode=0;mode<3;mode++)foreach(bool old in new[]{true,false})foreach(bool treated in new[]{false,true})
        {
            if(old && treated)continue;
            var state=Clone(source);var u=U(Members(state)[1]);S(u,"BattleActive",false);S(u,"PermanentInjuryImmune",true);
            u.Health=Math.Max(1,(int)(u.MaxHealth*health[grade]));
            C(T("CompanyInjuries"),"Add",u,Enum.Parse(T("CompanyBodyPart"),"Arm"),Enum.Parse(T("CompanyInjuryGrade"),grades[grade]));
            var injury=L(u,"Injuries")[0];S(injury,"DaysRemaining",old?oldDays[grade]:newDays[grade]);S(injury,"RecoveryRoll",.9);
            if(treated)C(T("CompanyInjuries"),"TreatInjuries",u);
            var sample=new RecoverySample{rules=old?"previous-counterfactual":"current",grade=grades[grade],mode=new[]{"ordinary","field-rest","town-rest"}[mode],treated=treated};
            double rate=old?new[]{.03,.10,.15}[mode]:(double)C(T("CompanyDailyRecovery"),"Rate",mode!=0,mode==2);
            for(int day=1;day<=60;day++)
            {
                C(T("CompanyDailyRecovery"),"Recover",u,rate,old?mode+1:1);
                if(sample.hpDays==0 && u.Health>=(int)G(u,"HealingCeiling"))sample.hpDays=day;
                if(sample.injuryDays==0 && !L(u,"Injuries").Cast<object>().Any(i=>(bool)C(T("CompanyInjuries"),"Temporary",i)))sample.injuryDays=day;
                if(sample.hpDays>0 && sample.injuryDays>0){sample.readyDays=day;break;}
            }
            if(sample.readyDays==0 || (!old && (sample.hpDays>sample.injuryDays || (!treated && sample.hpDays==sample.injuryDays) || sample.injuryDays!=newDays[grade]-(treated?2:0))))throw new Exception("Recovery timeline did not separate general HP from specific injury: "+grades[grade]+"/"+mode);
            report.recoverySamples.Add(sample);
        }
        var wages=Clone(source);S(wages,"Gold",10000);var merc=Members(wages)[1];var unit=U(merc);S(unit,"BattleActive",false);S(unit,"PermanentInjuryImmune",true);
        C(T("CompanyInjuries"),"Add",unit,Enum.Parse(T("CompanyBodyPart"),"Arm"),Enum.Parse(T("CompanyInjuryGrade"),"Mangled"));
        S(L(unit,"Injuries")[0],"RecoveryRoll",.9);unit.Health=unit.MaxHealth/5;
        int bill=Members(wages).Sum(c=>(int)C(T("CompanyEconomy"),"Wage",wages,c));
        for(int day=1;day<=7;day++)Day(wages,true);
        if(unit.Health!=unit.MaxHealth || (int)G(L(unit,"Injuries")[0],"DaysRemaining")!=21 || (int)G(wages,"Gold")!=10000-bill)throw new Exception("Injured full-HP mercenary recovery/wage week failed.");
        S(unit,"PermanentInjuryImmune",false); // End the controlled fixture's immunity before checking canonical campaign serialization.
        var restored=Clone(wages);C(T("CompanyCampaignSave"),"RequireHealthPreserved",wages,restored);
        report.assumptions.Add("Controlled Arm injuries at70/45/20% HP, no additional random complications, stored recovery roll0.9; prior timings are rate/progress/duration counterfactuals under the current mechanics. Current treated injuries shorten by two days. Native seven-day clock verified full HP with21 Mangled days remaining, wages still charged, and serialized injury/HP preservation.");
    }
    static bool Ready(object state) {return Members(state).All(c=>U(c).Health>=(int)G(U(c),"HealingCeiling") && !L(U(c),"Injuries").Cast<object>().Any(i=>(bool)C(T("CompanyInjuries"),"Temporary",i)));}
    static Recovery Recover(object source,string mode,int seed)
    {
        var state=Clone(source);State.Rand=new Rand(seed+44000);int before=OldCount(state),members=Members(state).Length,cash=(int)G(state,"Gold");
        int position=mode=="camp"?Enumerable.Range(0,((int[])G(state,"Terrain")).Length).First(tile=>!L(state,"Towns").Cast<object>().Any(t=>(int)G(t,"Tile")==tile)):(int)G(L(state,"Towns")[0],"Tile");S(state,"Position",position);
        var result=new Recovery{mode=mode};
        if(mode=="town-treated")foreach(var c in Members(state).Where(c=>(bool)C(T("CompanyInjuries"),"CanTreat",U(c))))
        {int gold=(int)G(state,"Gold");if(gold<(int)G(T("CompanyInjuries"),"TreatmentCost"))continue;C(T("CompanyInjuries"),"Treat",state,Id(c));result.treatment+=gold-(int)G(state,"Gold");}
        while(!Ready(state) && result.days<30){PayAffordable(state);Day(state,true);PayAffordable(state);result.days++;}
        result.ready=Ready(state);result.wagesPaid=cash-(int)G(state,"Gold")-result.treatment;result.debt=Members(state).Sum(c=>(int)G(c,"Debt"));result.departures=members-Members(state).Length;result.newOldInjuries=OldCount(state)-before;
        return result;
    }
    static int Day(object state,bool resting)
    {
        int notices=0;C(G(view,"clock"),"EndDay",state,resting);
        for(int guard=0;(bool)G(state,"UpdatingDay") && guard<12;guard++)
        {
            if(!string.IsNullOrEmpty((string)G(state,"PendingDecision"))){notices++;S(state,"PendingDecision",null);S(state,"PendingDecisionKind",null);S(state,"PendingCharacterIds",new string[0]);S(state,"DeferredWageDecision",null);}
            C(G(view,"clock"),"Resume",state);
        }
        if((bool)G(state,"UpdatingDay"))throw new Exception("Daily decision loop failed to settle.");return notices;
    }
    static void EconomyRuns()
    {
        foreach(int seed in new[]{101,202,303})foreach(int cadence in new[]{4,7,10,0})foreach(bool repay in new[]{false,true})
        {
            var state=Fresh(seed,new[]{0,1,2},1,1,false);int wages=0,rewards=0,notices=0;State.Rand=new Rand(seed+55000);
            for(int day=1;day<=28;day++)
            {
                // Explicit perfect-success income counterfactual, not fabricated battle victories.
                if(cadence>0 && day%cadence==0){int reward=(int)C(T("CompanyContracts"),"Reward",1,0);S(state,"Gold",(int)G(state,"Gold")+reward);rewards+=reward;}
                int cash=(int)G(state,"Gold");if(repay)PayAffordable(state);notices+=Day(state,false);if(repay)PayAffordable(state);wages+=cash-(int)G(state,"Gold");
            }
            report.economy.Add(new Economy{scenario="starter-income-cadence",debtPolicy=repay?"pay-affordable-full-debts-in-roster-order":"ignore-debt",seed=seed,days=28,rewardEveryDays=cadence,rewardRating=1,goldEarned=rewards,wagesPaid=wages,finalGold=(int)G(state,"Gold"),debt=L(state,"Characters").Cast<object>().Sum(c=>(int)G(c,"Debt")),departures=3-Members(state).Length,notices=notices});
        }
    }
    static void PayAffordable(object state)
    {
        foreach(var c in Members(state).Where(c=>(int)G(c,"Debt")>0))
            if((int)G(state,"Gold")>=(int)G(c,"Debt"))C(T("CompanyEconomy"),"Pay",state,c);
    }
    static object[] ReferenceEnemies(int seed, Scenario scenario)
    {
        string path = Path.Combine(report.enemyReference, seed + "-" + scenario.name, "initial-company.bin");
        var state = C(T("CompanyCampaignSave"), "Decode", File.ReadAllBytes(path));
        string id = (string)G(G(state, "Battle"), "ContractId");
        var contract = L(state, "Contracts").Cast<object>().Single(c => Id(c) == id);
        var ids = ((IEnumerable)G(contract, "EnemyIds")).Cast<string>();
        return ids.Select(enemyId => L(state, "Characters").Cast<object>().Single(c => Id(c) == enemyId)).ToArray();
    }

    [Serializable] public sealed class NativeRollSample
    {
        public double accuracy, expectedGraze, expectedCritical;
        public int samples, hits, grazes, criticals;
    }

    [Serializable] public sealed class NativeRollReport
    {
        public List<NativeRollSample> samples = new List<NativeRollSample>();
    }

    static void VerifyNativeHitRolls(Actor_Unit attacker, Actor_Unit target)
    {
        var results = new NativeRollReport();
        var random = State.Rand;
        var targetBoosts = G(target.Unit, "TraitBoosts");
        var attackerBoosts = G(attacker.Unit, "TraitBoosts");
        float flat = (float)G(targetBoosts, "FlatHitReduction");
        try
        {
            State.Rand = new Rand(38001);
            foreach (float wanted in new[] { .50f, .5101f, .625f, .95f })
            {
                S(targetBoosts, "FlatHitReduction", 1f);
                float raw = target.GetAttackChance(attacker, false);
                S(targetBoosts, "FlatHitReduction", wanted / raw);
                float accuracy = target.GetAttackChance(attacker, false);
                float critical = attacker.CritCheck(attacker, target);
                critical += (float)G(G(attackerBoosts, "Outgoing"), "CritRateShift") -
                    (float)G(G(targetBoosts, "Incoming"), "CritRateShift");
                critical *= (float)C(T("TagConditionChecker"), "ApplyTagEffect", attacker.Unit, target.Unit,
                    Enum.Parse(T("UnitTagModifierEffect"), "CritRateShift"));
                critical += (float)C(T("TieredTraits"), "Amount", attacker.Unit, Enum.Parse(T("TierEffect"), "CriticalChance")) -
                    (float)C(T("CompanyInjuries"), "CriticalPenalty", attacker.Unit);
                var row = new NativeRollSample
                {
                    accuracy = accuracy,
                    expectedGraze = accuracy < .51f ? 0 : (1 - accuracy) * (attacker.Unit.HasTrait(Traits.KeenEye) ? 1 : accuracy),
                    expectedCritical = accuracy * Mathf.Clamp01(critical),
                    samples = 10000
                };
                for (int i = 0; i < row.samples; i++)
                {
                    object[] args = { target, false, 0f };
                    int outcome = (int)C(attacker, "ResolveWeaponHit", args);
                    if (outcome == 1 || outcome == 2) row.hits++;
                    if (outcome == 2) row.criticals++;
                    if (outcome == 3) row.grazes++;
                }
                VerifyFrequency(row.hits, row.samples, row.accuracy);
                VerifyFrequency(row.grazes, row.samples, row.expectedGraze);
                VerifyFrequency(row.criticals, row.samples, row.expectedCritical);
                results.samples.Add(row);
            }
            File.WriteAllText(Path.Combine(report.directory, "calibration-native-rolls.json"), JsonUtility.ToJson(results, true));
        }
        finally
        {
            S(targetBoosts, "FlatHitReduction", flat);
            State.Rand = random;
        }
    }

    static void VerifyFrequency(int observed, int samples, double probability)
    {
        double tolerance = .002 + 6 * Math.Sqrt(probability * (1 - probability) / samples);
        if (Math.Abs(observed / (double)samples - probability) > tolerance)
            throw new InvalidOperationException("Native hit/graze/critical frequencies disagree with the expected model.");
        report.calibrationChecks++;
    }

    static void ConfigureCalibrationVariant()
    {
        report.calibrationVariant = StringArgument("-balanceVariant") ?? "current";
        if (report.calibrationVariant == "current") return;
        if (report.calibrationVariant != "evasive")
            throw new ArgumentException("Unknown calibration variant.");
        // In-memory candidate only. Finish restores definitions, including failed runs.
        float[][] values =
        {
            new[] { .03f, .04f, .06f, .03f },
            new[] { .06f, .08f, .08f, .06f },
            new[] { .08f, .10f, .14f, .08f }
        };
        foreach (var definition in (IEnumerable)G(T("CompanyEquipment"), "Catalog"))
        {
            float evasion = (float)G(definition, "Evasion");
            int slot = Convert.ToInt32(G(definition, "Slot"));
            if (evasion <= 0 || slot < 0 || slot > 3) continue;
            oldArmorEvasion[definition] = evasion;
            int tier = (int)G(definition, "Tier");
            S(definition, "Evasion", values[tier - 1][slot]);
        }
        report.assumptions.Add("Historical candidate sets light armor to 16/28/40 percentage points by tier in memory on both sides. The report records active totals; the compiled current catalog depends on source version. Finish restores the original definitions.");
    }

    static IEnumerable<Scenario> CalibrationScenarios()
    {
        var contexts = new List<Scenario>
        {
            new Scenario { name = "starter", level = 1, leaderLevel = 1, rating = 1 },
            new Scenario { name = "mixed", level = 5, leaderLevel = 5, rating = 4 },
            new Scenario { name = "elite", level = 10, leaderLevel = 10, rating = 8 },
            new Scenario { name = "five", level = 5, leaderLevel = 5, rating = 8, roles = new[] { 0, 1, 2, 0, 1 } },
            new Scenario { name = "melee", level = 5, leaderLevel = 5, rating = 4, roles = new[] { 0, 0, 0 } },
            new Scenario { name = "ranged", level = 5, leaderLevel = 5, rating = 4, roles = new[] { 1, 1, 1 } },
            new Scenario { name = "magic", level = 5, leaderLevel = 5, rating = 4, roles = new[] { 2, 2, 2 } },
            new Scenario { name = "wounded", level = 5, leaderLevel = 5, rating = 4, hp = .45f, injury = true },
            new Scenario { name = "leader", level = 1, leaderLevel = 8, rating = 4 }
        };
        if (Environment.GetCommandLineArgs().Contains("-balanceExtended"))
        {
            contexts.Add(new Scenario { name = "hard", level = 5, leaderLevel = 5, rating = 8 });
            contexts.Add(new Scenario { name = "highhit", level = 5, leaderLevel = 5, rating = 4, enemyDamageBonus = 12 });
            contexts.Add(new Scenario { name = "accurate", level = 5, leaderLevel = 5, rating = 4, enemyAccuracyBonus = .75f });
            report.assumptions.Add("Extended stress fixtures explicitly add 12 base enemy weapon damage or 0.75 enemy weapon accuracy rating, or face rating8 with three level5 members. These are diagnostic counterfactuals; their recorded enemy power differs from nominal rating where gear was modified.");
        }
        foreach (var context in contexts)
        {
            int tier = context.level >= 10 ? 3 : context.level >= 5 ? 2 : 1;
            for (int branch = 0; branch < 3; branch++)
                yield return new Scenario
                {
                    name = context.name + new[] { "-none", "-heavy", "-light" }[branch],
                    level = context.level,
                    leaderLevel = context.leaderLevel,
                    rating = context.rating,
                    roles = context.roles,
                    hp = context.hp,
                    injury = context.injury,
                    gear = branch == 0 ? 0 : tier,
                    armorOnly = true,
                    light = branch == 2,
                    enemyDamageBonus = context.enemyDamageBonus,
                    enemyAccuracyBonus = context.enemyAccuracyBonus
                };
        }
        // Single-piece alternatives at the same level, with all other armor slots empty.
        foreach (bool light in new[] { false, true })
            yield return new Scenario
            {
                name = "torso-" + (light ? "light" : "heavy"),
                level = 5, leaderLevel = 5, rating = 4,
                gear = 2, armorOnly = true, armorSlot = 1, light = light
            };
    }

    static void CalibrationArmor(object state, object member, Scenario scenario)
    {
        for (int slot = 0; slot < 4; slot++)
            C(T("CompanyEquipment"), "Equip", state, member, null, slot);
        if (scenario.gear == 0) return;
        string[] keys = { "helmet", "torso", "legs", "arms" };
        for (int slot = 0; slot < 4; slot++)
        {
            if (scenario.armorSlot >= 0 && slot != scenario.armorSlot) continue;
            string id;
            if (scenario.gear > 1)
                id = "armor." + keys[slot] + (scenario.light ? ".light." : ".heavy.") + scenario.gear;
            else if (scenario.light)
                id = slot == 2 ? "armor.legs" : "armor." + keys[slot] + ".light.1";
            else
                id = slot == 2 ? "armor.legs.heavy.1" : "armor." + keys[slot];
            EquipBuy(state, member, id, slot);
        }
    }

    [Serializable] public sealed class MitigationSample
    {
        public int incomingDamage, armor, barrier, temporaryHP;
        public double expectedPoolLoss, expectedLastingLoss;
    }

    [Serializable] public sealed class OutcomeSample
    {
        public double accuracy, criticalChance, miss, normal, critical, graze, damageMultiplier;
        public bool keenEye;
    }

    [Serializable] public sealed class CalibrationCalculation
    {
        public int productionChecks;
        public float grazeMultiplier, criticalMultiplier;
        public List<MitigationSample> mitigation = new List<MitigationSample>();
        public List<OutcomeSample> outcomes = new List<OutcomeSample>();
        public List<string> assumptions = new List<string>();
    }

    static void CalibrationMath()
    {
        var calculation = new CalibrationCalculation
        {
            grazeMultiplier = (float)G(T("Config"), "GrazeDamageMod"),
            criticalMultiplier = (float)G(T("Config"), "CritDamageMod")
        };
        calculation.assumptions.Add("Incoming damage is after outgoing/incoming modifiers, elemental resistance and mana-barrier reduction. Armor deflects uniformly from zero through AC, with minimum one before barrier/temporary HP.");
        calculation.assumptions.Add("Outcome multipliers are before integer rounding, flat damage additions and armor. Native battle fixtures handle those effects, statuses, vore and positioning. Crit rolls apply only to initial hits; grazes never become critical hits.");
        foreach (int damage in new[] { 1, 2, 4, 8, 12, 20, 40 })
        foreach (int armor in new[] { 0, 1, 2, 3, 5, 10, 15 })
        foreach (int barrier in new[] { 0, 5 })
        foreach (int temporaryHP in new[] { 0, 5 })
        {
            double poolLoss = 0, lastingLoss = 0;
            for (int roll = 0; roll <= armor; roll++)
            {
                int hit = Math.Max(1, damage - roll);
                poolLoss += Math.Max(0, hit - barrier) / (armor + 1.0);
                lastingLoss += Math.Max(0, hit - barrier - temporaryHP) / (armor + 1.0);
            }
            double productionPool = (double)C(T("CompanyEquipment"), "ExpectedAfterArmor", damage, armor, barrier);
            double productionLasting = (double)C(T("CompanyEquipment"), "ExpectedAfterArmor", damage, armor, barrier + temporaryHP);
            if (Math.Abs(poolLoss - productionPool) > .000001 || Math.Abs(lastingLoss - productionLasting) > .000001)
                throw new InvalidOperationException("Enumerated armor outcomes disagree with production arithmetic.");
            calculation.productionChecks += 2;
            calculation.mitigation.Add(new MitigationSample
            {
                incomingDamage = damage, armor = armor, barrier = barrier, temporaryHP = temporaryHP,
                expectedPoolLoss = poolLoss, expectedLastingLoss = lastingLoss
            });
        }
        foreach (double accuracy in new[] { .25, .50, .51, .625, .75, .95, 1.0 })
        foreach (double criticalChance in new[] { .075, .15, .30 })
        foreach (bool keenEye in new[] { false, true })
        {
            double graze = accuracy < .51 ? 0 : (1 - accuracy) * (keenEye ? 1 : accuracy);
            var outcome = new OutcomeSample
            {
                accuracy = accuracy, criticalChance = criticalChance, keenEye = keenEye,
                critical = accuracy * criticalChance,
                normal = accuracy * (1 - criticalChance), graze = graze,
                miss = 1 - accuracy - graze
            };
            outcome.damageMultiplier = outcome.normal + outcome.critical * calculation.criticalMultiplier + graze * calculation.grazeMultiplier;
            if (Math.Abs(outcome.normal + outcome.critical + outcome.graze + outcome.miss - 1) > .000001)
                throw new InvalidOperationException("Attack outcome probabilities do not sum to one.");
            double production = (double)C(T("CompanyPower"), "WeaponOutcomeFactor", accuracy, criticalChance,
                (double)calculation.criticalMultiplier, (double)calculation.grazeMultiplier, keenEye);
            if (Math.Abs(production - outcome.damageMultiplier) > .000001)
                throw new InvalidOperationException("Healthy power disagrees with the exact weapon outcome probabilities.");
            calculation.productionChecks++;
            calculation.outcomes.Add(outcome);
        }
        report.calibrationMath = Path.Combine(report.directory, "calibration-math.json");
        report.calibrationChecks = calculation.productionChecks;
        File.WriteAllText(report.calibrationMath, JsonUtility.ToJson(calculation, true));
    }

    static void CleanupBattle()
    {
        var session=G(gm,"CompanyBattle");
        if(gm.CurrentScene==gm.TacticalMode)C(gm,"ReturnFromCompanyBattle");
        if(session!=null)C(session,"RestoreServices");
        S(view,"returnedCampaign",null);S(view,"returningSession",null);gm.StatScreen.gameObject.SetActive(false);
    }
}
