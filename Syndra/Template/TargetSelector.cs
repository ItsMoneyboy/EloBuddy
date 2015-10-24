using System;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;



namespace Template
{
    public static class TargetSelector
    {
        public static DamageType damageType;
        public static AIHeroClient ForcedTarget;
        public static float Range;
        public static void Init(float range, DamageType d)
        {
            damageType = d;
            Range = range;
            Game.OnWndProc += Game_OnWndProc;
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg == (uint)WindowMessages.LeftButtonDown)
            {
                var target = EloBuddy.SDK.TargetSelector.GetTarget(200f, damageType, Util.MousePos);
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
                if (ForcedTarget != null)
                {
                    if (ForcedTarget.IsValidTarget(Range))
                    {
                        return ForcedTarget;
                    }
                }
                return EloBuddy.SDK.TargetSelector.GetTarget(Range, damageType, Util.MyHero.Position);
            }
        }


    }
}
