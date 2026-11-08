// Modified for the VoreWar mercenary-company conversion.
// 2026-11-06: Resolve per-character frozen species metadata (spec 40A).
// 2026-11-05: Remove Flight from every species baseline at the user's direction.
// 2026-11-01: Resolve species metadata directly without constructing a randomized dummy Unit (spec 05A7).
// 2026-11-01: Retire all 90 rejected stat prototypes and reject retired metadata lookups (spec 03B3D).
// 2026-11-01: Provisional approved species specialties and weak SpitterSlug support profile (spec 07).
// 2026-11-03: Remove permanently retired identity branches/entries while preserving shared behavior (spec 21).
// See CHANGES.md for the fork change record and License.txt for GPL-3.0 terms.
// 2026-11-03: Implement four active stats and agreed combat rules (spec 29).
using OdinSerializer;
using System;
using System.Collections.Generic;

static class RaceParameters
{
    static readonly RaceTraits Default;
    static readonly RaceTraits Bunnies;
    static readonly RaceTraits Lizards;
    static readonly RaceTraits Kangaroos;
    static readonly RaceTraits Equines;
    static readonly RaceTraits Sergal;
    static readonly RaceTraits Bees;
    static readonly RaceTraits Demibats;
    static readonly RaceTraits Panthers;
    static readonly RaceTraits Avians;
    static readonly RaceTraits Demiants;
    static readonly RaceTraits Demifrogs;
    static readonly RaceTraits Demisharks;
    static readonly RaceTraits Deer;
    static readonly RaceTraits Mice;
    static readonly RaceTraits Gnolls;
    static readonly RaceTraits Alligators;
    static readonly RaceTraits Kobolds;
    static readonly RaceTraits Hippos;
    static readonly RaceTraits Komodos;
    static readonly RaceTraits Vargul;
    static readonly RaceTraits Serpents;
    static readonly RaceTraits Wyvern;
    static readonly RaceTraits FeralWolves;
    static readonly RaceTraits Harvesters;
    static readonly RaceTraits Bats;
    static readonly RaceTraits Dragons;
    static readonly RaceTraits Dragonfly;
    static readonly RaceTraits Plants;
    static readonly RaceTraits FeralAnts;
    static readonly RaceTraits Gryphons;
    static readonly RaceTraits SpitterSlugs;
    static readonly RaceTraits SpringSlugs;
    static readonly RaceTraits RockSlugs;
    static readonly RaceTraits CoralSlugs;
    static readonly RaceTraits Salamanders;
    static readonly RaceTraits Mantis;
    static readonly RaceTraits EasternDragon;
    static readonly RaceTraits Catfish;
    static readonly RaceTraits WarriorAnts;
    static readonly RaceTraits Gazelle;
    static readonly RaceTraits FeralLions;
    static readonly RaceTraits FeralLizards;
    static readonly RaceTraits Monitors;
    static readonly RaceTraits Terrorbirds;
    static readonly RaceTraits Dratopyr;
    static readonly RaceTraits Goodra;
    static readonly RaceTraits FeralHorses;
    static readonly RaceTraits MatronsMinions;
    static readonly RaceTraits WyvernMatron;
    static readonly RaceTraits FeralFox;
    static readonly RaceTraits Terminid;
    static readonly RaceTraits FeralSlime;
    static readonly RaceTraits FeralUmbreon;
    static readonly RaceTraits Lupine;
    static readonly RaceTraits Jackals;
    static readonly RaceTraits Otachi;
    static readonly RaceTraits Raiju;
    static readonly RaceTraits Smudger;
    static readonly RaceTraits SpaceCroach;
    static readonly RaceTraits Trex;
    static readonly RaceTraits Utahraptor;
    static readonly RaceTraits BlackWidow;
    static readonly RaceTraits Badgers;
    static readonly RaceTraits Draconians;

    // Preserve the legacy disabled-traits/null-unit result, but never bypass retirement.
    internal static RaceTraits GetTraitData(Unit unit)
    {
        if (unit == null && Config.RaceTraitsEnabled == false)
            return Default;
        return unit.SpeciesBalance != null && unit.SpeciesBalance.Species == unit.Race ? unit.SpeciesBalance.Metadata() : GetRaceTraits(unit.Race);
    }

    // Metadata reads must not construct units, roll stats or consume gameplay RNG.
    internal static RaceTraits GetRaceTraits(Race race)
    {
        // Retired identities remain serialized IDs, not requests for substitute stats.
        if (SpeciesPolicy.IsRetired(race))
            throw new System.InvalidOperationException("Species metadata has been retired: " + race);
        if (Config.RaceTraitsEnabled == false)
        {
            return Default;
        }
        switch (race)
        {
            case Race.Bunnies:
                return Bunnies;
            case Race.Lizards:
                return Lizards;
            case Race.Kangaroos:
                return Kangaroos;
            case Race.Equines:
                return Equines;
            case Race.Sergal:
                return Sergal;
            case Race.Bees:
                return Bees;
            case Race.Bats:
                return Demibats;
            case Race.Avians:
                return Avians;
            case Race.Ants:
                return Demiants;
            case Race.Frogs:
                return Demifrogs;
            case Race.Sharks:
                return Demisharks;
            case Race.Deer:
                return Deer;
            case Race.Mice:
                return Mice;
            case Race.Kobolds:
                return Kobolds;
            case Race.Hippos:
                return Hippos;
            case Race.Komodos:
                return Komodos;
            case Race.Vargul:
                return Vargul;
            case Race.Serpents:
                return Serpents;
            case Race.Wyvern:
                return Wyvern;
            case Race.FeralWolves:
                return FeralWolves;
            case Race.FeralLions:
                return FeralLions;
            case Race.Alligators:
                return Alligators;
            case Race.Harvesters:
                return Harvesters;
            case Race.FeralBats:
                return Bats;
            case Race.Dragon:
                return Dragons;
            case Race.Dragonfly:
                return Dragonfly;
            case Race.TwistedVines:
                return Plants;
            case Race.FeralAnts:
                return FeralAnts;
            case Race.Gryphons:
                return Gryphons;
            case Race.SpitterSlugs:
                return SpitterSlugs;
            case Race.SpringSlugs:
                return SpringSlugs;
            case Race.RockSlugs:
                return RockSlugs;
            case Race.CoralSlugs:
                return CoralSlugs;
            case Race.Salamanders:
                return Salamanders;
            case Race.Mantis:
                return Mantis;
            case Race.EasternDragon:
                return EasternDragon;
            case Race.Catfish:
                return Catfish;
            case Race.WarriorAnts:
                return WarriorAnts;
            case Race.Gazelle:
                return Gazelle;
            case Race.FeralLizards:
                return FeralLizards;
            case Race.Monitors:
                return Monitors;
            case Race.Terrorbird:
                return Terrorbirds;
            case Race.Dratopyr:
                return Dratopyr;
            case Race.Panthers:
                return Panthers;
            case Race.Goodra:
                return Goodra;
            case Race.FeralHorses:
                return FeralHorses;
            case Race.MatronsMinions:
                return MatronsMinions;
            case Race.WyvernMatron:
                return WyvernMatron;
            case Race.FeralFox:
                return FeralFox;
            case Race.Terminid:
                return Terminid;
            case Race.Gnolls:
                return Gnolls;
            case Race.FeralSlime:
                return FeralSlime;
            case Race.FeralUmbreon:
                return FeralUmbreon;
            case Race.Lupine:
                return Lupine;
            case Race.Jackals:
                return Jackals;
            case Race.Otachi:
                return Otachi;
            case Race.Raiju:
                return Raiju;
            case Race.Smudger:
                return Smudger;
            case Race.SpaceCroach:
                return SpaceCroach;
            case Race.Draconians:
                return Draconians;
            case Race.Trex:
                return Trex;
            case Race.Utahraptor:
                return Utahraptor;
            case Race.BlackWidow: 
                return BlackWidow;
            case Race.Badgers:
                return Badgers;
            case (Race)700: //Singled out so that it doesn't make the debug message
                return Default;
            case (Race)701:
                return Default;
            default:
                UnityEngine.Debug.Log("Couldn't find race, substituting the Default");
                return Default;
        }
    }

