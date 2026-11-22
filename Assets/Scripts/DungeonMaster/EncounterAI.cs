using System;
using UnityEngine;

namespace TheHouseIsAlive.DungeonMaster
{
    public enum AIStance
    {
        Aggressive,
        Tactical,
        Defensive,
        Fleeing
    }

    public enum CombatActionType
    {
        MeleeAttack,
        RangedAttack,
        CastSpell,
        Dodge,
        Disengage,
        Flee
    }

    public struct AICombatDecision
    {
        public CombatActionType Action;
        public string ActionName;
        public int AttackModifier;
        public int DamageDiceCount;
        public int DamageDieSides;
        public int DamageModifier;
        public DamageType DamageType;
        public string Rationale;
    }

    /// <summary>
    /// Automated tactical enemy AI engine for D&D-style encounters.
    /// Evaluates current health, distance, spell slots, and player AC to select the optimal combat action.
    /// </summary>
    public class EncounterAI : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private string enemyName = "Gargoyle";
        [SerializeField] private int maxHealth = 45;
        [SerializeField] private int currentHealth = 45;
        [SerializeField] private int armorClass = 15;
        [SerializeField] private int spellSlots = 2;

        public AIStance CurrentStance { get; private set; } = AIStance.Aggressive;
        public int CurrentHealth => currentHealth;
        public int ArmorClass => armorClass;

        public void ApplyDamage(int amount)
        {
            currentHealth = Mathf.Max(0, currentHealth - amount);
            UpdateStance();
        }

        public void UpdateStance()
        {
            float hpPct = (float)currentHealth / maxHealth;

            if (hpPct <= 0.20f)
            {
                CurrentStance = AIStance.Fleeing;
            }
            else if (hpPct <= 0.45f)
            {
                CurrentStance = AIStance.Defensive;
            }
            else if (spellSlots > 0)
            {
                CurrentStance = AIStance.Tactical;
            }
            else
            {
                CurrentStance = AIStance.Aggressive;
            }
        }

        public AICombatDecision DecideTurnAction(float distanceToPlayer, int playerAC)
        {
            UpdateStance();

            switch (CurrentStance)
            {
                case AIStance.Fleeing:
                    return new AICombatDecision
                    {
                        Action = CombatActionType.Flee,
                        ActionName = "Disengage & Flee",
                        Rationale = $"{enemyName} is near death ({currentHealth}/{maxHealth} HP) and panics, attempting to escape!"
                    };

                case AIStance.Defensive:
                    if (distanceToPlayer < 2.0f)
                    {
                        return new AICombatDecision
                        {
                            Action = CombatActionType.Dodge,
                            ActionName = "Defensive Dodge",
                            Rationale = $"{enemyName} raises its stone wings into a defensive stance, imposing disadvantage on incoming attacks."
                        };
                    }
                    else
                    {
                        return new AICombatDecision
                        {
                            Action = CombatActionType.RangedAttack,
                            ActionName = "Hurl Stone",
                            AttackModifier = 4,
                            DamageDiceCount = 1,
                            DamageDieSides = 6,
                            DamageModifier = 2,
                            DamageType = DamageType.Bludgeoning,
                            Rationale = $"{enemyName} keeps safe distance and hurls a chunk of stone."
                        };
                    }

                case AIStance.Tactical:
                    if (spellSlots > 0 && distanceToPlayer > 3.0f)
                    {
                        spellSlots--;
                        return new AICombatDecision
                        {
                            Action = CombatActionType.CastSpell,
                            ActionName = "Chaos Fireball",
                            AttackModifier = 6,
                            DamageDiceCount = 3,
                            DamageDieSides = 6,
                            DamageModifier = 3,
                            DamageType = DamageType.Fire,
                            Rationale = $"{enemyName} channels an explosive spell from optimal range."
                        };
                    }
                    goto case AIStance.Aggressive;

                case AIStance.Aggressive:
                default:
                    return new AICombatDecision
                    {
                        Action = CombatActionType.MeleeAttack,
                        ActionName = "Rending Claws",
                        AttackModifier = 5,
                        DamageDiceCount = 2,
                        DamageDieSides = 6,
                        DamageModifier = 3,
                        DamageType = DamageType.Slashing,
                        Rationale = $"{enemyName} lunges forward with razor-sharp stone claws."
                    };
            }
        }
    }
}
