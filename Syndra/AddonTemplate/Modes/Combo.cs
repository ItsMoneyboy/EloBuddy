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



namespace AddonTemplate
{
    public static class Combo
    {
        public static Menu Menu
        {
            get
            {
                return MenuManager.GetSubMenu("Combo");
            }
        }
        public static bool IsActive
        {
            get
            {
                return ModeManager.IsCombo;
            }
        }
        public static void Execute()
        {
            var target = TargetSelector.Target;
            if (target.IsValidTarget())
            {
            }
        }

    }
}
