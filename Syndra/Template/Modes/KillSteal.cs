using System;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace Template
{
    public static class KillSteal
    {
        public static Menu Menu
        {
            get
            {
                return MenuManager.GetSubMenu("KillSteal");
            }
        }
        public static void Execute()
        {
            foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies.Where(m => m.IsValidTarget(TargetSelector.Range) && m.HealthPercent < 40))
            {
                var result = enemy.GetBestCombo();
                if (result.IsKillable)
                {
                    if (Menu.GetCheckBoxValue("Q") && result.CanKillWith(SpellSlot.Q)) { SpellManager.CastQ(enemy); }
                    if (Menu.GetCheckBoxValue("W") && result.CanKillWith(SpellSlot.W)) { SpellManager.CastW(enemy); }
                    if (Menu.GetCheckBoxValue("E") && result.CanKillWith(SpellSlot.E))
                    {
                        SpellManager.CastE(enemy);
                    }
                    if (Menu.GetCheckBoxValue("Q") && Menu.GetCheckBoxValue("E") && (result.CanKillWith(SpellSlot.Q) || result.CanKillWith(SpellSlot.E)))
                    {
                        SpellManager.CastQE(enemy);
                    }
                    if (Menu.GetCheckBoxValue("W") && Menu.GetCheckBoxValue("E") && (result.CanKillWith(SpellSlot.W) || result.CanKillWith(SpellSlot.E)))
                    {
                        SpellManager.CastWE(enemy);
                    }
                    if (Menu.GetCheckBoxValue("R") && result.CanKillWith(SpellSlot.R)) { SpellManager.CastR(enemy); }
                }
                if (Menu.GetCheckBoxValue("Ignite") && SpellManager.Ignite_IsReady && Util.MyHero.GetSummonerSpellDamage(enemy, DamageLibrary.SummonerSpells.Ignite) >= enemy.Health)
                {
                    SpellManager.Ignite.Cast(enemy);
                }
            }
        }
    }
}