    static RaceParameters()
    {
        Default = new RaceTraits()
        {
            BodySize = 10,
            StomachSize = 12,
            HasTail = false,
            FavoredStat = Stat.None,
            RacialTraits = new List<Traits>(),
            RaceDescription = "Whip, whup, brrr. I'm a Default. Phear me!",
        };



        Bunnies = new RaceTraits()
        {
            BodySize = 12,
            StomachSize = 15,
            HasTail = true,
            FavoredStat = Stat.Dexterity,
            DeployCost = 1,
            Upkeep = 2f,
            RacialTraits = new List<Traits>()
        {
            Traits.ProlificBreeder,
            Traits.EasyToVore,
            Traits.ArtfulDodge,
            Traits.EvasiveBattler
        },
            RaceDescription = "Among the weaker but more numerous of the native sapient species, the Bunnies are on the verge of turning predators themselves. While lacking in sheer strength they make up for it with agility and numbers, having much fun ensuring the latter.",
        };



        Lizards = new RaceTraits()
        {
            BodySize = 12,
            StomachSize = 20,
            HasTail = true,
            FavoredStat = Stat.Mind,
            RaceStats = new RaceStats { Mind = new RaceStats.StatRange(10, 17), Will = new RaceStats.StatRange(9, 16) },
            DeployCost = 1,
			PowerAdjustment = 1.2f,
            Upkeep = 4f,
            RacialTraits = new List<Traits>()
        {
            Traits.Resilient,
            Traits.Intimidating,
            Traits.GiantSlayer,
            Traits.NaturalCaster,
        },

            RaceDescription = "Sapient people with a natural affinity for magic.",
        };



        Kangaroos = new RaceTraits()
        {
            BodySize = 12,
            StomachSize = 20,
            HasTail = true,
            FavoredStat = Stat.Agility,
            DeployCost = 1,
            Upkeep = 3f,
            RacialTraits = new List<Traits>()
        {
            Traits.BornToMove,
            Traits.Resourceful,
        },
            RaceDescription = "Their old home turning ever drier and hotter, the Kangaroo tribes did not hesitate when mysterious portals opened and granted them passage to greener lands. Nomadic by nature, the Kangaroos are very adept at carrying plenty of gear with them and aren't unused to traveling with a full belly either.",
        };



        Equines = new RaceTraits()
        {
            BodySize = 15,
            StomachSize = 20,
            HasTail = true,
            FavoredStat = Stat.Strength,
            DeployCost = 1,
            Upkeep = 4f,
            RacialTraits = new List<Traits>()
        {
                Traits.Charge,
                Traits.StrongMelee,
                Traits.RangedIneptitude,
        },
            RaceDescription = "Mighty horsefolk from another world, the Equines are a proud people, and while from the outside they can seem to move and act with remarkable unity, within their ranks is a game of constant power struggles and jockeying for position and influence.",
        };

        Sergal = new RaceTraits()
        {
            BodySize = 12,
            StomachSize = 15,
            HasTail = true,
            FavoredStat = Stat.Agility,
            DeployCost = 1,
            Upkeep = 4f,
			PowerAdjustment = 1.2f,
            RacialTraits = new List<Traits>()
        {
                Traits.KeenReflexes,
                Traits.StrongMelee,
                Traits.EscapeArtist
        },
            RaceDescription = "When the portal appeared over Lake ColVilous in the year RC102, the city of Gold Ring quickly fell into a panic.  Nothing hostile came from the rift, however, and that initial panic changed into curiosity.  After the progressive minds determined that it was a gateway to another world, and a world full of green plants at that, the only thing to do was form a Scouting Regiment to secure a foothold and see what might be gained from this mysterious place.  But just as that first settlement was established, the portal blinked out of existence, cutting off passage between Tal and this new world.  The changes began a mere week into isolation: People became more violent and sexually aggressive, with physical changes to match the mental ones shortly afterward.  And then... Then the hunger set in.",
        };

        Bees = new RaceTraits()
        {
            BodySize = 12,
            StomachSize = 15,
            HasTail = true,
            FavoredStat = Stat.Agility,
            DeployCost = 1,
            Upkeep = 4f,
			PowerAdjustment = 1.2f,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral, VoreType.Unbirth, VoreType.CockVore, VoreType.BreastVore, VoreType.Anal, VoreType.TailVore, VoreType.BladderVore },
            RacialTraits = new List<Traits>()
        {
                Traits.KeenReflexes,
                Traits.PackDefense,
                Traits.Stinger
        },
            RaceDescription = "The Bees exist in a sort of loose hive mind, and tend to care little for the loss of individual members of their kind. They tend to be ruthless when it comes to the defence of what they already own, though they tend to only strike outwards when they feel that there is something to be gained by doing so. Or when they're hungry. At their size, the typical diet of smaller bees, pollen, nectar, and honey, just doesn't quite cut it.",
        };



        Demibats = new RaceTraits()
        {
            BodySize = 8,
            StomachSize = 15,
            HasTail = false,
            FavoredStat = Stat.Agility,
            DeployCost = 1,
            Upkeep = 3f,
            RacialTraits = new List<Traits>()
        {
                Traits.ArtfulDodge,
                Traits.Vampirism
        },
            RaceDescription = "Winged half-humans, the Bats generally prefer to live in caves, or, failing that, stick to being mostly active at night. Of the so called \"civilized\" races, the bats are one of the more aggressive towards outsiders, their habit of near constant flight while landing less than they should demanding lots of nutrients to support, resulting in them being far more eager to devour their foes than most.",
        };

        Panthers = new RaceTraits()
        {
            BodySize = 12,
            StomachSize = 15,
            HasTail = true,
            FavoredStat = Stat.Strength,
            DeployCost = 1,
            Upkeep = 4f,
			PowerAdjustment = 1.2f,
            RacialTraits = new List<Traits>()
        {
                Traits.TasteForBlood,
                Traits.Pounce,
                Traits.Frenzy,
        },
            RaceDescription = "Long before \"elder races\" walked among the stars, the Panthers thrived. Long before first words of power were uttered, they have carved their homes into the lightning-struck bark, feeding off its power. And long after the last bastion of so-called civilization will fall to onslaught of wings, claws and fangs, they will thrive in the darkness, pouncing on unsuspecting prey.",
        };


        Avians = new RaceTraits()
        {
            BodySize = 12,
            StomachSize = 15,
            HasTail = true,
            FavoredStat = Stat.Dexterity,
            RaceStats = new RaceStats { Dexterity = new RaceStats.StatRange(9, 17) },
            DeployCost = 1,
            Upkeep = 4f,
			PowerAdjustment = 1.2f,
            RacialTraits = new List<Traits>()
        {
                Traits.KeenShot,
                Traits.Featherweight,
        },
            RaceDescription = "Sapient people with a natural affinity for ranged combat.",
        };

        Demiants = new RaceTraits()
        {
            BodySize = 12,
            StomachSize = 15,
            HasTail = false,
            FavoredStat = Stat.Strength,
            DeployCost = 1,
            Upkeep = 4f,
			PowerAdjustment = 1.2f,
            RacialTraits = new List<Traits>()
        {
                Traits.PackStrength,
                Traits.RangedIneptitude,
                Traits.AntPheromones
        },
            RaceDescription = "The Ants exist in a sort of loose hive mind, and tend to care little for the loss of individual members of their swarms. Ants have an innate and insatiable drive to expand their territory and conquer all there is. Ants often control lesser ant species through complex pheromones, which are also generally used for ant-to-ant communication.",
        };

        Demifrogs = new RaceTraits()
        {
            BodySize = 12,
            StomachSize = 20,
            HasTail = false,
            FavoredStat = Stat.Voracity,
            DeployCost = 1,
            Upkeep = 3f,
            RacialTraits = new List<Traits>()
        {
                Traits.Pounce,
                Traits.HeavyPounce,
                Traits.RangedVore,
                Traits.Clumsy,
        },
            RaceDescription = "Native to the marshlands of this world, Frogs are well known for their highly territorial behavior, with Frog chiefdoms going to war with each other over perceived slights just as often as they go to war with others. Their wide mouths and big bellies aren't just for show, and many an unfortunate soul have found themselves on the wrong side of a Frog's flesh. Namely, the inside.",
        };

        Demisharks = new RaceTraits()
        {
            BodySize = 12,
            StomachSize = 15,
            HasTail = true,
            FavoredStat = Stat.Voracity,
            DeployCost = 1,
            Upkeep = 4f,
			PowerAdjustment = 1.2f,
            RacialTraits = new List<Traits>()
        {
                Traits.Biter,
                Traits.SenseWeakness,
                Traits.StrongGullet,
                Traits.WaterWalker,
        },
            RaceDescription = "The Sharks are not natives of this world. In their own world, they are the only sapient people, and had a wide variety of cultures and lifestyles. However, all Sharks in this world came through when a pirate ship in their realm accidently went through a portal to this one. Even today, most Sharks are pirates, and a majority of pirates are Sharks.",
        };

        Deer = new RaceTraits()
        {
            BodySize = 12,
            StomachSize = 15,
            HasTail = true,
            FavoredStat = Stat.Agility,
            DeployCost = 1,
            Upkeep = 3f,
            RacialTraits = new List<Traits>()
        {
                Traits.EvasiveBattler,
                Traits.ArtfulDodge,
                Traits.PackDefense,
        },
            RaceDescription = "The deer are one of the Realm's native peoples, and one with a long history of being labeled as \"prey.\" Whether historical Deer couldn't vore or simply didn't know how to vore is largely unknown, but in recent times anyone who calls a Deer prey in front of them often gets a personal tour of the cervid digestive system.",
        };


        Mice = new RaceTraits()
        {
            BodySize = 8,
            StomachSize = 15,
            HasTail = true,
            FavoredStat = Stat.Agility,
            DeployCost = 1,
            Upkeep = 2f,
            RacialTraits = new List<Traits>()
        {
                Traits.ProlificBreeder,
                Traits.Timid,
                Traits.SwiftStrike,
                Traits.Clever,
        },
            RaceDescription = "Originally ordinary lab mice, tucked away in a secret laboratory, the gift of sentience and a heightened sense of intellect was suddenly bestowed upon them. No mouse knows the identity of this mysterious actor or the reason they were given such a boon, but all are grateful for their improved status.",
        };

        Gnolls = new RaceTraits()
        {
            BodySize = 12,
            StomachSize = 15,
            HasTail = true,
            FavoredStat = Stat.Strength,
            RaceStats = new RaceStats { Strength = new RaceStats.StatRange(9, 17) },
            DeployCost = 1,
            Upkeep = 4f,
			PowerAdjustment = 1.2f,
            RacialTraits = new List<Traits>()
            {
                Traits.PackVoracity,
                Traits.SenseWeakness,
                Traits.Biter,
                Traits.TasteForBlood,
            },
            RaceDescription = "Sapient people with a natural affinity for melee combat.",
        };



        Lupine = new RaceTraits()
        {
            BodySize = 12,
            StomachSize = 15,
            HasTail = false,
            FavoredStat = Stat.Strength,
            DeployCost = 1,
            Upkeep = 4f,
			PowerAdjustment = 1.2f,
            RacialTraits = new List<Traits>()
        {
            Traits.PackStrength,
            Traits.Pounce,
            Traits.Biter,
        },
            RaceDescription = "Canine people from another world, the Lupines are a curious people, with unique views on vore and the realm they find themselves trapped within. On the whole, they view vore as highly taboo, but they're also seemingly more prone to becoming addicted to it, resulting in a very strange society.",
        };

