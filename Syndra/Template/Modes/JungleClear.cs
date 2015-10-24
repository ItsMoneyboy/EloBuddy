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
            }
        }
    }
}
