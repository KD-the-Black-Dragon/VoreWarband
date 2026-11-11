// Modified for the VoreWar mercenary-company conversion.
// 2026-11-08: Display company weapon art for the last melee or ranged weapon used.
// 2026-11-07: Keep inborn traits out of form-based trait sharing (spec 40D).
// 2026-11-07: Persist per-actor evidence for post-battle trait progression (spec 40D).
// 2026-11-09: Allow AI accuracy previews from proposed tiles without moving the actor (tactical AI review).
// 2026-11-09: Release all living occupants immediately on every new Downed transition (tactical AI follow-up).
// 2026-11-10: Record spell activity once per cast for post-battle event eligibility (spec 40D narrative follow-up).
// 2026-11-06: Use frozen per-character AP and body size (spec 40A).
// 2026-11-05: Use the shared downed-prey capacity rule in special vore actions.
// See CHANGES.md and License.txt for GPL-3.0 terms.
// Modified for the VoreWar mercenary-company conversion.
// 2026-11-04: Correct caster targeting and share side-effect-free spell damage estimates (spec 34 slice 1).
// 2026-11-04: Apply armor/evasion to direct hits and share non-rolling equipment previews (spec 34 slice 4).
// 2026-11-04: Apply approved injury tiers, base-STR HP, healing ceilings and recovery rules (spec 33).
// 2026-11-04: Implement exclusive baseline trait tiers, flat combat modifiers and categorized Help (spec 32).
// 2026-10-30: Reject disallowed disguise templates before appearance, trait or side changes (spec 03A2).
// 2026-10-30: 04B2: Replace callback prose with neutral action and status notices.
// 2026-10-31: Retire Respawns grants/death triggers and CloseCall casualty presentation (spec 05A3B7).
// 2026-10-31: Retire the Reanimator trait's automatic Reanimate spell grant (spec 05A3B10).
// 2026-10-31: Retire Binder grant and summon capture/persistent resummoning routines (spec 05A3B11).
// 2026-10-31: Retire damage/surrender defection and Goddess Mercy full-health recovery (spec 05A3B13).
// 2026-10-31: Retire charm/hypnosis trait grants and reject retired direct status-spell casts (spec 05A3B14).
// 2026-11-01: Retire SeductiveTouch support-action conversion/distraction runtime (spec 05A3B15).
// 2026-11-01: Complete approved ability retirement and close legacy reactivation paths (spec 05A3B17).
// 2026-11-01: Render company weapons through the unit item context without a legacy World (specs 07–08).
// 2026-11-01: Latch downed status until battle ends; fear and zero HP cannot restore command ability (specs 09–10).
// 2026-11-01: Apply persistent weapon/movement penalties and route damage through the single lasting-injury check (spec 11).
// 2026-11-01: Use the actual caster-target level difference for spell XP challenge scaling (spec 11 feedback).
// 2026-11-02: Dismiss magical summons instead of placing them in a persistent downed state through fear/nonlethal damage (spec 12).
// 2026-11-03: Persist pending animation progress and tactical stance for battle saves (spec 19).
// 2026-11-03: Remove permanently retired identity branches/entries while preserving shared behavior (spec 21).
// See CHANGES.md for the fork change record and License.txt for GPL-3.0 terms.
// 2026-11-03: Implement four active stats and agreed combat rules (spec 29).
// 2026-11-04: Free melee reactions before move commands, without reaction knockback or AP spending (spec 30).
// Modified for the VoreWar mercenary-company conversion.
// 2026-11-04: Track turn movement and first weapon attack for conditional DoubleAttack and Maul; persist battle state (spec 31).
// See CHANGES.md and License.txt for GPL-3.0 terms.
// 2026-11-04: Rename DoubleAttack presentation to DoubleShot and restrict its bonus to ranged attacks (spec 31).
// 2026-11-04: Retire QuickShooter and align FastCaster with standing-ground action economy (spec 31).
// 2026-11-04: Apply approved Dodger, KeenEye and action-economy trait refinements (spec 31).
// 2026-11-04: Apply EasyPrey, MadMage, retreat and approved trait retirements (spec 31).
// 2026-11-04: Implement approved Dazzle, Vindication, Talented and description refinements (spec 31).
// 2026-11-04: Distinguish Juggernaut active turns from scheduled and actual skipped turns (grouped review).
// 2026-11-04: Apply approved trait removals, digestion triggers, names and plain-effect descriptions (spec 31).
using OdinSerializer;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using static UnityEngine.UI.CanvasScaler;

public class Actor_Unit
{
    enum DisplayMode
    {
        None,
        Attacking,
        RangeAttacking,
        MeleeAttacking,
        OralVore,
        CockVore,
        TailVore,
        BreastVore,
        AnalVore,
        Unbirth,
        BladderVore,
        FrogPouncing,
        VoreSuccess,
        VoreFail,
        Digesting,
        Absorbing,
        Birthing,
        Suckling,
        Rubbing,
        Suckled,
        Rubbed,
        Injured,
        Hurt,
        SpecialAttack,
        IdleAnimation,

    }

    [OdinSerialize] int animationStep = 4;
    [OdinSerialize] float animationUpdateTime;
    [OdinSerialize] DisplayMode mode;
    [OdinSerialize]
    public List<KeyValuePair<int, float>> modeQueue;

    private DisplayMode Mode
    {
        get => mode;
        set
        {
            if (State.GameManager.TacticalMode.turboMode && value >= DisplayMode.Attacking && value <= DisplayMode.FrogPouncing)
                HasAttackedThisCombat = true;
            mode = value;
        }
    }


    [OdinSerialize]
    public Unit Unit { get; private set; }
    [OdinSerialize] CompanyBattleEvidence battleEvidence = new CompanyBattleEvidence();
    internal CompanyBattleEvidence Evidence
    {
        get
        {
            if (battleEvidence == null) battleEvidence = new CompanyBattleEvidence();
            return battleEvidence;
        }
    }
    [OdinSerialize]
    public Vec2i Position { get; private set; }
    [OdinSerialize]
    int movement;
    public int Movement { get { return Unit != null && Unit.Downed ? 0 : movement; } set { movement = value; } }
    [OdinSerialize]
    public bool Visible;
    [OdinSerialize]
    public bool Targetable;
    public bool ReceivedRub => (RubCount >= Config.BellyRubsPerTurn && Config.BellyRubsPerTurn >= 0);
    [OdinSerialize]
    public int RubCount;

    public bool BeingRubbed;


    [OdinSerialize]
    bool surrendered;
    public bool Surrendered
    {
        get { return Unit != null && Unit.BattleActive ? Unit.Downed : surrendered; }
        set
        {
            if (Unit != null && Unit.BattleActive)
            {
                if (value)
                {
                    if (Unit.Type == UnitType.Summon) { Unit.Health = 0; Unit.Downed = false; }
                    else if (!Unit.Downed)
                    {
                        Unit.Downed = true;
                        HandleDownedTransition();
                    }
                    Movement = 0;
                }
            }
            else surrendered = value;
        }
    }

    internal void HandleDownedTransition()
    {
        if (Unit == null || !Unit.BattleActive || !Unit.Downed || Unit.Type == UnitType.Summon || Unit.Digested)
            return;
        CompanyPostBattleTraits.RecordDowned(Unit);
        PredatorComponent?.FreeAnyAlivePrey();
    }

    public bool DefectedThisTurn;

    [OdinSerialize]
    public bool WasJustFreed;

    [OdinSerialize] public bool SurrenderedThisTurn = false;

    internal bool Intimidated;

    [OdinSerialize] internal bool IsAllIn = false;

    [OdinSerialize]
    public Weapon BestMelee;
    [OdinSerialize]
    public Weapon BestRanged;

    [OdinSerialize]
    public bool Fled;

    // DayNight addition
    [OdinSerialize]
    public bool Hidden;
    [OdinSerialize]
    public bool InSight = true;
	
    [OdinSerialize]
    internal Prey SelfPrey;

    [OdinSerialize]
    public bool Slimed;
    [OdinSerialize]
    public int TurnsSlacking = 0;
    [OdinSerialize]
    public bool Paralyzed;

    [OdinSerialize]
    public int Corruption;

    [OdinSerialize]
    public int Possessed;

    [OdinSerialize]
    public bool Infected;

    [OdinSerialize]
    public Race InfectedRace;

    [OdinSerialize]
    public int InfectedSide;

    [OdinSerialize]
    public bool KilledByDigestion;

    [OdinSerialize]
    public int TimesParalyzed;

    [OdinSerialize]
    public bool GoneBerserk;

    [OdinSerialize]
    internal int AIAvoidEat;

    [OdinSerialize]
    internal int TurnUsedShun = -5;
    [OdinSerialize]
    internal int TurnUsedStunStrike = -5;
    [OdinSerialize]
    internal int MultifacetedCooldown = 0;
    [OdinSerialize]
    internal int TurnsSinceLastDamage = 9999;

    [OdinSerialize]
    internal int TurnsSinceLastParalysis = 9999;

    [OdinSerialize]
    internal float RampStacks = 0;

    [OdinSerialize]
    internal int spriteLayerOffset = 0;

    [OdinSerialize]
    public bool HasAttackedThisCombat = false;

    [OdinSerialize]
    public bool allowedToDefect = false;



    private UnitSprite _unitSprite;
    internal UnitSprite UnitSprite
    {
        get
        {
            if (_unitSprite == null)
                GenerateSpritePrefab(State.GameManager.TacticalMode.ActorFolder);
            return _unitSprite;
        }
        set => _unitSprite = value;
    }

    internal bool SquishedBreasts = false;

    [OdinSerialize]
    public PredatorComponent PredatorComponent;

    [OdinSerialize] Race animationControllerRace;

    [OdinSerialize]
    private AnimationController animationController = new AnimationController();

    public AnimationController AnimationController
    {
        get
        {
            if (animationController == null || animationControllerRace != Unit.Race)
            {
                animationController = new AnimationController();
                animationControllerRace = Unit.Race;
            }

            return animationController;
        }
        set => animationController = value;
    }

    public void ClearMovement() => Movement = 0;
    public void DrainExp(int damage) => Unit.GiveExp(-1 * damage);
    [OdinSerialize] internal bool MovedThisTurn;
    [OdinSerialize] bool WeaponAttackThisTurn;
    [OdinSerialize] bool SpellCastThisTurn;
    public void SetPos(Vec2i p)
    {
        if (Position != null && !Position.Matches(p)) MovedThisTurn = true;
        Position = p;
    }

    void SpendWeaponAP(bool ranged, int attacks, bool free)
    {
        if (free) return;
        bool extra = !MovedThisTurn && !WeaponAttackThisTurn && Movement > 1 &&
            ((ranged && Unit.HasTrait(Traits.DoubleAttack)) || (!ranged && Unit.HasTrait(Traits.Maul)));
        WeaponAttackThisTurn = true;
        if (extra) Movement = 1;
        else if (attacks > 1)
        {
            int cost = 1 + MaxMovement() / attacks;
            Movement = Movement > cost ? Movement - cost : 0;
        }
        else Movement = 0;
    }

    void SpendSpellAP()
    {
        bool extra = !MovedThisTurn && !SpellCastThisTurn && Movement > 1 && Unit.HasTrait(Traits.FastCaster);
        SpellCastThisTurn = true;
        if (extra) Movement = 1;
        else if (Unit.TraitBoosts.SpellAttacks > 1)
        {
            int cost = 1 + MaxMovement() / Unit.TraitBoosts.SpellAttacks;
            Movement = Movement > cost ? Movement - cost : 0;
        }
        else Movement = 0;
    }

    [OdinSerialize] internal bool SkippedBySlacking;

    private void RestoreMP()
    {
        SkippedBySlacking = false;
        if (WasJustFreed)
        {
            Movement = Mathf.Min(Mathf.Max(2, (int)(.2f * CurrentMaxMovement())), 5);
            WasJustFreed = false;
        }
        if (Paralyzed)
        {
            State.GameManager.TacticalMode.Log.RegisterMiscellaneous($"<b>{Unit.Name}</b> was paralyzed and cannot move this turn.");
            Movement = 0;
            TimesParalyzed++;
            TurnsSinceLastParalysis = 0;
            Paralyzed = false;
            Slimed = false;
        }
        else if ((Unit.GetStatusEffect(StatusEffectType.Petrify) != null) || (Unit.GetStatusEffect(StatusEffectType.Frozen) != null)
            || Unit.GetStatusEffect(StatusEffectType.Sleeping) != null || Unit.GetStatusEffect(StatusEffectType.Stunned) != null)
        {
            Movement = 0;
            Slimed = false;
        }
        else if (Unit.GetStatusEffect(StatusEffectType.Glued) != null)
        {
            Movement = 2;
            Slimed = false;
        }
        else if ((Unit.GetStatusEffect(StatusEffectType.Webbed) != null) || (Unit.GetStatusEffect(StatusEffectType.Snared) != null))
        {
            Movement = 1;
            Slimed = false;
        }
        else if (Unit.GetStatusEffect(StatusEffectType.Staggering) != null)
        {
            Movement = CurrentMaxMovement() / 2;
            Slimed = false;
        }
        else if (Slimed)
        {
            Movement = CurrentMaxMovement() / 2;
            Slimed = false;
        }
        else if (TurnsSlacking >= 1) 
        {
            SkippedBySlacking = true;
            Movement = 0;
            TurnsSlacking--;
        }
        else
            Movement = CurrentMaxMovement();

        if ((Unit.HasTrait(Traits.Slacker) || Unit.HasTrait(Traits.Juggernaut)) && Movement > 0)
        {
            TurnsSlacking++;
            if (Unit.HasTrait(Traits.Slacker) && Unit.HasTrait(Traits.Juggernaut))
            {
                TurnsSlacking++;
            }
        }

        if (Movement > Config.TacticalMovementHardCap && Config.TacticalMovementHardCap > 0)
        {
            Movement = Config.TacticalMovementHardCap;
        }
        if (Movement > Config.TacticalMovementSoftCap && Config.TacticalMovementSoftCap >= 0)
        {
            int excess = Movement - Config.TacticalMovementSoftCap;
            int required = 2;
            Movement = Config.TacticalMovementSoftCap;
            while (excess >= required) 
            {
                Movement++;
                required *= 2;
            }
        }
    }



    public int MaxMovement()
    {
        return Unit.SpeciesSettings.GetBaseAP(Unit.Race);
    }

    public int CurrentMaxMovement()
    {
        int sizePenalty = (int)((PredatorComponent?.Fullness ?? 0) * Unit.TraitBoosts.SpeedLossFromWeightMultiplier);
        return Math.Max(1, MaxMovement() - sizePenalty - CompanyInjuries.MovementPenalty(Unit));
    }

    /// <summary>
    /// This one is a dummy for some cases
    /// </summary>
    /// <param name="unit"></param>
    public Actor_Unit(Unit unit)
    {
        unit.SetBreastSize(-1); //Resets to default
        Mode = DisplayMode.None;
        modeQueue = new List<KeyValuePair<int, float>>();
        animationUpdateTime = 0;
        Position = new Vec2i(0, 0);
        Unit = unit;
        Evidence.StartingMaxHealth = Math.Max(1, unit.MaxHealth);
        Visible = true;
        Targetable = true;
        MultifacetedCooldown = Unit.HasTrait(Traits.Multifaceted) && Unit.IsHighestStat(Stat.Agility) ? Unit.Level + 1 : 0; // Set timer for extra attacks.
    }

    public Actor_Unit(Unit unit, Actor_Unit reciepient)
    {
        PredatorComponent = new PredatorComponent(this, unit);
        unit.SetBreastSize(-1); //Resets to default
        Mode = DisplayMode.None;
        modeQueue = new List<KeyValuePair<int, float>>();
        animationUpdateTime = 0;
        Position = reciepient.Position;
        Unit = unit;
        Evidence.StartingMaxHealth = Math.Max(1, unit.MaxHealth);
        Visible = false;
        Targetable = false;
        MultifacetedCooldown = Unit.HasTrait(Traits.Multifaceted) && Unit.IsHighestStat(Stat.Agility) ? Unit.Level + 1 : 0; // Set timer for extra attacks.
        RestoreMP();
        ReloadSpellTraits();
    }

    public Actor_Unit(Vec2i p, Unit unit)
    {
        PredatorComponent = new PredatorComponent(this, unit);
        unit.SetBreastSize(-1); //Resets to default
        Mode = DisplayMode.None;
        modeQueue = new List<KeyValuePair<int, float>>();
        animationUpdateTime = 0;
        Position = p;
        Unit = unit;
        Evidence.StartingMaxHealth = Math.Max(1, unit.MaxHealth);
        if (!unit.BattleActive && State.GameManager.CurrentScene == State.GameManager.TacticalMode) unit.BeginCombat();
        MultifacetedCooldown = Unit.HasTrait(Traits.Multifaceted) && Unit.IsHighestStat(Stat.Agility) ? Unit.Level + 1 : 0; // Set timer for extra attacks.
        Visible = true;
        Targetable = true;
        RestoreMP();
        ReloadSpellTraits();
    }

    public void ReloadSpellTraits()
    {
        Unit.SingleUseSpells = new List<SpellTypes>();
        Unit.MultiUseSpells = new List<SpellTypes>();
        if (Unit.HasTrait(Traits.MadScience) && State.World?.ItemRepository != null) //protection for the create strat screen
        {
            Unit.SingleUseSpells.Add(((SpellBook)State.World.ItemRepository.GetRandomBook(1, 4)).ContainedSpell);
            Unit.UpdateSpells();
        }
        if (Unit.HasTrait(Traits.PollenProjector) && State.World?.ItemRepository != null) //protection for the create strat screen
        {
            Unit.SingleUseSpells.Add(SpellList.AlraunePuff.SpellType);
            Unit.UpdateSpells();
        }
        if (Unit.HasTrait(Traits.Webber) && State.World?.ItemRepository != null) //protection for the create strat screen
        {
            Unit.SingleUseSpells.Add(SpellList.Web.SpellType);
            Unit.UpdateSpells();
        }
        if (Unit.HasTrait(Traits.GlueBomb) && State.World?.ItemRepository != null) //protection for the create strat screen
        {
            Unit.SingleUseSpells.Add(SpellList.GlueBomb.SpellType);
            Unit.UpdateSpells();
        }
        if (Unit.HasTrait(Traits.PoisonSpit) && State.World?.ItemRepository != null) //protection for the create strat screen
        {
            Unit.SingleUseSpells.Add(SpellList.ViperPoisonStatus.SpellType);
            Unit.UpdateSpells();
        }
        if (Unit.HasTrait(Traits.Petrifier) && State.World?.ItemRepository != null) //protection for the create strat screen
        {
            Unit.SingleUseSpells.Add(SpellList.Petrify.SpellType);
            Unit.UpdateSpells();
        }
        // Multi-use section
        if (Unit.HasTrait(Traits.ForceFeeder) && State.World?.ItemRepository != null) //protection for the create strat screen
        {
            Unit.MultiUseSpells.Add(SpellList.ForceFeed.SpellType);
            Unit.UpdateSpells();
        }
        if (Unit.HasTrait(Traits.DimensionalAntilock) && State.World?.ItemRepository != null) //protection for the create strat screen
        {
            Unit.SingleUseSpells.Add(SpellList.DimensionShift.SpellType);
            Unit.UpdateSpells();
        }
        if (State.World?.ItemRepository != null) //protection for the create strat screen
        {
            foreach (Traits trait in Unit.GetTraits.Where(t => (TraitList.GetTrait(t) != null) && TraitList.GetTrait(t) is IProvidesSingleSpell))
            {
                IProvidesSingleSpell callback = (IProvidesSingleSpell)TraitList.GetTrait(trait);
                foreach(SpellTypes spell in callback.GetSingleSpells(Unit))
                    Unit.SingleUseSpells.Add(spell);
            }
            foreach (Traits trait in Unit.GetTraits.Where(t => (TraitList.GetTrait(t) != null) && TraitList.GetTrait(t) is IProvidesMultiSpell))
            {
                IProvidesMultiSpell callback = (IProvidesMultiSpell)TraitList.GetTrait(trait);
                foreach (SpellTypes spell in callback.GetMultiSpells(Unit))
                    Unit.MultiUseSpells.Add(spell);
            }
            Unit.UpdateSpells();
        }
    }

