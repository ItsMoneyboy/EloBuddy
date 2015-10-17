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
    public static class DrawManager
    {
        public static Menu Menu
        {
            get
            {
                return MenuManager.GetSubMenu("Drawings");
            }
        }
        public static void Init()
        {
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Util.myHero.IsDead) { return; }
            if (Menu.GetCheckBoxValue("Disable")) { return; }
            if (MenuManager.DrawingsMenu.GetCheckBoxValue("Target") && TargetSelector.Target != null && TargetSelector.Target.IsValidTarget())
            {
                Circle.Draw(Color.Red, 150f, 5, TargetSelector.Target.Position);
            }
            if (Menu.GetCheckBoxValue("Combo.Mode"))
            {
                var pos = Util.myHero.Position.WorldToScreen();
                pos.X = pos.X - 50;
                Drawing.DrawText(pos, System.Drawing.Color.White, "Combo Mode: " + Combo.Menu["Mode"].Cast<Slider>().DisplayName, 15);
            }

            if (Menu.GetCheckBoxValue("Insec.Line") && Insec.IsReady)
            {
                var blue = System.Drawing.Color.Blue;
                var target = TargetSelector.Target;
                if (target.IsValidTarget())
                {
                    Drawing.DrawLine(target.Position.WorldToScreen(), Insec.EndPosition.WorldToScreen(), 2, blue);
                }
            }
            var color = new ColorBGRA(255, 255, 255, 100);
            if (Menu.GetCheckBoxValue("Q") && SpellSlot.Q.IsReady())
            {
                Circle.Draw(color, SpellManager.Q_Range, Util.myHero.Position);
            }
            if (Menu.GetCheckBoxValue("W") && SpellSlot.W.IsReady())
            {
                Circle.Draw(color, SpellManager.W_Range, Util.myHero.Position);
            }
            if (Menu.GetCheckBoxValue("E") && SpellSlot.E.IsReady())
            {
                Circle.Draw(color, SpellManager.E_Range, Util.myHero.Position);
            }
            if (Menu.GetCheckBoxValue("R") && SpellSlot.R.IsReady())
            {
                Circle.Draw(color, SpellManager.R.Range, Util.myHero.Position);
            }
        }
    }
}
