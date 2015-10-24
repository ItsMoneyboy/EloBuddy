using System;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;


namespace Template
{
    public static class LaneClear
    {
        public static Menu Menu
        {
            get
            {
                return MenuManager.GetSubMenu("LaneClear");
            }
        }
        public static bool IsActive
        {
            get
            {
                return ModeManager.IsLaneClear;
            }
        }
        public static void Execute()
        {
            if (Util.MyHero.ManaPercent >= Menu.GetSliderValue("Mana"))
            {
                if (Menu.GetCheckBoxValue("Q2")) { SpellManager.Q.LastHit(); }
                SpellManager.Q.LaneClear(Menu.GetSliderValue("Q"));
                var count = SpellManager.IsW2 ? (Menu.GetSliderValue("W") - 1) : Menu.GetSliderValue("W");
                SpellManager.CastW(SpellManager.W.LaneClear(count, false));
            }
        }
    }
}
