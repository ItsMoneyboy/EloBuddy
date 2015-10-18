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
        }

        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (sender.IsCastingSpell)
            {
                LastCastSpell = Game.Time;
            }
        }

        public static bool IsActive
        {
            get
            {
                return ModeManager.IsJungleClear;
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
                if (Champion.PassiveStack > 0 && Util.myHero.IsInAutoAttackRange(minion)) { return; }
                if (SpellSlot.Q.IsReady() && !SpellSlot.Q.IsFirstSpell() && Menu.GetCheckBoxValue("Q"))
                {
                    SpellManager.CastQ2(_Q.Target);
                    return;
                }
                if (SpellSlot.W.IsReady() && !SpellSlot.W.IsFirstSpell() && Menu.GetCheckBoxValue("W"))
                {
                    SpellManager.CastW2();
                    return;
                }
                if (SpellSlot.E.IsReady() && !SpellSlot.E.IsFirstSpell() && Menu.GetCheckBoxValue("E"))
                {
                    SpellManager.CastE2(minion);
                    return;
                }
                if (minion.IsDragon() && SpellManager.Smite_IsReady && SpellSlot.Q.IsReady())
                {
                    if (2.5f * SpellSlot.Q.GetSpellDamage(minion, 2) + Util.myHero.GetSummonerSpellDamage(minion, DamageLibrary.SummonerSpells.Smite) >= minion.Health && SpellSlot.Q.GetSpellDamage(minion, 2) + Util.myHero.GetSummonerSpellDamage(minion, DamageLibrary.SummonerSpells.Smite) <= minion.Health)
                    {
                        return;
                    }
                }
                if (Game.Time - LastCastSpell < 0.25) { return; }
                if (Menu.GetCheckBoxValue("Q")) { SpellManager.CastQ(minion); }
                if (Game.Time - LastCastSpell < 0.25) { return; }
                if (Menu.GetCheckBoxValue("W") && minion.IsInAutoAttackRange(Util.myHero)) { SpellManager.CastW(Util.myHero); }
                if (Game.Time - LastCastSpell < 0.25) { return; }
                if (Menu.GetCheckBoxValue("E")) { SpellManager.CastE(minion); }
            }
        }
        private static bool IsDragon(this Obj_AI_Minion minion)
        {
            return minion.IsValidTarget() && (minion.Name.ToLower().Contains("baron") || minion.Name.ToLower().Contains("dragon"));
        }
    }
}