        Jackals = new RaceTraits()
        {
            BodySize = 12,
            StomachSize = 15,
            HasTail = false,
            FavoredStat = Stat.Endurance,
            DeployCost = 1,
            Upkeep = 4f,
			PowerAdjustment = 1.2f,
            RacialTraits = new List<Traits>()
        {
            Traits.PackMind,
            Traits.DexterousDefense,
            Traits.Finesse,
        },
            RaceDescription = "A race of desert dwelling canine, in recent times, the constant warfare of the realm has brought them out of their isolation. Where they traditionally stood guard against any fools who sought to plunder their sacred pyramids, now they venture out to practice a doctrine of \"Defense by Offense.\".",
        };


        Draconians = new RaceTraits()
        {
            BodySize = 15,
            StomachSize = 20,
            HasTail = true,
            FavoredStat = Stat.Mind,
            DeployCost = 1,
            Upkeep = 5f,
            RacialTraits = new List<Traits>()
        {
                Traits.NaturalCaster,
                Traits.StrongGullet,
                Traits.Hoarder,
        },
            RaceDescription = "Naturally prideful and greedy, the Draconians historically struggled somewhat with maintaining cohesive societies, however, in recent times they managed to turn the very traits that historically kept them scattered and divided into a force to keep together, directing their greed to build their own kingdoms ever greater at the cost of any others unlucky enough to be nearby. While they respect the more feral dragons of the realm, they generally deny any relation to them, as well as relation to anyone else. Whether this is because they're actually unrelated or just too proud to admit it is, however, unknown.",
        };



