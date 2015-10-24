using System;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace Template
{
    public static class JungleClear
    {
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
        
        public static void Execute()
        {
            if (Util.MyHero.ManaPercent >= Menu.GetSliderValue("Mana"))
            {
                if (Menu.GetCheckBoxValue("E")) { SpellManager.CastE(SpellManager.E.JungleClear(false)); }
                if (Menu.GetCheckBoxValue("W")) { SpellManager.CastW(SpellManager.W.JungleClear(false)); }
                if (Menu.GetCheckBoxValue("Q")) { SpellManager.CastQ(SpellManager.Q.JungleClear(false)); }
            }
        }
    }
}
