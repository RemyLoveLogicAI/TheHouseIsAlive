using System;
using UnityEngine;

namespace TheHouseIsAlive.DungeonMaster
{
    public enum AdvantageMode
    {
        Normal,
        Advantage,
        Disadvantage
    }

    public enum DamageType
    {
        Slashing,
        Piercing,
        Bludgeoning,
        Fire,
        Cold,
        Radiant,
        Necrotic
    }

    public struct RollResult
    {
        public int D20Roll;
        public int FirstDie;
        public int SecondDie;
        public AdvantageMode Mode;
        public int Modifier;
        public int Total => D20Roll + Modifier;
        public bool IsNatural20 => D20Roll == 20;
        public bool IsNatural1 => D20Roll == 1;
        public bool IsCriticalHit => IsNatural20;
        public bool IsCriticalFumble => IsNatural1;
    }

    public struct AttackResolution
    {
        public RollResult AttackRoll;
        public int TargetAC;
        public bool IsHit;
        public bool IsCritical;
        public int DamageDealt;
        public DamageType Type;
        public string CombatMessage;
    }

    /// <summary>
    /// Implements standard D20 tabletop combat resolution rules:
    /// Initiative, Attack vs AC, Crits, Fumbles, Advantage/Disadvantage, and Damage Types.
    /// </summary>
    public static class D20RulesEngine
    {
        private static readonly System.Random Rng = new System.Random();

        public static RollResult RollD20(int modifier = 0, AdvantageMode mode = AdvantageMode.Normal)
        {
            int r1 = Rng.Next(1, 21);
            int r2 = Rng.Next(1, 21);
            int selected;

            switch (mode)
            {
                case AdvantageMode.Advantage:
                    selected = Math.Max(r1, r2);
                    break;
                case AdvantageMode.Disadvantage:
                    selected = Math.Min(r1, r2);
                    break;
                default:
                    selected = r1;
                    break;
            }

            return new RollResult
            {
                D20Roll = selected,
                FirstDie = r1,
                SecondDie = r2,
                Mode = mode,
                Modifier = modifier
            };
        }

        public static AttackResolution ResolveAttack(
            string attackerName,
            string targetName,
            int attackModifier,
            int targetAC,
            int damageDiceCount,
            int damageDieSides,
            int damageModifier,
            DamageType damageType,
            AdvantageMode advantage = AdvantageMode.Normal)
        {
            RollResult roll = RollD20(attackModifier, advantage);
            bool hit = false;
            bool crit = false;
            int damage = 0;

            if (roll.IsNatural1)
            {
                hit = false;
                crit = false;
            }
            else if (roll.IsNatural20)
            {
                hit = true;
                crit = true;
                // Critical hit doubles damage dice count
                damage = RollDice(damageDiceCount * 2, damageDieSides) + damageModifier;
            }
            else
            {
                hit = (roll.Total >= targetAC);
                if (hit)
                {
                    damage = RollDice(damageDiceCount, damageDieSides) + damageModifier;
                }
            }

            damage = Math.Max(1, damage); // Minimum 1 damage on hit

            string msg = crit
                ? $"CRITICAL HIT! {attackerName} rolls a Natural 20 against {targetName} (AC {targetAC}) for {damage} {damageType} damage!"
                : roll.IsNatural1
                ? $"CRITICAL FUMBLE! {attackerName} rolled a Natural 1 and misses {targetName} completely!"
                : hit
                ? $"{attackerName} attacks {targetName}: Rolled {roll.D20Roll}+{attackModifier} = {roll.Total} vs AC {targetAC} -> HIT! Deals {damage} {damageType} damage."
                : $"{attackerName} attacks {targetName}: Rolled {roll.D20Roll}+{attackModifier} = {roll.Total} vs AC {targetAC} -> MISS.";

            return new AttackResolution
            {
                AttackRoll = roll,
                TargetAC = targetAC,
                IsHit = hit,
                IsCritical = crit,
                DamageDealt = hit ? damage : 0,
                Type = damageType,
                CombatMessage = msg
            };
        }

        public static int RollDice(int count, int sides)
        {
            int sum = 0;
            for (int i = 0; i < count; i++)
            {
                sum += Rng.Next(1, sides + 1);
            }
            return sum;
        }
    }
}