        Alligators = new RaceTraits()
        {
            BodySize = 24,
            StomachSize = 24,
            HasTail = true,
            FavoredStat = Stat.Strength,
            CanUseRangedWeapons = false,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral, VoreType.CockVore, VoreType.Unbirth, VoreType.Anal, VoreType.BladderVore },
            ExpMultiplier = 1.25f,
            PowerAdjustment = 1.5f,
            DeployCost = 1,
            Upkeep = 10f,
            RaceStats = new RaceStats() // Stronger, tougher, slower moving and with slower digestion. (Crocodilians would normally have a very strong digestion, but that reguires focusing on it, not going on fighting.)
            // Wider, shorter throats also make eating easier, but also make prey's escape easier. (Not in RL, obviously. Or perhaps they would, if crocodilians had a habit of swallowing sizeable living prey.)
            {
                Strength = new RaceStats.StatRange(16, 22),
                Dexterity = new RaceStats.StatRange(4, 7),
                Endurance = new RaceStats.StatRange(22, 32),
                Mind = new RaceStats.StatRange(5, 10),
                Will = new RaceStats.StatRange(8, 14),
                Agility = new RaceStats.StatRange(6, 10),
                Voracity = new RaceStats.StatRange(13, 19),
                Stomach = new RaceStats.StatRange(8, 14),
            },
            RacialTraits = new List<Traits>() // Alligator = Lizard+
            {
                Traits.Ravenous, // Bonus to voracity before eating
                Traits.Resilient, // Damage decrease
                Traits.Intimidating, // Penalty to enemies in melee range
				Traits.Crusher,
            },
            RaceDescription = "Natives to great swamps on another dimension, the Alligators emerge sporadically from portals across the land. Either unwilling or unable to settle this realm, they instead work as mercenaries for hire. Large, tough and intimidating, they make great bruisers, but seem totally unable to understand the principle of ranged weapons.",
        };



        Kobolds = new RaceTraits()
        {
            BodySize = 8,
            StomachSize = 12,
            HasTail = true,
            FavoredStat = Stat.Agility,
            DeployCost = 1,
            Upkeep = 2f,
			PowerAdjustment = 0.9f,
            RacialTraits = new List<Traits>()
            {
                Traits.ProlificBreeder,
                Traits.EasyToVore,
                Traits.Replaceable,
            },
            RaceDescription = "Typical servants to dragon overlords, they slavishly carry out their desires, even to their own detriments.",
            RaceAI = RaceAI.ServantRace
        };


        Hippos = new RaceTraits()
        {
            BodySize = 24,
            StomachSize = 24,
            HasTail = true,
            FavoredStat = Stat.Endurance,
            PowerAdjustment = 1.3f,
            DeployCost = 1,
            Upkeep = 11f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(22, 28),
                Dexterity = new RaceStats.StatRange(6, 14),
                Endurance = new RaceStats.StatRange(18, 28),
                Mind = new RaceStats.StatRange(6, 12),
                Will = new RaceStats.StatRange(8, 16),
                Agility = new RaceStats.StatRange(8, 14),
                Voracity = new RaceStats.StatRange(12, 20),
                Stomach = new RaceStats.StatRange(12, 18),
            },
            RacialTraits = new List<Traits>()
            {
                Traits.StrongMelee,
                Traits.HardSkin,
                Traits.Crusher,
            },
            RaceDescription = "The Hippos come from the same universe as the Feral Lions, and within that world, they either were or still are the dominant power of that world. Here, they act as mercenaries, believing fully that the portals will someday reopen, and that they'll go home rich.",
        };


        Komodos = new RaceTraits()
        {
            BodySize = 20,
            StomachSize = 20,
            HasTail = true,
            FavoredStat = Stat.Strength,
            CanUseRangedWeapons = false,
            PowerAdjustment = 1.3f,
            DeployCost = 1,
            Upkeep = 5f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(16, 24),
                Dexterity = new RaceStats.StatRange(6, 10),
                Endurance = new RaceStats.StatRange(14, 24),
                Mind = new RaceStats.StatRange(6, 12),
                Will = new RaceStats.StatRange(8, 16),
                Agility = new RaceStats.StatRange(10, 16),
                Voracity = new RaceStats.StatRange(16, 24),
                Stomach = new RaceStats.StatRange(12, 18),
            },
            RacialTraits = new List<Traits>()
            {
                Traits.Biter,
                Traits.VenomousBite,
                Traits.Intimidating,
                Traits.Resilient,
                Traits.Crusher,
            },
            RaceDescription = "While the frequency with which Komodos and Monitors can be found together would lead you to think them related, in truth, they just happen to have very compatible cultures, and enjoy one another's company. Unlike the Monitors, the Komodos are natives of this world, and as such often act as the \"face\" whenever they are forced to do diplomacy with other groups.",
        };


        Vargul = new RaceTraits()
        {
            BodySize = 20,
            StomachSize = 20,
            HasTail = true,
            FavoredStat = Stat.Strength,
            ExpMultiplier = 1.25f,
            PowerAdjustment = 2.5f,
            DeployCost = 1,
            Upkeep = 15f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(14, 20),
                Dexterity = new RaceStats.StatRange(14, 20),
                Endurance = new RaceStats.StatRange(14, 20),
                Mind = new RaceStats.StatRange(8, 16),
                Will = new RaceStats.StatRange(8, 16),
                Agility = new RaceStats.StatRange(14, 20),
                Voracity = new RaceStats.StatRange(14, 20),
                Stomach = new RaceStats.StatRange(12, 18),
            },
            RacialTraits = new List<Traits>()
            {
                Traits.Intimidating,
                Traits.SenseWeakness,
                Traits.StrongGullet,
                Traits.Berserk,
                Traits.Maul,
            },
            RaceDescription = "A long time ago, a Dragon captured a Wolf village. He demanded the women as tribute, and before long, the first Vargul were born. Their draconic father ruled as a tyrant, and the Vargul rebelled and slew their father. Initially, the Varguls remained as protectors. But eventually, according to the Varguls, the Wolves came to fear them, and the Varguls were kicked out, becoming mercenaries. According to the Wolves, the Varguls grew lazy and didn't want to protect the village anymore, and left of their own accord. Most scholars believe the truth is some combination of the two versions of events.",
        };



        Badgers = new RaceTraits()
        {
            BodySize = 20,
            StomachSize = 20,
            HasTail = true,
            FavoredStat = Stat.Endurance,
            CanUseRangedWeapons = false,
            PowerAdjustment = 1.4f,
            DeployCost = 1,
            Upkeep = 8f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(16, 24),
                Dexterity = new RaceStats.StatRange(6, 10),
                Endurance = new RaceStats.StatRange(18, 26),
                Mind = new RaceStats.StatRange(6, 10),
                Will = new RaceStats.StatRange(8, 16),
                Agility = new RaceStats.StatRange(10, 16),
                Voracity = new RaceStats.StatRange(14, 24),
                Stomach = new RaceStats.StatRange(12, 18),
            },
            RacialTraits = new List<Traits>()
            {
                Traits.WildFury,
                Traits.GiantSlayer,
                Traits.Berserk,
                Traits.Resilient,
            },
            RaceDescription = "Created by taking wild, feral badgers and infusing them various types of blood, the Badgers were originally made by a consortium of wizards to serve as their personal bodyguards. However, said consortium were rather unpleasant, and it was not long before they ended up as naught but badger padding. Ever since, the badgers have lived as fighters-for-hire, renowned among the civilized peoples of the realm for their battle prowess and resilience against even the toughest of foes. However, they are still just regular badgers at heart, often having quite the temper, and they aren't always the cleverest. But still, a valuable ally to have in battle.",
        };



        Serpents = new RaceTraits()
        {
            BodySize = 20,
            StomachSize = 30,
            HasTail = true,
            FavoredStat = Stat.Voracity,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral },
            ExpMultiplier = 1.25f,
            PowerAdjustment = .9f,
            DeployCost = 1,
            Upkeep = 8f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(8, 20),
                Dexterity = new RaceStats.StatRange(6, 14),
                Endurance = new RaceStats.StatRange(10, 18),
                Mind = new RaceStats.StatRange(6, 14),
                Will = new RaceStats.StatRange(4, 8),
                Agility = new RaceStats.StatRange(6, 12),
                Voracity = new RaceStats.StatRange(18, 26),
                Stomach = new RaceStats.StatRange(18, 26),
            },
            RacialTraits = new List<Traits>()
            {
                Traits.SlowAbsorption,
                Traits.Biter,
                Traits.Ravenous,
                Traits.StrongGullet,
				Traits.Resilient,
            },
            RaceDescription = "When the lizard folk emerged from their portal to this land, some young snakes from their old world managed to slip along. Growing fast under the effect of this new realm, the Serpents soon emerged as a ravenous horde.",
        };

        Wyvern = new RaceTraits()
        {
            BodySize = 30,
            StomachSize = 30,
            HasTail = true,
            FavoredStat = Stat.Agility,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral, VoreType.Unbirth, VoreType.CockVore, VoreType.Anal, VoreType.BladderVore },
            ExpMultiplier = 1.5f,
            PowerAdjustment = 4f,
            DeployCost = 2,
            Upkeep = 13f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(8, 20),
                Dexterity = new RaceStats.StatRange(6, 14),
                Endurance = new RaceStats.StatRange(22, 32),
                Mind = new RaceStats.StatRange(12, 28),
                Will = new RaceStats.StatRange(6, 14),
                Agility = new RaceStats.StatRange(10, 22),
                Voracity = new RaceStats.StatRange(10, 18),
                Stomach = new RaceStats.StatRange(8, 16),
            },
            RacialTraits = new List<Traits>()
        {
				Traits.Biter,
				Traits.Berserk,
				Traits.Resilient,
        },
            RaceDescription = "Fast, winged, and ravenous. These lesser cousins of dragons do not have the magical abilities of true dragons, but they are still a dangerous force. They are often seen following a larger \"Wyvern Matron\". While you would assume they act as leaders to the Wyverns, it's more a case that, without a force to keep some amount of order, Wyverns often try to snack on the weakest of their ranks.",

        };

        WyvernMatron = new RaceTraits()
        {
            BodySize = 40,
            StomachSize = 40,
            FavoredStat = Stat.Agility,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral, VoreType.Unbirth, VoreType.CockVore, VoreType.Anal, VoreType.BladderVore },
            ExpMultiplier = 1.6f,
            PowerAdjustment = 9f,
            DeployCost = 3,
            Upkeep = 30f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(24, 30),
                Dexterity = new RaceStats.StatRange(9, 21),
                Endurance = new RaceStats.StatRange(30, 42),
                Mind = new RaceStats.StatRange(21, 44),
                Will = new RaceStats.StatRange(12, 27),
                Agility = new RaceStats.StatRange(16, 33),
                Voracity = new RaceStats.StatRange(15, 27),
                Stomach = new RaceStats.StatRange(28, 39),
            },
            RacialTraits = new List<Traits>()
        {
				Traits.Biter,
				Traits.Berserk,
				Traits.Resilient,
        },
            RaceDescription = "The beings called Wyvern Matrons are a rare, larger, hermaphroditic variant of the wyvern. Why exactly some wyverns turn into such is unknown, but the resulting being is an even greater threat than average wyverns are.",
        };



        FeralWolves = new RaceTraits()
        {
            BodySize = 20,
            StomachSize = 20,
            HasTail = true,
            FavoredStat = Stat.Strength,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral, VoreType.Anal, VoreType.BladderVore },
            ExpMultiplier = 1.75f,
            PowerAdjustment = 1.75f,
            DeployCost = 1,
            Upkeep = 5f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(14, 22),
                Dexterity = new RaceStats.StatRange(9, 13),
                Endurance = new RaceStats.StatRange(17, 26),
                Mind = new RaceStats.StatRange(6, 12),
                Will = new RaceStats.StatRange(12, 18),
                Agility = new RaceStats.StatRange(17, 26),
                Voracity = new RaceStats.StatRange(19, 27),
                Stomach = new RaceStats.StatRange(21, 29),
            },
            RacialTraits = new List<Traits>()
        {
                Traits.Biter,
                Traits.PackTactics,
                Traits.Pounce,
        },
            RaceDescription = "Natives of this realm, the wolves were more than happy for a chance to welcome the newcomers to their bellies. While likely related to their bipedal cousins, the ferals only consider them as familiar smelling food.",
        };



        Harvesters = new RaceTraits()
        {
            BodySize = 30,
            StomachSize = 30,
            HasTail = true,
            FavoredStat = Stat.Voracity,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral },
            ExpMultiplier = 1.5f,
            PowerAdjustment = 2.2f,
            DeployCost = 1,
            Upkeep = 6f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(12, 22),
                Dexterity = new RaceStats.StatRange(6, 14),
                Endurance = new RaceStats.StatRange(18, 24),
                Mind = new RaceStats.StatRange(6, 10),
                Will = new RaceStats.StatRange(10, 22),
                Agility = new RaceStats.StatRange(18, 28),
                Voracity = new RaceStats.StatRange(18, 24),
                Stomach = new RaceStats.StatRange(10, 14),
            },
            RacialTraits = new List<Traits>()
        {
                Traits.SlowDigestion,
                Traits.Intimidating,
                Traits.BornToMove,
                Traits.NimbleClimber,
                Traits.Crusher,
        },
            RaceDescription = "A lifeform from far beyond the stars, the Harvesters saw the empty lands fill and felt rising hunger. How they made their way here is unknown, but their mission is readily understood. They are here to feed until the land is empty once more.",

        };



        Bats = new RaceTraits()
        {
            BodySize = 12,
            StomachSize = 12,
            HasTail = false,
            FavoredStat = Stat.Agility,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral, VoreType.Anal, VoreType.Unbirth, VoreType.CockVore, VoreType.BladderVore },
            ExpMultiplier = 1.2f,
            PowerAdjustment = 1.5f,
            DeployCost = 1,
            Upkeep = 1f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(8, 12),
                Dexterity = new RaceStats.StatRange(12, 16),
                Endurance = new RaceStats.StatRange(8, 12),
                Mind = new RaceStats.StatRange(6, 8),
                Will = new RaceStats.StatRange(8, 12),
                Agility = new RaceStats.StatRange(12, 16),
                Voracity = new RaceStats.StatRange(10, 14),
                Stomach = new RaceStats.StatRange(8, 12),
            },
            RacialTraits = new List<Traits>()
        {
                Traits.EvasiveBattler
        },
            RaceDescription = "A species with large difference in size between genders, the male bats being barely half the female's size. This has led many to believe that the tendency of the females to hunt both for sustenance and pleasure is due to the males being unable to satisfy some of the female's needs.",
        };



        Dragons = new RaceTraits()
        {
            BodySize = 100,
            StomachSize = 80,
            HasTail = true,
            FavoredStat = Stat.Voracity,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral, VoreType.Anal, VoreType.Unbirth, VoreType.CockVore, VoreType.BladderVore },
            ExpMultiplier = 6f,
            PowerAdjustment = 12f,
            DeployCost = 3,
            Upkeep = 30f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(24, 32),
                Dexterity = new RaceStats.StatRange(10, 18),
                Endurance = new RaceStats.StatRange(30, 42),
                Mind = new RaceStats.StatRange(24, 32),
                Will = new RaceStats.StatRange(12, 24),
                Agility = new RaceStats.StatRange(16, 22),
                Voracity = new RaceStats.StatRange(20, 28),
                Stomach = new RaceStats.StatRange(12, 24),
            },
            RacialTraits = new List<Traits>()
        {
                Traits.Maul,
                Traits.StrongGullet,
                Traits.Greedy,
                Traits.Cruel,
                Traits.AdeptLearner,

        },
            RaceDescription = "Among the most feared things in this world, Dragons are creatures of immense power and cruelty, who often delight in tormenting their prey before devouring them. To summarize: Avoid."
        };

        Dragonfly = new RaceTraits()
        {
            BodySize = 9,
            StomachSize = 9,
            HasTail = false,
            FavoredStat = Stat.Agility,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral },
            ExpMultiplier = 1.2f,
            PowerAdjustment = 1.4f,
            DeployCost = 1,
            Upkeep = 2f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(8, 10),
                Dexterity = new RaceStats.StatRange(15, 20),
                Endurance = new RaceStats.StatRange(8, 10),
                Mind = new RaceStats.StatRange(6, 8),
                Will = new RaceStats.StatRange(8, 12),
                Agility = new RaceStats.StatRange(15, 20),
                Voracity = new RaceStats.StatRange(8, 12),
                Stomach = new RaceStats.StatRange(8, 10),
            },
            RacialTraits = new List<Traits>()
        {
                Traits.Tempered
        },
            RaceDescription = "The ambient energies that abound in this world sometimes cause normal creatures to grow to abnormal sizes. These dragonflies have adapted their diet to suit their new size and abilities, and are a terror to face unprepared.",
        };


        Plants = new RaceTraits()
        {
            BodySize = 12,
            StomachSize = 12,
            HasTail = false,
            FavoredStat = Stat.Voracity,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral },
            ExpMultiplier = 1.2f,
            PowerAdjustment = 1.4f,
            DeployCost = 1,
            Upkeep = 2f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(10, 16),
                Dexterity = new RaceStats.StatRange(6, 8),
                Endurance = new RaceStats.StatRange(8, 14),
                Mind = new RaceStats.StatRange(4, 6),
                Will = new RaceStats.StatRange(6, 12),
                Agility = new RaceStats.StatRange(4, 8),
                Voracity = new RaceStats.StatRange(8, 15),
                Stomach = new RaceStats.StatRange(8, 13),
            },
            RacialTraits = new List<Traits>()
        {
                Traits.Tempered,
                Traits.SlowDigestion
        },
            RaceDescription = "Strange and enigmatic, the Twisted Vines are, unsurprisingly, not natives of this world, instead emerging during a particularly harsh period where innumerable portals connecting to realms of chaos opened and closed at random. The Twisted Vines are the native people of one of these chaotic planes. Despite what some think, they are sapient, they just generally prefer to live as nomadic bandits, swallowing any unlucky travelers they come across, and then coughing up or passing their valuables."
        };



        FeralAnts = new RaceTraits()
        {
            BodySize = 3,
            StomachSize = 12,
            HasTail = false,
            FavoredStat = Stat.Dexterity,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral },
            ExpMultiplier = 1.0f,
            PowerAdjustment = 1.2f,
            DeployCost = 1,
            Upkeep = 1f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(4, 6),
                Dexterity = new RaceStats.StatRange(12, 15),
                Endurance = new RaceStats.StatRange(6, 8),
                Mind = new RaceStats.StatRange(6, 10),
                Will = new RaceStats.StatRange(6, 10),
                Agility = new RaceStats.StatRange(8, 12),
                Voracity = new RaceStats.StatRange(6, 10),
                Stomach = new RaceStats.StatRange(8, 10),
            },
            RacialTraits = new List<Traits>()
        {
                Traits.AcidResistant,
                Traits.BornToMove,
                Traits.SlowDigestion
        },
            RaceDescription = "Tiny insects grown to a slightly larger size, the Ants still wouldn't be considered dangerous were it not for their ability to swallow and carry things hundred times their own size.",
        };

        Gryphons = new RaceTraits()
        {
            BodySize = 20,
            StomachSize = 22,
            HasTail = true,
            FavoredStat = Stat.Agility,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral, VoreType.Anal, VoreType.Unbirth, VoreType.CockVore, VoreType.BladderVore },
            ExpMultiplier = 1.75f,
            PowerAdjustment = 2.5f,
            DeployCost = 1,
            Upkeep = 5f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(18, 28),
                Dexterity = new RaceStats.StatRange(8, 16),
                Endurance = new RaceStats.StatRange(12, 16),
                Mind = new RaceStats.StatRange(8, 16),
                Will = new RaceStats.StatRange(8, 16),
                Agility = new RaceStats.StatRange(22, 29),
                Voracity = new RaceStats.StatRange(8, 16),
                Stomach = new RaceStats.StatRange(8, 14),
            },
            RacialTraits = new List<Traits>()
        {
                Traits.Intimidating,
                Traits.Charge,
                Traits.Greedy,
                Traits.Pathfinder,
        },
            RaceDescription = "Originally the hybrid offspring of the Avians and Feral Lions, Gryphons are a proud and shockingly intelligent race, who could easily found an empire of their own if they were able to work together in larger groups. Instead, they prefer to spend most of their time hunting travelers, raiding small settlements, and stalking mountain passes. If you ever find yourself at a mountain pass with a Gryphon telling you to do something before they let you pass, don't bother. They just want something, and will most likely eat you after you're done to avoid the embarrassment of admitting they needed help."
        };

        FeralLions = new RaceTraits()
        {
            BodySize = 20,
            StomachSize = 20,
            HasTail = true,
            FavoredStat = Stat.Voracity,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral, VoreType.Anal, VoreType.Unbirth, VoreType.CockVore, VoreType.BladderVore },
            ExpMultiplier = 1.75f,
            PowerAdjustment = 3f,
            DeployCost = 1,
            Upkeep = 6f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(14, 20),
                Dexterity = new RaceStats.StatRange(8, 16),
                Endurance = new RaceStats.StatRange(12, 20),
                Mind = new RaceStats.StatRange(6, 12),
                Will = new RaceStats.StatRange(12, 18),
                Agility = new RaceStats.StatRange(14, 20),
                Voracity = new RaceStats.StatRange(18, 24),
                Stomach = new RaceStats.StatRange(18, 24),
            },
            RacialTraits = new List<Traits>()
            {
                Traits.Biter,
                Traits.Pounce,
                Traits.Ravenous,
                Traits.TasteForBlood,
                Traits.PleasurableTouch,
            },
            RaceDescription = $"Big hedonistic felines. They were probably following a migration of gazelle before they came upon this land.\nMuch older texts claim they are the children of Raha, another world's godess of pleasure. She spread her blessing to this realm, and in exchange, these kitties are feeling right at home digesting the natives.",
            RaceAI = RaceAI.Hedonist,
        };

        SpitterSlugs = new RaceTraits()
        {
            BodySize = 8,
            StomachSize = 20,
            HasTail = false,
            FavoredStat = Stat.Voracity,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral },
            ExpMultiplier = 1.2f,
            PowerAdjustment = 0.45f,
            DeployCost = 1,
            Upkeep = 3f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(3, 5),
                Dexterity = new RaceStats.StatRange(6, 9),
                Endurance = new RaceStats.StatRange(4, 7),
                Mind = new RaceStats.StatRange(3, 5),
                Will = new RaceStats.StatRange(4, 6),
                Agility = new RaceStats.StatRange(4, 6),
                Voracity = new RaceStats.StatRange(7, 11),
                Stomach = new RaceStats.StatRange(6, 9),
            },
            RacialTraits = new List<Traits>()
        {
            Traits.BoggingSlime,
            Traits.GelatinousBody, // or resilient
            Traits.SoftBody,
            Traits.SlowMovement,
            Traits.GlueBomb
        },
            RaceDescription = "Animals providing light ranged support."
        };

        SpringSlugs = new RaceTraits()
        {
            BodySize = 12,
            StomachSize = 12,
            HasTail = false,
            FavoredStat = Stat.Voracity,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral },
            ExpMultiplier = .9f,
            PowerAdjustment = .9f,
            DeployCost = 1,
            Upkeep = 2f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(8, 12),
                Dexterity = new RaceStats.StatRange(6, 8),
                Endurance = new RaceStats.StatRange(8, 12),
                Mind = new RaceStats.StatRange(4, 6),
                Will = new RaceStats.StatRange(8, 12),
                Agility = new RaceStats.StatRange(4, 6),
                Voracity = new RaceStats.StatRange(16, 24),
                Stomach = new RaceStats.StatRange(8, 16),
            },
            RacialTraits = new List<Traits>()
        {
            Traits.BoggingSlime,
            Traits.EasyToVore,
            Traits.Replaceable,
            Traits.Pounce,
            Traits.SoftBody,
            Traits.SlowMovement
        },
            RaceDescription = "One of the four species of giant slug that can be found in the Realm, the Spring Slugs are rather weak compared to the other giant slugs, though they do have one noteworthy trick; by building up tension in a special organ inside their body and then releasing, the Spring Slugs can fling, or spring(hence the name), themselves across a battlefield. If you notice one arching its \"back,\" get ready to dodge."
        };

        RockSlugs = new RaceTraits()
        {
            BodySize = 32,
            StomachSize = 50,
            HasTail = false,
            FavoredStat = Stat.Voracity,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral },
            ExpMultiplier = 2.5f,
            PowerAdjustment = 3.0f,
            DeployCost = 1,
            Upkeep = 3f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(16, 24),
                Dexterity = new RaceStats.StatRange(6, 8),
                Endurance = new RaceStats.StatRange(32, 40),
                Mind = new RaceStats.StatRange(4, 6),
                Will = new RaceStats.StatRange(8, 12),
                Agility = new RaceStats.StatRange(4, 6),
                Voracity = new RaceStats.StatRange(32, 40),
                Stomach = new RaceStats.StatRange(12, 24),
            },
            RacialTraits = new List<Traits>()
        {
            Traits.Resilient,
            Traits.SoftBody,
            Traits.VerySlowMovement,
            Traits.HardSkin
        },
            RaceDescription = "One of the four species of giant slug that can be found in the Realm, the Rock Slugs are known for their high resilience, being able to withstand some of the strongest beasts in the Realm. They would be far more difficult to deal with were it not for their speed, which makes the other already slow species of giant slug look like speed demons in comparison."
        };

        CoralSlugs = new RaceTraits()
        {
            BodySize = 12,
            StomachSize = 16,
            HasTail = false,
            FavoredStat = Stat.Voracity,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral },
            ExpMultiplier = 1.2f,
            PowerAdjustment = 1.5f,
            DeployCost = 1,
            Upkeep = 5f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(8, 12),
                Dexterity = new RaceStats.StatRange(6, 8),
                Endurance = new RaceStats.StatRange(10, 15),
                Mind = new RaceStats.StatRange(4, 6),
                Will = new RaceStats.StatRange(8, 12),
                Agility = new RaceStats.StatRange(4, 6),
                Voracity = new RaceStats.StatRange(16, 24),
                Stomach = new RaceStats.StatRange(8, 16),
            },
            RacialTraits = new List<Traits>()
        {
            Traits.Paralyzer,
            Traits.Stinger,
            Traits.GelatinousBody, // or resilient
            Traits.SoftBody,
            Traits.SlowMovement,
            Traits.Toxic
        },
            InnateSpells = new List<SpellTypes>() { SpellTypes.Poison },
            RaceDescription = "One of the four species of giant slug that can be found in the Realm, the Coral Slugs are mostly known for their unique appearance compared to the other slugs and their highly toxic bodies and stingers. Like all giant slugs, they are rather slow, but that makes them no less dangerous."
        };

        Salamanders = new RaceTraits()
        {
            BodySize = 20,
            StomachSize = 18,
            HasTail = true,
            FavoredStat = Stat.Mind,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral },
            ExpMultiplier = 1.2f,
            PowerAdjustment = 1.5f,
            DeployCost = 1,
            Upkeep = 3f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(10, 16),
                Dexterity = new RaceStats.StatRange(8, 14),
                Endurance = new RaceStats.StatRange(8, 14),
                Mind = new RaceStats.StatRange(12, 20),
                Will = new RaceStats.StatRange(8, 16),
                Agility = new RaceStats.StatRange(8, 14),
                Voracity = new RaceStats.StatRange(12, 20),
                Stomach = new RaceStats.StatRange(10, 16),
            },
            RacialTraits = new List<Traits>()
        {
            Traits.Biter,
            Traits.HotBlooded
        },
            InnateSpells = new List<SpellTypes>() { SpellTypes.Fireball },
            RaceDescription = "The fire on the tail isn't for show. Despite being dumber than most plants, Salamanders are shockingly good at claiming prey. Avoid if you don't want to become fat on a creature that can't tell its own reflection from another Salamander."
        };

        Mantis = new RaceTraits()
        {
            BodySize = 20,
            StomachSize = 20,
            HasTail = false,
            FavoredStat = Stat.Strength,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral, VoreType.Unbirth, VoreType.BladderVore },
            ExpMultiplier = 1.2f,
            PowerAdjustment = 2.5f,
            DeployCost = 1,
            Upkeep = 6f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(20, 28),
                Dexterity = new RaceStats.StatRange(12, 20),
                Endurance = new RaceStats.StatRange(8, 12),
                Mind = new RaceStats.StatRange(6, 10),
                Will = new RaceStats.StatRange(6, 10),
                Agility = new RaceStats.StatRange(18, 26),
                Voracity = new RaceStats.StatRange(6, 10),
                Stomach = new RaceStats.StatRange(6, 10),
            },
            RacialTraits = new List<Traits>()
        {
            Traits.Pounce,
            Traits.SenseWeakness,
            Traits.BladeDance,
            Traits.LightFrame
        },
            RaceDescription = "Despite being native to the warmer, wetter regions of this world, the Mantises can be seen virtually anywhere, and they are not a good sight. In battle, they tend to leap around unpredictably, and those scythe arms aren't just for show. While it's best to avoid them, if battle is inevitable, it's imperative that you maintain as much distance as possible."
        };

        EasternDragon = new RaceTraits()
        {
            BodySize = 80,
            StomachSize = 80,
            HasTail = true,
            FavoredStat = Stat.Agility,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral, VoreType.CockVore, VoreType.Unbirth, VoreType.BladderVore },
            ExpMultiplier = 1.6f,
            PowerAdjustment = 12f,
            DeployCost = 3,
            Upkeep = 36f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(12, 22),
                Dexterity = new RaceStats.StatRange(8, 16),
                Endurance = new RaceStats.StatRange(22, 23),
                Mind = new RaceStats.StatRange(30, 42),
                Will = new RaceStats.StatRange(8, 18),
                Agility = new RaceStats.StatRange(14, 28),
                Voracity = new RaceStats.StatRange(22, 32),
                Stomach = new RaceStats.StatRange(32, 40),
            },
            RacialTraits = new List<Traits>()
        {
                Traits.Ravenous,
                Traits.AdeptLearner,
                Traits.StrongGullet,
                Traits.Maul,
        },
            InnateSpells = new List<SpellTypes>() { SpellTypes.Fireball },
            RaceDescription = "A variety of dragons especially attuned to magic, the Eastern Dragons, or Lung Dragons as they are also known as, are able to fly without wings. Reminiscent of snakes, the Eastern Dragons are readily able to prove that the resemblance is more than skin deep, devouring large prey with ease.",

        };

        Catfish = new RaceTraits()
        {
            BodySize = 20,
            StomachSize = 16,
            HasTail = true,
            FavoredStat = Stat.Stomach,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral },
            ExpMultiplier = 1.2f,
            PowerAdjustment = 1.5f,
            DeployCost = 1,
            Upkeep = 5f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(8, 12),
                Dexterity = new RaceStats.StatRange(6, 10),
                Endurance = new RaceStats.StatRange(16, 24),
                Mind = new RaceStats.StatRange(8, 12),
                Will = new RaceStats.StatRange(8, 12),
                Agility = new RaceStats.StatRange(10, 16),
                Voracity = new RaceStats.StatRange(20, 28),
                Stomach = new RaceStats.StatRange(12, 20),
            },
            RacialTraits = new List<Traits>()
        {
            Traits.Slippery,
            Traits.Ravenous,
            Traits.Nauseous,
            Traits.SlowDigestion
        },
            RaceDescription = "These large and incredibly stupid fish long ago figured out that much larger and tastier \"food\"(people) exists on land, and so they figured out how to haphazardly leap, slide, and slither on land, adapting their gills into pseudo-lungs capable of breathing air. With the much larger meals available on land, they've been able to get much larger, though this has only increased their appetite."
        };


        WarriorAnts = new RaceTraits()
        {
            BodySize = 12,
            StomachSize = 12,
            HasTail = false,
            FavoredStat = Stat.Strength,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral },
            ExpMultiplier = 1.1f,
            PowerAdjustment = 1.3f,
            DeployCost = 1,
            Upkeep = 2f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(8, 12),
                Dexterity = new RaceStats.StatRange(12, 15),
                Endurance = new RaceStats.StatRange(8, 12),
                Mind = new RaceStats.StatRange(16, 20),
                Will = new RaceStats.StatRange(6, 10),
                Agility = new RaceStats.StatRange(8, 12),
                Voracity = new RaceStats.StatRange(8, 12),
                Stomach = new RaceStats.StatRange(8, 12),
            },
            RacialTraits = new List<Traits>()
        {
                Traits.AcidResistant,
                Traits.PackStrength,
                Traits.SlowDigestion
        },
            RaceDescription = "The Warrior Ants are specialized members of the Feral Ant species, whose pheromones control and coordinate their lesser kin. When in the presence of the larger Ants, they are overwhelmed by the much stronger pheromones they exude, and join them without second thought."
        };

        Gazelle = new RaceTraits()
        {
            BodySize = 20,
            StomachSize = 12,
            HasTail = true,
            FavoredStat = Stat.Agility,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral, VoreType.Anal, VoreType.Unbirth, VoreType.CockVore, VoreType.BladderVore },
            ExpMultiplier = 1.1f,
            PowerAdjustment = 1.3f,
            DeployCost = 1,
            Upkeep = 3f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(8, 12),
                Dexterity = new RaceStats.StatRange(10, 16),
                Endurance = new RaceStats.StatRange(10, 16),
                Mind = new RaceStats.StatRange(6, 10),
                Will = new RaceStats.StatRange(6, 10),
                Agility = new RaceStats.StatRange(20, 30),
                Voracity = new RaceStats.StatRange(10, 16),
                Stomach = new RaceStats.StatRange(10, 16),
            },
            RacialTraits = new List<Traits>()
        {
            Traits.Charge,
            Traits.ForcefulBlow,
        },
            RaceDescription = "Gazelles are native to the same universe as the Feral Lions. Despite first impressions, Gazelles are sapient, and actually have a rich storytelling tradition. They came through the portals, hoping to be free of the Feral Lions, only to get trapped in a world with things far worse than the Feral Lions and where the Gazelles' lack of hands is very important. Also, the Feral Lions followed them in, rendering it a moot point anyway."
        };


        FeralLizards = new RaceTraits()
        {
            BodySize = 17,
            StomachSize = 16,
            HasTail = true,
            FavoredStat = Stat.Strength,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral, VoreType.Anal, VoreType.Unbirth, VoreType.CockVore, VoreType.BladderVore },
            ExpMultiplier = 1.5f,
            PowerAdjustment = 1.75f,
            DeployCost = 1,
            Upkeep = 2f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(12, 20),
                Dexterity = new RaceStats.StatRange(8, 16),
                Endurance = new RaceStats.StatRange(10, 18),
                Mind = new RaceStats.StatRange(8, 16),
                Will = new RaceStats.StatRange(8, 16),
                Agility = new RaceStats.StatRange(8, 16),
                Voracity = new RaceStats.StatRange(10, 18),
                Stomach = new RaceStats.StatRange(8, 16),
            },
            RacialTraits = new List<Traits>()
        {
            Traits.Intimidating,
            Traits.Biter,
            Traits.Resilient
        },
            RaceDescription = "Hailing from the wetter, warmer parts of this world, the Feral Lizards are common predators. It is widely understood that the Lizards evolved from the Feral Lizards. Despite no actual relation, their reptilian nature makes them vaguely resemble dragons, and as such many cultures view Feral Lizards as a bad omen."
        };

        Monitors = new RaceTraits()
        {
            BodySize = 20,
            StomachSize = 20,
            HasTail = true,
            FavoredStat = Stat.Voracity,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral, VoreType.Anal, VoreType.Unbirth, VoreType.CockVore, VoreType.BladderVore },
            PowerAdjustment = 1.3f,
            DeployCost = 1,
            Upkeep = 5f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(12, 20),
                Dexterity = new RaceStats.StatRange(6, 10),
                Endurance = new RaceStats.StatRange(20, 28),
                Mind = new RaceStats.StatRange(6, 12),
                Will = new RaceStats.StatRange(8, 16),
                Agility = new RaceStats.StatRange(10, 16),
                Voracity = new RaceStats.StatRange(16, 24),
                Stomach = new RaceStats.StatRange(12, 18),
            },
            RacialTraits = new List<Traits>()
            {
                Traits.HardSkin,
                Traits.Resilient,
                Traits.VenomShock,
            },
            RaceDescription = "While the frequency with which Monitors and Komodos can be found together would lead you to think them related, in truth, they just happen to have very compatible cultures, and enjoy one another's company. Unlike the Komodos, the Monitors are not natives of this world, instead hailing from the Feral Lion home dimension.",
        };


        Terrorbirds = new RaceTraits()
        {
            BodySize = 18,
            StomachSize = 18,
            HasTail = true,
            FavoredStat = Stat.Agility,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral, VoreType.TailVore, VoreType.Unbirth, VoreType.BladderVore},
            ExpMultiplier = 1.5f,
            PowerAdjustment = 1.75f,
            DeployCost = 1,
            Upkeep = 3f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(12, 20),
                Dexterity = new RaceStats.StatRange(8, 16),
                Endurance = new RaceStats.StatRange(8, 16),
                Mind = new RaceStats.StatRange(6, 12),
                Will = new RaceStats.StatRange(6, 12),
                Agility = new RaceStats.StatRange(12, 20),
                Voracity = new RaceStats.StatRange(10, 18),
                Stomach = new RaceStats.StatRange(8, 16),
            },
            RacialTraits = new List<Traits>()
        {
                Traits.Intimidating,
                Traits.ArtfulDodge,
                Traits.Tenacious
        },
            RaceDescription = "Terrorbirds are highly aggressive, and possess high strength. While they aren't particularly clever, they are able to use brute force to great effect."
        };

        Dratopyr = new RaceTraits()
        {
            BodySize = 12,
            StomachSize = 15,
            HasTail = true,
            FavoredStat = Stat.Agility,
            AllowedVoreTypes = new List<VoreType> { VoreType.CockVore, VoreType.Oral, VoreType.Unbirth, VoreType.BladderVore },
            ExpMultiplier = .95f,
            PowerAdjustment = .95f,
            DeployCost = 1,
            Upkeep = 2f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(8, 12),
                Dexterity = new RaceStats.StatRange(6, 8),
                Endurance = new RaceStats.StatRange(8, 12),
                Mind = new RaceStats.StatRange(7, 9),
                Will = new RaceStats.StatRange(10, 15),
                Agility = new RaceStats.StatRange(9, 17),
                Voracity = new RaceStats.StatRange(8, 14),
                Stomach = new RaceStats.StatRange(8, 16),
            },
            RacialTraits = new List<Traits>()
        {
                Traits.ArtfulDodge,
                Traits.Cruel
        },
            RaceDescription = "With an appearance reminiscent of a reptilian bat, the Dratopyr are likely a hybrid race. Smaller than most monsters but just as fierce, the Dratopyr specialize in weakening their prey while avoiding attempts to fend them off. Dratopyr are very fast breeders and would thus be a major threat to everyone, were it not for their tendency toward cannibalism.",
        };

        FeralHorses = new RaceTraits()
        {
            BodySize = 20,
            StomachSize = 20,
            HasTail = true,
            FavoredStat = Stat.Agility,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral, VoreType.Anal, VoreType.Unbirth, VoreType.CockVore, VoreType.BladderVore },
            ExpMultiplier = 1.1f,
            PowerAdjustment = 1.3f,
            DeployCost = 1,
            Upkeep = 4f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(20, 28),
                Dexterity = new RaceStats.StatRange(10, 16),
                Endurance = new RaceStats.StatRange(16, 20),
                Mind = new RaceStats.StatRange(6, 10),
                Will = new RaceStats.StatRange(6, 10),
                Agility = new RaceStats.StatRange(16, 24),
                Voracity = new RaceStats.StatRange(10, 16),
                Stomach = new RaceStats.StatRange(10, 16),
            },
            RacialTraits = new List<Traits>()
        {
            Traits.Charge,
            Traits.ForcefulBlow,
            Traits.BornToMove,

        },
            RaceDescription = "It's a horse!  Go ahead, try to ride one.  We dare you!",
        };

        FeralFox = new RaceTraits()
        {
            BodySize = 16,
            StomachSize = 24,
            FavoredStat = Stat.Voracity,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral, VoreType.Unbirth, VoreType.CockVore, VoreType.Anal, VoreType.BladderVore },
            ExpMultiplier = 1.6f,
            PowerAdjustment = 1.3f,
            DeployCost = 1,
            Upkeep = 4f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(11, 15),
                Dexterity = new RaceStats.StatRange(9, 13),
                Endurance = new RaceStats.StatRange(17, 23),
                Mind = new RaceStats.StatRange(7, 11),
                Will = new RaceStats.StatRange(12, 18),
                Agility = new RaceStats.StatRange(13, 19),
                Voracity = new RaceStats.StatRange(19, 27),
                Stomach = new RaceStats.StatRange(21, 29),
            },
            RacialTraits = new List<Traits>()
        {
            Traits.Pounce,
            Traits.Ravenous,
        },
            RaceDescription = "Abnormally large foxes with a voracious appetite.",
        };

        Terminid = new RaceTraits()
        {
            BodySize = 16,
            StomachSize = 24,
            FavoredStat = Stat.Voracity,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral },
            ExpMultiplier = 1.6f,
            PowerAdjustment = 1.3f,
            DeployCost = 1,
            Upkeep = 5f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(11, 15),
                Dexterity = new RaceStats.StatRange(9, 13),
                Endurance = new RaceStats.StatRange(18, 24),
                Mind = new RaceStats.StatRange(7, 11),
                Will = new RaceStats.StatRange(12, 18),
                Agility = new RaceStats.StatRange(13, 19),
                Voracity = new RaceStats.StatRange(19, 27),
                Stomach = new RaceStats.StatRange(21, 29),
            },
            RacialTraits = new List<Traits>()
        {
            Traits.Disgusting,
            Traits.Pounce,
            Traits.FireVulnerable,
			Traits.Frenzy,
        },
            RaceDescription = "A vicious and territorial race of voracious insects prized for their ability to produce an element known as E-710, though some say it's just oil.  They are very dangerous, especially so in larger numbers and will often attempt to swarm their prey.  No known portal signified their arrival in the realm so how they ended up here is anybody's guess, though a few conspiracy rumors claim they were brought here by someone else.  There is a curious phenomenon surrounding these insects: When struck by them in combat, there is a high likelyhood that the wounded will shout \"No pain, no freedom!\"",
        };



        Goodra = new RaceTraits()
        {
            BodySize = 32,
            StomachSize = 50,
            HasTail = true,
            FavoredStat = Stat.Stomach,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral },
            ExpMultiplier = 6f,
            PowerAdjustment = 12f,
            DeployCost = 2,
            Upkeep = 21f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(20, 28),
                Dexterity = new RaceStats.StatRange(12, 18),
                Endurance = new RaceStats.StatRange(36, 44),
                Mind = new RaceStats.StatRange(18, 24),
                Will = new RaceStats.StatRange(22, 28),
                Agility = new RaceStats.StatRange(10, 16),
                Voracity = new RaceStats.StatRange(22, 30),
                Stomach = new RaceStats.StatRange(36, 46),
            },
            RacialTraits = new List<Traits>()
        {
            Traits.BoggingSlime,
            Traits.FriendlyStomach,
            Traits.Resilient,
            Traits.SoftBody,
            Traits.VerySlowMovement,
            Traits.HardSkin
        },
            RaceDescription = "Goodra, the Slug Dragon Pokemon. Goodra are large soft dragon type pokemon coated in slime. They love to give hugs and often confuse friends from food.",
        };


        FeralSlime = new RaceTraits()
        {
            BodySize = 7,
            StomachSize = 15,
			ExpMultiplier = 1f,
            PowerAdjustment = .75f,
            DeployCost = 1,
            Upkeep = 2f,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral },
            FavoredStat = Stat.Endurance,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(6, 10),
                Dexterity = new RaceStats.StatRange(2, 4),
                Endurance = new RaceStats.StatRange(10, 15),
                Mind = new RaceStats.StatRange(6, 8),
                Will = new RaceStats.StatRange(4, 8),
                Agility = new RaceStats.StatRange(5, 8),
                Voracity = new RaceStats.StatRange(10, 18),
                Stomach = new RaceStats.StatRange(10, 15),
            },
            RacialTraits = new List<Traits>()
            {
                Traits.BoggingSlime,
                Traits.SoftBody,
                Traits.GelatinousBody,

            },
            RaceDescription = "One of the most basic monsters. The humble slime pursues anything that moves. They may or may not taste like assorted friuts.",
        };



        Otachi = new RaceTraits()
        {
            BodySize = 80,
            StomachSize = 80,
            FavoredStat = Stat.Voracity,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral, VoreType.CockVore, VoreType.Unbirth, VoreType.BladderVore },
            ExpMultiplier = 6f,
            PowerAdjustment = 7f,
            DeployCost = 2,
            Upkeep = 30f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(20, 28),
                Dexterity = new RaceStats.StatRange(14, 20),
                Endurance = new RaceStats.StatRange(20, 30),
                Mind = new RaceStats.StatRange(10, 20),
                Will = new RaceStats.StatRange(10, 20),
                Agility = new RaceStats.StatRange(20, 28),
                Voracity = new RaceStats.StatRange(40, 50),
                Stomach = new RaceStats.StatRange(30, 40),
            },
            RacialTraits = new List<Traits>()
        {
            Traits.TailStrike,
            Traits.HealingBlood,
            Traits.PoisonSpit,
        },
            //InnateSpells = new List<SpellTypes>() { SpellTypes.Poison },
            RaceDescription = "Somehow a Kaiju, with poison!",
        };

        Raiju = new RaceTraits()
        {
            BodySize = 60,
            StomachSize = 50,
            FavoredStat = Stat.Endurance,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral, VoreType.CockVore, VoreType.Unbirth, VoreType.BladderVore },
            ExpMultiplier = 6f,
            PowerAdjustment = 1.3f,
            DeployCost = 2,
            Upkeep = 30f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(20, 28),
                Dexterity = new RaceStats.StatRange(14, 20),
                Endurance = new RaceStats.StatRange(20, 30),
                Mind = new RaceStats.StatRange(10, 20),
                Will = new RaceStats.StatRange(10, 20),
                Agility = new RaceStats.StatRange(20, 28),
                Voracity = new RaceStats.StatRange(40, 50),
                Stomach = new RaceStats.StatRange(30, 40),
            },
            RacialTraits = new List<Traits>()
        {
            Traits.Intimidating,
            Traits.Resilient,
            Traits.Grounded,
            Traits.MetabolicSurge,
            Traits.Biter,
        },
            InnateSpells = new List<SpellTypes>() { SpellTypes.LightningBolt, SpellTypes.CrossShock },
            RaceDescription = "Somehow another Kaiju, but lightning this time!",
        };

        Smudger = new RaceTraits()
        {
            BodySize = 25,
            StomachSize = 20,
            HasTail = true,
            FavoredStat = Stat.Voracity,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral, VoreType.Anal, VoreType.Unbirth, VoreType.CockVore, },
            ExpMultiplier = 1f,
            PowerAdjustment = 2f,
            DeployCost = 1,
            Upkeep = 10f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(12, 20),
                Dexterity = new RaceStats.StatRange(6, 10),
                Endurance = new RaceStats.StatRange(20, 28),
                Mind = new RaceStats.StatRange(6, 12),
                Will = new RaceStats.StatRange(8, 16),
                Agility = new RaceStats.StatRange(10, 16),
                Voracity = new RaceStats.StatRange(16, 24),
                Stomach = new RaceStats.StatRange(12, 18),
            },
            RacialTraits = new List<Traits>()
            {
                Traits.SlowMovement,
                Traits.EnthrallingDepths,
                Traits.SteadyStomach,
                Traits.Ravenous,
                Traits.ExtraNutritious,
                Traits.Tasty,
            },
            RaceDescription = "A race of slug-like beings from somewhere beyond the portals.  Their friendly and soft shape is but a ploy to lure in the more gullible prey of our world, and those that aren't fooled by it will discover that they are no pushovers in a struggle either.  Those who find themselves as prey to these creatures will find they don't wish to leave their insides, though if a predator can manage to devour one they will find a delectable meal as their bodies are incredibly nutritious.",
        };

        SpaceCroach = new RaceTraits()
        {
            BodySize = 40,
            StomachSize = 30,
            HasTail = false,
            FavoredStat = Stat.Strength,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral },
            ExpMultiplier = 4f,
            PowerAdjustment = 2f,
            DeployCost = 1,
            Upkeep = 13f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(20, 28),
                Dexterity = new RaceStats.StatRange(12, 20),
                Endurance = new RaceStats.StatRange(15, 20),
                Mind = new RaceStats.StatRange(6, 10),
                Will = new RaceStats.StatRange(6, 10),
                Agility = new RaceStats.StatRange(18, 26),
                Voracity = new RaceStats.StatRange(6, 10),
                Stomach = new RaceStats.StatRange(12, 20),
            },
            RacialTraits = new List<Traits>()
        {
            Traits.SenseWeakness,
            Traits.LightFrame,
            Traits.BoggingSlime,
            Traits.Ravenous,
            Traits.Pounce,
            Traits.NimbleClimber,
        },
            RaceDescription = "A mysterious race of roach-like beings from another universe."
        };

        Trex = new RaceTraits()
        {
            BodySize = 90,
            StomachSize = 50,
            HasTail = false,
            FavoredStat = Stat.Strength,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral, VoreType.Anal, VoreType.CockVore, VoreType.Unbirth, VoreType.BladderVore },
            ExpMultiplier = 12f,
            PowerAdjustment = 12f,
            DeployCost = 2,
            Upkeep = 24f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(30, 42),
                Dexterity = new RaceStats.StatRange(8, 12),
                Endurance = new RaceStats.StatRange(24, 30),
                Mind = new RaceStats.StatRange(10, 14),
                Will = new RaceStats.StatRange(10, 14),
                Agility = new RaceStats.StatRange(12, 18),
                Voracity = new RaceStats.StatRange(24, 32),
                Stomach = new RaceStats.StatRange(24, 32),
            },
            RacialTraits = new List<Traits>()
        {
            Traits.Berserk,
            Traits.Biter,
            Traits.Ravenous,
            Traits.IronGut,
			Traits.Resilient,
        },
            RaceDescription = "They're big, they're scaled, they're hungry, and they got little arms.  Woe betide those who find themselves trapped behind their powerful jaws, for this is the ultimate predator whose iconic roar is known far and wide and strikes fear in all those who hear it."
        };

        Utahraptor = new RaceTraits()
        {
            BodySize = 30,
            StomachSize = 30,
            HasTail = false,
            FavoredStat = Stat.Strength,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral, VoreType.Anal, VoreType.CockVore, VoreType.Unbirth, VoreType.BladderVore },
            ExpMultiplier = 4f,
            PowerAdjustment = 2f,
            DeployCost = 2,
            Upkeep = 5f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(15, 20),
                Dexterity = new RaceStats.StatRange(8, 12),
                Endurance = new RaceStats.StatRange(15, 20),
                Mind = new RaceStats.StatRange(10, 14),
                Will = new RaceStats.StatRange(10, 14),
                Agility = new RaceStats.StatRange(12, 18),
                Voracity = new RaceStats.StatRange(14, 20),
                Stomach = new RaceStats.StatRange(14, 20),
            },
            RacialTraits = new List<Traits>()
        {
            Traits.Charge,
            Traits.Pounce,
            Traits.Biter,
            Traits.BornToMove,
        },
            RaceDescription = "Utahraptors are much faster and more powerful than the usual raptors you see here and there.  More hungry too."
        };


        BlackWidow = new RaceTraits()
        {
            BodySize = 20,
            StomachSize = 25,
            HasTail = false,
            FavoredStat = Stat.Voracity,
            AllowedVoreTypes = new List<VoreType> { VoreType.Oral },
            ExpMultiplier = 2f,
            PowerAdjustment = 2f,
            DeployCost = 1,
            Upkeep = 8f,
            RaceStats = new RaceStats()
            {
                Strength = new RaceStats.StatRange(20, 25),
                Dexterity = new RaceStats.StatRange(12, 20),
                Endurance = new RaceStats.StatRange(13, 27),
                Mind = new RaceStats.StatRange(15, 18),
                Will = new RaceStats.StatRange(6, 10),
                Agility = new RaceStats.StatRange(13, 22),
                Voracity = new RaceStats.StatRange(15, 18),
                Stomach = new RaceStats.StatRange(12, 20),
            },
            RacialTraits = new List<Traits>()
            {
                Traits.Biter,
                Traits.VenomousBite,
                Traits.VenomShock,
                Traits.Webber,    

            },
            RaceDescription = "Giant Spiders. Of course they had to be widows..."
        };



    }

}


