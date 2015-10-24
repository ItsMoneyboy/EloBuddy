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
                if (Menu.GetCheckBoxValue("E")) { SpellManager.CastE(target); }
                if (Menu.GetCheckBoxValue("W")) { SpellManager.CastW(target); }
                if (Menu.GetCheckBoxValue("Q")) { SpellManager.CastQ(target); }
                if (Menu.GetCheckBoxValue("WE")) { SpellManager.CastWE(target); }
                if (Menu.GetCheckBoxValue("QE")) { SpellManager.CastQE(target); }
            }
        }

    }
}
