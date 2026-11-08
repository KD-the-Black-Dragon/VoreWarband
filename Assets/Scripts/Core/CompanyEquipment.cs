// Added by the VoreWar mercenary-company conversion fork.
// 2026-11-08: Use generic tiered weapon names and assign heavy art to the strongest half of each active weapon class.
// 2026-11-08: Assign explicit shared artwork families and preserve a retired dagger art option.
// 2026-11-08: Standardize new ranged weapons to four tiered choices with shared range and accuracy.
// 2026-11-08: Lower shop ranged weapon damage while preserving each weapon's range and accuracy trade-off.
// 2026-11-08: Rebalance weapon tiers against full armor sets and update armor bonuses and names.
// 2026-11-06: Add reputation-tiered utility and high-level spellbooks (spec 40C).
// 2026-11-05: Make basic protection affordable while preserving premium investment and combat values (spec 38 phase B).
// 2026-11-05: Calibrate light armor evasion from paired native battle measurements (spec 38 phase A).
// 2026-11-01: Nine-slot company catalog, stable gear ownership and safe stat changes (spec 08).
// 2026-11-04: Armor deflection/evasion, item comparisons and merchant categories (spec 34 slice 4).
// 2026-11-04: Three reputation-unlocked gear tiers, investment prices and recruitment valuation (spec 34 slice 5).
// 2026-11-08: Retire evasion-granting armor from shops and new recruit generation while preserving saved items.
// 2026-11-01: Define enemy-only weak slug ranged gear without adding it to company shops (spec 09).
// 2026-11-01: Provide safe body-part armor protection for both company and original Pure Tactical inventories (spec 11).
// See CHANGES.md for the fork change record and License.txt for GPL-3.0 terms.
// 2026-11-03: Implement four active stats and agreed combat rules (spec 29).
using System;
using System.Collections.Generic;
using System.Linq;