internal class RaceTraits
{
    internal RaceTraits Copy() { return (RaceTraits)MemberwiseClone(); }
    /// <summary>
    /// Controls the size of the body, used for determining how much space they take up in a belly
    /// </summary>
    internal int BodySize;

    internal RaceAI RaceAI;
    /// <summary>
    /// Controls the base stomach size, at 12 stomach, the capacity will equal this value, and increases or decreases linearly
    /// </summary>
    internal int StomachSize;
    internal bool HasTail;
    internal Stat FavoredStat;
    internal List<Traits> RacialTraits;
    internal List<Traits> LeaderTraits;
    internal List<Traits> SpawnTraits;
    internal List<int> RacialTags = new List<int>();
    //internal List<Traits> RandomTraits;
    internal List<VoreType> AllowedVoreTypes = new List<VoreType> { VoreType.Anal, VoreType.Oral, VoreType.CockVore, VoreType.BreastVore, VoreType.Unbirth, VoreType.BladderVore };
    internal Race SpawnRace = Race.none;
    internal Race ConversionRace = Race.none;
    internal Race LeaderRace = Race.none;
    internal Race MorphRace = Race.none;
    internal List<SpellTypes> InnateSpells = new List<SpellTypes>();
    internal RaceStats RaceStats = new RaceStats();

