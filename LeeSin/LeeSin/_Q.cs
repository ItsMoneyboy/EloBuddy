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
    public static class _Q
    {
        public static Obj_AI_Base Target = null;
        public static MissileClient Missile = null;
        public static float LastCastTime = 0f;
        public static bool IsFlying = false;
        public static void Init()
        {
            Obj_AI_Base.OnBuffGain += Obj_AI_Base_OnBuffGain;
            Obj_AI_Base.OnBuffLose += Obj_AI_Base_OnBuffLose;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            MissileClient.OnCreate += MissileClient_OnCreate;
            MissileClient.OnDelete += MissileClient_OnDelete;
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (args.SData.Name.Equals(SpellSlot.Q.GetSpellDataInst().SData.Name) && args.SData.Name.ToLower().Contains("one"))
            {
                LastCastTime = Game.Time;
            }
        }

        public static bool HaveQ(this Obj_AI_Base unit)
        {
            return unit.IsValidTarget() && Target != null && unit.NetworkId == Target.NetworkId;
        }
        private static void MissileClient_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender is MissileClient)
            {
                var missile = sender as MissileClient;
                if (missile.SpellCaster.IsMe)
                {
                    if (missile.SData.Name.ToLower().Contains("blindmonkqone"))
                    {
                        Missile = missile;
                    }
                }
            }
        }

        private static void MissileClient_OnDelete(GameObject sender, EventArgs args)
        {
            if (sender is MissileClient)
            {
                var missile = sender as MissileClient;
                if (missile.SpellCaster.IsMe)
                {
                    if (MissileIsValid)
                    {
                        if (Missile.NetworkId == missile.NetworkId)
                        {
                            Missile = null;
                        }
                    }
                }
            }
        }

        private static void Obj_AI_Base_OnBuffGain(Obj_AI_Base sender, Obj_AI_BaseBuffGainEventArgs args)
        {

            if (args.Buff.Caster.IsMe)
            {
                Chat.Print(args.Buff.Name);
                if (args.Buff.Name.ToLower().Contains("blindmonkqone"))
                {
                    Target = sender;
                    Core.DelayAction(delegate
                    {
                        if (!ModeManager.IsNone)
                        {
                            Champion.ForceQ2();
                        }
                    }, 1000 * (int)(args.Buff.EndTime - args.Buff.StartTime) - 200);
                }
            }
        }
        private static void Obj_AI_Base_OnBuffLose(Obj_AI_Base sender, Obj_AI_BaseBuffLoseEventArgs args)
        {
            if (args.Buff.Caster.IsMe)
            {
                if (args.Buff.Name.ToLower().Contains("blindmonkqone"))
                {
                    Target = null;
                }
            }
        }
        public static bool HasQ2Buff
        {
            get
            {
                return Target != null;
            }
        }
        public static bool MissileIsValid
        {
            get
            {
                return Missile != null;
            }
        }
        public static bool IsWaitingMissile
        {
            get
            {
                return MissileIsValid || Game.Time - LastCastTime < 0.28f;
            }
        }

    }
}