internal enum CompanyEmployment { Candidate, Employed, Former, Unavailable }
internal enum CompanySlot { Helmet, Torso, Legs, Arms, Accessory, Melee, Ranged, Spell1, Spell2 }
internal static class CompanyEquipment
{
    internal sealed class Definition
    {
        public string Id, Name;
        public CompanySlot Slot;
        public int Cost, Protection, ArmorClass, Tier = 1, Graphic = -1;
        public float Evasion;
        public bool EnemyOnly, Retired;
        public Func<Item> Create;
    }
    // Whitelist intentionally omits potions, healing equipment and retired ability grants.
    internal static readonly Definition[] Catalog = BuildCatalog().ToArray();
    static CompanyEquipment()
    {
        AssignWeaponArtFamilies();
    }
    static IEnumerable<Definition> BuildCatalog()
    {
        // Stable legacy IDs/stats remain valid for already serialized equipment.
        yield return Weapon("weapon.mace", "Sword", 70, false, 4, 1, 0, graphic: CompanyWeaponArtCatalog.LightMelee);
        yield return Weapon("weapon.axe", "Sword II", 200, false, 18, 1, 2, retired: true, graphic: CompanyWeaponArtCatalog.HeavyMelee);
        yield return Weapon("weapon.bow", "Bow", 90, true, 3, 6, 0, graphic: CompanyWeaponArtCatalog.LightRanged);
        yield return Weapon("weapon.compound", "Bow II", 240, true, 16, 6, 2, graphic: CompanyWeaponArtCatalog.HeavyRanged);
        yield return Armor("armor.helmet", "Helmet", CompanySlot.Helmet, 12, Stat.Agility, 2, 1);
        yield return Armor("armor.torso", "Tunic", CompanySlot.Torso, 25, Stat.Strength, 4, 2);
        yield return Armor("armor.legs", "Greaves", CompanySlot.Legs, 18, Stat.Strength, 2, 0, .06f);
        yield return Armor("armor.arms", "Bracers", CompanySlot.Arms, 14, Stat.Strength, 2, 1);
        yield return Armor("accessory.charm", "Insight charm", CompanySlot.Accessory, 60, Stat.Intelligence, 1);
        yield return new Definition { Id = "natural.slug", Name = "Slug slime", Slot = CompanySlot.Ranged, EnemyOnly = true, Graphic = -1,
            Create = () => new Weapon("Slug slime", "Weak natural ranged attack", 0, -1, 2, 5, 1, true) };
        yield return Book("spell.bolt", "Power Bolt", SpellTypes.PowerBolt, 30);
        yield return Book("spell.valor", "Valor", SpellTypes.Valor, 60);
        yield return Book("spell.shield", "Shield", SpellTypes.Shield, 60);
        yield return Book("spell.mending", "Mending", SpellTypes.Mending, 90);
        yield return Book("spell.predation", "Predation", SpellTypes.Predation, 170, 2);
        yield return Book("spell.cinder", "Cinder Field", SpellTypes.CinderField, 270, 2);
        yield return Book("spell.poison", "Poison", SpellTypes.Poison, 220, 2);
        yield return Book("spell.trance", "Trance", SpellTypes.Trance, 360, 3);
        yield return Book("spell.summon", "Summon", SpellTypes.Summon, 440, 3);
        yield return Book("spell.gatemaw", "Gate Maw", SpellTypes.GateMaw, 600, 3);
        yield return Weapon("weapon.cleaver", "Sword I", 100, false, 11, 1, graphic: CompanyWeaponArtCatalog.LightMelee);
        yield return Weapon("weapon.hunting", "Bow I", 120, true, 9, 6, graphic: CompanyWeaponArtCatalog.LightRanged);
        yield return Weapon("weapon.mace.2", "Sword II", 180, false, 18, 1, 2, 1.4f, graphic: CompanyWeaponArtCatalog.HeavyMelee);
        yield return Weapon("weapon.bow.2", "Bow II", 230, true, 16, 6, 2, 1.4f, true, CompanyWeaponArtCatalog.HeavyRanged);
        yield return Weapon("weapon.warbow.2", "Bow II", 260, true, 16, 6, 2, 1, true, CompanyWeaponArtCatalog.HeavyRanged);
        yield return Weapon("weapon.mace.3", "Sword III", 750, false, 25, 1, 3, 1.55f, graphic: CompanyWeaponArtCatalog.HeavyMelee);
        yield return Weapon("weapon.axe.3", "Sword III", 900, false, 25, 1, 3, retired: true, graphic: CompanyWeaponArtCatalog.HeavyMelee);
        yield return Weapon("weapon.bow.3", "Bow III", 850, true, 23, 6, 3, graphic: CompanyWeaponArtCatalog.HeavyRanged);
        yield return Weapon("weapon.warbow.3", "Bow III", 1000, true, 23, 6, 3, 1, true, CompanyWeaponArtCatalog.HeavyRanged);
        // Kept as a non-purchasable catalog option for the alternate light-melee artwork.
        yield return Weapon("weapon.dagger", "Sword", 55, false, 3, 1, 0, 1, true, CompanyWeaponArtCatalog.AlternateLightMelee);
        var labels = new[] { "Helmet", "Tunic", "Greaves", "Bracers" };
        var keys = new[] { "helmet", "torso", "legs", "arms" };
        var heavyPrices = new[] { new[] { 12, 25, 18, 14 }, new[] { 120, 230, 140, 140 }, new[] { 450, 900, 550, 550 } };
        var lightPrices = new[] { new[] { 14, 24, 18, 12 }, new[] { 150, 240, 190, 130 }, new[] { 550, 950, 750, 500 } };
        // Retained only so existing saved evasion armor can still be resolved; new availability filters it out.
        var evasions = new[] { new[] { .03f, .04f, .06f, .03f }, new[] { .06f, .08f, .08f, .06f }, new[] { .08f, .10f, .14f, .08f } };
        for (int tier = 1; tier <= 3; tier++) for (int slot = 0; slot < 4; slot++)
        {
            string quality = tier == 1 ? "" : tier == 2 ? "Fine " : "Masterwork ";
            var stat = slot == 0 ? Stat.Agility : Stat.Strength;
            int bonus = slot == 1 ? 2 * tier + 2 : 2 * tier;
            // Tier-I helmet/tunic/bracers use their stable legacy IDs; greaves use a new non-evasion ID.
            if (tier > 1 || slot == 2)
                yield return Armor("armor." + keys[slot] + ".heavy." + tier, quality + labels[slot],
                    (CompanySlot)slot, heavyPrices[tier - 1][slot], stat, bonus, (slot == 1 ? 2 : 1) * tier, 0, tier);
            if (tier > 1 || slot != 2)
                yield return Armor("armor." + keys[slot] + ".light." + tier, quality + "Light " + labels[slot].ToLowerInvariant(),
                    (CompanySlot)slot, lightPrices[tier - 1][slot], stat, slot == 2 ? 1 : 0, 0, evasions[tier - 1][slot], tier);
        }
        foreach (var stat in WarbandStats.Active) for (int tier = 1; tier <= 3; tier++)
        {
            if (stat == Stat.Intelligence && tier == 1) continue;
            string quality = tier == 1 ? "" : tier == 2 ? "Fine " : "Masterwork ";
            yield return Armor("accessory." + WarbandStats.ShortName(stat).ToLowerInvariant() + "." + tier,
                quality + WarbandStats.Name(stat) + " charm", CompanySlot.Accessory, new[] { 60, 220, 800 }[tier - 1], stat, new[] { 1, 2, 4 }[tier - 1], tier: tier);
        }
    }
    static ItemRepository repository;
    internal static ItemRepository Repository { get { return repository ?? (repository = new ItemRepository()); } }
    static Definition Weapon(string id, string name, int cost, bool ranged, int damage, int range, int tier = 1, float accuracy = 1, bool retired = false, int graphic = -1)
    {
        var definition = new Definition { Id = id, Name = name, Cost = cost, Tier = tier, Retired = retired,
            Slot = ranged ? CompanySlot.Ranged : CompanySlot.Melee, Graphic = graphic };
        definition.Create = () => new Weapon(definition.Name, "Company weapon", cost, definition.Graphic, damage, range,
            id == "weapon.mace" ? 1.25f : accuracy);
        return definition;
    }
    static void AssignWeaponArtFamilies()
    {
        foreach (CompanySlot slot in new[] { CompanySlot.Melee, CompanySlot.Ranged })
        {
            // Range is fixed within each class, so damage times accuracy rating is a stable power proxy.
            var active = Catalog.Where(d => d.Slot == slot && !d.EnemyOnly && !d.Retired && d.Graphic >= 0)
                .OrderBy(d => WeaponPowerRank(d)).ThenBy(d => d.Tier).ThenBy(d => d.Cost)
                .ThenBy(d => d.Id, StringComparer.Ordinal).ToArray();
            // Integer division deliberately rounds the heavy-art share down.
            int heavyCount = active.Length / 2;
            var heavy = active.Skip(active.Length - heavyCount).ToArray();
            int lightGraphic = slot == CompanySlot.Melee ? CompanyWeaponArtCatalog.LightMelee : CompanyWeaponArtCatalog.LightRanged;
            int heavyGraphic = slot == CompanySlot.Melee ? CompanyWeaponArtCatalog.HeavyMelee : CompanyWeaponArtCatalog.HeavyRanged;

            foreach (var definition in Catalog.Where(d => d.Slot == slot && !d.EnemyOnly && d.Graphic >= 0))
            {
                if (definition.Id == "weapon.dagger") continue;
                var assignedWeapon = active.FirstOrDefault(d => d.Id == definition.Id) ?? active.FirstOrDefault(d => d.Tier == definition.Tier);
                definition.Graphic = assignedWeapon != null && heavy.Contains(assignedWeapon) ? heavyGraphic : lightGraphic;
            }
        }
    }
    static float WeaponPowerRank(Definition definition)
    {
        var weapon = definition.Create() as Weapon;
        return weapon == null ? 0 : weapon.Damage * weapon.AccuracyModifier;
    }
    internal static void BindCompanyItem(Item item)
    {
        if (item == null) return;
        var definition = Find(item.CompanyDefinitionId);
        if (definition == null) return;
        item.SetCompanyDisplayName(definition.Name);
        var weapon = item as Weapon;
        if (weapon != null && definition.Graphic >= 0) weapon.SetCompanyGraphic(definition.Graphic);
    }
    static Definition Armor(string id, string name, CompanySlot slot, int cost, Stat stat, int bonus, int armorClass = 0, float evasion = 0, int tier = 1)
    { return new Definition { Id = id, Name = name, Cost = cost, Tier = tier, Slot = slot, Protection = slot == CompanySlot.Accessory ? 0 : 1, ArmorClass = armorClass, Evasion = evasion,
        Create = () => new Accessory(name, bonus == 0 ? "Armor" : "+" + bonus + " " + WarbandStats.ShortName(stat), cost, (int)stat, bonus) }; }
    static Definition Book(string id, string name, SpellTypes spell, int cost, int tier = 1)
    { return new Definition { Id = id, Name = name, Slot = CompanySlot.Spell1, Cost = cost, Tier = tier,
        Create = () => new SpellBook(name, "Company spell", cost, tier, spell) }; }
    internal static Definition Find(string id) { return Catalog.FirstOrDefault(d => d.Id == id); }
    // Keep old definitions resolvable for saved characters and inventories, but do not offer them to new campaigns.
    internal static bool IsRetiredEvasionArmor(Definition d)
    { return (int)d.Slot <= (int)CompanySlot.Arms && d.Evasion > 0; }
    internal static int RequiredReputation(int tier) { return new[] { 0, 10, 25 }[Math.Max(1, Math.Min(3, tier)) - 1]; }
    internal static int UnlockedTier(int reputation) { return reputation >= RequiredReputation(3) ? 3 : reputation >= RequiredReputation(2) ? 2 : 1; }
    internal static IEnumerable<Definition> Available(CompanyCampaign state)
    { return Catalog.Where(d => !d.EnemyOnly && !d.Retired && !IsRetiredEvasionArmor(d) && state.Reputation >= RequiredReputation(d.Tier)).OrderBy(d => d.Tier).ThenBy(d => d.Cost).ThenBy(d => d.Id, StringComparer.Ordinal); }
    internal static int Value(Item item) { var d = item == null ? null : Find(item.CompanyDefinitionId); return d == null || d.EnemyOnly ? 0 : d.Cost; }
    internal static int GearPrice(Unit unit) { return checked(unit.Items.Where(item => item != null).Sum(Value)); }
    internal static Definition Generated(CompanyCampaign state, CompanySlot slot, int level)
    {
        int tier = level >= 10 ? 3 : level >= 5 ? 2 : 1;
        var choices = Catalog.Where(d => !d.EnemyOnly && !d.Retired && !IsRetiredEvasionArmor(d) && d.Slot == slot && d.Tier == tier).ToArray();
        return choices[state.Next(choices.Length)];
    }
    internal static string SlotName(CompanySlot slot)
    { return new[] { "Helmet", "Tunic", "Greaves", "Bracers", "Accessory", "Melee Weapon", "Ranged Weapon", "Spell 1", "Spell 2" }[(int)slot]; }
    internal static string Summary(Item item)
    {
        if (item == null) return "Empty";
        var weapon = item as Weapon;
        if (weapon != null) return "Dmg " + weapon.Damage + " | Range " + weapon.Range + " | Accuracy rating " + weapon.AccuracyModifier.ToString("0.##") + "x";
        var d = Find(item.CompanyDefinitionId);
        if (d != null && item is Accessory) return item.Description + (d.ArmorClass > 0 ? " | AC +" + d.ArmorClass : "") +
            (d.Evasion > 0 ? " | Evasion +" + (d.Evasion * 100).ToString("0") + "%" : "");
        var book = item as SpellBook;
        if (book != null)
        {
            Spell spell;
            if (SpellList.SpellDict.TryGetValue(book.ContainedSpell, out spell))
                return spell.Description + " | Mana " + spell.ManaCost + " | Range " + spell.Range.Min + "–" + spell.Range.Max;
        }
        return item.Description;
    }
    static IEnumerable<Definition> WornArmor(Unit unit)
    {
        if (unit.Items == null) yield break;
        for (int slot = 0; slot < 4; slot++)
        {
            if (unit.CompanyLoadout)
            {
                var item = slot < unit.Items.Length ? unit.Items[slot] : null;
                var d = item == null ? null : Find(item.CompanyDefinitionId);
                if (d != null && (int)d.Slot == slot) yield return d;
            }
            else if (Protection(unit, (CompanySlot)slot) > 0)
                yield return Catalog.First(d => (int)d.Slot == slot && !d.EnemyOnly);
        }
    }
    internal static int ArmorClass(Unit unit) { return WornArmor(unit).Sum(d => Math.Max(0, d.ArmorClass)); }
    internal static float Evasion(Unit unit) { return Math.Min(1, WornArmor(unit).Sum(d => Math.Max(0, d.Evasion))); }
    internal static int Deflect(Unit unit, int damage)
    {
        int armor = ArmorClass(unit);
        return Math.Max(1, damage - (armor > 0 ? State.Rand.Next(armor + 1) : 0));
    }
    // Exact average of max(1, damage - roll), without consuming combat RNG.
    internal static double ExpectedAfterArmor(int damage, int armor, int barrier = 0)
    {
        damage = Math.Max(1, damage); armor = Math.Max(0, armor); barrier = Math.Max(0, barrier);
        if (barrier == 0)
        {
            int useful = Math.Min(armor, damage - 1);
            return ((double)damage * (useful + 1) - (double)useful * (useful + 1) / 2 + armor - useful) / (armor + 1.0);
        }
        int positive = Math.Min(armor + 1, Math.Max(0, damage - barrier));
        return ((double)(damage - barrier) * positive - (double)positive * (positive - 1) / 2) / (armor + 1.0);
    }
    internal static Item Create(string id)
    {
        var d = Find(id); if (d == null) throw new InvalidOperationException("Unknown company gear definition.");
        Item item = d.Create(); item.CompanyDefinitionId = id; item.CompanyInstanceId = Guid.NewGuid().ToString("N"); return item;
    }
    internal static bool CanPlace(Item item, int slot)
    {
        if (slot < 0 || slot >= 9) return false;
        if (item == null) return true;
        var d = Find(item.CompanyDefinitionId);
        return d != null && ((int)d.Slot == slot || (d.Slot == CompanySlot.Spell1 && slot == (int)CompanySlot.Spell2));
    }
    internal static int Protection(Unit unit, CompanySlot slot)
    {
        if (unit.Items == null) return 0;
        if (unit.CompanyLoadout)
        {
            var item = (int)slot < unit.Items.Length ? unit.Items[(int)slot] : null;
            var d = item == null ? null : Find(item.CompanyDefinitionId);
            return d == null ? 0 : d.Protection;
        }
        // Pure Tactical retains the original two-item inventory; do not index it as nine slots.
        return unit.Items.Any(i => i != null && (slot == CompanySlot.Helmet ? i.Name == "Helmet" :
            slot == CompanySlot.Torso ? i.Name == "Body Armor" : slot == CompanySlot.Legs ? i.Name == "Shoes" :
            slot == CompanySlot.Arms && (i.Name == "Gauntlet" || i.Name == "Gloves"))) ? 1 : 0;
    }
    internal static int Power(Item item)
    { var d = item == null ? null : Find(item.CompanyDefinitionId); return d == null ? 0 : item is Weapon ? ((Weapon)item).Damage * (((Weapon)item).Range > 1 ? 2 : 1) : item is Accessory ? ((Accessory)item).StatBonus * 2 + d.Protection : 6; } // Provisional combat input for09, independent of price.
    internal static void Equip(CompanyCampaign state, CompanyCharacter who, Item item, int slot)
    {
        if (state.InBattle || state.UpdatingDay || !state.Members.Any(m => m.CharacterId == who.Id)) throw new InvalidOperationException("Equipment can only change outside battle for current members.");
        if (!CanPlace(item, slot) || (item != null && !state.Inventory.Contains(item))) throw new InvalidOperationException("Invalid equipment selection.");
        if (item != null && state.Characters.Any(c => c.Unit.Items.Contains(item))) throw new InvalidOperationException("That item is already equipped.");
        var unit = who.Unit; Item previous = unit.Items[slot]; int hp = unit.Health, maximum = unit.MaxHealth;
        int uses = unit.ItemUses[slot], cooldown = unit.ItemCooldowns[slot];
        unit.SetItem(item, slot);
        int newHealth = hp + unit.MaxHealth - maximum;
        if (newHealth < 1)
        {
            unit.SetItem(previous, slot); unit.Health = hp; unit.ItemUses[slot] = uses; unit.ItemCooldowns[slot] = cooldown;
            throw new InvalidOperationException("Recover more health before removing this HP-granting item.");
        }
        unit.Health = Math.Min(unit.MaxHealth, newHealth);
        unit.UpdateSpells();
    }
    internal static void Buy(CompanyCampaign state, string id)
    {
        if (state.InBattle || state.Busy || !state.Towns.Any(t => t.Tile == state.Position)) throw new InvalidOperationException("Visit a town to shop.");
        var d = Find(id); if (d == null || !Available(state).Contains(d)) throw new InvalidOperationException("That equipment is not available at the company's reputation.");
        if (state.Gold < d.Cost) throw new InvalidOperationException("Not enough gold.");
        Item item = Create(id); state.Inventory.Add(item); state.Gold -= d.Cost;
    }
    internal static void Sell(CompanyCampaign state, Item item)
    {
        if (state.InBattle || state.Busy || !state.Towns.Any(t => t.Tile == state.Position) || !state.Inventory.Contains(item) || state.Characters.Any(c => c.Unit.Items.Contains(item)))
            throw new InvalidOperationException("Only unequipped company items can be sold.");
        var d = Find(item.CompanyDefinitionId); if (d == null) throw new InvalidOperationException("Unknown gear.");
        state.Inventory.Remove(item); state.Gold += d.Cost / 2; state.TotalGoldEarned += d.Cost / 2;
    }
}