    /// <summary>
    /// Controls the amount of unit slots unit takes up in an army.
    /// </summary>
    internal int DeployCost = 1;

    /// <summary>
    /// Controls the upkeep a unit requires.
    /// </summary>
    internal float Upkeep = 1f;

    /// <summary>
    /// Attacks against this race will have their experience gained modified by this ratio
    /// </summary>
    internal float ExpMultiplier = 1f;
    /// <summary>
    /// A straight multiplier to a unit's perceived power
    /// Eventually, this may be incorporated as reading stats instead
    /// </summary>
    internal float PowerAdjustment = 1f;
    internal bool CanUseRangedWeapons = true;
    internal bool CanUseMeleeWeapons = true;
    internal string RaceDescription = "";

}

internal enum VoreType
{
    All,
    Oral,
    Unbirth,
    CockVore,
    BreastVore,
    TailVore,
    Anal,
    BladderVore,


}

internal class RaceStats
{
    /// <summary>
    /// Sets the minimum and maximum stats
    /// </summary>
    internal struct StatRange
    {
        [OdinSerialize]
        internal int Minimum;
        [OdinSerialize]
        internal int Roll;
        /// <summary>
        /// Sets up the stat range
        /// </summary>
        /// <param name="minimum">the lowest the stat will be</param>
        /// <param name="maximum">the highest, inclusive</param>
        public StatRange(int minimum, int maximum)
        {
            Minimum = minimum;
            Roll = 1 + maximum - minimum;
            if (Roll < 1)
            {
                UnityEngine.Debug.LogWarning("Maximum is less than minimum for one of the Stat Ranges");
                Roll = 1;
            }
        }

