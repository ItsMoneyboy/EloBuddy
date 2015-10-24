using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;


namespace Template
{
    public static class Damage
    {

        public static float RefreshTime = 0.4f;
        static Dictionary<int, DamageResult> PredictedDamage = new Dictionary<int, DamageResult>() { };
        static float Overkill
        {
            get
            {
                return (float)((100 + MenuManager.MiscMenu.GetSliderValue("Overkill")) / 100);
            }
        }
        public static float GetSpellDamage(this SpellSlot slot, Obj_AI_Base target)
        {
            if (target.IsValidTarget())
            {
                switch (slot)
                {
                    case SpellSlot.Q:
                        if (target is AIHeroClient && slot.GetSpellDataInst().Level == 5)
                        {
                            return Util.MyHero.CalculateDamageOnUnit(target, DamageType.Magical, (float)45 * slot.GetSpellDataInst().Level + 5 + 0.6f * Util.MyHero.FlatMagicDamageMod) * 1.15f;
                        }
                        return Util.MyHero.CalculateDamageOnUnit(target, DamageType.Magical, (float)45 * slot.GetSpellDataInst().Level + 5 + 0.6f * Util.MyHero.FlatMagicDamageMod);
                    case SpellSlot.W:
                        return Util.MyHero.CalculateDamageOnUnit(target, DamageType.Magical, (float)40 * slot.GetSpellDataInst().Level + 40 + 0.7f * Util.MyHero.FlatMagicDamageMod);
                    case SpellSlot.E:
                        return Util.MyHero.CalculateDamageOnUnit(target, DamageType.Physical, (float)45 * slot.GetSpellDataInst().Level + 25 + 0.4f * Util.MyHero.FlatPhysicalDamageMod);
                    case SpellSlot.R:
                        return (3 + BallManager.Balls.Count) * Util.MyHero.CalculateDamageOnUnit(target, DamageType.Physical, (float)45 * slot.GetSpellDataInst().Level + 45 + 0.2f * Util.MyHero.FlatPhysicalDamageMod);
                }
            }
            return Util.MyHero.GetSpellDamage(target, slot);
        }

        public static DamageResult GetComboDamage(this Obj_AI_Base target, bool q, bool w, bool e, bool r)
        {
            var ComboDamage = 0f;
            var ManaWasted = 0f;
            if (target.IsValidTarget())
            {
                if (q)
                {
                    ComboDamage += SpellSlot.Q.GetSpellDamage(target);
                    ManaWasted += SpellSlot.Q.Mana();
                }
                if (w)
                {
                    ComboDamage += SpellSlot.W.GetSpellDamage(target);
                    ManaWasted += SpellSlot.W.Mana();
                }
                if (e)
                {
                    ComboDamage += SpellSlot.E.GetSpellDamage(target);
                    ManaWasted += SpellSlot.E.Mana();
                }
                if (r)
                {
                    ComboDamage += SpellSlot.R.GetSpellDamage(target);
                    ManaWasted += SpellSlot.R.Mana();
                }
                if (SpellManager.Ignite_IsReady)
                {
                    ComboDamage += Util.MyHero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite);
                }
                if (SpellManager.Smite_IsReady)
                {
                    ComboDamage += Util.MyHero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Smite);
                }
                ComboDamage += Util.MyHero.GetAutoAttackDamage(target, true);
            }
            ComboDamage = ComboDamage * Overkill;
            return new DamageResult(target, ComboDamage, ManaWasted);
        }

        public static DamageResult GetBestCombo(this Obj_AI_Base target)
        {
            var q = SpellSlot.Q.IsReady() ? new bool[] { false, true } : new bool[] { false };
            var w = SpellSlot.W.IsReady() ? new bool[] { false, true } : new bool[] { false };
            var e = SpellSlot.E.IsReady() ? new bool[] { false, true } : new bool[] { false };
            var r = SpellSlot.R.IsReady() ? new bool[] { false, true } : new bool[] { false };
            if (target.IsValidTarget())
            {
                if (PredictedDamage.ContainsKey(target.NetworkId))
                {
                    var damageI = PredictedDamage[target.NetworkId];
                    if (Game.Time - damageI.Time <= RefreshTime)
                    {
                        return PredictedDamage[target.NetworkId];
                    }
                    else
                    {
                        bool[] best = new bool[] {
                                    SpellSlot.Q.IsReady(),
                                    SpellSlot.W.IsReady(),
                                    SpellSlot.E.IsReady(),
                                    SpellSlot.R.IsReady()
                                };
                        var bestdmg = 0f;
                        var bestmana = 0f;
                        foreach (bool r1 in r)
                        {
                            foreach (bool q1 in q)
                            {
                                foreach (bool w1 in w)
                                {
                                    foreach (bool e1 in e)
                                    {
                                        DamageResult damageI2 = target.GetComboDamage(q1, w1, e1, r1);
                                        float d = damageI2.Damage;
                                        float m = damageI2.Mana;
                                        if (Util.MyHero.Mana >= m)
                                        {
                                            if (bestdmg >= target.Health)
                                            {
                                                if (d >= target.Health && (d < bestdmg || m < bestmana))
                                                {
                                                    bestdmg = d;
                                                    bestmana = m;
                                                    best = new bool[] { q1, w1, e1, r1 };
                                                }
                                            }
                                            else
                                            {
                                                if (d >= bestdmg)
                                                {
                                                    bestdmg = d;
                                                    bestmana = m;
                                                    best = new bool[] { q1, w1, e1, r1 };
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        PredictedDamage[target.NetworkId] = new DamageResult(target, bestdmg, bestmana, best[0], best[1], best[2], best[3], Game.Time);
                        return PredictedDamage[target.NetworkId];
                    }
                }
                else
                {
                    var damageI2 = target.GetComboDamage(SpellSlot.Q.IsReady(), SpellSlot.W.IsReady(), SpellSlot.R.IsReady(), SpellSlot.R.IsReady());
                    PredictedDamage[target.NetworkId] = new DamageResult(target, damageI2.Damage, damageI2.Mana, false, false, false, false, Game.Time - (Game.Ping * 2) / 2000);
                    return target.GetBestCombo();
                }
            }
            return new DamageResult(target, 0, 0, false, false, false, false, 0);
        }
    }
}
