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

namespace LeeSin
{
    public static class Damage
    {

        public static float RefreshTime = 0.4f;
        static Dictionary<int, DamageInfo> PredictedDamage = new Dictionary<int, DamageInfo>() { };
        static float Overkill
        {
            get
            {
                return (float)((100 + MenuManager.MiscMenu.GetSliderValue("Overkill")) / 100);
            }
        }
        public static float GetSpellDamage(this Obj_AI_Base target, SpellSlot slot)
        {
            if (target.IsValidTarget())
            {
                if (slot == SpellSlot.W)
                {
                }
                else if (slot == SpellSlot.E)
                {
                    return Util.myHero.CalculateDamageOnUnit(target, DamageType.Physical, (float)35 * Util.myHero.Spellbook.GetSpell(SpellSlot.E).Level + 35 + 0.5f * Util.myHero.FlatPhysicalDamageMod);
                }
                else if (slot == SpellSlot.R)
                {
                    return 2 * Util.myHero.CalculateDamageOnUnit(target, DamageType.Physical, (float)100 * Util.myHero.Spellbook.GetSpell(SpellSlot.R).Level + 75 + 1.1f * Util.myHero.FlatPhysicalDamageMod);
                }
            }
            return Util.myHero.GetSpellDamage(target, slot);
        }

        public static DamageInfo GetComboDamage(this Obj_AI_Base target, bool q, bool w, bool e, bool r)
        {
            var ComboDamage = 0f;
            var ManaWasted = 0f;
            if (target.IsValidTarget())
            {
                if (q)
                {
                    ComboDamage += target.GetSpellDamage(SpellSlot.Q);
                    ManaWasted += Util.myHero.Spellbook.GetSpell(SpellSlot.Q).SData.Mana;
                }
                if (w)
                {
                    ComboDamage += target.GetSpellDamage(SpellSlot.W);
                    ManaWasted += Util.myHero.Spellbook.GetSpell(SpellSlot.W).SData.Mana;
                }
                if (e)
                {
                    ComboDamage += target.GetSpellDamage(SpellSlot.E);
                    ManaWasted += Util.myHero.Spellbook.GetSpell(SpellSlot.E).SData.Mana;
                }
                if (r)
                {
                    ComboDamage += target.GetSpellDamage(SpellSlot.R);
                    ManaWasted += Util.myHero.Spellbook.GetSpell(SpellSlot.R).SData.Mana;
                }
                if (SpellManager.Ignite != null && SpellManager.Ignite.IsReady())
                {
                    ComboDamage += Util.myHero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite);
                }
                if (SpellManager.Smite != null && SpellManager.Smite.IsReady())
                {
                    ComboDamage += Util.myHero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Smite);
                }
                ComboDamage += Util.myHero.GetAutoAttackDamage(target, true);
            }
            ComboDamage = ComboDamage * Overkill;
            return new DamageInfo(ComboDamage, ManaWasted);
        }

        public static DamageInfo GetBestCombo(this Obj_AI_Base target)
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
                        return damageI;
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
                                        DamageInfo damageI2 = target.GetComboDamage(q1, w1, e1, r1);
                                        float d = damageI2.Damage;
                                        float m = damageI2.Mana;
                                        if (Util.myHero.Mana >= m)
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
                        PredictedDamage[target.NetworkId] = new DamageInfo(target, bestdmg, bestmana, best[0], best[1], best[2], best[3], Game.Time);
                        return PredictedDamage[target.NetworkId];
                    }
                }
                else
                {
                    var damageI2 = target.GetComboDamage(SpellSlot.Q.IsReady(), SpellSlot.W.IsReady(), SpellSlot.R.IsReady(), SpellSlot.R.IsReady());
                    PredictedDamage[target.NetworkId] = new DamageInfo(target, damageI2.Damage, damageI2.Mana, false, false, false, false, Game.Time - (Game.Ping * 2) / 2000);
                    return target.GetBestCombo();
                }
            }
            return new DamageInfo(target, 0, 0, false, false, false, false, 0);
        }
    }
}
