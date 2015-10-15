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
    public static class Combo
    {
        public static Menu Menu
        {
            get
            {
                return MenuManager.GetSubMenu("Combo");
            }
        }
        public static void Execute()
        {
            var target = TargetSelector.Target;
            if (target.IsValidTarget())
            {
                var comboMode = Menu.GetSliderValue("Mode");
                if (Menu.GetCheckBoxValue("Items")) { ItemManager.UseOffensiveItems(target); }
                if (Util.myHero.IsInAutoAttackRange(target) && Champion.PassiveStack > 2 - Menu.GetSliderValue("Stack")) { return; }
                switch (comboMode)
                {
                    case 0:
                        NormalCombo();
                        break;
                    case 1:
                        StarCombo();
                        break;
                    case 2:
                        GankCombo();
                        break;
                    default:
                        break;
                }
            }
        }
        public static void NormalCombo()
        {
            var target = TargetSelector.Target;
            if (target.IsValidTarget())
            {
                if (Menu.GetCheckBoxValue("E")) { SpellManager.CastE(target); }
                if (Menu.GetCheckBoxValue("Q")) { SpellManager.CastQ(target); }
                if (Menu.GetCheckBoxValue("W") && SpellSlot.W.IsReady() && !SpellSlot.W.IsFirstSpell()) { SpellManager.CastW2(); }
                if (_Q.IsFlying || _Q.IsWaitingMissile || _Q.HasQ2Buff) { return; }
                if (Extensions.Distance(Util.myHero, target, true) > Math.Pow(450, 2) && Menu.GetCheckBoxValue("W") && SpellManager.CanCastW1)
                {
                    if (Menu.GetCheckBoxValue("Ward"))
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

        public static void StarCombo()
        {
            var target = TargetSelector.Target;
            if (target.IsValidTarget())
            {
                if (Menu.GetSliderValue("StarMode") == 0 && Menu.GetCheckBoxValue("W") && Menu.GetCheckBoxValue("Ward") && WardManager.CanCastWard && SpellManager.CanCastW1 && Insec.IsReady)
                {
                    Insec.Execute();
                }
                if (Insec.IsRecent) { return; }
                if (Menu.GetCheckBoxValue("Q") && SpellSlot.Q.IsReady())
                {
                    if (SpellSlot.Q.IsFirstSpell())
                    {

                        if (Menu.GetSliderValue("StarMode") == 0)
                        {
                            SpellManager.CastQ1(target);
                        }
                        else if (Menu.GetSliderValue("StarMode") == 1)
                        {
                            if (target.HaveR())
                            {
                                SpellManager.CastQ1(target, 1);
                            }
                        }
                    }
                    else
                    {
                        if (!_R.RecentKick && !SpellSlot.R.IsReady())
                        {
                            SpellManager.CastQ2(target);
                        }
                    }
                }
                if (Menu.GetCheckBoxValue("E") && SpellSlot.E.IsReady())
                {
                    if (!SpellSlot.R.IsReady())
                    {
                        SpellManager.CastE(target);
                    }
                }
                if (Menu.GetCheckBoxValue("R") && SpellSlot.R.IsReady())
                {
                    if (Menu.GetSliderValue("StarMode") == 0)
                    {
                        if (target.HaveQ())
                        {
                            SpellManager.CastR(target);
                        }
                    }
                    else if (Menu.GetSliderValue("StarMode") == 1)
                    {
                        if (SpellSlot.Q.IsReady() && SpellSlot.Q.IsFirstSpell())
                        {
                            SpellManager.CastR(target);
                        }
                    }
                }
            }
        }
        public static void GankCombo()
        {
            var target = TargetSelector.Target;
            if (target.IsValidTarget())
            {
                if (Menu.GetCheckBoxValue("W") && target.IsValidTarget(900f))
                {
                    if (Menu.GetCheckBoxValue("Ward") && WardManager.CanCastWard)
                    {
                        if (Insec.IsReady && Menu.GetCheckBoxValue("R"))
                        {
                            Insec.Execute(); // C H E C K
                        }
                        else if (Extensions.Distance(Util.myHero, target, true) > Math.Pow(450, 2) && SpellManager.CanCastW1)
                        {
                            Champion.GapCloseWithWard(target);
                        }
                    }
                    else
                    {
                        Champion.GapCloseWithoutWard(target);
                    }
                }
                if (SpellManager.CanCastW1 && !target.IsValidTarget(SpellManager.W_Range)) { return; }
                if (Menu.GetCheckBoxValue("E") && !SpellSlot.Q.IsReady()) { SpellManager.CastE(target); }
                if (Menu.GetCheckBoxValue("Q")) { SpellManager.CastQ(target); }
            }
        }
    }
}