    public void GenerateSpritePrefab(Transform folder)
    {
        UnitSprite = UnityEngine.Object.Instantiate(State.GameManager.UnitBase, new Vector3(Position.x, Position.y), new Quaternion(), folder).GetComponent<UnitSprite>();
        UnitSprite.UpdateHealthBar(this);
    }

    public void UpdateBestWeapons()
    {
        if (Unit.HasTrait(Traits.Feral))
        {
            BestMelee = Unit.ItemCatalog.Claws;
            BestRanged = null;
            return;
        }


        BestMelee = Unit.GetBestMelee();
        BestRanged = Unit.GetBestRanged();
        Unit.UpdateSpells();
    }

    public void SetPredMode(PreyLocation preyType)
    {
        switch (preyType)
        {
            case PreyLocation.breasts:
                Mode = DisplayMode.BreastVore;
                break;
            case PreyLocation.balls:
                Mode = DisplayMode.CockVore;
                break;
            case PreyLocation.stomach:
                Mode = DisplayMode.OralVore;
                break;
            case PreyLocation.womb:
                Mode = DisplayMode.Unbirth;
                break;
            case PreyLocation.tail:
                Mode = DisplayMode.TailVore;
                break;
            case PreyLocation.anal:
                Mode = DisplayMode.AnalVore;
                break;
            case PreyLocation.bladder:
                Mode = DisplayMode.BladderVore;
                break;
        }
        animationUpdateTime = 1.5F;
    }

    public void SetBurpMode()
    {
        Mode = DisplayMode.OralVore;
        animationUpdateTime = 1;
    }

    /// <summary>
    /// Used for idle Animations - ignored if the unit is already animating something
    /// </summary>
    /// <param name="frameNum">The Number of Frames of animation, starts at this and counts down to 0</param>
    /// <param name="time">The seconds per step</param>
    public void SetAnimationMode(int frameNum, float time)
    {
        if (Mode == DisplayMode.None)
        {
            Mode = DisplayMode.IdleAnimation;
            animationStep = frameNum;
            animationUpdateTime = time;
        }

    }

    public void SetVoreSuccessMode()
    {
        DisplayMode displayMode = DisplayMode.VoreSuccess;
        float time = 2f;
        modeQueue.Add(new KeyValuePair<int, float>(((int)displayMode), time));
    }

    public void SetVoreFailMode()
    {
        DisplayMode displayMode = DisplayMode.VoreFail;
        float time = 1f;
        modeQueue.Add(new KeyValuePair<int, float>((int)displayMode, time));
    }

    public void SetAbsorbtionMode()
    {
        if (Config.BurpOnDigest || Config.BurpFraction < .1f)
        {
            Mode = DisplayMode.Absorbing;
            animationUpdateTime = 2f;
        }
    }

    public void SetDigestionMode()
    {
        Mode = DisplayMode.Digesting;
        animationUpdateTime = 1.0f;
    }

    public void SetBirthMode()
    {
        Mode = DisplayMode.Birthing;
        animationUpdateTime = 1.0f;
    }
    public void SetSuckleMode()
    {
        Mode = DisplayMode.Suckling;
        animationUpdateTime = 1.0f;
    }

    public void SetSuckledMode()
    {
        Mode = DisplayMode.Suckled;
        animationUpdateTime = 1.0f;
    }

    public void SetRubMode()
    {
        Mode = DisplayMode.Rubbing;
        animationUpdateTime = 1.0f;
    }

    public void SetRubbedMode()
    {
        Mode = DisplayMode.Rubbed;
        animationUpdateTime = 1.0f;
    }

    public void SetPainMode()
    {
        Mode = DisplayMode.Hurt;
        animationUpdateTime = 1.0f;
    }
    public int CheckAnimationFrame()
    {
        if (Mode == DisplayMode.IdleAnimation)
            return animationStep;
        return 0;
    }

    internal bool DamagedColors => Mode == DisplayMode.Injured;

    /// <summary>
    /// Splits the chain of sprites evenly based on the ball size (Oral + Unbirth)
    /// </summary>
    /// <param name="highestSprite">the end sprite, 4 would make it 0,1,2,3,4 </param>
    /// <param name="multiplier">Controls the speed it moves through the sprites, 2 would be double, 0.5f would be half</param>
    /// <returns></returns>
    public int GetBallSize(int highestSprite, float multiplier = 1)
    {
        float v = (PredatorComponent?.BallsFullness ?? 0) * 4 * ((highestSprite + 1) / 16f) * multiplier;
        if (v > highestSprite)
            v = highestSprite;
        return (int)v;
    }

    /// <summary>
    /// Splits the chain of sprites evenly based on the tail size (Oral + Unbirth)
    /// </summary>
    /// <param name="highestSprite">the end sprite, 4 would make it 0,1,2,3,4 </param>
    /// <param name="multiplier">Controls the speed it moves through the sprites, 2 would be double, 0.5f would be half</param>
    /// <returns></returns>
    public int GetTailSize(int highestSprite, float multiplier = 1)
    {
        float v = (PredatorComponent?.TailFullness ?? 0) * 4 * ((highestSprite + 1) / 16f) * multiplier;
        if (v > highestSprite)
            v = highestSprite;
        return (int)v;
    }


    /// <summary>
    /// Splits the chain of sprites evenly based on the stomach size (Oral + Unbirth)
    /// </summary>
    /// <param name="highestSprite">the end sprite, 4 would make it 0,1,2,3,4 </param>
    /// <param name="multiplier">Controls the speed it moves through the sprites, 2 would be double, 0.5f would be half</param>
    /// <returns></returns>
    public int GetStomachSize(int highestSprite = 15, float multiplier = 1)
    {
        float v = (PredatorComponent?.VisibleFullness ?? 0) * 4 * ((highestSprite + 1) / 16f) * multiplier;
        if (v > highestSprite)
            v = highestSprite;
        return (int)v;
    }

    /// <summary>
    /// Splits the chain of sprites evenly based on the stomach size (excludes womb)
    /// </summary>
    /// <param name="highestSprite">the end sprite, 4 would make it 0,1,2,3,4 </param>
    /// <param name="multiplier">Controls the speed it moves through the sprites, 2 would be double, 0.5f would be half</param>
    /// <returns></returns>
    public int GetExclusiveStomachSize(int highestSprite = 15, float multiplier = 1)
    {
        float v = (PredatorComponent?.ExclusiveStomachFullness ?? 0) * 4 * ((highestSprite + 1) / 16f) * multiplier;
        if (v > highestSprite)
            v = highestSprite;
        return (int)v;
    }

    /// <summary>
    /// Splits the chain of sprites evenly based on the stomach size (excludes womb)
    /// </summary>
    /// <param name="highestSprite">the end sprite, 4 would make it 0,1,2,3,4 </param>
    /// <param name="multiplier">Controls the speed it moves through the sprites, 2 would be double, 0.5f would be half</param>
    /// <returns></returns>
    public int GetWombSize(int highestSprite = 15, float multiplier = 1)
    {
        float v = (PredatorComponent?.WombFullness ?? 0) * 4 * ((highestSprite + 1) / 16f) * multiplier;
        if (v > highestSprite)
            v = highestSprite;
        return (int)v;
    }

    /// <summary>
    /// Splits the chain of sprites unevenly based on the stomach size (Oral + Unbirth)
    /// This one causes the low sprites to be passed through quicker, and the high sprites to be passed through slower.
    /// </summary>
    /// <param name="highestSprite">the end sprite, 4 would make it 0,1,2,3,4 </param>
    /// <param name="multiplier">Controls the speed it moves through the sprites, 2 would be double, 0.5f would be half</param>
    /// <returns></returns>
    public int GetRootedStomachSize(int highestSprite = 15, float multiplier = 1)
    {
        highestSprite = highestSprite * highestSprite;
        float v = (PredatorComponent?.VisibleFullness ?? 0) * 4 * ((highestSprite + 1) / 16f) * multiplier;
        if (v > highestSprite)
            v = highestSprite;
        v = Mathf.Sqrt(v);
        return (int)v;
    }

    /// <summary>
    /// Splits the chain of sprites evenly based on the left breast size
    /// </summary>
    /// <param name="highestSprite">the end sprite, 4 would make it 0,1,2,3,4 </param>
    /// <param name="multiplier">Controls the speed it moves through the sprites, 2 would be double, 0.5f would be half</param>
    /// <returns></returns>
    public int GetLeftBreastSize(int highestSprite = 15, float multiplier = 1)
    {
        float v = (PredatorComponent?.LeftBreastFullness ?? 0) * 4 * ((highestSprite + 1) / 16f) * multiplier;
        if (v > highestSprite)
            v = highestSprite;
        return (int)v;
    }

    /// <summary>
    /// Splits the chain of sprites evenly based on the right breast size
    /// </summary>
    /// <param name="highestSprite">the end sprite, 4 would make it 0,1,2,3,4 </param>
    /// <param name="multiplier">Controls the speed it moves through the sprites, 2 would be double, 0.5f would be half</param>
    /// <returns></returns>
    public int GetRightBreastSize(int highestSprite = 15, float multiplier = 1)
    {
        float v = (PredatorComponent?.RightBreastFullness ?? 0) * 4 * ((highestSprite + 1) / 16f) * multiplier;
        if (v > highestSprite)
            v = highestSprite;
        return (int)v;
    }

    /// <summary>
    /// Splits the chain of sprites evenly based on the second stomach's size.
    /// </summary>
    /// <param name="highestSprite">the end sprite, 4 would make it 0,1,2,3,4 </param>
    /// <param name="multiplier">Controls the speed it moves through the sprites, 2 would be double, 0.5f would be half</param>
    /// <returns></returns>
    public int GetStomach2Size(int highestSprite = 15, float multiplier = 1)
    {
        float v = (PredatorComponent?.Stomach2ndFullness ?? 0) * 4 * ((highestSprite + 1) / 16f) * multiplier;
        if (v > highestSprite)
            v = highestSprite;
        return (int)v;
    }

    /// <summary>
    /// Splits the chain of sprites evenly based on the combined size of bellies 1 & 2. For units with DualStomach trait.
    /// </summary>
    /// <param name="highestSprite">the end sprite, 4 would make it 0,1,2,3,4 </param>
    /// <param name="multiplier">Controls the speed it moves through the sprites, 2 would be double, 0.5f would be half</param>
    /// <returns></returns>
    public int GetCombinedStomachSize(int highestSprite = 15, float multiplier = 1)
    {
        float v = (PredatorComponent?.CombinedStomachFullness ?? 0) * 4 * ((highestSprite + 1) / 16f) * multiplier;
        if (v > highestSprite)
            v = highestSprite;
        return (int)v;
    }

    /// <summary>
    /// An alternate version of GetStomachSize used for combining all vore types into a single value
    /// </summary>
    /// <param name="highestSprite">the end sprite, 4 would make it 0,1,2,3,4</param>
    /// <param name="multiplier">Controls the speed it moves through the sprites, 2 would be double, 0.5f would be half</param>
    /// <returns></returns>
    public int GetUniversalSize(int highestSprite = 15, float multiplier = 1)
    {
        float v = (PredatorComponent?.Fullness ?? 0) * 4 * multiplier;
        if (v > highestSprite)
            v = highestSprite;
        return (int)v;
    }

    public bool HasBelly => ((Config.LamiaUseTailAsSecondBelly || Unit.HasTrait(Traits.DualStomach) == false) && (PredatorComponent?.VisibleFullness ?? 0) > 0) || (!Config.LamiaUseTailAsSecondBelly && (PredatorComponent?.CombinedStomachFullness ?? 0) > 0);

    public bool HasPreyInBreasts => (PredatorComponent?.BreastFullness > 0 || PredatorComponent?.LeftBreastFullness > 0 || PredatorComponent?.RightBreastFullness > 0);
    public bool HasBodyWeight => Unit.Race != Race.Lizards;

    public int GetBodyWeight() => (Config.WeightGain || Unit.BodySizeManuallyChanged) ? Unit.BodySize : Mathf.Min(Config.DefaultStartingWeight, Races.GetRace(Unit).BodySizes);
    internal Weapon WeaponForDisplay
    {
        get
        {
            if (Unit == null || !Unit.CompanyLoadout)
                return BestRanged ?? (BestMelee != null && BestMelee != Unit?.ItemCatalog?.Claws ? BestMelee : null);

            Weapon melee = BestMelee;
            if (melee != null && Unit.ItemCatalog != null && melee == Unit.ItemCatalog.Claws)
                melee = null;
            Weapon ranged = BestRanged;
            return Unit.CompanyLastUsedWeaponRanged ? (ranged ?? melee) : (melee ?? ranged);
        }
    }

    public int GetWeaponSprite()
    {
        if (Unit != null && Unit.CompanyLoadout)
        {
            Weapon selected = WeaponForDisplay;
            if (selected == null || selected.Graphic < 0)
                return 0;
            return selected.Graphic + (Mode == DisplayMode.Attacking ? 1 : 0);
        }
        if (BestRanged != null)
        {
            if (Mode != DisplayMode.Attacking)
                return BestRanged.Graphic;
            else
                return BestRanged.Graphic + 1;
        }
        else if (BestMelee != null && BestMelee != Unit.ItemCatalog.Claws)
        {
            if (Mode != DisplayMode.Attacking)
                return BestMelee.Graphic;
            else
                return BestMelee.Graphic + 1;
        }
        return 0;
    }

    public int GetSimpleBodySprite()
    {
        if (Mode == DisplayMode.Attacking)
            return 1;
        if (Mode == DisplayMode.OralVore || Mode == DisplayMode.BreastVore || Mode == DisplayMode.CockVore || Mode == DisplayMode.Unbirth || Mode == DisplayMode.AnalVore || Mode == DisplayMode.BladderVore)
            return 2;
        return 0;
    }


    public bool IsAttacking => Mode == DisplayMode.Attacking;
    public bool IsSpecialAttacking => Mode == DisplayMode.SpecialAttack;
    public bool IsRangeAttacking => Mode == DisplayMode.RangeAttacking;
    public bool IsMeleeAttacking => Mode == DisplayMode.MeleeAttacking;

    /// <summary>
    /// This one Covers all forms of consuming
    /// </summary>
    public bool IsEating => IsOralVoring || IsCockVoring || IsBreastVoring || IsUnbirthing || IsTailVoring || IsAnalVoring || IsBladderVoring;
    public bool IsOralVoring => Mode == DisplayMode.OralVore;
    public bool IsOralVoringHalfOver => Mode == DisplayMode.OralVore && animationUpdateTime < .75f;

    public bool IsCockVoring => Mode == DisplayMode.CockVore || IsBladderVoring;
    public bool IsBreastVoring => Mode == DisplayMode.BreastVore;
    public bool IsUnbirthing => Mode == DisplayMode.Unbirth || IsBladderVoring;
    public bool IsTailVoring => Mode == DisplayMode.TailVore;
    public bool IsAnalVoring => Mode == DisplayMode.AnalVore;
    public bool IsBladderVoring => Mode == DisplayMode.BladderVore;
    public bool IsPouncingFrog => Mode == DisplayMode.FrogPouncing;
    public bool HasJustVored => Mode == DisplayMode.VoreSuccess;
    public bool HasJustFailedToVore => Mode == DisplayMode.VoreFail;
    public bool IsDigesting => Mode == DisplayMode.Digesting;
    public bool IsAbsorbing => Mode == DisplayMode.Absorbing;
    public bool IsBirthing => Mode == DisplayMode.Birthing;
    public bool IsSuckling => Mode == DisplayMode.Suckling;
    public bool IsBeingSuckled => Mode == DisplayMode.Suckled;
    public bool IsRubbing => Mode == DisplayMode.Rubbing;
    public bool IsBeingRubbed => Mode == DisplayMode.Rubbed;
    public bool IsBeingHurt => Mode == DisplayMode.Hurt;
    [OdinSerialize]
    public List<int> sidesAttackedThisBattle { get; set; }

    public float GetSpecialChance(SpecialAction action)
    {
        if (action == SpecialAction.CockVore)
            return .8f;
        else
            return 1f;
    }

    public float GetPureStatClashChance(int attackStat, int defenseStat, float shift) // generic AF
    {
        return (float)attackStat / (attackStat + (defenseStat * (1 + shift)));
    }

    internal float GetMagicChance(Actor_Unit attacker, Spell currentSpell, float modifier = 0, Stat stat = Stat.Intelligence)
    {
        if (Surrendered && currentSpell != null && currentSpell.IsConsumption) return 1;
        if (TacticalUtilities.SneakAttackCheck(attacker.Unit, Unit)) // sneakAttack
        {
            modifier -= 0.3f;
        }
        int attackStat = attacker.Unit.GetStat(stat);
        int defenseStat = Unit.GetStat(Stat.Intelligence);
        if (currentSpell?.Resistable ?? false) //Skips if no spell is specified since that only involves AI calcs
        {
            defenseStat = (int)(defenseStat * currentSpell.ResistanceMult);
        }

        float shift = Unit.TraitBoosts.Incoming.MagicShift + attacker.Unit.TraitBoosts.Outgoing.MagicShift + modifier + TagConditionChecker.ApplyTagEffect(Unit, attacker.Unit, UnitTagModifierEffect.MagicShift);
        return Mathf.Clamp01((float)attackStat / (attackStat + (defenseStat * (1 + shift))) - DazzlePenalty(attacker) +
            TieredTraits.Amount(attacker.Unit, TierEffect.MagicAccuracy) - CompanyInjuries.AccuracyPenalty(attacker.Unit) +
            (TacticalUtilities.TreatAsHostile(attacker, this) ? TieredTraits.Amount(Unit, TierEffect.AttackAccuracyReceived) -
                ((currentSpell?.Resistable ?? true) ? CompanyEquipment.Evasion(Unit) : 0) : 0));
    }

    //public float GetMeleeChance(Actor_Unit attacker, bool includeSecondaries = false)
    //{
    //    float attackerScore = 2 * attacker.Unit.GetStat(Stat.Strength) + attacker.Unit.GetStat(Stat.Agility);
    //    float defenderScore = 2 * Unit.GetStat(Stat.Agility) * (15 / (BodySize() + 5) / (1 + (PredatorComponent?.Fullness ?? 0) * Unit.TraitBoosts.DodgeLossFromWeightMultiplier * 5 / Unit.GetStat(Stat.Strength)));

    //    if (Unit.GetStatusEffect(StatusEffectType.Clumsiness) != null)
    //    {
    //        defenderScore *= Unit.GetStatusEffect(StatusEffectType.Clumsiness).Strength;
    //    }

    //    if (attacker.Intimidated)
    //        attackerScore *= .8f;

