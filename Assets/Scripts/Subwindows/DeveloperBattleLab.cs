// 2026-11-06: Identify the official or developer draft used by native scenarios (spec 40A).
// Modified for the VoreWar mercenary-company conversion.
// 2026-11-08: Hide retired ranged weapon variants from new Battle Lab loadout choices.
// 2026-11-08: Hide retired evasion-granting armor from newly configured Battle Lab fixtures.
// 2026-11-05: Separate faster general HP recovery from longer calendar-day injuries.
// See CHANGES.md and License.txt for GPL-3.0 terms.
// Added by this fork for the VoreWar mercenary-company conversion.
// 2026-11-04: Apply explainable healthy power and approved capped encounter scaling (spec 34 slice 3).
// 2026-11-04: Isolated deterministic battle, trait, equipment and injury/recovery lab (spec 34 slice 2).
// See CHANGES.md and License.txt for GPL-3.0 terms; existing asset/dependency notices remain applicable.
using System;
using System.Collections.Generic;
using System.Linq;
using OdinSerializer;
using UnityEngine;
using UnityEngine.UI;

public sealed class DeveloperBattleLab : MonoBehaviour
{
    internal static DeveloperBattleLab Active;
    internal static bool IsOpen => Active != null && Active.open;
    bool open, running;
    World previousWorld;
    WorldConfig previousConfig;
    Rand previousRandom;
    GameObject root, page;
    Transform frame;
    Text summary, result;
    Race[] species;
    internal Actor_Unit[] Fighters { get; private set; }
    int selected, tab, seed = 12345;
    int traitChoice, spellChoice, injuryPartChoice, injuryGradeChoice, oldKindChoice;
    byte[] fixture;
    int[] fixtureRandom;
    GameManager GM => State.GameManager;
    Actor_Unit Actor => Fighters[selected];
    Actor_Unit Other => Fighters[1-selected];
    Dropdown template;

