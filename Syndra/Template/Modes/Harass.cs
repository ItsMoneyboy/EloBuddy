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
    public static class Harass
    {
        public static Menu Menu
        {
            get
            {
                return MenuManager.GetSubMenu("Harass");
            }
        }
        public static bool IsActive
        {
            get
            {
                return ModeManager.IsHarass;
            }
        }
        public static void Execute()
        {
            if (Menu.GetSliderValue("Mana") <= Util.MyHero.ManaPercent)
            {
            }
        }
    }
}