    //    foreach (IPhysicalDefenseOdds trait in Unit.PhysicalDefenseOdds)
    //    {
    //        trait.PhysicalDefense(this, ref defenderScore);
    //    }

    //    float odds = attackerScore / (attackerScore + defenderScore);

    //    odds *= Unit.TraitBoosts.FlatHitReduction;

    //    if (odds < 20)
    //        odds = 20;

    //    if (Config.BoostedAccuracy)
    //        odds = 100 - ((100 - odds) * .5f);

    //    if (includeSecondaries)
    //    {
    //        if (Unit.HasTrait(Traits.Dazzle))
    //        {
    //            odds *= 1 - WillCheckOdds(attacker, this);
    //        }
    //    }
    //    return odds / 100;
    //}


    float DazzlePenalty(Actor_Unit attacker)
    {
        return Unit.HasTrait(Traits.Dazzle) && TacticalUtilities.TreatAsHostile(attacker, this)
            ? Mathf.Min(.20f, Unit.GetStat(Stat.Intelligence) * .002f) : 0f;
    }

    public float GetAttackChance(Actor_Unit attacker, bool ranged, bool includeSecondaries = false, float mod = 0, int distanceOverride = -1)
    {
        int attack;
        if (ranged)
        {
            attack = attacker.Unit.GetStat(Stat.Agility);
            attacker.UpdateBestWeapons();
        }
        else
        {
            attack = attacker.Unit.GetStat(Stat.Strength);
            attacker.UpdateBestWeapons();
        }

        int range = distanceOverride >= 0 ? distanceOverride : attacker.Position.GetNumberOfMovesDistance(Position);
        if (Surrendered || Unit.GetStatusEffect(StatusEffectType.Petrify) != null || Unit.GetStatusEffect(StatusEffectType.Sleeping) != null || Unit.GetStatusEffect(StatusEffectType.Frozen) != null)
            return 1f;
        const int maximumBoost = 75;
        const int minimumOdds = 25;
        float defenderBonusShift = 0;
        int defense = Unit.GetStat(Stat.Agility);

        if (Unit.GetStatusEffect(StatusEffectType.Clumsiness) != null)
        {
            defenderBonusShift -= Unit.GetStatusEffect(StatusEffectType.Clumsiness).Strength;
        }

        attack += 15;
        defense += 15;

        if (attack <= 0)
            attack = 1;
        if (defense <= 0)
            defense = 1;

        if (range > 2)
            defenderBonusShift += .05f * (range - 2);

        if (ranged)
            defenderBonusShift += Unit.TraitBoosts.Incoming.RangedShift + attacker.Unit.TraitBoosts.Outgoing.RangedShift + mod + TagConditionChecker.ApplyTagEffect(Unit, attacker.Unit, UnitTagModifierEffect.RangedShift);
        else
            defenderBonusShift += Unit.TraitBoosts.Incoming.MeleeShift + attacker.Unit.TraitBoosts.Outgoing.MeleeShift + mod + TagConditionChecker.ApplyTagEffect(Unit, attacker.Unit, UnitTagModifierEffect.MeleeShift);

        if (Unit.HasTrait(Traits.AllOutFirstStrike))
        {
            if (HasAttackedThisCombat)
                defenderBonusShift -= 2;
            else
                defenderBonusShift += 4;
        }
        if (TacticalUtilities.SneakAttackCheck(attacker.Unit, Unit)) // sneakAttack
        {
            defenderBonusShift += 2;
        }

        defenderBonusShift -= Unit.TraitBoosts.DodgeLossFromWeightMultiplier * 0.1f * PredatorComponent?.Fullness ?? 0;


        if (BestRanged == null && Movement > 0 && ranged)
            defenderBonusShift += .2f;

        foreach (IPhysicalDefenseOdds trait in Unit.PhysicalDefenseOdds)
        {
            trait.PhysicalDefense(this, ref defenderBonusShift);
        }

        if (attacker.Intimidated)
            defenderBonusShift += .2f;

        if (ranged)
        {
            if (attacker.BestRanged?.AccuracyModifier > 1)
                attack = (int)(attack * attacker.BestRanged.AccuracyModifier);
            if (attacker.BestRanged?.Magic == true)
                attack = attack + attacker.Unit.GetStat(Stat.Intelligence);
        }
        else
        {
            if (attacker.BestMelee?.AccuracyModifier > 1)
                attack = (int)(attack * attacker.BestMelee.AccuracyModifier);
            if (attacker.BestMelee?.Magic == true)
                attack = attack + attacker.Unit.GetStat(Stat.Intelligence);
        }

        float ratio = (float)attack / defense;
        float adjustment;
        if (ratio > 1)
            adjustment = 1 - ratio;
        else
            adjustment = (1 / ratio) - 1;

        float oddsReductionFactor = (3 * adjustment) + defenderBonusShift; //Lower factor is increased odds
        float odds = minimumOdds + (maximumBoost / (1 + Mathf.Pow(2, oddsReductionFactor)));

        if (attacker.Unit.HasTrait(Traits.Farsighted))
        {
            if (range <= 5)
            {
                // Math wouldn't work here, no clue why.
                switch (range)
                {
                    case 5:
                        odds *= 0.8f;
                        break;
                    case 4:
                        odds *= 0.6f;
                        break;
                    case 3:
                        odds *= 0.4f;
                        break;
                    case 2:
                        odds *= 0.2f;
                        break;
                    default:
                        break;
                }
            };
        }

        odds *= Unit.TraitBoosts.FlatHitReduction;

        if (Config.SizeAccuracyMod > 0 && Config.SizeAccuracyInterval > 0)
        {
            float sizeDiff = Math.Abs(BodySize() - attacker.BodySize());
            if (sizeDiff > Config.SizeAccuracyLowerBound)
            {
                float oddMod = ((sizeDiff - Config.SizeAccuracyLowerBound) / Config.SizeAccuracyInterval) * Config.SizeAccuracyMod;
                oddMod += 1;
                if (oddMod > Config.SizeAccuracyCap && Config.SizeAccuracyCap > 0) 
                {
                    oddMod = Config.SizeAccuracyCap;
                }
                // If we are larger than the attacker, increase accuracy of attack. Otherwise, reduce it
                if (BodySize() > attacker.BodySize())
                {
                    odds *= oddMod;
                }
                else
                {
                    if (Config.SizeAccuracyInverse)
                    {
                        odds /= oddMod;
                    }
                }
              
            }
        }

        if (Unit.HasTrait(Traits.Multifaceted) && Unit.IsHighestStat(Stat.Agility) && Unit.HealthPct <= 0.5f)
        {
            odds *= 0.9f;
        }

        if (Config.BoostedAccuracy)
            odds = 100 - ((100 - odds) * .5f);

        if (includeSecondaries)
        {

        }
        if (odds > 100)
        {
            odds = 100;
        }
        if (odds < 0)
        {
            odds = 0;
        }
        float accuracy = odds / 100 + (Unit.GetStatusEffect(StatusEffectType.Marked)?.Strength ?? 0) * .01f;
        accuracy += TieredTraits.Amount(attacker.Unit, ranged ? TierEffect.RangedAccuracy : TierEffect.MeleeAccuracy) - CompanyInjuries.AccuracyPenalty(attacker.Unit);
        if (TacticalUtilities.TreatAsHostile(attacker, this)) accuracy += TieredTraits.Amount(Unit, TierEffect.AttackAccuracyReceived) - CompanyEquipment.Evasion(Unit);
        return Mathf.Clamp01(accuracy - DazzlePenalty(attacker));
    }

    public int WeaponDamageAgainstTarget(Actor_Unit target, bool ranged, float multiplier = 1, bool forceBite = false, bool opportunity = false)
    {
        int damage;
        if (ranged)
        {
            float damageScalar = Unit.TraitBoosts.Outgoing.RangedDamage * target.Unit.TraitBoosts.Incoming.RangedDamage;
			damageScalar *= multiplier;
			damageScalar *= TagConditionChecker.ApplyTagEffect(Unit, target.Unit, UnitTagModifierEffect.RangedDamageMult);
            if (Unit.HasTrait(Traits.AllOutFirstStrike) && HasAttackedThisCombat == false)
                damageScalar *= 5;
            if ((target.Unit.GetStatusEffect(StatusEffectType.Petrify) != null) || (target.Unit.GetStatusEffect(StatusEffectType.Frozen) != null))
                damageScalar /= 2;
            if (Unit.HasTrait(Traits.Competitive) && Unit.Race == target.Unit.Race)
            {
                damageScalar *= 1.15f;
            }
            if (target.Unit.GetStatusEffect(StatusEffectType.Fractured) == null && target.Unit.HasTrait(Traits.Crystaline))
            {
                damageScalar *= 0.75f;
            }
            if (target.Unit.GetStatusEffect(StatusEffectType.Fractured) != null)
            {
                damageScalar *= 1.50f;
            }
            if (Unit.GetStatusEffect(StatusEffectType.Valor) != null)
            {
                damageScalar *= 1.25f;
            }
            if (Unit.HasTrait(Traits.WeaponChanneler) && Unit.Mana >= 6)
            {
                damageScalar *= 1.2f;
            }
            if (Unit.GetStatusEffect(StatusEffectType.Bloodrite) != null)
            {
                damageScalar *= 1.1f;
            }
            if (target.Unit.GetStatusEffect(StatusEffectType.Shielded) != null)
            {
                damageScalar *= 1 - target.Unit.GetStatusEffect(StatusEffectType.Shielded).Strength;
            }
            if (target.Unit.GetStatusEffect(StatusEffectType.DivineShield) != null)
            {
                damageScalar *= 1 - target.Unit.GetStatusEffect(StatusEffectType.DivineShield).Strength;
            }
            if (Unit.HasTrait(Traits.SenseWeakness))
            {
                damageScalar *= 1.4f - (target.Unit.HealthPct * .4f) + (0.1f * target.Unit.GetNegativeStatusEffects());
            }
            int statBoost = Unit.GetStat(Stat.Agility) + (Unit.HasTrait(Traits.SpellBlade) ? Unit.GetStat(Stat.Intelligence) / 2 : 0);
            if (Unit.HasTrait(Traits.BoundWeapon))
            {statBoost = Unit.GetStat(Stat.Intelligence);}
            if (Unit.HasTrait(Traits.Finesse))
            { statBoost = (int)(Unit.GetStat(Stat.Strength) * .8f + Unit.GetStat(Stat.Agility) * .3f) + (Unit.HasTrait(Traits.SpellBlade) ? Unit.GetStat(Stat.Intelligence) / 2 : 0); }
            damage = (int)(damageScalar * (BestRanged?.Damage ?? 2) * (60 + statBoost) / 60);

            if (target.Unit.GetStatusEffect(StatusEffectType.Staggering) != null)
            {
                damage += (int)(damage * 0.2f);
            }

        }
        else
        {

            float damageScalar = Unit.TraitBoosts.Outgoing.MeleeDamage * target.Unit.TraitBoosts.Incoming.MeleeDamage;
            damageScalar *= TagConditionChecker.ApplyTagEffect(Unit, target.Unit, UnitTagModifierEffect.MeleeDamageMult);

            if (Unit.HasTrait(Traits.AllOutFirstStrike) && HasAttackedThisCombat == false)
                damageScalar *= 5;
            damageScalar *= multiplier;

            if ((target.Unit.GetStatusEffect(StatusEffectType.Petrify) != null) || (target.Unit.GetStatusEffect(StatusEffectType.Frozen) != null))
                damageScalar /= 2;

            if (Unit.HasTrait(Traits.Competitive) && Unit.Race == target.Unit.Race)
            {
                damageScalar *= 1.15f;
            }
            if (target.Unit.GetStatusEffect(StatusEffectType.Fractured) == null && target.Unit.HasTrait(Traits.Crystaline))
            {
                damageScalar *= 0.75f;
            }
            if (target.Unit.GetStatusEffect(StatusEffectType.Fractured) != null)
            {
                damageScalar *= 1.50f;
            }
            if (target.Unit.GetStatusEffect(StatusEffectType.Errosion) != null)
                damageScalar += damageScalar * (target.Unit.GetStatusEffect(StatusEffectType.Errosion).Strength / 5);

            if (Unit.GetStatusEffect(StatusEffectType.Valor) != null)
            {
                damageScalar *= 1.25f;
            }
            if (Unit.HasTrait(Traits.WeaponChanneler) && Unit.Mana >= 6)
            {
                damageScalar *= 1.2f;
            }
            if (Unit.GetStatusEffect(StatusEffectType.Bloodrite) != null)
            {
                damageScalar *= 2.5f;
            }
            if (target.Unit.GetStatusEffect(StatusEffectType.Shielded) != null)
            {
                damageScalar *= 1 - target.Unit.GetStatusEffect(StatusEffectType.Shielded).Strength;
            }
            if (target.Unit.GetStatusEffect(StatusEffectType.DivineShield) != null)
            {
                damageScalar *= 1 - target.Unit.GetStatusEffect(StatusEffectType.DivineShield).Strength;
            }
            if (!opportunity && Unit.HasTrait(Traits.ForcefulBlow))
                TacticalUtilities.CheckKnockBack(this, target, ref damageScalar);
            if (Unit.HasTrait(Traits.SenseWeakness))
            {
                damageScalar *= 1.4f - (target.Unit.HealthPct * .4f) + (0.1f * target.Unit.GetNegativeStatusEffects());
            }
            if (Unit.HasTrait(Traits.VenomShock) && target.Unit.GetStatusEffect(StatusEffectType.Poisoned) != null)
            {
                damageScalar *= 1.5f;
            }
            if (forceBite)
            {
                damage = (int)(damageScalar * State.World.ItemRepository.Bite.Damage * (60 + Unit.GetStat(Stat.Strength)) / 60);
            }
            else
            {
                if (Unit.HasTrait(Traits.Feral) && Unit.GetBestMelee() == State.World.ItemRepository.Claws)
                    damageScalar *= 3f;
                int statBoost = Unit.GetStat(Stat.Strength) + (Unit.HasTrait(Traits.SpellBlade) ? Unit.GetStat(Stat.Intelligence) / 2 : 0);
                if (Unit.HasTrait(Traits.BoundWeapon))
                {statBoost = Unit.GetStat(Stat.Intelligence);}
                if (Unit.HasTrait(Traits.Finesse))
                { statBoost = (int)(Unit.GetStat(Stat.Strength) * .8f + Unit.GetStat(Stat.Agility) * .3f) + (Unit.HasTrait(Traits.SpellBlade) ? Unit.GetStat(Stat.Intelligence) / 2 : 0); }
                damage = (int)(damageScalar * BestMelee.Damage * (60 + statBoost) / 60);
            }




            if (target.Unit.GetStatusEffect(StatusEffectType.Staggering) != null)
            {
                damage += (int)(damage * 0.2f);
            }
        }

        if (Unit.HasTrait(Traits.Multifaceted) && Unit.IsHighestStat(Stat.Strength) && MultifacetedCooldown == 0)
        {
            damage += (int)Math.Round(target.Unit.MaxHealth * 0.05f);
        }

        if (Unit.HasTrait(Traits.SwiftStrike))
        {
            int current_weapon_class = GetWeaponSprite();
            if (current_weapon_class == 0 || current_weapon_class == 1 || current_weapon_class == 4 || current_weapon_class == 5)
                current_weapon_class = 0;
            int stat_diff = Unit.GetStat(Stat.Agility) - target.Unit.GetStat(Stat.Agility);
            if (stat_diff >= 25)
            {
                float ss_bonus = 0.25f * (current_weapon_class == 0 ? 3 : 1);
                damage += (int)(damage * ss_bonus);
            }
            else if (stat_diff > 0)
            {
                damage += (int)(damage * (stat_diff/ (current_weapon_class == 0 ? 33 : 100)));
            }
        }

        if (Unit.HasTrait(Traits.Duelist))
        {
            damage *= 2;

            int adj = TacticalUtilities.UnitsWithinTiles(Position, 1).Where(u => u.Unit.IsEnemyOfSide(Unit.Side)).Count();
            damage /= adj != 0 ? adj : 1;
        }

        if (Unit.HasTrait(Traits.Fervor))
        {
            damage = (int)Math.Ceiling(damage * .25f);

            int adj = TacticalUtilities.UnitsWithinTiles(Position, 1).Where(u => u.Unit.IsEnemyOfSide(Unit.Side)).Count();
            damage *= adj != 0 ? adj : 1;
        }

        if (TacticalUtilities.SneakAttackCheck(Unit, target.Unit)) // sneakAttack
        {
            damage *= 3;
        }

        if (Config.SizeDamageMod > 0 && Config.SizeDamageInterval > 0)
        {
            float sizeDiff = Math.Abs(BodySize() - target.BodySize());
            if (sizeDiff > Config.SizeDamageLowerBound)
            {
                float damMod = 0;
                int bonusDamage = 0;
                if (Unit.HasTrait(Traits.Crusher))
                {
                    damMod = (sizeDiff / Config.SizeDamageInterval) * Config.SizeDamageMod;
                    damMod *= 1.5f;
                }
                else
                {
                    damMod = ((sizeDiff - Config.SizeDamageLowerBound) / Config.SizeDamageInterval) * Config.SizeDamageMod;
                }
                if (damMod > Config.SizeDamageCap && Config.SizeDamageCap > 0)
                {
                    damMod = Config.SizeDamageCap;
                }
                // If we are larger than the attacker, increase damage of attack. Otherwise, reduce it
                if (BodySize() > target.BodySize())
                {
                    if (target.Unit.HasTrait(Traits.GiantSlayer))
                    {
                        damMod *= 0.25f;
                    }
                    bonusDamage = (int)Math.Round(damage * damMod);
                }
                else
                {
                    if (Config.SizeDamageInverse && !Unit.HasTrait(Traits.GiantSlayer))
                    {
                        bonusDamage = (int)Math.Round(damage * damMod) * -1;
                    }
                }
                damage += bonusDamage;
            }
        }
        else if (Unit.HasTrait(Traits.GiantSlayer) && BodySize() < target.BodySize()) // Setting off GiantSlayer Version
        {
            float sizeDiff = Math.Abs(BodySize() - target.BodySize());
            damage = (int)Math.Round(damage * (1 + (.01f * Math.Min(sizeDiff, 25))));
        }
        else if (Unit.HasTrait(Traits.Crusher) && BodySize() > target.BodySize()) // Setting off Crusher Version
        {
            float sizeDiff = Math.Abs(BodySize() - target.BodySize());
            damage = (int)Math.Round(damage * (1 + (.01f * Math.Min(sizeDiff, 25))));
        }

        if (target.Unit.HasTrait(Traits.Multifaceted) && target.Unit.IsHighestStat(Stat.Strength))
        {
            int tenP = (int)Math.Round(target.Unit.Health * 0.1f);
            if (damage > tenP) // Check if damage is over 10% current
            {
                int mitigation = damage - tenP;
                mitigation = (int)Math.Floor(mitigation * 0.5f);
                damage -= mitigation;
            }
        }


        damage += (int)Math.Round(TieredTraits.Amount(Unit, ranged ? TierEffect.RangedDamage : TierEffect.MeleeDamage) * multiplier);
        damage += (int)TieredTraits.Amount(target.Unit, TierEffect.PhysicalDamageReceived);
        damage -= CompanyInjuries.DamagePenalty(Unit);
        if (damage < 1)
            damage = 1;
        return damage;
    }

