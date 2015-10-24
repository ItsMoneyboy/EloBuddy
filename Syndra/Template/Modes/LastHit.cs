using System;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace Template
{
    public static class LastHit
    {
        public static Menu Menu
        {
            get
            {
                return MenuManager.GetSubMenu("LastHit");
            }
        }
        public static bool IsActive
        {
            get
            {
                return ModeManager.IsLastHit;
            }
        }
        public static void Execute()
        {
            if (Util.MyHero.ManaPercent >= Menu.GetSliderValue("Mana"))
            {
                if (Menu.GetCheckBoxValue("Q")) { SpellManager.Q.LastHit(); }
            }
        }
    }
}
