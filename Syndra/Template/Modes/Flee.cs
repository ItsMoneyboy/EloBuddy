using System;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;



namespace Template
{
    public static class Flee
    {
        public static Menu Menu
        {
            get
            {
                return MenuManager.GetSubMenu("Flee");
            }
        }
        public static bool IsActive
        {
            get
            {
                return ModeManager.IsFlee;
            }
        }
        public static void Execute()
        {
            if (Menu.GetCheckBoxValue("E"))
            {
                var target = EloBuddy.SDK.TargetSelector.GetTarget(500, DamageType.Magical, Util.MousePos);
                if (target.IsValidTarget())
                {
                    SpellManager.CastE(target);
                    SpellManager.CastQE(target);
                    SpellManager.CastWE(target);
                }
            }
        }
    }
}