    ///// <summary>
    ///// Tells if the unit is able to move at all.   Attackers should use TacticalUtilities.FreeSpaceAroundTaget
    ///// </summary>
    //public bool Surrounded()
    //{
    //    Vec2i p = new Vec2i(0, 0);
    //    for (int i = 0; i < 8; i++)
    //    {
    //        p = GetPos(i);
    //        if (TacticalUtilities.OpenTile(p.x, p.y, this))
    //        {
    //            return false;

    //        }
    //    }
    //    return true;
    //}


    public Vec2i PounceAt(Actor_Unit target)
    {
        if (Movement < 2 || (!target.UnitSprite.isActiveAndEnabled && State.World.IsNight))
            return null;
        if (Position.GetNumberOfMovesDistance(target.Position) < 2 || Position.GetNumberOfMovesDistance(target.Position) > 4)
            return null;
        for (int i = 0; i < 8; i++)
        {
            Vec2i p = target.GetPos(i);
            if (TacticalUtilities.OpenTile(p.x, p.y, this))
            {
                return p;

            }
        }
        return null;
    }

    public bool MeleePounce(Actor_Unit target)
    {
        if (Movement < 2 || Unit.HasTrait(Traits.Pounce) == false)
            return false;
        var landingZone = PounceAt(target);
        if (landingZone != null)
        {
            State.GameManager.TacticalMode.Translator.SetTranslator(UnitSprite.transform, Position, landingZone, 0.5f, State.GameManager.TacticalMode.IsPlayerTurn);
            State.GameManager.TacticalMode.DirtyPack = true;
            SetPos(landingZone);
            float multiplier = 1;
            if (Unit.HasTrait(Traits.HeavyPounce))
            {
                multiplier = Mathf.Min(1 + ((PredatorComponent?.Fullness ?? 0) / 4), 2);
                Unit.ApplyStatusEffect(StatusEffectType.Clumsiness, Mathf.Min((PredatorComponent?.Fullness ?? 0) / 10, .8f), 1);
            }

            Attack(target, false, damageMultiplier: multiplier);
            return true;
        }
        return false;
    }

    public bool VorePounce(Actor_Unit target, SpecialAction voreType = SpecialAction.None, bool AIAutoPick = false)
    {
        if (Movement < 2 || Unit.HasTrait(Traits.Pounce) == false)
            return false;
        if (TacticalUtilities.AppropriateVoreTarget(this, target) == false)
            return false;
        if (!PredatorComponent.HasCapacityFor(target))
            return false;
        var pounceLandingZone = PounceAt(target);
        if (pounceLandingZone != null)
        {
            Vec2i originalLoc = Position;
            SetPos(pounceLandingZone);
            if (AIAutoPick)
            {
                PredatorComponent.UsePreferredVore(target);
            }
            else
            {
                switch (voreType)
                {
                    case SpecialAction.BreastVore:
                        PredatorComponent.BreastVore(target);
                        break;
                    case SpecialAction.CockVore:
                        PredatorComponent.CockVore(target);
                        break;
                    case SpecialAction.Unbirth:
                        PredatorComponent.Unbirth(target);
                        break;
                    case SpecialAction.TailVore:
                        PredatorComponent.TailVore(target);
                        break;
                    case SpecialAction.AnalVore:
                        PredatorComponent.AnalVore(target);
                        break;
                    case SpecialAction.BladderVore:
                        PredatorComponent.BladderVore(target);
                        break;
                    default:
                        PredatorComponent.Devour(target);
                        break;
                }
            }

            if (target.Visible == false)
            { //Done this way to avoid doubling up references                        
                pounceLandingZone.x = target.Position.x;
                pounceLandingZone.y = target.Position.y;
                SetPos(pounceLandingZone);
            }
            State.GameManager.TacticalMode.Translator.SetTranslator(UnitSprite.transform, originalLoc, pounceLandingZone, 0.5f, State.GameManager.TacticalMode.IsPlayerTurn);
            State.GameManager.TacticalMode.DirtyPack = true;
            return true;
        }
        return false;
    }

    public bool ShunGokuSatsu(Actor_Unit target)
    {
        if (Movement < 1 || Unit.HasTrait(Traits.ShunGokuSatsu) == false)
            return false;
        List<AbilityTargets> targetTypes = new List<AbilityTargets>();
        targetTypes.Add(AbilityTargets.Enemy);
        if (!TacticalUtilities.MeetsQualifier(targetTypes, this, target))
            return false;
        if (target.Position.GetNumberOfMovesDistance(Position) > 1)
            return false;

        int damage = 2 * WeaponDamageAgainstTarget(target, false);
        if (damage >= target.Unit.Health)
            damage = target.Unit.Health - 1;
        if (target.Defend(this, ref damage, false, out float chance))
        {
            if (State.GameManager.TacticalMode.TacticalSoundBlocked() == false)
            {
                var obj = UnityEngine.Object.Instantiate(State.GameManager.TacticalEffectPrefabList.ShunGokuSatsu);
                obj.transform.SetPositionAndRotation(new Vector3(target.Position.x, target.Position.y), new Quaternion());
                MiscUtilities.DelayedInvoke(() => State.GameManager.SoundManager.PlayArrowHit(null), .06f);
                MiscUtilities.DelayedInvoke(() => State.GameManager.SoundManager.PlayMeleeHit(null), .12f);
                MiscUtilities.DelayedInvoke(() => State.GameManager.SoundManager.PlayArmorHit(null), .18f);
                MiscUtilities.DelayedInvoke(() => State.GameManager.SoundManager.PlayMeleeHit(null), .24f);
                State.GameManager.TacticalMode.CreateSwipeHitEffect(target.Position, 0);
                State.GameManager.TacticalMode.CreateSwipeHitEffect(target.Position, 90);
                State.GameManager.TacticalMode.CreateSwipeHitEffect(target.Position, 180);
                State.GameManager.TacticalMode.CreateSwipeHitEffect(target.Position, 270);
            }
            Unit.GiveScaledExp(2 * target.Unit.ExpMultiplier, Unit.Level - target.Unit.Level);
        }

        PredatorComponent?.Devour(target, .3f);
        TurnUsedShun = State.GameManager.TacticalMode.currentTurn;
        Movement = 0;

        return true;
    }

    public bool StunningStrike(Actor_Unit target)
    {
        if (Movement < 1 || Unit.HasTrait(Traits.StunningStrike) == false)
            return false;
        if (!Unit.SpendMana(10))
        {
            return false;
        }
        List<AbilityTargets> targetTypes = new List<AbilityTargets>();
        targetTypes.Add(AbilityTargets.Enemy);
        if (!TacticalUtilities.MeetsQualifier(targetTypes, this, target))
            return false;
        if (target.Position.GetNumberOfMovesDistance(Position) > 1)
            return false;
        Mode = DisplayMode.SpecialAttack;
        animationUpdateTime = 1;
        float movementBonus = Movement / MaxMovement();
        int damage = WeaponDamageAgainstTarget(target, false) + (int)Math.Round(WeaponDamageAgainstTarget(target, false) * movementBonus);
        if (damage >= target.Unit.Health)
            damage = target.Unit.Health - 1;
        if (target.Defend(this, ref damage, false, out float chance))
        {
            if (State.GameManager.TacticalMode.TacticalSoundBlocked() == false)
            {
                MiscUtilities.DelayedInvoke(() => State.GameManager.SoundManager.PlayMeleeHit(null), .12f);
                State.GameManager.TacticalMode.CreateSwipeHitEffect(target.Position, 0);
            };
            int stunDur = (int)Math.Floor((double)(Unit.GetStat(Stat.Intelligence) / 18));
            if (movementBonus > State.Rand.NextDouble())
                stunDur++;
            target.Unit.ApplyStatusEffect(StatusEffectType.Stunned, 1, stunDur);
            Unit.GiveScaledExp(2 * target.Unit.ExpMultiplier, Unit.Level - target.Unit.Level);
        }
        if (target.Unit.Health <= 1)
        {
            target.Surrendered = true;
        }
        TurnUsedStunStrike = State.GameManager.TacticalMode.currentTurn;
        Movement = 0;

        return true;
    }
    
    // Hey, wanna see the hackiest code I'll ever cobble together?
    public bool Regurgitate(Actor_Unit a, Vec2i l)
    {
        Prey regurged = a.PredatorComponent.FreeRandomPreyToSquare(l);
        if (regurged != null)
        {
            return true;
        }
        else
            return false;
    }

    public bool TailStrike(Actor_Unit target)
    {
        if (Movement < 1 || Unit.HasTrait(Traits.TailStrike) == false)
            return false;
        List<AbilityTargets> targetTypes = new List<AbilityTargets>();
        targetTypes.Add(AbilityTargets.Enemy);
        if (!TacticalUtilities.MeetsQualifier(targetTypes, this, target))
            return false;
        if (target.Position.GetNumberOfMovesDistance(Position) != 1)
            return false;

        Attack(target, false, damageMultiplier: .66f);
        Actor_Unit tempTarget = TacticalUtilities.GetActorAt(target.Position + new Vec2(1, 0));
        TestAttack(tempTarget);
        tempTarget = TacticalUtilities.GetActorAt(target.Position + new Vec2(0, 1));
        TestAttack(tempTarget);
        tempTarget = TacticalUtilities.GetActorAt(target.Position + new Vec2(-1, 0));
        TestAttack(tempTarget);
        tempTarget = TacticalUtilities.GetActorAt(target.Position + new Vec2(0, -1));
        TestAttack(tempTarget);
        Attack(target, false, damageMultiplier: .66f);

        Movement = 0;

        return true;

        void TestAttack(Actor_Unit sideTarget)
        {
            if (sideTarget != null && sideTarget.Position.GetNumberOfMovesDistance(Position) == 1)
            {
                Movement = 1;
                Attack(sideTarget, false, damageMultiplier: .66f);
            }
        }
    }
    public bool AllInVore(Actor_Unit target, SpecialAction voreType = SpecialAction.None, bool AIAutoPick = false)
    {
        if (Movement < 1 || Unit.HasTrait(Traits.AllIn) == false)
            return false;
        if (TacticalUtilities.AppropriateVoreTarget(this, target) == false)
            return false;
        if (!PredatorComponent.HasCapacityFor(target))
            return false;
        if (target.Position.GetNumberOfMovesDistance(Position) > 1)
            return false;

        bool succeded_attempt;
        IsAllIn = true;
        if (AIAutoPick)
        {
            succeded_attempt = PredatorComponent.UsePreferredVore(target);
        }
        else
        {
            switch (voreType)
            {
                case SpecialAction.BreastVore:
                    succeded_attempt = PredatorComponent.BreastVore(target);
                    break;
                case SpecialAction.CockVore:
                    succeded_attempt = PredatorComponent.CockVore(target);
                    break;
                case SpecialAction.Unbirth:
                    succeded_attempt = PredatorComponent.Unbirth(target);
                    break;
                case SpecialAction.TailVore:
                    succeded_attempt = PredatorComponent.TailVore(target);
                    break;
                case SpecialAction.AnalVore:
                    succeded_attempt = PredatorComponent.AnalVore(target);
                    break;
                case SpecialAction.BladderVore:
                    succeded_attempt = PredatorComponent.BladderVore(target);
                    break;
                default:
                    succeded_attempt = PredatorComponent.Devour(target);
                    break;
            }
        }
        IsAllIn = false;
        if (!succeded_attempt && target.Unit.Predator)
        {
            return !target.PredatorComponent.UsePreferredVore(this);
        }
        return true;
    }

    public bool DireInfection(Actor_Unit target)
    {
        if (Movement < 1 || Unit.HasTrait(Traits.DireInfection) == false)
            return false;
        List<AbilityTargets> targetTypes = new List<AbilityTargets>();
        targetTypes.Add(AbilityTargets.Enemy);
        if (!TacticalUtilities.MeetsQualifier(targetTypes, this, target))
            return false;
        if (target.Position.GetNumberOfMovesDistance(Position) > 1)
            return false;
        int curr = target.Unit.Health;
        Attack(target, false, damageMultiplier: .75f);
        bool didAttackHit = curr != target.Unit.Health;
        if (didAttackHit)
        {
            target.Unit.ApplyStatusEffect(StatusEffectType.Poisoned, 5f, 6);
            target.Unit.ApplyStatusEffect(StatusEffectType.Snared, 1f, 1);
        }
        TacticalGraphicalEffects.CreateGenericMagic(Position, target.Position, target, TacticalGraphicalEffects.SpellEffectIcon.Poison);
        Movement = 0;

        return true;
    }

