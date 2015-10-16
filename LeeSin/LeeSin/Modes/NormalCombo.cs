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
    public static class NormalCombo
    {
        public static Menu Menu
        {
            get
            {
                return Combo.Menu;
            }
        }
        public static void Execute()
        {
            var target = TargetSelector.Target;
            if (target.IsValidTarget())
            {
                if (Util.myHero.HealthPercent <= Menu.GetSliderValue("Normal.W") && (target.IsInAutoAttackRange(Util.myHero) || Util.myHero.IsInAutoAttackRange(target))) { SpellManager.CastW(Util.myHero); }
                if (Util.myHero.IsInAutoAttackRange(target) && Champion.PassiveStack > 2 - Menu.GetSliderValue("Normal.Stack")) { return; }
                if (Menu.GetCheckBoxValue("Normal.R")) { SpellManager.CastR(target); }
                if (Menu.GetCheckBoxValue("E")) { SpellManager.CastE(target); }
                if (Menu.GetCheckBoxValue("Q")) { SpellManager.CastQ(target); }
                if (Menu.GetCheckBoxValue("W") && SpellSlot.W.IsReady() && !SpellSlot.W.IsFirstSpell()) { SpellManager.CastW2(); }
                if (_Q.IsDashing || _Q.IsWaitingMissile || _Q.HasQ2Buff) { return; }
                if (Extensions.Distance(Util.myHero, target, true) > Math.Pow(400, 2) && Menu.GetCheckBoxValue("W") && SpellManager.CanCastW1)
                {
                    if (Menu.GetCheckBoxValue("Normal.Ward"))
                    {
                        Champion.GapCloseWithWard(target);
                    }
                    else
                    {
                        Champion.GapCloseWithoutWard(target);
                    }
                }
            }
        }
    }
}
