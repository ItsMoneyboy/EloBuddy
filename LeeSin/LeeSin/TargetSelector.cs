﻿using System;
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
    public static class TargetSelector
    {
        public static DamageType damageType;
        public static AIHeroClient ForcedTarget;
        public static float Range;
        public static void Init(float range, DamageType d)
        {
            Game.OnWndProc += Game_OnWndProc;
            damageType = d;
        }
        
        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg == (uint)WindowMessages.LeftButtonDown)
            {
                var target = EloBuddy.SDK.TargetSelector.GetTarget(250f, damageType, Util.mousePos);
                if (target.IsValidTarget())
                {
                    ForcedTarget = target;
                }
            }
        }
        public static AIHeroClient Target
        {
            get
            {
                if (ForcedTarget != null && ForcedTarget.IsValidTarget() && Extensions.Distance(Util.myHero, ForcedTarget, true) < Range * Range)
                {
                    return ForcedTarget;
                }
                return EloBuddy.SDK.TargetSelector.GetTarget(Range, damageType, Util.myHero.Position);
            }
        }


    }
}