    public bool Attack(Actor_Unit target, bool ranged, bool forceBite = false, float damageMultiplier = 1, bool canKill = true, bool canRecurse = true, bool opportunity = false)
    {
        bool freeAttack = opportunity || OpportunityAttacks.Resolving;
        if (freeAttack && (!OpportunityAttacks.CanAct(this) || !OpportunityAttacks.CanAct(target))) return false;
        if (Surrendered || target == null || target.Surrendered && !forceBite) return false;
        Weapon weapon;
        if (ranged)
            weapon = BestRanged;
        else
            weapon = BestMelee;
        if (forceBite)
            weapon = State.World.ItemRepository.Bite;
        if ((!freeAttack && Movement == 0) || weapon == null)
        {
            return false;
        }
        //check range
        int targetRange = target.Position.GetNumberOfMovesDistance(Position);


        if (Unit.HasTrait(Traits.HaplessPrey) && target.Unit.Predator && targetRange < 2)
        {
            float haplessChance = .10f;
            int levelDiff = target.Unit.Level - Unit.Level;
            if (levelDiff < 0)
                levelDiff = 0;
            haplessChance += levelDiff / 20;
            if (haplessChance >= State.Rand.NextDouble())
            {
                if (!freeAttack) Movement = 0;
                TacticalUtilities.ForceFeed(this, target, false);
                return false;
            }
        }

        int extraAttacks = CalculateExtraAttacks();

        bool grazebool = false;
        bool critbool = false;
        if (weapon.Range > 1)
        {
            if ((targetRange >= 2 || (targetRange >= 1 && weapon.Omni)) && targetRange <= weapon.Range)
            {
                animationUpdateTime = 1.0F;
                if (!forceBite) Unit.RecordCompanyWeaponUse(true);
                Mode = DisplayMode.Attacking;

                if (Unit.HasTrait(Traits.AwfulAim))
                {
                    var possibleTargets = TacticalUtilities.UnitsWithinTiles(target.Position, 2);
                    target = possibleTargets[State.Rand.Next(0,possibleTargets.Count()-1)];
                }

                int rangedattacks = Unit.TraitBoosts.RangedAttacks;
                rangedattacks += extraAttacks + Unit.TempBoosts.ExtraAttacks;
                SpendWeaponAP(true, rangedattacks, freeAttack);
                int remainingHealth = target.Unit.Health;
                float resolvedChance;
                int result = ResolveWeaponHit(target, true, out resolvedChance);
                CompanyPostBattleTraits.RecordIncomingAttack(target, freeAttack);
                grazebool = result == 3; critbool = result == 2;
                damageMultiplier *= ResultDamageMultiplier(target, result);
                int damage = WeaponDamageAgainstTarget(target, true, multiplier: damageMultiplier);

                EquipmentFunctions.CheckEquipment(Unit, EquipmentActivator.OnRangedAttack, new object[] { this, target, damage });

                if (Unit.GetStatusEffect(StatusEffectType.Sharpness) != null)
                    damage += damage * (Unit.GetStatusEffect(StatusEffectType.Sharpness).Duration / 100);
                State.GameManager.SoundManager.PlaySwing(this);
                bool wasDowned = target.Unit.Downed;
                if (target.Defend(this, ref damage, true, out float chance, canKill, result != 0, resolvedChance, result != 3))
                {
                    CompanyPostBattleTraits.RecordDamageDealt(this, true, Mathf.Min(damage, remainingHealth));
                    if (!wasDowned && target.Unit.Downed) CompanyPostBattleTraits.RecordDownedBy(this, true, false);
                    EquipmentFunctions.CheckEquipment(Unit, EquipmentActivator.OnRangedHit, new object[] { this, target, damage });

                    foreach (IAttackStatusEffect trait in Unit.AttackStatusEffects)
                    {
                        trait.ApplyStatusEffect(this, target, true, damage);
                    }
                    if (Unit.HasTrait(Traits.WeaponChanneler) && Unit.Mana >= 6)
                        Unit.SpendMana(6);
                    if (Unit.HasTrait(Traits.Elementist) && Unit.SpendMana(3))
                        TacticalUtilities.CreateEffect(target.Position,  (TileEffectType)State.Rand.Next(0, (int)TileEffectType.None), 0, 1 + Unit.GetStat(Stat.Intelligence) / 30, 4);
                    if (Unit.HasTrait(Traits.Tenacious))
                        Unit.RemoveTenacious();
                    if (target.Unit.HasTrait(Traits.Tenacious))
                        target.Unit.AddTenacious();
                    if (target.Unit.GetStatusEffect(StatusEffectType.Focus) != null)                  
                        target.Unit.RemoveFocus();
                    if (target.Unit.HasTrait(Traits.Crystaline) && State.Rand.Next(4) == 0)
                        target.Unit.ApplyStatusEffect(StatusEffectType.Fractured, 1, 1);
                    if (target.Unit.IsACopy())
                    {
                        if (target.Unit.OriginalUnit.HasTrait(Traits.InherentGlamour) && (target.Unit.HealthPct - 1) * 2 > (float)State.Rand.NextDouble())
                        {
                            target.Unit.RevertCopiedUnit();
                        }
                    }
                    if (Unit.GetStatusEffect(StatusEffectType.Sharpness) != null)                  
                        Unit.RemoveStackStatus(StatusEffectType.Sharpness, Unit.GetStatusEffect(StatusEffectType.Sharpness).Duration / 2);

                    TacticalGraphicalEffects.CreateProjectile(this, target);

                    State.GameManager.TacticalMode.TacticalStats.RegisterHit(BestRanged, Mathf.Min(damage, remainingHealth), Unit.Side);
                    TacticalUtilities.Log.RegisterHit(Unit, target.Unit, weapon, damage, chance, grazebool ? "graze" : critbool ? "critical" : null);
                    if (Unit.FixedSide == TacticalUtilities.GetMindControlSide(target.Unit))
                    {
                        StatusEffect charm = target.Unit.GetStatusEffect(StatusEffectType.Charmed);
                        if (charm != null)
                        {
                            target.Unit.StatusEffects.Remove(charm);                // betrayal dispels charm
                        }
                    }
                    Unit.GiveScaledExp(2 * target.Unit.ExpMultiplier, Unit.Level - target.Unit.Level);
                    if (target.Unit.IsDead)
                    {
                        State.GameManager.TacticalMode.TacticalStats.RegisterKill(BestRanged, Unit.Side);
                        KillUnit(target, weapon);
                    }
                    if (critbool)
                        target.UnitSprite.DisplayCrit();
                    else if (grazebool)
                        target.UnitSprite.DisplayGraze();
                    return true;
                }
                else
                {
                    Unit.GiveScaledExp(0.5f * target.Unit.ExpMultiplier, Unit.Level - target.Unit.Level);
                    State.GameManager.TacticalMode.TacticalStats.RegisterMiss(Unit.Side);
                    TacticalUtilities.Log.RegisterMiss(Unit, target.Unit, weapon, chance);
                    if (Unit.HasTrait(Traits.Tenacious))
                        Unit.AddTenacious();

                    EquipmentFunctions.CheckEquipment(Unit, EquipmentActivator.OnRangedMiss, new object[] { this, target, damage });

                }
            }
        }
        else
        {
            if (targetRange < 2)
            {
                animationUpdateTime = 1.0F;
                if (!forceBite) Unit.RecordCompanyWeaponUse(false);
                Mode = DisplayMode.Attacking;
                int meleeAttacks = Unit.TraitBoosts.MeleeAttacks;
                meleeAttacks += extraAttacks + Unit.TempBoosts.ExtraAttacks;
                if (Unit.HasTrait(Traits.LightFrame) && PredatorComponent?.PreyCount == 0)
                    meleeAttacks++;
                if (Unit.HasTrait(Traits.WildFury) && Unit.GetBestMelee() == State.World.ItemRepository.Claws)
                    meleeAttacks++;
                SpendWeaponAP(false, meleeAttacks, freeAttack);
                int remainingHealth = target.Unit.Health;
                float resolvedChance;
                int result = ResolveWeaponHit(target, false, out resolvedChance);
                CompanyPostBattleTraits.RecordIncomingAttack(target, freeAttack);
                grazebool = result == 3; critbool = result == 2;
                damageMultiplier *= ResultDamageMultiplier(target, result);
                int damage = WeaponDamageAgainstTarget(target, false, multiplier: damageMultiplier, forceBite: forceBite, opportunity: freeAttack);

                if (Unit.GetStatusEffect(StatusEffectType.Sharpness) != null)
                    damage += damage * (Unit.GetStatusEffect(StatusEffectType.Sharpness).Duration / 50);

                EquipmentFunctions.CheckEquipment(Unit, EquipmentActivator.OnMeleeAttack, new object[] { this, target, damage });
                bool blocked = false;
                if (target.Unit.HasTrait(Traits.DexterousDefense) && !ranged)
                {
                    float blockchance = DexCheckOdds(this, target);
                    if (State.Rand.NextDouble() < blockchance)
                    {
                        blocked = true;
                        damage /= 2;
                        if (!freeAttack) Movement = 0;
                    }
                }
                if (Unit.HasTrait(Traits.SweepingStrikes) && canRecurse)
                {
                    int movementholder = Movement; //Store current AP
                    var inrange = TacticalUtilities.UnitsWithinTiles(target.Position, 1).Where(u => u != target && u != this).ToList();
                    foreach (var sweepTarget in inrange)
                    {
                        if (!freeAttack) Movement = 1; // Ordinary sweep AP only; reactions never grant/restore AP.
                        if (freeAttack && !OpportunityAttacks.CanAct(this)) break;
                        Attack(sweepTarget, false, false, 0.33f, true, false, freeAttack);
                    }
                    if (!freeAttack && !Surrendered && SelfPrey == null) Movement = movementholder; // Do not revive incapacitated attackers.
                }
                bool wasDowned = target.Unit.Downed;
                if (target.Defend(this, ref damage, false, out float chance, canKill, result != 0, resolvedChance, result != 3))
                {
                    CompanyPostBattleTraits.RecordDamageDealt(this, false, Mathf.Min(damage, remainingHealth));
                    if (!wasDowned && target.Unit.Downed) CompanyPostBattleTraits.RecordDownedBy(this, false, false);

                    EquipmentFunctions.CheckEquipment(Unit, EquipmentActivator.OnMeleeHit, new object[] { this, target, damage });

                    foreach (IAttackStatusEffect trait in Unit.AttackStatusEffects)
                    {
                        trait.ApplyStatusEffect(this, target, false, damage);
                    }
                    if (target.Unit.HasTrait(Traits.PoorConstitution) && State.Rand.Next(10) == 0)
                        target.Unit.ApplyStatusEffect(StatusEffectType.Sleeping, 1, 2);
                    if (Unit.HasTrait(Traits.WeaponChanneler) && Unit.Mana >= 6)
                        Unit.SpendMana(6);
                    if (Unit.HasTrait(Traits.Elementist) && Unit.SpendMana(3))
                        TacticalUtilities.CreateEffect(target.Position, (TileEffectType)State.Rand.Next(0, (int)TileEffectType.None), 1, 1 + Unit.GetStat(Stat.Intelligence) / 30, 4);
                    if (Unit.HasTrait(Traits.BladeDance))
                        Unit.AddBladeDance();
                    if (target.Unit.HasTrait(Traits.BladeDance))
                        target.Unit.RemoveBladeDance();
                    if (Unit.HasTrait(Traits.Tenacious))
                        Unit.RemoveTenacious();
                    if (target.Unit.HasTrait(Traits.Tenacious))
                        target.Unit.AddTenacious();
                    if (target.Unit.GetStatusEffect(StatusEffectType.Focus) != null)
                        target.Unit.RemoveFocus();
                    if (target.Unit.HasTrait(Traits.Toxic) && State.Rand.Next(8) == 0)
                        Unit.ApplyStatusEffect(StatusEffectType.Poisoned, 2 + target.Unit.GetStat(Stat.Strength) / 20, 3);
                    if (target.Unit.HasTrait(Traits.Crystaline) && State.Rand.Next(4) == 0)
                        target.Unit.ApplyStatusEffect(StatusEffectType.Fractured, 1, 1);
                    if (Unit.HasTrait(Traits.ForcefulBlow))
                        TacticalUtilities.KnockBack(this, target);
                    if (Unit.GetStatusEffect(StatusEffectType.Sharpness) != null)
                        Unit.RemoveStackStatus(StatusEffectType.Sharpness, Unit.GetStatusEffect(StatusEffectType.Sharpness).Duration / 2);
                    if (target.Unit.IsACopy())
                    {
                        if (target.Unit.OriginalUnit.HasTrait(Traits.InherentGlamour) && (target.Unit.HealthPct - 1) * 2 > (float)State.Rand.NextDouble())
                        {
                            target.Unit.RevertCopiedUnit();
                        }
                    }
                    State.GameManager.SoundManager.PlayMeleeHit(target);

                    State.GameManager.TacticalMode.TacticalStats.RegisterHit(BestMelee, Mathf.Min(damage, remainingHealth), Unit.Side);
                    if (blocked)
                    {
                        UnitSprite.DisplayBlock();
                        TacticalUtilities.Log.RegisterBlock(Unit, target.Unit, weapon, damage, chance);
                    }
                    else
                    {
                        TacticalUtilities.Log.RegisterHit(Unit, target.Unit, weapon, damage, chance, grazebool ? "graze" : critbool ? "critical" : null);
                    }
                    if (Unit.FixedSide == TacticalUtilities.GetMindControlSide(target.Unit))
                    {
                        StatusEffect charm = target.Unit.GetStatusEffect(StatusEffectType.Charmed);
                        if (charm != null)
                        {
                            target.Unit.StatusEffects.Remove(charm);                // betrayal dispels charm
                        }
                    }
                    CreateHitEffects(target);
                    Unit.GiveScaledExp(2 * target.Unit.ExpMultiplier, Unit.Level - target.Unit.Level);
                    if (target.Unit.IsDead)
                    {
                        State.GameManager.TacticalMode.TacticalStats.RegisterKill(BestMelee, Unit.Side);
                        KillUnit(target, weapon);
                    }
                    if (critbool)
                        target.UnitSprite.DisplayCrit();
                    else if (grazebool)
                        target.UnitSprite.DisplayGraze();
                    return true;
                }
                else
                {
                    Unit.GiveScaledExp(0.5f * target.Unit.ExpMultiplier, Unit.Level - target.Unit.Level);
                    State.GameManager.TacticalMode.TacticalStats.RegisterMiss(Unit.Side);
                    TacticalUtilities.Log.RegisterMiss(Unit, target.Unit, weapon, chance);
                    State.GameManager.SoundManager.PlaySwing(this);
                    if (Unit.HasTrait(Traits.Tenacious))
                        Unit.AddTenacious();

                    EquipmentFunctions.CheckEquipment(Unit, EquipmentActivator.OnMeleeMiss, new object[] { this, target, damage });
                   
                }
            }

        }
        return false;
    }

    void CreateHitEffects(Actor_Unit target)
    {
        State.GameManager.TacticalMode.CreateBloodHitEffect(target.Position);
    }

    private void KillUnit(Actor_Unit target, Weapon weapon)
    {
        TacticalUtilities.Log.RegisterKill(Unit, target.Unit, weapon);
        Unit.KilledUnits++;
        target.KilledByDigestion = false;
        Unit.EnemiesKilledThisBattle++;
        target.Unit.KilledBy = Unit;
        target.Unit.Kill();
        foreach (var item in target.Unit.AllConditionalTraits.Keys.Where(t => t.trigger == TraitConditionTrigger.OnDeath || t.trigger == TraitConditionTrigger.All).ToList())
        {
            if (ConditionalTraitConditionChecker.TacticalTraitConditionActive(target, item))
            {
                target.Unit.ActivateConditionalTrait(item.id);
            }
            else
            {
                target.Unit.DeactivateConditionalTrait(item.id);
            }
        }
        if (Unit.HasTrait(Traits.KillerKnowledge) && Unit.KilledUnits % 4 == 0)
            Unit.GeneralStatIncrease(1);
        if (Unit.HasTrait(Traits.KillingMomentum))
        {
            RestoreMP();
            Movement /= 2;
        }       

        if (Unit.HasTrait(Traits.InfectiousReproduction) && target.Unit.GetStatusEffect(StatusEffectType.Poisoned) != null)
        {
            Race spawnRace = Unit.DetermineSpawnRace();
            target.PredatorComponent.CreateSpawn(spawnRace, Unit.Side, Unit.Experience / 2, true);
        }

        Unit.GiveScaledExp(4 * target.Unit.ExpMultiplier, Unit.Level - target.Unit.Level);

    }

    private void KillUnit(Actor_Unit target, Spell spell)
    {
        TacticalUtilities.Log.RegisterSpellKill(Unit, target.Unit, spell.SpellType);
        Unit.KilledUnits++;
        target.KilledByDigestion = false;
        Unit.EnemiesKilledThisBattle++;
        target.Unit.KilledBy = Unit;
        target.Unit.Kill();
        foreach (var item in target.Unit.AllConditionalTraits.Keys.Where(t => t.trigger == TraitConditionTrigger.OnDeath || t.trigger == TraitConditionTrigger.All).ToList())
        {
            if (ConditionalTraitConditionChecker.TacticalTraitConditionActive(target, item))
            {
                target.Unit.ActivateConditionalTrait(item.id);
            }
            else
            {
                target.Unit.DeactivateConditionalTrait(item.id);
            }
        }
        if (Unit.HasTrait(Traits.KillerKnowledge) && Unit.KilledUnits % 4 == 0)
            Unit.GeneralStatIncrease(1);
        if (Unit.HasTrait(Traits.KillingMomentum))
        {
            RestoreMP();
            Movement /= 2;
        }

        Unit.GiveScaledExp(4 * target.Unit.ExpMultiplier, Unit.Level - target.Unit.Level);
    }

    /// <summary>
    /// Gives a random temporary boost, associated with the Taste for blood trait
    /// </summary>
    internal void GiveRandomBoost()
    {
        int rand = State.Rand.Next(5);
        switch (rand)
        {
            case 0:
                Unit.StatusEffects.Add(new StatusEffect(StatusEffectType.Valor, .25f, 5));
                break;
            case 1:
                Unit.StatusEffects.Add(new StatusEffect(StatusEffectType.Fast, 0.3f, 5));
                break;
            case 2:
                Unit.StatusEffects.Add(new StatusEffect(StatusEffectType.Mending, 24, 5));
                break;
            case 3:
                Unit.StatusEffects.Add(new StatusEffect(StatusEffectType.Shielded, .25f, 5));
                break;
            case 4:
                Unit.StatusEffects.Add(new StatusEffect(StatusEffectType.Predation, Unit.GetStat(Stat.Voracity) / 4, 5));
                break;
        }
    }

    internal bool DefendSpellCheck(Spell spell, Actor_Unit attacker, out float chance, float mod = 0, Stat stat = Stat.Intelligence)
    {
        State.GameManager.TacticalMode.AITimer = Config.TacticalAttackDelay;
        if (State.GameManager.CurrentScene == State.GameManager.TacticalMode && State.GameManager.TacticalMode.IsPlayerInControl == false && State.GameManager.TacticalMode.turboMode == false)
            State.GameManager.CameraCall(Position);
        chance = spell.Resistable ? GetMagicChance(attacker, spell, mod, stat) : 1;
        float r = (float)State.Rand.NextDouble();
        bool hit = r < chance;
        CompanyPostBattleTraits.RecordSpellAttempt(attacker, hit);
        return hit;

    }

    internal int ApplySpellTraitDamage(Actor_Unit attacker, int damage)
    {
        return Math.Max(1, damage - CompanyInjuries.DamagePenalty(attacker.Unit) + (int)TieredTraits.Amount(attacker.Unit, TierEffect.MagicDamage) +
            (int)TieredTraits.Amount(Unit, TierEffect.MagicDamageReceived));
    }

    int ScaleSpellDamage(Actor_Unit attacker, int damage, float pendingMultiplier = 1)
    {
        damage = (int)(damage * attacker.Unit.TraitBoosts.Outgoing.MagicDamage * Unit.TraitBoosts.Incoming.MagicDamage *
            pendingMultiplier * TagConditionChecker.ApplyTagEffect(attacker.Unit, Unit, UnitTagModifierEffect.MagicDamageMult));
        if (attacker.Unit.HasTrait(Traits.Multifaceted) && attacker.Unit.IsHighestStat(Stat.Intelligence))
            damage += (int)Math.Round(Unit.GetStat(Stat.Intelligence) * .1f);
        return damage;
    }

    // Expected damage on a successful hit, not hit chance. Never executes equipment callbacks,
    // spends mana, rolls RNG or mutates trait/status/barrier state.
    internal int PreviewSpellDamage(Actor_Unit attacker, int damage, DamageTypes type)
    {
        if (TacticalUtilities.SneakAttackCheck(attacker.Unit, Unit)) damage *= 3;
        float pending = Unit.GetStatusEffect(StatusEffectType.Fractured) != null ? 1.50f : Unit.HasTrait(Traits.Crystaline) ? .75f : 1f;
        damage = ScaleSpellDamage(attacker, damage, pending);
        int mana = Unit.Mana;
        if (attacker.Unit.HasTrait(Traits.ManaBurn))
        {
            int cost = Unit.ManaCost(damage / 2);
            if (mana >= cost) mana -= cost;
            else damage += damage / 2;
        }
        damage = ApplySpellTraitDamage(attacker, damage);
        damage = ElementalDamage(damage, type);
        damage -= ManaBarrierReduction(damage, mana);
        return (int)Math.Round(CompanyEquipment.ExpectedAfterArmor(damage, CompanyEquipment.ArmorClass(Unit), Unit.Barrier));
    }

    internal int PreviewHitDamage(int damage)
    {
        damage -= ManaBarrierReduction(damage, Unit.Mana);
        return (int)Math.Round(CompanyEquipment.ExpectedAfterArmor(damage, CompanyEquipment.ArmorClass(Unit), Unit.Barrier));
    }

    internal int PreviewWeaponDamageAgainstTarget(Actor_Unit target, bool ranged, float multiplier = 1, bool opportunity = false)
    { return target.PreviewHitDamage(WeaponDamageAgainstTarget(target, ranged, multiplier, opportunity: opportunity)); }

    internal bool DefendDamageSpell(DamageSpell spell, Actor_Unit attacker, int damage)
    {
        if (TacticalUtilities.SneakAttackCheck(attacker.Unit, Unit))
        {
            attacker.Unit.hiddenFixedSide = false;
            damage *= 3;
            State.GameManager.TacticalMode.Log.RegisterMiscellaneous($"<color=purple>{attacker.Unit.Name} sneak-attacked {Unit.Name}!</color>");
        }
        if (attacker.Unit.GetApparentSide() != Unit.GetApparentSide())
        {
            if (attacker.sidesAttackedThisBattle == null)
                attacker.sidesAttackedThisBattle = new List<int>();
            attacker.sidesAttackedThisBattle.Add(Unit.GetApparentSide());
        }
        if (Unit.IsDead)
            return false;

        EquipmentFunctions.CheckEquipment(Unit, EquipmentActivator.WhenTargetedBySpellDamage, new object[] { this, attacker, damage });

        if (DefendSpellCheck(spell, attacker, out float chance))
        {
            // KNOWN BROKEN: these cached multipliers compound on repeated hits.
            // Extremely low priority by user direction (2026-11-04); deliberately not fixed here.
            if (Unit.GetStatusEffect(StatusEffectType.Fractured) == null && Unit.HasTrait(Traits.Crystaline))
            {
                Unit.TraitBoosts.Incoming.MagicDamage *= 0.75f;
            }
            if (Unit.GetStatusEffect(StatusEffectType.Fractured) != null)
            {
                Unit.TraitBoosts.Incoming.MagicDamage *= 1.50f;
            }
            damage = ScaleSpellDamage(attacker, damage);
            if (attacker.Unit.HasTrait(Traits.ManaBurn))
            {
                if (!Unit.SpendMana(damage/2))
                {
                    damage += damage / 2; 
                }
            }
            damage = ApplySpellTraitDamage(attacker, damage);
            EquipmentFunctions.CheckEquipment(Unit, EquipmentActivator.WhenHitBySpellDamage, new object[] { this, attacker, damage });
            int remainingHealth = Unit.Health;
            bool wasDowned = Surrendered;
            damage = Math.Max(1, damage);
            ResolveDamage(damage, true, true, spell.DamageType, true, true, out damage);
            CompanyPostBattleTraits.RecordMagicDamage(attacker, Mathf.Min(damage, remainingHealth));
            if (!wasDowned && Surrendered) CompanyPostBattleTraits.RecordDownedBy(attacker, false, true);
            State.GameManager.TacticalMode.TacticalStats.RegisterHit(spell, Mathf.Min(damage, remainingHealth), attacker.Unit.Side);
            State.GameManager.TacticalMode.Log.RegisterSpellHit(attacker.Unit, Unit, spell.SpellType, damage, chance);
            if (attacker.Unit.FixedSide == TacticalUtilities.GetMindControlSide(Unit))
            {
                StatusEffect charm = Unit.GetStatusEffect(StatusEffectType.Charmed);
                if (charm != null)
                {
                    Unit.StatusEffects.Remove(charm);                // betrayal dispels charm
                }
            }
            if (Unit.HasTrait(Traits.Crystaline) && State.Rand.Next(4) == 0)
                Unit.ApplyStatusEffect(StatusEffectType.Fractured, 1, 1);
            if (attacker.Unit.HasTrait(Traits.ArcaneMagistrate))
            {
                attacker.Unit.AddFocus((Unit.IsDead || (!wasDowned && Surrendered) ? 5 : 1));
            }
            if (Unit.HasTrait(Traits.MagicSynthesis))
            {
                Unit.SingleUseSpells.Add(spell.SpellType);
                Unit.RestoreMana((int)(spell.ManaCost * 0.75f));
                Unit.UpdateSpells();
            }
            attacker.Unit.GiveScaledExp(1 * Unit.ExpMultiplier, attacker.Unit.Level - Unit.Level);
            if (Unit.IsDead)
            {
                attacker.Unit.GiveScaledExp(4 * Unit.ExpMultiplier, attacker.Unit.Level - Unit.Level);
            }
            return true;
        }
        else
        {
            UnitSprite.DisplayDamage(0);
            State.GameManager.TacticalMode.Log.RegisterSpellMiss(attacker.Unit, Unit, spell.SpellType, chance);
            attacker.Unit.GiveScaledExp(.25f * Unit.ExpMultiplier, attacker.Unit.Level - Unit.Level);
            EquipmentFunctions.CheckEquipment(Unit, EquipmentActivator.WhenMissedBySpellDamage, new object[] { this, attacker, damage });
        }

        return false;
    }

