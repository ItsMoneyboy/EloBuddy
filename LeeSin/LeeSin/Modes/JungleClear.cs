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
    public static class JungleClear
    {
        private static float LastCastSpell;
        public static bool IsActive
        {
            get
            {
                return ModeManager.IsJungleClear;
            }
        }
        public static Menu Menu
        {
            get
            {
                return MenuManager.GetSubMenu("JungleClear");
            }
        }
        public static void Init()
        {
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
            Obj_AI_Base.OnBuffGain += Obj_AI_Base_OnBuffGain;
            Obj_AI_Base.OnBuffLose += Obj_AI_Base_OnBuffLose;
        }

        private static void Obj_AI_Base_OnBuffLose(Obj_AI_Base sender, Obj_AI_BaseBuffLoseEventArgs args)
        {
            if (args.Buff.Caster.IsMe)
            {

            }
        }

        private static void Obj_AI_Base_OnBuffGain(Obj_AI_Base sender, Obj_AI_BaseBuffGainEventArgs args)
        {
            if (args.Buff.Caster.IsMe)
            {

            }
        }

        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (sender.IsCastingSpell && IsActive)
            {
            }
        }

        public static void Execute()
        {
            if (SpellManager.Smite_IsReady)
            {
                if (Menu.GetCheckBoxValue("Smite"))
                {
                    var dragon = EntityManager.MinionsAndMonsters.GetJungleMonsters(Util.myHero.Position, SpellManager.Q2.Range, true).Where(m => m.IsInSmiteRange() && m.IsDragon()).FirstOrDefault();
                    if (dragon != null)
                    {
                        if (dragon.Health <= Util.myHero.GetSummonerSpellDamage(dragon, DamageLibrary.SummonerSpells.Smite))
                        {
                            Util.myHero.Spellbook.CastSpell(SpellManager.Smite.Slot, dragon);
                        }
                    }
                }
            }
            var minion = EntityManager.MinionsAndMonsters.GetJungleMonsters(Util.myHero.Position, SpellManager.Q1.Range, true).Where(m => m.IsValidTarget()).OrderBy(m => m.MaxHealth).LastOrDefault();
            if (minion != null)
            {
                if (minion.IsDragon() && SpellManager.Smite_IsReady && SpellSlot.Q.IsReady())
                {
                    if (2.5f * SpellSlot.Q.GetSpellDamage(minion, 2) + Util.myHero.GetSummonerSpellDamage(minion, DamageLibrary.SummonerSpells.Smite) >= minion.Health && SpellSlot.Q.GetSpellDamage(minion, 2) + Util.myHero.GetSummonerSpellDamage(minion, DamageLibrary.SummonerSpells.Smite) <= minion.Health)
                    {
                        return;
                    }
                }
                if (Champion.PassiveStack > 0 && Util.myHero.IsInAutoAttackRange(minion)) { return; }
                if (Menu.GetCheckBoxValue("W") && minion.IsInAutoAttackRange(Util.myHero) && Util.myHero.HealthPercent <= 40)
                {
                    if (SpellSlot.W.IsReady() && SpellSlot.W.IsFirstSpell())
                    {
                        Util.myHero.Spellbook.CastSpell(SpellSlot.W, Util.myHero, true);
                        return;
                    }
                }
                if (SpellSlot.Q.IsReady() && !SpellSlot.Q.IsFirstSpell() && Menu.GetCheckBoxValue("Q"))
                {
                    Util.myHero.Spellbook.CastSpell(SpellSlot.Q, true);
                    return;
                }
                if (SpellSlot.W.IsReady() && !SpellSlot.W.IsFirstSpell() && Menu.GetCheckBoxValue("W"))
                {
                    Util.myHero.Spellbook.CastSpell(SpellSlot.W, true);
                    return;
                }
                if (SpellSlot.E.IsReady() && !SpellSlot.E.IsFirstSpell() && Menu.GetCheckBoxValue("E") && Extensions.Distance(minion, Util.myHero, true) <= Math.Pow(SpellManager.E_Range, 2))
                {
                    Util.myHero.Spellbook.CastSpell(SpellSlot.E, true);
                    return;
                }
                if (SpellSlot.Q.IsReady() && SpellSlot.Q.IsFirstSpell() && Menu.GetCheckBoxValue("Q"))
                {
                    var pred = SpellManager.Q1.GetPrediction(minion);
                    if (pred.HitChancePercent >= SpellSlot.Q.HitChancePercent())
                    {
                        Util.myHero.Spellbook.CastSpell(SpellSlot.Q, pred.CastPosition, true);
                        return;
                    }
                }
                if (SpellSlot.E.IsReady() && SpellSlot.E.IsFirstSpell() && Menu.GetCheckBoxValue("E") && Extensions.Distance(minion, Util.myHero, true) <= Math.Pow(SpellManager.E_Range, 2))
                {
                    Util.myHero.Spellbook.CastSpell(SpellSlot.E, true);
                    return;
                }
                if (Menu.GetCheckBoxValue("W") && minion.IsInAutoAttackRange(Util.myHero))
                {
                    if (SpellSlot.W.IsReady() && SpellSlot.W.IsFirstSpell())
                    {
                        Util.myHero.Spellbook.CastSpell(SpellSlot.W, Util.myHero, true);
                        return;
                    }
                }
            }
        }
        public static bool IsDragon(this Obj_AI_Minion minion)
        {
            return minion.IsValidTarget() && (minion.Name.ToLower().Contains("baron") || minion.Name.ToLower().Contains("dragon"));
        }
    }
}