    internal static void AddMenuButton(StartMode start)
    {
        if (start.UI.transform.Find("Developer Battle Lab") != null) return;
        var race = start.UI.GetComponentsInChildren<Button>(true).FirstOrDefault(b =>
            b.GetComponentsInChildren<Text>(true).Any(t => string.Equals(t.text.Trim(), "Race Editor", StringComparison.OrdinalIgnoreCase)));
        if (race == null) return;
        var rect = race.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 0);
        rect.sizeDelta = new Vector2(300,58); rect.anchoredPosition = new Vector2(-158,54);
        foreach(var text in race.GetComponentsInChildren<Text>(true)) text.fontSize=24;
        var go=UITheme.Node("Developer Battle Lab",start.UI.transform);var r=go.GetComponent<RectTransform>();
        r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,0);r.sizeDelta=new Vector2(300,58);r.anchoredPosition=new Vector2(158,54);
        UITheme.Surface(go,UITheme.Control,true);var button=go.AddComponent<Button>();UITheme.StyleSelectable(button);
        var label=UITheme.Label(go.transform,"Battle Lab",0,0,300,58,24);label.alignment=TextAnchor.MiddleCenter;
        button.onClick.AddListener(Gate);
    }
    static void Gate() { State.GameManager.Menu.OpenBattleLab(); }

    internal void Open()
    {
        if (open || !GM.Menu.HasRaceEditorAccess || GM.CurrentScene!=GM.Start_Mode || GM.CompanyBattle!=null) return;
        previousWorld=State.World;previousConfig=Config.World;previousRandom=State.Rand;
        species=SpeciesPolicy.GetAvailable(SpeciesUse.Enemy);
        if(species.Length==0) {GM.Menu.EndRaceEditorSession();GM.CreateMessageBox(SpeciesPolicy.UnavailableMessage);return;}
        open=true;Active=this;
        try
        {
            Config.World=SerializationUtility.DeserializeValue<WorldConfig>(SerializationUtility.SerializeValue(previousConfig,DataFormat.Binary),DataFormat.Binary);
            Config.World.AutoScaleTactical=false;Config.World.TacticalSizeX=12;Config.World.TacticalSizeY=12;
            template=GM.Start_Mode.CreateTacticalGame.GetComponentsInChildren<Dropdown>(true).First();
            Build(); ResetScenario(seed);
        }
        catch { Close();throw; }
    }
    internal void Close()
    {
        if(!open)return;
        try { if(GM.CurrentScene==GM.TacticalMode)GM.SwitchToMainMenu(); }
        finally
        {
            State.World=previousWorld;Config.World=previousConfig;State.Rand=previousRandom;
            open=false;Active=null;fixture=null;Fighters=null;
            if(root!=null){root.SetActive(false);Destroy(root);root=null;}
            GM.Menu.EndRaceEditorSession();
        }
    }
    void Update() { if(open && !GM.ActiveInput && UnityEngine.Input.GetKeyDown(KeyCode.Escape))Close(); }
    internal void ResetScenario(int value)
    {
        seed=value;State.Rand=new Rand(seed);State.World=new World(CompanyEquipment.Repository);
        var units=new Unit[2];
        for(int i=0;i<2;i++)
        {
            units[i]=new Unit(i,species[Math.Min(i,species.Length-1)],0,true,UnitType.Soldier,itemContext:CompanyEquipment.Repository,companyLoadout:true);
            units[i].Name=i==0?"Test A":"Test B";
            units[i].SetItem(CompanyEquipment.Create("weapon.mace"),(int)CompanySlot.Melee);
            units[i].SetItem(CompanyEquipment.Create("weapon.bow"),(int)CompanySlot.Ranged);
            units[i].SetItem(CompanyEquipment.Create("spell.bolt"),(int)CompanySlot.Spell1);
            units[i].Health=units[i].MaxHealth;units[i].RestoreManaPct(1);
        }
        LoadUnits(units); fixture=null;
    }
    void LoadUnits(Unit[] units)
    {
        if(GM.CurrentScene==GM.TacticalMode)GM.TacticalMode.CleanUp();
        var lasting=units.Select(u=>u.LastingHealth).ToArray();var temporary=units.Select(u=>u.TemporaryHealth).ToArray();var mana=units.Select(u=>u.Mana).ToArray();
        var armies=new[]{new Army(new Empire(),new Vec2i(0,0),0),new Army(new Empire(),new Vec2i(0,0),1)};
        for(int i=0;i<2;i++){units[i].Side=i;armies[i].Empire.ReplacedRace=units[i].Race;armies[i].Units.Add(units[i]);}
        GM.ActivatePureTacticalMode(StrategicTileType.grass,null,armies[0],armies[1],TacticalAIType.None,TacticalAIType.None);
        Fighters=units.Select(TacticalUtilities.GetActorOf).ToArray();
        if(Fighters.Any(a=>a==null))throw new InvalidOperationException("Could not bind isolated battle actors.");
        for(int i=0;i<2;i++){Fighters[i].SetPos(new Vec2i(4+i,4));units[i].SetLabResources(lasting[i],temporary[i],mana[i]);Fighters[i].UpdateBestWeapons();}
        GM.TacticalMode.UI.SetActive(false);
        if(root!=null){root.SetActive(true);Draw();}
    }
    internal void StoreFixture()
    {
        if(Fighters.Any(a=>a.SelfPrey!=null || a.Unit.Digested))throw new InvalidOperationException("Restore or release both units before storing a configuration fixture.");
        fixture=SerializationUtility.SerializeValue(Fighters.Select(a=>a.Unit).ToArray(),DataFormat.Binary);fixtureRandom=State.Rand.Capture();
    }
    internal void RestoreFixture()
    {
        if(fixture==null)throw new InvalidOperationException("Store a fixture first.");
        var units=SerializationUtility.DeserializeValue<Unit[]>(fixture,DataFormat.Binary);LoadUnits(units);State.Rand=Rand.Resume(fixtureRandom);
    }
    void Run(Action action)
    {
        if(running)return;running=true;
        try {action();Draw();result.text="Done. Combat round "+GM.TacticalMode.currentTurn+"; native battle progression remains paused.";}
        catch(Exception ex){result.text=ex.Message;}
        finally{running=false;}
    }
    internal void AdvanceTurn() { GM.TacticalMode.currentTurn++; foreach(var a in Fighters.Where(a=>!a.Unit.Digested)){a.Unit.BattleActive=true;a.NewTurn();} }
    internal void RecoveryDay(int kind)
    {
        foreach(var a in Fighters)
        {
            if(a.SelfPrey!=null)throw new InvalidOperationException("Release swallowed units before advancing campaign recovery.");
        }
        foreach(var a in Fighters)
        {
            a.Unit.FinishCombat();
            CompanyDailyRecovery.Recover(a.Unit,CompanyDailyRecovery.Rate(kind != 0,kind == 2),1);
            a.Unit.ClampInjuryMana();
        }
    }
    void PrepareAction(bool ranged)
    {
        if(Actor.Unit.Digested || Other.Unit.Digested || Actor.Surrendered || Actor.SelfPrey!=null || Other.SelfPrey!=null)
            throw new InvalidOperationException("Reset incapacitated units or release swallowed units first.");
        if(Actor.Movement<=0)throw new InvalidOperationException("No AP remains. Use Next turn or Reset AP.");
        foreach(var a in Fighters) a.Unit.BattleActive=true;
        Actor.SetPos(new Vec2i(4,4));Other.SetPos(new Vec2i(ranged?6:5,4));Actor.UpdateBestWeapons();Other.UpdateBestWeapons();
    }
    void Build()
    {
        root=new GameObject("Developer Battle Lab",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        root.transform.SetParent(GM.transform,false);var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=250;
        var scale=root.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1280,800);scale.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        var back=UITheme.Node("Backdrop",root.transform);UITheme.Stretch(back.GetComponent<RectTransform>());UITheme.Surface(back,UITheme.Background);
        var panel=UITheme.Node("Lab frame",root.transform);var rect=panel.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(1240,770);UITheme.Surface(panel,UITheme.Panel,true);frame=panel.transform;
        Label(frame,"Developer Battle Lab — isolated Single Battle; no campaign saves",20,12,1030,30,23);
        Button(frame,"Close",1080,12,140,30,Close);
        Button(frame,"Test A",20,53,115,30,()=>{selected=0;Draw();});Button(frame,"Test B",145,53,115,30,()=>{selected=1;Draw();});
        var seedInput=Input(frame,seed.ToString(),325,53,100);Label(frame,"Seed",272,54,50,28);
        Button(frame,"New scenario",435,53,160,30,()=>Run(()=>ResetScenario(Number(seedInput,-2000000000,2000000000))));
        Button(frame,"Store fixture",610,53,160,30,()=>Run(StoreFixture));Button(frame,"Restore fixture",785,53,165,30,()=>Run(RestoreFixture));
        summary=Label(frame,"",20,94,1200,96,17);
        string[] tabs={"Combatants","Traits","Equipment","Injuries / recovery","Actions","Power"};
        for(int i=0;i<tabs.Length;i++){int n=i;Button(frame,tabs[i],20+i*201,197,190,33,()=>{tab=n;Draw();});}
        page=UITheme.Node("Controls",frame);UITheme.Place(page.GetComponent<RectTransform>(),20,245,1200,430);
        result=Label(frame,"Configuration fixtures stay in memory. New scenario uses the seed; Close restores prior settings and RNG.",20,690,1200,60,17);
    }
    void Draw()
    {
        if(Fighters==null || summary==null)return;
        summary.text=BalanceAuthoring.Identity+"\n"+string.Join("\n",Fighters.Select((a,i)=>(i==selected?"> ":"  ")+(i==0?"A":"B")+" "+SpeciesPolicy.DisplayName(a.Unit.Race)+" Lv"+a.Unit.Level+
            " | STR "+a.Unit.GetStat(Stat.Strength)+" AGI "+a.Unit.GetStat(Stat.Agility)+" INT "+a.Unit.GetStat(Stat.Intelligence)+" VOR "+a.Unit.GetStat(Stat.Voracity)+
            " | HP "+a.Unit.Health+"/"+a.Unit.MaxHealth+" (lasting "+a.Unit.LastingHealth+", temp "+a.Unit.TemporaryHealth+") | Mana "+a.Unit.Mana+"/"+a.Unit.MaxMana+" | AP "+a.Movement+"/"+a.CurrentMaxMovement()+
            "\n    "+CompanyInjuries.Summary(a.Unit)+(a.Surrendered?" | DOWNED":"")+(a.Unit.Digested?" | DIGESTED":"")));
        foreach(Transform child in page.transform){child.gameObject.SetActive(false);Destroy(child.gameObject);}
        var parent=page.transform;var unit=Actor.Unit;
        if(tab==0)
        {
            Drop(parent,"Species",species.Select(SpeciesPolicy.DisplayName).ToList(),Array.IndexOf(species,unit.Race),0,0,index=>Run(()=>
            {
                var replacement=new Unit(selected,species[index],0,true,UnitType.Soldier,itemContext:CompanyEquipment.Repository,companyLoadout:true);
                replacement.Name=selected==0?"Test A":"Test B";replacement.Health=replacement.MaxHealth;replacement.RestoreManaPct(1);var units=Fighters.Select(a=>a.Unit).ToArray();units[selected]=replacement;LoadUnits(units);
            }));
            var level=Input(parent,unit.Level.ToString(),500,0,95);Label(parent,"Level",435,2,65,28);Button(parent,"Set level",610,0,130,30,()=>Run(()=>unit.SetLevel(Number(level,1,100))));
            var statInputs=new List<InputField>();
            for(int i=0;i<4;i++){var stat=WarbandStats.Active[i];Label(parent,WarbandStats.Name(stat),0,52+i*40,100,28);statInputs.Add(Input(parent,unit.GetStatBase(stat).ToString(),110,52+i*40,100));}
            Button(parent,"Apply base stats",240,90,190,36,()=>Run(()=>{for(int i=0;i<4;i++)unit.SetStatBase(WarbandStats.Active[i],Number(statInputs[i],1,9999));}));
            Label(parent,"Lasting HP / Temporary HP / Mana",500,70,650,28);
            var hp=Input(parent,unit.LastingHealth.ToString(),500,110,110);var temp=Input(parent,unit.TemporaryHealth.ToString(),625,110,110);var mana=Input(parent,unit.Mana.ToString(),750,110,110);
            Button(parent,"Set resources",875,110,190,34,()=>Run(()=>{unit.SetLabResources(Number(hp,0,999999),Number(temp,0,999999),Number(mana,0,999999));Actor.Targetable=true;}));
            Button(parent,"Full HP / mana",500,165,200,34,()=>Run(()=>unit.SetLabResources(unit.MaxHealth,0,unit.MaxMana)));
            Button(parent,"Downed (0 HP)",715,165,200,34,()=>Run(()=>unit.SetLabResources(0,0,unit.Mana)));
            Label(parent,"Level changes here do not grant stat growth. Configure base stats explicitly.\nResources are developer overrides; they intentionally bypass the healing ceiling.",0,255,760,65);
            Portrait(parent,Actor,1000,300);
        }
        else if(tab==1)
        {
            var traits=Enum.GetValues(typeof(Traits)).Cast<Traits>().Where(t=>(int)t<1000 && !TraitsMethods.IsRetired(t)).Select(TieredTraits.Canonical).Distinct().OrderBy(TraitsMethods.DisplayName).ToArray();
            var pick=Drop(parent,"Trait",traits.Select(TraitsMethods.DisplayName).ToList(),traitChoice,0,0,i=>traitChoice=i);
            Button(parent,"Add / upgrade",410,0,190,34,()=>Run(()=>unit.AddTrait(traits[pick.value])));
            Button(parent,"Remove",615,0,155,34,()=>Run(()=>unit.RemoveTrait(traits[pick.value])));
            Button(parent,"Clear traits",785,0,170,34,()=>Run(()=>{unit.ClearLabTraits();}));
            Label(parent,unit.ListTraits()+"\n\nOnly one tier or opposite trait of a family applies.",0,60,1100,335);
        }
        else if(tab==2)
        {
            for(int slot=0;slot<9;slot++)
            {
                int n=slot;var defs=CompanyEquipment.Catalog.Where(d=>!d.Retired && !CompanyEquipment.IsRetiredEvasionArmor(d) && ((int)d.Slot==n || (n==(int)CompanySlot.Spell2 && d.Slot==CompanySlot.Spell1))).ToArray();
                var names=new List<string>{"Empty"};names.AddRange(defs.Select(d=>d.Name));
                int choice=unit.Items[n]==null?0:Array.FindIndex(defs,d=>d.Id==unit.Items[n].CompanyDefinitionId)+1;
                Drop(parent,((CompanySlot)n).ToString(),names,choice,(slot/5)*600,(slot%5)*58,index=>Run(()=>{unit.SetItem(index==0?null:CompanyEquipment.Create(defs[index-1].Id),n);unit.UpdateSpells();Actor.UpdateBestWeapons();}));
            }
            Label(parent,"Equipment is created for this fixture only. No gold or inventory is taken from a campaign.",0,330,1160,55);
        }
        else if(tab==3) DrawInjuries(parent);
        else if(tab==4) DrawActions(parent);
        else if(tab==5)
        {
            var report=CompanyPower.Evaluate(unit);
            Label(parent,report.Describe(unit),0,0,1180,405,17);
        }
        else DrawLog(parent);
    }
    void DrawInjuries(Transform parent)
    {
        var unit=Actor.Unit;var parts=Enum.GetValues(typeof(CompanyBodyPart)).Cast<CompanyBodyPart>().ToArray();
        var part=Drop(parent,"Part",parts.Select(p=>p.ToString()).ToList(),injuryPartChoice,0,0,i=>injuryPartChoice=i);
        var grade=Drop(parent,"Severity",new List<string>{"Bruised","Battered","Mangled"},injuryGradeChoice,410,0,i=>injuryGradeChoice=i);
        Button(parent,"Add injury",830,0,160,32,()=>Run(()=>CompanyInjuries.Add(unit,parts[part.value],new[]{CompanyInjuryGrade.Bruised,CompanyInjuryGrade.Battered,CompanyInjuryGrade.Mangled}[grade.value])));
        var old=Drop(parent,"Old family",new List<string>{"Selected body part","Poor Focus","Dulled Instincts","Sluggish Recovery","Sensitive Stomach"},oldKindChoice,0,50,i=>oldKindChoice=i);
        Button(parent,"Add / upgrade old",410,50,220,32,()=>Run(()=>CompanyInjuries.AddOld(unit,parts[part.value],(CompanyComplication)old.value)));
        Button(parent,"Clear all injuries",650,50,205,32,()=>Run(()=>unit.Injuries.Clear()));
        Button(parent,"Treat selected unit",0,104,225,32,()=>Run(()=>CompanyInjuries.TreatInjuries(unit)));
        Button(parent,"Ordinary day",240,104,200,32,()=>Run(()=>RecoveryDay(0)));
        Button(parent,"Camp rest day",455,104,200,32,()=>Run(()=>RecoveryDay(1)));
        Button(parent,"Town rest day",670,104,200,32,()=>Run(()=>RecoveryDay(2)));
        if(unit.Injuries.Count>0)
        {
            var existing=Drop(parent,"Existing injury",unit.Injuries.Select(CompanyInjuries.Name).ToList(),0,0,150,null);
            Button(parent,"Remove selected injury",410,150,260,32,()=>Run(()=>{unit.Injuries.RemoveAt(existing.value);unit.ClampInjuryMana();}));
        }
        Label(parent,CompanyInjuries.DamageDescription(unit)+"\nHealing ceiling "+unit.HealingCeiling+"/"+unit.MaxHealth+"\n"+
            string.Join("\n",unit.Injuries.Take(7).Select(CompanyInjuries.Description))+(unit.Injuries.Count>7?"\nMore injuries exist; use the Existing injury selector to inspect/remove each one.":""),0,195,1190,230,16);
    }
    void DrawActions(Transform parent)
    {
        Button(parent,"Melee (adjacent)",0,0,235,35,()=>Run(()=>{PrepareAction(false);Actor.Attack(Other,false);}));
        Button(parent,"Ranged (2 tiles)",250,0,235,35,()=>Run(()=>{PrepareAction(true);Actor.Attack(Other,true);}));
        var spells=SpellList.SpellDict.Values.OfType<DamageSpell>().Where(s=>!s.IsRetired).OrderBy(s=>s.Name).ToArray();
        var spell=Drop(parent,"Spell",spells.Select(s=>s.Name).ToList(),spellChoice,0,60,i=>spellChoice=i);
        Button(parent,"Cast at other unit",410,60,225,35,()=>Run(()=>{PrepareAction(true);Actor.CastOffensiveSpell(spells[spell.value],Other);}));
        Button(parent,"Next combat turn",0,120,235,35,()=>Run(AdvanceTurn));
        Button(parent,"Reset AP",250,120,180,35,()=>Run(()=>{foreach(var a in Fighters)a.Movement=a.CurrentMaxMovement();}));
        Button(parent,"Battle log",850,120,220,35,()=>{tab=6;Draw();});
        Button(parent,"Heal 10 (temporary)",0,178,235,35,()=>Run(()=>{Actor.Unit.BattleActive=true;Actor.Unit.Heal(10);}));
        Button(parent,"Heal 10 (lasting)",250,178,235,35,()=>Run(()=>Actor.Unit.HealLasting(10)));
        Button(parent,"End temporary HP",500,178,235,35,()=>Run(()=>{foreach(var a in Fighters)a.Unit.FinishCombat();}));
        Button(parent,"Vore",0,238,155,35,()=>Run(()=>{PrepareAction(false);Actor.PredatorComponent.Devour(Other,0f);}));
        Button(parent,"Regurgitate",170,238,190,35,()=>Run(()=>Actor.PredatorComponent.FreeAnyAlivePrey()));
        Button(parent,"Digestion tick",375,238,190,35,()=>Run(()=>Actor.PredatorComponent.Digest("")));
        Label(parent,"Actions use native hit, graze, critical and injury rolls. Set target HP to 0 for guaranteed vore.\nRecovery days end temporary HP first, then use the native recovery/treatment rules.\nRead the battle log in this panel; no campaign time, wages or saved games are advanced.",0,300,1170,85);
        var log=GM.TacticalMode.Log.DebugDump();Label(parent,string.Join("\n",log.Split('\n').Reverse().Take(2).Reverse().ToArray()),0,387,1160,42,15);
    }
    void DrawLog(Transform parent)
    {
        var area=UITheme.Node("Log viewport",parent);UITheme.Place(area.GetComponent<RectTransform>(),0,0,1170,425);
        UITheme.Surface(area,UITheme.Row,true);area.AddComponent<RectMask2D>();var scroll=area.AddComponent<ScrollRect>();
        scroll.viewport=area.GetComponent<RectTransform>();scroll.horizontal=false;scroll.vertical=true;scroll.scrollSensitivity=30;
        var text=Label(area.transform,GM.TacticalMode.Log.DebugDump(),10,5,1140,420,16);text.alignment=TextAnchor.UpperLeft;text.verticalOverflow=VerticalWrapMode.Overflow;
        Canvas.ForceUpdateCanvases();text.rectTransform.sizeDelta=new Vector2(1140,Math.Max(420,text.preferredHeight+20));scroll.content=text.rectTransform;
        var track=UITheme.Node("Log scrollbar",parent);UITheme.Place(track.GetComponent<RectTransform>(),1182,0,12,425);UITheme.Surface(track,UITheme.Row);
        var handle=UITheme.Node("Handle",track.transform);UITheme.Stretch(handle.GetComponent<RectTransform>());var image=UITheme.Surface(handle,UITheme.Accent);
        var bar=track.AddComponent<Scrollbar>();bar.direction=Scrollbar.Direction.BottomToTop;bar.handleRect=handle.GetComponent<RectTransform>();bar.targetGraphic=image;
        scroll.verticalScrollbar=bar;scroll.verticalNormalizedPosition=0;
    }
    void Portrait(Transform parent,Actor_Unit actor,float x,float y)
    {
        var go=UITheme.Node("Paper doll",parent);UITheme.Place(go.GetComponent<RectTransform>(),x,y,0,0);go.transform.localScale=Vector3.one*1.5f;
        var image=go.AddComponent<UIUnitSprite>();image.Name=Label(go.transform,"",0,0,1,1);image.SetIndex(-1);image.UpdateSprites(actor);
        foreach(var g in go.GetComponentsInChildren<Graphic>())g.raycastTarget=false;
    }
    static Text Label(Transform parent,string text,float x,float y,float width,float height,int size=18) => UITheme.Label(parent,text,x,y,width,height,size);
    static void Button(Transform parent,string text,float x,float y,float width,float height,Action action)
    {
        var go=UITheme.Node(text,parent);UITheme.Place(go.GetComponent<RectTransform>(),x,y,width,height);UITheme.Surface(go,UITheme.Control,true);
        var button=go.AddComponent<Button>();UITheme.StyleSelectable(button);button.onClick.AddListener(()=>action());
        var label=Label(go.transform,text,3,1,width-6,height-2,18);label.alignment=TextAnchor.MiddleCenter;
    }
    static InputField Input(Transform parent,string value,float x,float y,float width)
    {
        var go=UITheme.Node("Input",parent);UITheme.Place(go.GetComponent<RectTransform>(),x,y,width,32);UITheme.Surface(go,UITheme.Row,true);
        var input=go.AddComponent<InputField>();input.textComponent=Label(go.transform,"",6,1,width-12,30,18);input.contentType=InputField.ContentType.IntegerNumber;input.text=value;
        input.customCaretColor=true;input.caretColor=UITheme.Foreground;return input;
    }
    Dropdown Drop(Transform parent,string caption,List<string> options,int value,float x,float y,Action<int> changed)
    {
        Label(parent,caption,x,y+2,120,28,17);
        var drop=Instantiate(template,parent);drop.name=caption;drop.onValueChanged=new Dropdown.DropdownEvent();drop.ClearOptions();drop.AddOptions(options);
        drop.gameObject.SetActive(true);UITheme.Place(drop.GetComponent<RectTransform>(),x+125,y,270,32);drop.transform.localScale=Vector3.one;
        foreach(var text in drop.GetComponentsInChildren<Text>(true)){text.fontSize=17;text.color=UITheme.Foreground;}
        UITheme.StylePopup(drop.gameObject);UITheme.Surface(drop.gameObject,UITheme.Control,true);UITheme.StyleSelectable(drop);drop.value=Math.Max(0,value);drop.RefreshShownValue();
        if(changed!=null)drop.onValueChanged.AddListener(i=>changed(i));return drop;
    }
    static int Number(InputField field,int minimum,int maximum)
    { int value;if(!int.TryParse(field.text,out value)||value<minimum||value>maximum)throw new InvalidOperationException("Enter a number from "+minimum+" to "+maximum+".");return value; }
}