    internal bool DefendStatusSpell(StatusSpell spell, Actor_Unit attacker, Stat stat = Stat.Intelligence)
    {
        if (spell.Id == "hypno-fart" && Unit.FixedSide == attacker.Unit.FixedSide)
        {
            return false;
        }
        bool sneakAttack = false;
        if (TacticalUtilities.SneakAttackCheck(attacker.Unit, Unit) && !spell.AcceptibleTargets.Contains(AbilityTargets.Ally))     // Replace when there is an unresistable negative status
        {
            attacker.Unit.hiddenFixedSide = false;
            sneakAttack = true;
            State.GameManager.TacticalMode.Log.RegisterMiscellaneous($"<color=purple>{attacker.Unit.Name} sneak-attacked {Unit.Name}!</color>");
        }
        if (attacker.Unit.GetApparentSide() != Unit.GetApparentSide() && !spell.AcceptibleTargets.Contains(AbilityTargets.Ally))
        {
            if (attacker.sidesAttackedThisBattle == null)
                attacker.sidesAttackedThisBattle = new List<int>();
            attacker.sidesAttackedThisBattle.Add(Unit.GetApparentSide());
        }

        EquipmentFunctions.CheckEquipment(Unit, EquipmentActivator.WhenTargetedBySpellStatus, new object[] { this, attacker, spell });


        if (DefendSpellCheck(spell, attacker, out float chance, sneakAttack ? -0.3f : 0f, stat))
        {
            State.GameManager.TacticalMode.Log.RegisterSpellHit(attacker.Unit, Unit, spell.SpellType, 0, chance);
            if (spell.ExpireEffect != null)
            {
                Unit.ApplyStatusEffect(spell.Type, spell.Effect(attacker, this), spell.Duration(attacker, this), attacker.Unit, spell.ExpireEffect(attacker, this));
            }
            else
            {
                Unit.ApplyStatusEffect(spell.Type, spell.Effect(attacker, this), spell.Duration(attacker, this), attacker.Unit);
            }
            EquipmentFunctions.CheckEquipment(Unit, EquipmentActivator.WhenHitBySpellStatus, new object[] { this, attacker, spell });
            if (spell.Id == "charm")
            {
                UnitSprite.DisplayCharm();
                if (attacker.Unit.HasTrait(Traits.Temptation))
                {
                    Unit.ApplyStatusEffect(StatusEffectType.Temptation, spell.Effect(attacker, this), spell.Duration(attacker, this));
                }
            }
            if (spell.Id == "hypno-fart")
            {
                UnitSprite.DisplayHypno();
                if (attacker.Unit.HasTrait(Traits.Temptation))
                {
                    Unit.ApplyStatusEffect(StatusEffectType.Temptation, spell.Effect(attacker, this), spell.Duration(attacker, this));
                }
            }
            if (spell.Alraune)
            {
                if (Unit.HasTrait(Traits.PollenProjector) == false)
                {
                    Unit.ApplyStatusEffect(StatusEffectType.Clumsiness, 1.5f, 3);
                    Unit.ApplyStatusEffect(StatusEffectType.Poisoned, 2 + attacker.Unit.GetStat(Stat.Intelligence) / 10, 3);
                    Unit.ApplyStatusEffect(StatusEffectType.WillingPrey, 0, 3);
                }
            }
            if (Unit.HasTrait(Traits.MagicSynthesis))
            {
                Unit.SingleUseSpells.Add(spell.SpellType);
                Unit.RestoreMana((int)(spell.ManaCost * 0.75f));
                Unit.UpdateSpells();
            }
            if (spell.Id == "whispers-spell")
            {
                UnitSprite.DisplayCharm();
                Unit.ApplyStatusEffect(StatusEffectType.WillingPrey, 0, spell.Duration(attacker, this));
                Unit.ApplyStatusEffect(StatusEffectType.Temptation, 0, spell.Duration(attacker, this));
            }
            if (TacticalUtilities.MeetsQualifier(spell.AcceptibleTargets, attacker, this))
                attacker.Unit.GiveScaledExp(1, attacker.Unit.Level - Unit.Level);
            else
                attacker.Unit.GiveExp(1);
            return true;
        }
        else
        {
            UnitSprite.DisplayDamage(0);
            if (attacker.Unit.Side == Unit.Side)
                attacker.Unit.GiveScaledExp(.25f, attacker.Unit.Level - Unit.Level);
            else
            {
                attacker.Unit.GiveExp(.25f);
                UnitSprite.DisplayResist();
            }

            State.GameManager.TacticalMode.Log.RegisterSpellMiss(attacker.Unit, Unit, spell.SpellType, chance);
            EquipmentFunctions.CheckEquipment(Unit, EquipmentActivator.WhenMissedBySpellStatus, new object[] { this, attacker, spell });
        }

        return false;
    }

    public bool Defend(Actor_Unit attacker, ref int damage, bool ranged, out float chance, bool canKill = true, bool? resolvedHit = null, float resolvedChance = 0, bool canCauseInjury = true)
    {
        EquipmentFunctions.CheckEquipment(Unit, ranged ? EquipmentActivator.WhenRangedAttacked : EquipmentActivator.WhenMeleeAttacked, new object[] { this, attacker, damage });

        if (TacticalUtilities.SneakAttackCheck(attacker.Unit, Unit))
        {
            attacker.Unit.hiddenFixedSide = false;
            State.GameManager.TacticalMode.Log.RegisterMiscellaneous($"<color=purple>{attacker.Unit.Name} sneak-attacked {Unit.Name}!</color>");
        }
        if (attacker.Unit.GetApparentSide() != Unit.GetApparentSide())
        {
            if (attacker.sidesAttackedThisBattle == null)
                attacker.sidesAttackedThisBattle = new List<int>();
            attacker.sidesAttackedThisBattle.Add(Unit.GetApparentSide());
        }
        State.GameManager.TacticalMode.AITimer = Config.TacticalAttackDelay;
        if (State.GameManager.CurrentScene == State.GameManager.TacticalMode && State.GameManager.TacticalMode.IsPlayerInControl == false && State.GameManager.TacticalMode.turboMode == false)
            State.GameManager.CameraCall(Position);
        chance = resolvedHit.HasValue ? resolvedChance : GetAttackChance(attacker, ranged);
        bool hit = resolvedHit ?? (State.Rand.NextDouble() < chance);
        if (hit)
        {
            EquipmentFunctions.CheckEquipment(Unit, ranged ? EquipmentActivator.WhenRangedHit : EquipmentActivator.WhenMeleeHit, new object[] { this, attacker, damage });

            damage = Math.Max(1, damage);
            ResolveDamage(damage, false, canKill, DamageTypes.Generic, true, canCauseInjury, out damage);
            if (canKill == false && attacker.Unit.HasTrait(Traits.VenomousBite))
            {
                Unit.ApplyStatusEffect(StatusEffectType.Poisoned, 3, 3);
                Unit.ApplyStatusEffect(StatusEffectType.Shaken, .2f, 2);
            }

            if (Unit.HasTrait(Traits.Multifaceted) && Unit.IsHighestStat(Stat.Strength))
            {
                MultifacetedCooldown = 4;
            }

            return true;
        }
        else
            UnitSprite.DisplayDamage(0);

        EquipmentFunctions.CheckEquipment(Unit, ranged ? EquipmentActivator.WhenRangedMissed : EquipmentActivator.WhenMeleeMissed, new object[] { this, attacker, damage });

        return false;
    }

    //This is the chance to be devoured, so it belongs here and not in PredatorComponent
    public float GetDevourChance(Actor_Unit attacker, bool includeSecondaries = false, int skillBoost = 0, bool force = false)
    {
        if (attacker?.Unit.Predator == false && !force)
        {
            return 0;
        }
        if (Surrendered || ((attacker.Unit.HasTrait(Traits.FriendlyStomach) || attacker.Unit.HasTrait(Traits.Endosoma)) && (Unit.FixedSide == attacker.Unit.GetApparentSide(Unit)) || Unit.GetStatusEffect(StatusEffectType.Hypnotized)?.Strength == attacker.Unit.FixedSide))
            return 1f;

        float predVoracity = Mathf.Pow(15 + skillBoost + attacker.Unit.GetStat(Stat.Voracity), 1.5f);
        float preyStrength = Mathf.Pow(15 + Unit.GetStat(Stat.Agility), 1.5f);
        float preyWill = preyStrength;
        float attackerScore = predVoracity * (attacker.Unit.HealthPct + 1) * (30 + attacker.BodySize()) / 16 / (1 + 2 * Mathf.Pow(attacker.PredatorComponent.UsageFraction, 2));
        float defenderHealthPct = Unit.HealthPct;
        float defenderScore = 20 + (2 * (preyStrength + 2 * preyWill) * defenderHealthPct * defenderHealthPct * ((10 + BodySize()) / 2));

        if (Unit.GetStatusEffect(StatusEffectType.WillingPrey) != null || Unit.GetStatusEffect(StatusEffectType.Sleeping) != null)
        {
            defenderScore /= 2;
        }

        switch (Config.VoreRate)
        {
            case -1:
                attackerScore *= .4f;
                break;
            case 1:
                attackerScore *= 2;
                break;
            case 2:
                attackerScore *= 4;
                break;
        }

        defenderScore /= Unit.TraitBoosts.Incoming.VoreOddsMult;
        attackerScore *= attacker.Unit.TraitBoosts.Outgoing.VoreOddsMult;

        if (TacticalUtilities.SneakAttackCheck(attacker.Unit, Unit)) // sneakAttack
        {
            attackerScore *= 3;
        }

        if (attacker.Unit.HasTrait(Traits.VenomShock) && Unit.GetStatusEffect(StatusEffectType.Poisoned) != null)
            attackerScore *= 1.5f;

        if (attacker.Unit.HasTrait(Traits.AllOutFirstStrike) && attacker.HasAttackedThisCombat == false)
            attackerScore *= 3.25f;

        foreach (IVoreAttackOdds trait in attacker.Unit.VoreAttackOdds)
        {
            trait.VoreAttack(attacker, ref attackerScore);
        }

        foreach (IVoreDefenseOdds trait in Unit.VoreDefenseOdds)
        {
            trait.VoreDefense(this, ref defenderScore);
        }

        float odds = attackerScore / (attackerScore + defenderScore) * 100;

        if (!attacker.Unit.HasTrait(Traits.Irresistable))
        odds *= Unit.TraitBoosts.FlatHitReduction;
        odds *= TagConditionChecker.ApplyTagEffect(Unit, attacker.Unit, UnitTagModifierEffect.VoreOddsMult);

        if (includeSecondaries)
        {

        }
        return Mathf.Clamp01(odds / 100 + TieredTraits.Amount(attacker.Unit, TierEffect.VoreAccuracy) - CompanyInjuries.AccuracyPenalty(attacker.Unit) +
            (TacticalUtilities.TreatAsHostile(attacker, this) ? TieredTraits.Amount(Unit, TierEffect.VoreAccuracyReceived) : 0));


    }

    public bool BellyRub(Actor_Unit target)
    {
        Prey prey;
        if (target.Unit.Predator == false)
            return false;
        List<int> possible = new List<int>();
        int type;
        if (target.PredatorComponent.Fullness <= 0)
        {
            return false;
        }
        if (Position.GetNumberOfMovesDistance(target.Position) > 1)
        {
            return false;
        }
        if (target.PredatorComponent.BreastFullness > 0)
        {
            possible.Add(1);
        }
        if (target.PredatorComponent.LeftBreastFullness > 0)
        {
            possible.Add(1);
        }
        if (target.PredatorComponent.RightBreastFullness > 0)
        {
            possible.Add(1);
        }
        if (target.PredatorComponent.CombinedStomachFullness > 0 || target.PredatorComponent.WombFullness > 0 || target.PredatorComponent.BladderFullness > 0)
        {
            possible.Add(0);
        }
        if (target.PredatorComponent.BallsFullness > 0)
        {
            possible.Add(2);
        }
        if (target.PredatorComponent.TailFullness > 0)
        {
            possible.Add(3);
        }

        if (possible.Count == 0)
            return false;
        if (target.ReceivedRub)
            return false;
        if ((target.Unit.GetApparentSide() != Unit.GetApparentSide() && target.Unit.GetApparentSide() != Unit.FixedSide) && !(Config.CanUseStomachRubOnEnemies || TacticalUtilities.GetMindControlSide(Unit) != -1))
            return false;
        target.RubCount++;
        target.BeingRubbed = true;
        int index = UnityEngine.Random.Range(0, possible.Count - 1);
        type = possible[index];
        switch (type)
        {
            case 0:// Split womb and stomachs (and added bladder) from being grouped up as of post version 44D to fix log issues
                prey = target.PredatorComponent.GetDirectPrey().FirstOrDefault(p => p.Location.Equals(PreyLocation.bladder));
                if (prey != null)
                {
                    TacticalUtilities.Log.RegisterBellyRub(Unit, target.Unit, prey.Unit, PreyLocation.bladder, 1f);
                    break;
                }
                prey = target.PredatorComponent.GetDirectPrey().FirstOrDefault(p => p.Location.Equals(PreyLocation.womb));
                if (prey != null)
                {
                    TacticalUtilities.Log.RegisterBellyRub(Unit, target.Unit, prey.Unit, PreyLocation.womb, 1f);
                    break;
                }
                prey = target.PredatorComponent.GetDirectPrey().FirstOrDefault(p => p.Location.Equals(PreyLocation.stomach) || p.Location.Equals(PreyLocation.stomach2) || p.Location.Equals(PreyLocation.anal));
                if (prey != null) 
                {
                    TacticalUtilities.Log.RegisterBellyRub(Unit, target.Unit, prey.Unit, PreyLocation.stomach, 1f);
                    break;
                }
                break;
            case 1:
                prey = target.PredatorComponent.GetDirectPrey().FirstOrDefault(p => p.Location.Equals(PreyLocation.breasts) || p.Location.Equals(PreyLocation.leftBreast) || p.Location.Equals(PreyLocation.rightBreast));
                if (prey == null) break;
                TacticalUtilities.Log.RegisterBreastRub(Unit, target.Unit, prey.Unit, 1f);
                break;
            case 2:
                prey = target.PredatorComponent.GetDirectPrey().FirstOrDefault(p => p.Location.Equals(PreyLocation.balls));
                if (prey == null) break;
                TacticalUtilities.Log.RegisterBallMassage(Unit, target.Unit, prey.Unit, 1f);
                break;
            case 3:
                prey = target.PredatorComponent.GetDirectPrey().FirstOrDefault(p => p.Location.Equals(PreyLocation.tail));
                if (prey == null) break;
                TacticalUtilities.Log.RegisterTailRub(Unit, target.Unit, prey.Unit, 1f);
                break;
            default:
                break;
        }
        if (!State.GameManager.TacticalMode.turboMode)
        {
            SetRubMode();
            target.SetRubbedMode();
            if (Config.BellyRubHands)
                GameObject.Instantiate(State.GameManager.TacticalMode.HandPrefab, new Vector3(target.Position.x + UnityEngine.Random.Range(-0.2F, 0.2F), target.Position.y + 0.1F + UnityEngine.Random.Range(-0.1F, 0.1F)), new Quaternion());
            State.GameManager.TacticalMode.AITimer = Config.TacticalVoreDelay;
        }
        target.DigestCheck();
        if (Unit.HasTrait(Traits.PleasurableTouch))
            target.DigestCheck();
        if (Unit.HasTrait(Traits.RoughMassage))
        {
            target.DigestCheck();
            target.Unit.AddWeakness();
        }
        target.BeingRubbed = false;
        int thirdMovement = Math.Max(1, MaxMovement() / 3);
        if (Movement > thirdMovement)
            Movement -= thirdMovement;
        else
            Movement = 0;

        return true;
    }

    public float BodySize()
    {
        float size = Unit.SpeciesSettings.GetBodySize(Unit.Race);

        size *= Unit.GetScale(2);

        size *= Unit.TraitBoosts.BulkMultiplier;

        if (Unit.GetStatusEffect(StatusEffectType.Petrify) != null)
            size *= 3;

        if (Unit.GetStatusEffect(StatusEffectType.Frozen) != null)
            size *= 2;

        return size;
    }

    public float Bulk(int count = 0)
    {
        if (Unit.HasTrait(Traits.Inedible))
            return float.MaxValue / 100;
        float bulk = 0;
        bulk += PredatorComponent?.GetBulkOfPrey(count) ?? 0;
        if (Unit.IsDead)
        {
            float myBulk = Unit.Health + Unit.MaxHealth;
            myBulk = myBulk / (Unit.MaxHealth) * BodySize();

            bulk += myBulk;
        }
        else
        {
            bulk += BodySize();
        }
        return bulk;
    }





    public bool Move(int direction, TacticalTileType[,] tiles, bool forced = false)
    {
        if (Surrendered) return false;
        //check if we can move in that direction
        switch (direction)
        {
            case 0:
                return Move(0, 1, tiles, forced);
            case 1:
                return Move(1, 1, tiles, forced);
            case 2:
                return Move(1, 0, tiles, forced);
            case 3:
                return Move(1, -1, tiles, forced);
            case 4:
                return Move(0, -1, tiles, forced);
            case 5:
                return Move(-1, -1, tiles, forced);
            case 6:
                return Move(-1, 0, tiles, forced);
            case 7:
                return Move(-1, 1, tiles, forced);

        }

        return false;
    }

    internal Vec2i GetTile(Vec2i start, int direction)
    {
        switch (direction)
        {
            case 0:
                return new Vec2i(start.x, start.y + 1);
            case 1:
                return new Vec2i(start.x + 1, start.y + 1);
            case 2:
                return new Vec2i(start.x + 1, start.y);
            case 3:
                return new Vec2i(start.x + 1, start.y - 1);
            case 4:
                return new Vec2i(start.x, start.y - 1);
            case 5:
                return new Vec2i(start.x - 1, start.y - 1);
            case 6:
                return new Vec2i(start.x - 1, start.y);
            case 7:
                return new Vec2i(start.x - 1, start.y + 1);
            default:
                return null;
        }
    }

    internal int DiffToDirection(int x, int y)
    {
        if (x == 0 && y > 0) return 0;
        else if (x > 0 && y > 0) return 1;
        else if (x > 0 && y == 0) return 2;
        else if (x > 0 && y < 0) return 3;
        else if (x == 0 && y < 0) return 4;
        else if (x < 0 && y < 0) return 5;
        else if (x < 0 && y == 0) return 6;
        else if (x < 0 && y > 0) return 7;
        else return 0;
    }


