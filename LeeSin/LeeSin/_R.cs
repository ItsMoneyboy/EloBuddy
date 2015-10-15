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
    public static class _R
    {
        public static float LastCastTime = 0f;
        public static void Init()
        {
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {

            if (args.SData.Name.Equals(SpellSlot.R.GetSpellDataInst().SData.Name))
            {
                LastCastTime = Game.Time;
            }
        }

        public static bool HaveR(this Obj_AI_Base target)
        {
            return false; // C H E C K
        }

        public static bool RecentKick
        {
            get
            {
                return Game.Time - LastCastTime < 10f;
            }
        }

    }
}