        public StatRange(bool junk, int minimum, int roll)
        {
            Minimum = minimum;
            Roll = roll;
        }
    }
    [OdinSerialize]
    internal StatRange Strength;
    [OdinSerialize]
    internal StatRange Dexterity;
    [OdinSerialize]
    internal StatRange Voracity;
    [OdinSerialize]
    internal StatRange Mind;
    [OdinSerialize]
    internal StatRange Agility;
    [OdinSerialize]
    internal StatRange Stomach;
    [OdinSerialize]
    internal StatRange Endurance;
    [OdinSerialize]
    internal StatRange Will;

    [OdinSerialize] internal int FourStatVersion;
    internal void Consolidate()
    {
        if (FourStatVersion == 1) return;
        Strength = Merge(Strength, Endurance, 2);
        Agility = Merge(Agility, Dexterity, 1);
        Mind = Merge(Mind, Will, 2);
        Voracity = Merge(Voracity, Stomach, 1);
        Mirror();
    }
    static StatRange Merge(StatRange a, StatRange b, int weight)
    {
        int min = Math.Max(1, (int)Math.Round((a.Minimum + weight * (double)b.Minimum) / (1 + weight)));
        int max = Math.Max(min, (int)Math.Round((a.Minimum + Math.Max(1, a.Roll) - 1 +
            weight * (double)(b.Minimum + Math.Max(1, b.Roll) - 1)) / (1 + weight)));
        return new StatRange(min, max);
    }
    internal void Mirror()
    {
        Endurance = Strength; Dexterity = Agility; Will = Mind; Stomach = Voracity;
        FourStatVersion = 1;
    }

    public RaceStats() //Default Stats
    {
        Strength = new StatRange(6, 14);
        Dexterity = new StatRange(6, 14);
        Endurance = new StatRange(8, 13);
        Mind = new StatRange(6, 13);
        Will = new StatRange(6, 13);
        Agility = new StatRange(6, 10);
        Voracity = new StatRange(6, 11);
        Stomach = new StatRange(12, 15);
    }
}