    internal bool MoveTo(Vec2i destination, TacticalTileType[,] tiles, float delay, bool forced = false)
    {
        var battle = State.GameManager.TacticalMode;
        var reactions = battle.Opportunities;
        bool completing = reactions.Matches(this, destination);
        if (reactions.Busy && (!completing || !reactions.Ready)) return false;
        if ((!forced && !OpportunityAttacks.CanAct(this)) || Surrendered || destination.Matches(Position) ||
            destination.GetNumberOfMovesDistance(Position) != 1 || destination.x < 0 || destination.y < 0 ||
            destination.x > tiles.GetUpperBound(0) || destination.y > tiles.GetUpperBound(1))
        {
            if (completing) reactions.Finish(true);
            return false;
        }
        int cost = Unit.HasTrait(Traits.Flight) ? 1 : TacticalTileInfo.TileCost(new Vec2(destination.x, destination.y));
        bool open = TacticalUtilities.OpenTile(destination, this);
        bool transit = Movement > 1 && (Unit.HasTrait(Traits.Flight) ||
            (TacticalUtilities.PassableOpenTile(destination, this) && (Unit.HasTrait(Traits.PassThrough) ||
            Unit.HasTrait(Traits.Blitz) || Unit.HasTrait(Traits.SpectralStep))));
        // Do not enter an occupied transit tile where a reaction could strand a
        // downed actor. Safe, unthreatened flight/pass-through remains available.
        if (Movement < cost || (!open && (!transit || (!forced &&
            (OpportunityAttacks.Threats(this, destination).Any() || OpportunityAttacks.Threats(this, Position).Any())))))
        {
            if (completing) reactions.Finish(true);
            return false;
        }
        if (!forced && !completing && reactions.Begin(this, destination)) return false;
        battle.Translator.SetTranslator(UnitSprite.transform, Position, destination, delay, battle.IsPlayerTurn);
        battle.AITimer = delay;
        battle.DirtyPack = true;
        MovedThisTurn = true;
        Position = destination;
        Movement -= cost;
        if (completing) reactions.Finish(false);
        return true;
    }

    bool Move(int changeX, int changeY, TacticalTileType[,] tiles, bool forced = false)
    {
        Vec2i newLocation = new Vec2i(Position.x + changeX, Position.y + changeY);
        return MoveTo(newLocation, tiles, Config.TacticalPlayerMovementDelay, forced);
    }


    public void Update(float dt)
    {
        if (animationUpdateTime > 0)
        {
            animationUpdateTime -= dt;
            if (animationUpdateTime <= 0)
            {
                if (Mode == DisplayMode.IdleAnimation)
                {
                    animationUpdateTime = 0.25f;
                    animationStep--;
                    if (animationStep == 0)
                    {
                        if (Mode > DisplayMode.Attacking && Mode < DisplayMode.VoreSuccess && modeQueue.Count > 0)
                        {
                            animationUpdateTime = modeQueue.FirstOrDefault().Value;
                            Mode = (DisplayMode)modeQueue.FirstOrDefault().Key;                                        // This casting back and forth saves dealing with accessibility hassles.
                            modeQueue.RemoveAt(0);
                        }
                        else
                        {
                            animationUpdateTime = 0;
                            Mode = 0;
                        }
                    }
                }
                else
                {
                    if (Mode >= DisplayMode.Attacking && Mode <= DisplayMode.FrogPouncing)
                        HasAttackedThisCombat = true;
                    if (modeQueue.Count > 0)
                    {
                        animationUpdateTime = modeQueue.FirstOrDefault().Value;
                        Mode = (DisplayMode)modeQueue.FirstOrDefault().Key;
                        modeQueue.RemoveAt(0);
                    }
                    else
                    {
                        animationUpdateTime = 0;
                        Mode = 0;
                    }

                }
            }
        }
        if (Unit.Predator)
            PredatorComponent.UpdateTransition();
    }

    public void NewTurn()
    {
        MovedThisTurn = false;
        WeaponAttackThisTurn = false;
        SpellCastThisTurn = false;
        AIAvoidEat--;

        EquipmentFunctions.TickCoolDown(Unit, EquipmentType.RechargeTactical);
        EquipmentFunctions.CheckEquipment(Unit, EquipmentActivator.OnTacticalTurnStart, new [] { this, null, null });

        NewTurnPreMPTraits();

        Unit.RestoreMana(Unit.TraitBoosts.ManaRegen);
        if (SelfPrey == null) // Restore stamina and proc regen if the unit isn't vored
        {
            Unit.RestoreStamPct(0.1f);
            Unit.Heal(Unit.TraitBoosts.HealthRegen);
        }
        UnitSprite.UpdateHealthBar(this);
        TurnsSinceLastParalysis++;
        if (Targetable && Visible && Surrendered == false && Fled == false)
            RestoreMP();
        Unit.TickStatusEffects();
        if (SelfPrey != null) // Stall prevention if a unit has been eaten for 100 turns
        {
            if (SelfPrey.TurnsDigested <= 100)
                Unit.Heal(Unit.TraitBoosts.HealthRegen);
        }

        if ((Config.AbsorbLoss ? PredatorComponent?.AlivePrey <= 0 : PredatorComponent.Fullness <= 0))
        {
            RampStacks -= Config.DigestionRampLoss;
            if (RampStacks < 0)
            {
                RampStacks = 0;
            }

        }
        else
            RampStacks += (Config.DigestionRampLoss >= 0 ? 1 : -1) / (Config.DigestionRampTurn == 0 ? 1 : Config.DigestionRampTurn);

        NewTurnPostMPTraits();
        
        RubCount = 0;
        TurnsSinceLastDamage++;
        if (Unit.GetStatusEffect(StatusEffectType.Agony) != null)
        {
            StatusEffect eff = Unit.GetStatusEffect(StatusEffectType.Agony);
            int totalDamage = (int)Math.Round(eff.Strength / eff.Duration);
            Damage(totalDamage, true, true);
            eff.Strength -= totalDamage;
        }
    }

    //Traits that should be applied before MP is refreshed.
    public void NewTurnPreMPTraits()
    {

        if (Surrendered && !Unit.BattleActive && Unit.HasTrait(Traits.Fearless))
        {
            Surrendered = false;
        }
        else if (SurrenderedThisTurn)
        {
            SurrenderedThisTurn = false;
            Movement = 0;
        }

        if (Unit.HasTrait(Traits.Multifaceted))
        {
            if (MultifacetedCooldown > 0)
            {
                MultifacetedCooldown--;
            }
        }


        if (Unit.HasTrait(Traits.ManaAttuned))
        {
            if (!Unit.SpendMana(Unit.MaxMana / 10))
                if (Unit.Mana > 0)
                    Unit.SpendMana(Unit.Mana); //Zero out mana
                else
                    Unit.ApplyStatusEffect(StatusEffectType.Sleeping, 1, 2);
            if (Unit.GetStatusEffect(StatusEffectType.Sleeping) != null)
                Unit.RestoreMana(Unit.MaxMana / 2);
        }

        if (Unit.HasTrait(Traits.SiphoningAura))
        {
            var targets = TacticalUtilities.UnitsWithinTiles(Position, 1).Where(u => this != u && !u.Unit.IsEnemyOfSide(Unit.Side)).ToList();
            if (targets.Any())
            {
                foreach (var target in targets)
                {
                    target.Unit.AddWeakness();
                }
                Unit.AddBolster(targets.Count);
            }
        }

        if (Unit.HasTrait(Traits.CurseOfImmolation) && Surrendered == false)
        {
            if (SelfPrey == null)
            {
                var targets = TacticalUtilities.UnitsWithinTiles(Position, 1).ToList();
                if (targets.Any())
                {
                    foreach (var target in targets)
                    {
                        target.Damage(Unit.Level, true, false, DamageTypes.Fire);
                    }
                }
            }
            else
            {
                Damage(Unit.Level, true, false, DamageTypes.Fire);
                SelfPrey.Predator.Damage(Unit.Level, true, false, DamageTypes.Fire);
            }
        }

        if (Unit.HasTrait(Traits.CurseOfEquivalency))
        {
            Unit.SpecificStatIncrease(-1, Unit.GetHighestStatIndex());
            Unit.SpecificStatIncrease(1, Unit.GetLowestStatIndex());
        }

        if (Unit.HasTrait(Traits.Perseverance) && TurnsSinceLastDamage > 3)
        {
            Unit.HealPercentage(0.03f * TurnsSinceLastDamage);
        }

        if (Unit.HasTrait(Traits.Timid) && ((Unit.NearbyEnemies - 1) > Unit.NearbyFriendlies))
        {
            Unit.ApplyStatusEffect(StatusEffectType.Shaken, .2f, 2);
        }

        if (Unit.HasTrait(Traits.FoodComaProne))
        {
            if (PredatorComponent != null && Unit.GetStatusEffect(StatusEffectType.Sleeping) == null)
            {
                if (PredatorComponent.UsageFraction >= State.Rand.NextDouble())
                {
                    Unit.ApplyStatusEffect(StatusEffectType.Sleeping, 1, 2);
                }
            }
        }

        if (Unit.HasTrait(Traits.BlessingOfNature))
        {
            if (State.GameManager.TacticalMode.currentTurn % 2 == 0)
            {
                var targets = TacticalUtilities.UnitsWithinTiles(Position, 2).Where(t => !t.Unit.IsEnemyOfSide(Unit.Side)).ToList();
                if (targets.Any())
                {
                    var target = targets[State.Rand.Next(0, targets.Count())];
                    target.Unit.ApplyStatusEffect(StatusEffectType.Mending, 24, (int)Math.Ceiling((double)(Unit.Level / 3)));
                }

            }
            foreach (StatusEffect effect in Unit.StatusEffects)
            {
                if (effect.Applicator != null)
                {
                    if (effect.Applicator.Side == Unit.Side)
                    {
                        effect.Applicator.Heal(Unit.GetStat(Stat.Strength) / 20);
                    }
                }
            }
        }

        if (Unit.HasTrait(Traits.BlessingOfEarth))
        {
            if (State.GameManager.TacticalMode.currentTurn % 2 == 0)
            {
                var targets = TacticalUtilities.UnitsWithinTiles(Position, 2).Where(t => !t.Unit.IsEnemyOfSide(Unit.Side)).ToList();
                if (targets.Any())
                {
                    var target = targets[State.Rand.Next(0, targets.Count())];
                    target.Unit.ApplyStatusEffect(StatusEffectType.Shielded, .25f, (int)Math.Ceiling((double)(Unit.Level / 2)));
                }
            }
            foreach (StatusEffect effect in Unit.StatusEffects)
            {
                if (effect.Applicator != null)
                {
                    if (effect.Applicator.Side == Unit.Side)
                    {
                        effect.Applicator.RestoreBarrier(Unit.GetStat(Stat.Intelligence) / 10);
                    }
                }
            }
        }

        if (Unit.HasTrait(Traits.BlessingOfWater))
        {
            if (State.GameManager.TacticalMode.currentTurn % 2 == 0)
            {
                var targets = TacticalUtilities.UnitsWithinTiles(Position, 2).Where(t => !t.Unit.IsEnemyOfSide(Unit.Side)).ToList();
                if (targets.Any())
                {
                    var target = targets[State.Rand.Next(0, targets.Count())];
                    target.Unit.AddFocus(Unit.Level);
                }

            }
            foreach (StatusEffect effect in Unit.StatusEffects)
            {
                if (effect.Applicator != null)
                {
                    if (effect.Applicator.Side == Unit.Side)
                    {
                        effect.Applicator.RestoreMana(Unit.GetStat(Stat.Intelligence) / 10);
                    }
                }
            }
        }

        if (Unit.HasTrait(Traits.BlessingOfFerocity))
        {
            if (State.GameManager.TacticalMode.currentTurn % 2 == 0)
            {
                var targets = TacticalUtilities.UnitsWithinTiles(Position, 2).Where(t => !t.Unit.IsEnemyOfSide(Unit.Side)).ToList();
                if (targets.Any())
                {
                    var target = targets[State.Rand.Next(0, targets.Count())];
                    target.Unit.ApplyStatusEffect(StatusEffectType.Valor, .25f, (int)Math.Ceiling((double)(Unit.Level / 2)));
                }

            }
            foreach (StatusEffect effect in Unit.StatusEffects)
            {
                if (effect.Applicator != null)
                {
                    if (effect.Applicator.Side == Unit.Side)
                    {
                        effect.Applicator.AddStackStatus(StatusEffectType.Sharpness, Unit.GetStat(Stat.Strength) / 10);
                    }
                }
            }
        }

    }

    //Traits that should be applied after MP is refreshed.
    public void NewTurnPostMPTraits()
    {
        if (Unit.HasTrait(Traits.IntrusiveAppetite))
        {
            if (Movement > 0 && SelfPrey == null && State.Rand.Next(10) == 0)
            {
                var targets = TacticalUtilities.UnitsWithinTiles(Position, 1).Where(u => this != u).ToList();
                if (targets.Any())
                {
                    var target = targets[State.Rand.Next(0, targets.Count())];
                    PredatorComponent.UsePreferredVore(target);
                }
            }
        }
    }

    // Inline calculation of extra attacks
    public int CalculateExtraAttacks()
    {
        int extra = 0;
        if (Unit.HasTrait(Traits.Multifaceted) && Unit.IsHighestStat(Stat.Agility))
        {
            if (Unit.Level >= State.GameManager.TacticalMode.currentTurn)
                extra++;
        }
        return extra;
    }

    public void SubtractHealth(int damage, bool canCauseInjury = true)
    {
        Unit.ApplyCombatDamage(damage, canCauseInjury);
        if (Unit.Health > Unit.MaxHealth)
            Unit.Health = Unit.MaxHealth;
        if (damage <= 0)
            return;
        TurnsSinceLastDamage = -1;
        EquipmentFunctions.CheckEquipment(this.Unit, EquipmentActivator.OnDamage, new object[] { this, damage, null });
    }

    int ElementalDamage(int damage, DamageTypes damageType)
    {
        switch (damageType)
        {
            case DamageTypes.Fire:
                damage = (int)Mathf.Round(damage * Unit.TraitBoosts.FireDamageTaken);
                break;
            case DamageTypes.Ice:
                damage = (int)Mathf.Round(damage * Unit.TraitBoosts.IceDamageTaken);
                break;
            case DamageTypes.Elec:
                float elecboost = 1f;
                if (Unit.GetStatusEffect(StatusEffectType.Static) != null)
                    elecboost = 1.5f;
                damage = (int)Mathf.Round(damage * (Unit.TraitBoosts.ElecDamageTaken * elecboost));
                break;
            case DamageTypes.Poison:
                if (Unit.HasTrait(Traits.PoisonSpit))
                    damage = 0;
                break;
            default:
                break;
        }
        return damage;
    }

    int ManaBarrierReduction(int damage, int mana)
    {
        float fraction = Unit.MaxMana > 0 ? mana / (float)Unit.MaxMana : 0;
        if (!Unit.HasTrait(Traits.ManaBarrier) || fraction < .51f) return 0;
        int reduction = damage < 0 ? 0 : (int)((fraction - .5f) * damage);
        return mana >= Unit.ManaCost(reduction) ? reduction : 0;
    }

    public int CalculateDamageWithResistance(int damage, DamageTypes damageType)
    {
        damage = ElementalDamage(damage, damageType);
        int reduction = ManaBarrierReduction(damage, Unit.Mana);
        if (reduction > 0 && Unit.SpendMana(reduction)) damage -= reduction;
        return damage;
    }

    public bool Damage(int damage, bool spellDamage = false, bool canKill = true, DamageTypes damageType = DamageTypes.Generic, bool damagingHit = false, bool canCauseInjury = true)
    {
        int resolved;
        return ResolveDamage(damage, spellDamage, canKill, damageType, damagingHit, canCauseInjury, out resolved);
    }

    bool ResolveDamage(int damage, bool spellDamage, bool canKill, DamageTypes damageType, bool damagingHit, bool canCauseInjury, out int resolved)
    {
        resolved = 0;
        if (Unit.IsDead)
        {
            //These are thrown in as insurance incase of weirdness - there was a bug report of a unit that had negative health and was not flagged as dead, and didn't die when hit.
            Targetable = false;
            Surrendered = true;
            PredatorComponent?.FreeAnyAlivePrey();
            //Debug.Log("Attack performed on target that was already dead");
            return false;
        }        
        int modifiedDamage = CalculateDamageWithResistance(damage, damageType);
        if (damagingHit) modifiedDamage = CompanyEquipment.Deflect(Unit, modifiedDamage);
        resolved = modifiedDamage;
        if (damageType != DamageTypes.Mutual)//Prevents damage from flashing every unit.
            UnitSprite.DisplayDamage(modifiedDamage, spellDamage);
        modifiedDamage = Unit.DamageBarrier(modifiedDamage);
        SubtractHealth(modifiedDamage, canCauseInjury);
        if (Unit.GetStatusEffect(StatusEffectType.Agony) != null)
        {
            StatusEffect eff = Unit.GetStatusEffect(StatusEffectType.Agony);
            eff.Strength += modifiedDamage * 0.35f;
        }
        if ((State.Rand.NextDouble() > Unit.HealthPct))
        {
            if (Unit.HasTrait(Traits.Cowardly))
            {
                Surrendered = true;
                Movement = 0;
                State.GameManager.TacticalMode.Log.RegisterMiscellaneous($"{Unit.Name} is downed.");
            }
        }
        if (Unit.HasTrait(Traits.MutualBiology) && damageType != DamageTypes.Mutual)
        {
            TacticalUtilities.MutuallyDamageUnits(this, damage);
        }
        if (Unit.HasTrait(Traits.Berserk) && GoneBerserk == false)
        {
            if (Unit.HealthPct < .5f)
            {
                GoneBerserk = true;
                Unit.ApplyStatusEffect(StatusEffectType.Berserk, 1, 3);
            }
        }
        if (Unit.HasTrait(Traits.CurseOfPhasing))
        {
            {
                var teleport_tiles = TacticalUtilities.TilesWithinRange(Position, 3).Where(t => TacticalUtilities.IsWalkable(t.x, t.y, this)).ToList();
                var target_tile = teleport_tiles[State.Rand.Next(0, teleport_tiles.Count())];
                var unit_check = TacticalUtilities.UnitOnTile(target_tile);
                if (unit_check == null)
                {
                    SetPos(target_tile);
                    State.GameManager.TacticalMode.Translator.SetTranslator(UnitSprite.transform, Position, target_tile, 0, State.GameManager.TacticalMode.IsPlayerTurn);
                    State.GameManager.TacticalMode.Log.RegisterMiscellaneous($"{Unit.Name} teleported.");
                }
                else
                {
                    if (unit_check.Unit.Predator)
                    {
                        unit_check.PredatorComponent.ForceConsumeAuto(this);
                        State.GameManager.TacticalMode.Log.RegisterMiscellaneous($"{Unit.Name} teleported into {unit_check.Unit.Name}.");
                    }
                }
            }
        }
        if ((canKill == false && Unit.IsDead) || (Config.AutoSurrender && Unit.IsDead && State.Rand.NextDouble() < Config.AutoSurrenderChance && Surrendered == false && Unit.HasTrait(Traits.Fearless) == false && !KilledByDigestion))
        {
            Unit.Health = 1;
            Surrendered = true;
            Movement = 0;
        }

        if (Config.DamageNumbers == false && !State.GameManager.TacticalMode.turboMode)
        {
            Mode = DisplayMode.Injured;
            animationUpdateTime = 1.0F;
        }
        if (!State.GameManager.TacticalMode.turboMode)
        {
            Mode = DisplayMode.Hurt;
            animationUpdateTime = 1.0F;
        }
        if (Unit.IsDead)
        {
            State.GameManager.TacticalMode.DirtyPack = true;
            Targetable = false;
            Surrendered = true;
            if (Config.VisibleCorpses)
            {
                float angle = 40 + State.Rand.Next(280);
                UnitSprite.transform.rotation = Quaternion.Euler(0, 0, angle);
                spriteLayerOffset = State.GameManager.TacticalMode.GetNextCorpseLayer();
            }
            else
            {
                Visible = false;
            }

            PredatorComponent?.FreeAnyAlivePrey();
        }
        return true;
    }

    public void DigestCheck(string feedtype = "")
    {
        if (Unit.IsDead == false)
            PredatorComponent?.Digest(feedtype);
    }

    //public List<Actor_Unit> BirthCheck()
    //{
    //    List<Actor_Unit> released = null;
    //    if (Unit.IsDead == false)
    //        released = PredatorComponent?.Birth();
    //    if (released == null)
    //    {
    //        released = new List<Actor_Unit>();
    //    }
    //    return released;
    //}

    public Vec2i GetPos(int i)
    {
        switch (i)
        {
            case 0:
                return new Vec2i(Position.x, Position.y + 1);
            case 1:
                return new Vec2i(Position.x + 1, Position.y + 1);
            case 2:
                return new Vec2i(Position.x + 1, Position.y);
            case 3:
                return new Vec2i(Position.x + 1, Position.y - 1);
            case 4:
                return new Vec2i(Position.x, Position.y - 1);
            case 5:
                return new Vec2i(Position.x - 1, Position.y - 1);
            case 6:
                return new Vec2i(Position.x - 1, Position.y);
            case 7:
                return new Vec2i(Position.x - 1, Position.y + 1);
        }
        return Position;
    }


    public static bool CheckForActor(List<Actor_Unit> actors, Vec2i p)
    {
        for (int i = 0; i < actors.Count; i++)
        {
            if (actors[i].Position.Matches(p) && actors[i].Targetable == true)
            {
                return false;
            }
        }
        return true;
    }

    public bool IsErect()
    {
        if (Unit.DickSize < 0)
            return false;
        if (Config.ErectionsFromVore)
        {
            if (PredatorComponent?.Fullness > 0)
                return true;
        }
        if (Config.ErectionsFromCockVore)
        {
            if (PredatorComponent?.BallsFullness > 0)
                return true;
        }
        return false;
    }

    public float WillCheckOdds(Actor_Unit actor, Actor_Unit target)
    {
        if (target.Unit.IsDead)
        {
            return 0;
        }
        float ratio = (float)target.Unit.GetStat(Stat.Intelligence) / actor.Unit.GetStat(Stat.Intelligence);

        if (ratio > 5)
            ratio = 5;
        return ratio / 25;
    }
    public float DexCheckOdds(Actor_Unit actor, Actor_Unit target)
    {
        if (target.Unit.IsDead)
        {
            return 0;
        }
        float ratio = (float)target.Unit.GetStat(Stat.Agility) / actor.Unit.GetStat(Stat.Agility);

        if (ratio > 7)
            ratio = 7;
        return ratio / 10;
    }
    public float CritCheck(Actor_Unit actor, Actor_Unit target)
    { return WarbandStats.CriticalChance(actor.Unit); }

    public float GrazeCheck(Actor_Unit actor, Actor_Unit target)
    { return 0; } // Legacy caller compatibility: grazes no longer have a separate stat chance.

    // 0 miss, 1 normal hit, 2 critical, 3 rescued graze. No intermediate feedback.
    int ResolveWeaponHit(Actor_Unit target, bool ranged, out float chance)
    {
        chance = target.GetAttackChance(this, ranged);
        int result;
        if (State.Rand.NextDouble() >= chance)
            result = chance >= .51f && (Unit.HasTrait(Traits.KeenEye) || State.Rand.NextDouble() < chance) ? 3 : 0;
        else
        {
            float critical = CritCheck(this, target);
            critical += Unit.TraitBoosts.Outgoing.CritRateShift - target.Unit.TraitBoosts.Incoming.CritRateShift;
            critical *= TagConditionChecker.ApplyTagEffect(Unit, target.Unit, UnitTagModifierEffect.CritRateShift);
            critical += TieredTraits.Amount(Unit, TierEffect.CriticalChance) - CompanyInjuries.CriticalPenalty(Unit);
            result = State.Rand.NextDouble() < Mathf.Clamp01(critical) ? 2 : 1;
        }
        CompanyPostBattleTraits.RecordWeaponAttempt(this, ranged, result);
        return result;
    }
    internal float ResultDamageMultiplier(Actor_Unit target, int result)
    {
        if (result == 3)
            return Config.GrazeDamageMod * Unit.TraitBoosts.Outgoing.GrazeDamageMult *
                target.Unit.TraitBoosts.Incoming.GrazeDamageMult *
                TagConditionChecker.ApplyTagEffect(Unit, target.Unit, UnitTagModifierEffect.GrazeDamageMult);
        if (result == 2)
            return Config.CritDamageMod * Unit.TraitBoosts.Outgoing.CritDamageMult *
                target.Unit.TraitBoosts.Incoming.CritDamageMult *
                TagConditionChecker.ApplyTagEffect(Unit, target.Unit, UnitTagModifierEffect.CritDamageMult);
        return 1;
    }

    internal bool CastSpell(Spell spell, Actor_Unit target)
    {
        if (Unit.SpendMana(spell.ManaCost) == false && spell.IsFree != true)
            return false;
        CompanyPostBattleTraits.RecordSpellCast(this, false);
        Unit.GiveExp(1);
        OnSpellCast(spell, target);
        //if (target != null && target.DefendSpellCheck(spell, this, out float chance) == false)
        //    return false;

        State.GameManager.SoundManager.PlaySpellCast(spell, this);
        if (target?.UnitSprite != null)
            State.GameManager.SoundManager.PlaySpellHit(spell, target.UnitSprite.transform.position);

        SpendSpellAP();
        return true;
    }

    internal void CastMawWithLocation(Spell spell, Actor_Unit target, Vec2i targetArea = null)
    {
        PreyLocation preyLocation = PreyLocation.stomach;
        var possibilities = new Dictionary<string, PreyLocation>();
        possibilities.Add("Maw", PreyLocation.stomach);
        if (PredatorComponent.CanAnalVore()) possibilities.Add("Anus", PreyLocation.anal);
        if (PredatorComponent.CanBreastVore()) possibilities.Add("Breast", PreyLocation.breasts);
        if (PredatorComponent.CanCockVore()) possibilities.Add("Cock", PreyLocation.balls);
        if (PredatorComponent.CanUnbirth()) possibilities.Add("Pussy", PreyLocation.womb);

        if (State.GameManager.TacticalMode.IsPlayerInControl && State.GameManager.CurrentScene == State.GameManager.TacticalMode && possibilities.Count > 1)
        {
            var box = State.GameManager.CreateOptionsBox();
            box.SetData($"Where do you want to put your prey?", "Maw", () => CastMaw(spell, target, PreyLocation.stomach, targetArea), possibilities.Keys.ElementAtOrDefault(1), () => CastMaw(spell, target, possibilities.Values.ElementAtOrDefault(1), targetArea), possibilities.Keys.ElementAtOrDefault(2), () => CastMaw(spell, target, possibilities.Values.ElementAtOrDefault(2), targetArea), possibilities.Keys.ElementAtOrDefault(3), () => CastMaw(spell, target, possibilities.Values.ElementAtOrDefault(3), targetArea), possibilities.Keys.ElementAtOrDefault(4), () => CastMaw(spell, target, possibilities.Values.ElementAtOrDefault(4), targetArea));
        }
        else
        {
            preyLocation = possibilities.Values.ToList()[State.Rand.Next(possibilities.Count)];
            CastMaw(spell, target, preyLocation, targetArea);
        }
    }
    internal void CastMaw(Spell spell, Actor_Unit target, PreyLocation preyLocation, Vec2i targetArea = null )
    {
        if (Unit.Predator == false)
            return;
        if (Unit.SpendMana(spell.ManaCost) == false && spell.IsFree != true)
            return;
        CompanyPostBattleTraits.RecordSpellCast(this, false);
        OnSpellCast(spell, target);
        State.GameManager.SoundManager.PlaySpellCast(spell, this);
        if (target != null)
        {
            if (spell.AreaOfEffect > 0)
            {
                foreach (var splashTarget in TacticalUtilities.UnitsWithinTiles(target.Position, spell.AreaOfEffect))
                {
                    if (PredatorComponent.MagicConsume(spell, splashTarget, preyLocation))
                    {
                        State.GameManager.SoundManager.PlaySpellHit(spell, splashTarget.UnitSprite.transform.position);
                    }
                }
            }
            else
            {
                if (PredatorComponent.MagicConsume(spell, target, preyLocation))
                {
                    State.GameManager.SoundManager.PlaySpellHit(spell, target.UnitSprite.transform.position);
                }
            }
        }
        else if (targetArea != null && spell.AreaOfEffect > 0)
        {
            foreach (var splashTarget in TacticalUtilities.UnitsWithinTiles(targetArea, spell.AreaOfEffect))
            {
                if (PredatorComponent.MagicConsume(spell, splashTarget, preyLocation))
                {
                    State.GameManager.SoundManager.PlaySpellHit(spell, splashTarget.UnitSprite.transform.position);
                }
            }
        }
        SpendSpellAP();
    }
    internal void CastOffensiveSpell(DamageSpell spell, Actor_Unit target, Vec2i targetArea = null)
    {
        if (Unit.SpendMana(spell.ManaCost) == false && spell.IsFree != true)
            return;
        CompanyPostBattleTraits.RecordSpellCast(this, true);
        State.GameManager.SoundManager.PlaySpellCast(spell, this);
        OnSpellCast(spell, target);

        if (target != null)
        {
            if (spell.AreaOfEffect > 0 && spell.AOEType == AreaOfEffectType.Full)
            {
                foreach (var splashTarget in TacticalUtilities.UnitsWithinTiles(target.Position, spell.AreaOfEffect).Where(s => s.Unit.IsDead == false))
                {
                    splashTarget.DefendDamageSpell(spell, this, spell.Damage(this, splashTarget));
                    CheckDead(splashTarget);
                }
                State.GameManager.SoundManager.PlaySpellHit(spell, target.UnitSprite.transform.position);
            }
            else if (spell.AOEType == AreaOfEffectType.FixedPattern)
            {
                foreach (var splashTarget in TacticalUtilities.UnitsWithinPattern(target.Position, spell.Pattern).Where(s => s.Unit.IsDead == false))
                {
                    splashTarget.DefendDamageSpell(spell, this, spell.Damage(this, splashTarget));
                    CheckDead(splashTarget);
                }
                State.GameManager.SoundManager.PlaySpellHit(spell, target.UnitSprite.transform.position);
            }
            else if (spell.AOEType == AreaOfEffectType.RotatablePattern)
            {                             
              
                foreach (var splashTarget in TacticalUtilities.UnitsWithinRotatingPattern(target.Position, spell.Pattern, TacticalUtilities.GetRotatingOctant(Position, target.Position)).Where(s => s.Unit.IsDead == false))
                {
                    splashTarget.DefendDamageSpell(spell, this, spell.Damage(this, splashTarget));
                    CheckDead(splashTarget);
                }
                State.GameManager.SoundManager.PlaySpellHit(spell, target.UnitSprite.transform.position);
            }
            else
            {
                if (target.DefendDamageSpell(spell, this, spell.Damage(this, target)))
                {
                    State.GameManager.SoundManager.PlaySpellHit(spell, target.UnitSprite.transform.position);
                }
                CheckDead(target);
            }
        }
        else if (targetArea != null && spell.AreaOfEffect > 0 && spell.AOEType == AreaOfEffectType.Full)
        {
            foreach (var splashTarget in TacticalUtilities.UnitsWithinTiles(targetArea, spell.AreaOfEffect).Where(s => s.Unit.IsDead == false))
            {
                splashTarget.DefendDamageSpell(spell, this, spell.Damage(this, splashTarget));
                CheckDead(splashTarget);
            }
            State.GameManager.SoundManager.PlaySpellHit(spell, targetArea);
        }
        else if (targetArea != null && spell.AOEType == AreaOfEffectType.FixedPattern)
        {
            foreach (var splashTarget in TacticalUtilities.UnitsWithinPattern(targetArea, spell.Pattern).Where(s => s.Unit.IsDead == false))
            {
                splashTarget.DefendDamageSpell(spell, this, spell.Damage(this, splashTarget));
                CheckDead(splashTarget);
            }
            State.GameManager.SoundManager.PlaySpellHit(spell, targetArea);
        }
        else if (targetArea != null && (spell.AOEType == AreaOfEffectType.RotatablePattern))
        {
            foreach (var splashTarget in TacticalUtilities.UnitsWithinRotatingPattern(targetArea, spell.Pattern, TacticalUtilities.GetRotatingOctant(Position, targetArea)).Where(s => s.Unit.IsDead == false))
            {
                splashTarget.DefendDamageSpell(spell, this, spell.Damage(this, splashTarget));
                CheckDead(splashTarget);
            }
            State.GameManager.SoundManager.PlaySpellHit(spell, targetArea);
        }
        SpendSpellAP();
        void CheckDead(Actor_Unit hitTarget)
        {
            if (hitTarget.Unit.IsDead)
            {
                State.GameManager.TacticalMode.TacticalStats.RegisterKill(spell, Unit.Side);
                KillUnit(hitTarget, spell);
            }
        }
    }

    internal bool CastStatusSpell(StatusSpell spell, Actor_Unit target, Vec2i targetArea = null, Stat stat = Stat.Intelligence)
    {
        if (spell.IsRetired)
            return false;
        if (Unit.SpendMana(spell.ManaCost) == false && spell.IsFree != true)
            return false;
        CompanyPostBattleTraits.RecordSpellCast(this, false);
        OnSpellCast(spell, target);
        bool hit = false;

        State.GameManager.SoundManager.PlaySpellCast(spell, this);

        if (target != null)
        {
            if (spell.AreaOfEffect > 0)
            {
                hit = true;
                foreach (var splashTarget in TacticalUtilities.UnitsWithinTiles(target.Position, spell.AreaOfEffect).Where(s => s.Unit.IsDead == false))
                {
                    splashTarget.DefendStatusSpell(spell, this);
                }
                State.GameManager.SoundManager.PlaySpellHit(spell, target.UnitSprite.transform.position);
            }
            else
            {
                if (target.DefendStatusSpell(spell, this))
                {
                    hit = true;
                    State.GameManager.SoundManager.PlaySpellHit(spell, target.UnitSprite.transform.position);
                }
            }
        }
        else if (targetArea != null && spell.AreaOfEffect > 0)
        {
            foreach (var splashTarget in TacticalUtilities.UnitsWithinTiles(targetArea, spell.AreaOfEffect).Where(s => s.Unit.IsDead == false))
            {
                hit = true;
                splashTarget.DefendStatusSpell(spell, this);
            }
            State.GameManager.SoundManager.PlaySpellHit(spell, targetArea);
        }

        SpendSpellAP();

        return hit;
    }

    internal void ShareTrait(Traits trait,Traits maxTrait = Traits.Infiltrator)
    {
        if (trait < maxTrait && !TraitsMethods.IsRaceModifying(trait) && !CompanyPostBattleTraits.IsInborn(trait))
        {
            if (!Unit.HasTrait(trait))
                Unit.AddSharedTrait(trait);
        }
    }

    public bool ChangeRaceAny(Unit template, bool permanent, bool isPrey)
    {
        if (template == null || !SpeciesPolicy.IsAllowed(template.Race, SpeciesUse.Transformation))
            return false;
        if (Unit.HiddenRace == Unit.Race)
        {
            TacticalGraphicalEffects.CreateSmokeCloud(Position, Unit.GetScale() / 2);
            Unit.HideRace(template.Race, template);
            foreach (Traits trait in template.GetTraits)
            {
                if ((!Unit.HasTrait(trait) || Unit.HasSharedTrait(trait)) && !TraitsMethods.IsRaceModifying(trait) && !CompanyPostBattleTraits.IsInborn(trait))
                    if (permanent)
                        Unit.AddPermanentTrait(trait);
                    else
                        ShareTrait(trait, TraitsMethods.LastTrait());
            }
            AnimationController = new AnimationController();
            Unit.ReloadTraits();
            Unit.InitializeTraits();
            ReloadSpellTraits();
            State.GameManager.TacticalMode.Log.RegisterMiscellaneous($"{Unit.Name} shifted form to resemble {template.Name}");
            Unit.FixedSide = Unit.Side;
            Unit.Side = template.Side;
            Unit.hiddenFixedSide = true;
            PredatorComponent?.UpdateFullness();
            return true;
        }
        return false;
    }

    public void OnSpellCast(Spell spell, Actor_Unit target)
    {
        EquipmentFunctions.CheckEquipment(Unit, EquipmentActivator.OnSpellCast, new object[] { this, target, spell });

        if (Unit.HasTrait(Traits.Multifaceted) && Unit.IsHighestStat(Stat.Intelligence))
        {
            if (Unit.IsEnemyOfSide(target.Unit.Side))
            {
                target.Unit.ApplyStatusEffect(StatusEffectType.Marked, Unit.GetStat(Stat.Intelligence) / 10, 2);
            }
            else
            {
                target.Unit.RestoreBarrier(Unit.GetStat(Stat.Intelligence) / 10);
            }
        }
    }

    public void ChangeRacePrey()
    {
        if (Unit.HiddenRace == Unit.Race)
        {
            bool isDead = true;
            if (Unit.HasTrait(Traits.GreaterChangeling))
            {
                isDead = false;
            }
            PredatorComponent?.ChangeRaceAuto(isDead,true);
        }
    }

    public void RevertRace()
    {
        if (Unit.HiddenRace != Unit.Race)
        {
            TacticalGraphicalEffects.CreateSmokeCloud(Position, Unit.GetScale() / 2);
            Unit.UnhideRace();
            Unit.SpawnRace = Race.none;
            State.GameManager.TacticalMode.Log.RegisterMiscellaneous($"{Unit.Name} shifted back to normal");
            Unit.Side = Unit.FixedSide;
            Unit.FixedSide = -1;
            Unit.hiddenFixedSide = false;
            PredatorComponent?.ResetTemplate();
            Unit.ResetPersistentSharedTraits();
            AnimationController = new AnimationController();
            Unit.ReloadTraits();
            Unit.InitializeTraits();
            ReloadSpellTraits();
            PredatorComponent?.UpdateFullness();
        }
    }
    
    internal void AddCorruption(int amount, int side)
    {
        // Retired: combat cannot transfer character allegiance.
    }

    internal void AddPossession(Actor_Unit possessor)
    {
        // Retired: combat cannot transfer character allegiance.
    }

    internal void RemovePossession(Actor_Unit possessor)
    {
        // Retired: combat cannot transfer character allegiance.
    }

    internal bool CheckPossession(Actor_Unit possessor)
    {
        return false;
    }

    internal void Shapeshift(Unit shape)
    {
        if (Unit == shape)
            return;
        TacticalGraphicalEffects.CreateSmokeCloud(Position, Unit.GetScale()/2);
        Unit.UpdateShapeExpAndItems();
        MiscUtilities.DelayedInvoke(() =>
        {
            shape.ShifterShapes = Unit.ShifterShapes;
            shape.Side = Unit.Side;
            Unit = shape;
            Unit.hiddenFixedSide = false;

            UnitSprite.UpdateSprites(this, true);
            AnimationController = new AnimationController();
            Unit.ReloadTraits();
            Unit.InitializeTraits();
            Unit.Health = (int)Math.Round(Math.Min(Unit.MaxHealth, Math.Max(Unit.MaxHealth * Unit.HealthPct, 1)));
        }, 0.4f);
        
    }
}
