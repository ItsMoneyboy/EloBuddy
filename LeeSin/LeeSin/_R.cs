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
        public static float BuffEndTime = 0f;
        public static Obj_AI_Base Target = null;

        public static Vector3 StartPos = Vector3.Zero;
        public static void Init()
        {
            Game.OnTick += Game_OnTick;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Obj_AI_Base.OnBuffGain += Obj_AI_Base_OnBuffGain;
            Obj_AI_Base.OnBuffLose += Obj_AI_Base_OnBuffLose;
        }

        private static void Game_OnTick(EventArgs args)
        {
            if (HasEndBuff)
            {
                if (!ModeManager.IsNone)
                {
                    Champion.ForceQ2();
                }
            }
        }

        private static void Obj_AI_Base_OnBuffGain(Obj_AI_Base sender, Obj_AI_BaseBuffGainEventArgs args)
        {
            if (args.Buff.Caster.IsMe)
            {
                if (!sender.IsMe)
                {
                    if (args.Buff.Name.ToLower().Contains("blindmonkrkick"))
                    {
                        //Chat.Print("Delay: " + (Game.Time - LastCastTime));
                        Target = sender;
                        BuffEndTime = args.Buff.EndTime;
                        StartPos = new Vector3(sender.Position.X, sender.Position.Y, sender.Position.Z);
                    }
                }
            }
        }

        private static void Obj_AI_Base_OnBuffLose(Obj_AI_Base sender, Obj_AI_BaseBuffLoseEventArgs args)
        {

            if (args.Buff.Caster.IsMe)
            {
                if (!sender.IsMe)
                {
                    if (args.Buff.Name.ToLower().Contains("blindmonkrkick"))
                    {
                        Target = null;
                        //Chat.Print("Speed: " + Extensions.Distance(StartPos, sender)/(args.Buff.EndTime - args.Buff.StartTime));
                        if (sender.HaveQ())
                        {
                            if (!ModeManager.IsNone)
                            {
                                Champion.ForceQ2();
                            }
                        }
                    }
                }
            }
        }


        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (args.SData.Name.Equals(SpellSlot.R.GetSpellDataInst().SData.Name))
            {
                LastCastTime = Game.Time;
            }
        }

        public static bool HaveR(this Obj_AI_Base unit)
        {
            return unit.IsValidTarget() && Target != null && unit.NetworkId == Target.NetworkId;
        }
        public static bool HasEndBuff
        {
            get
            {
                return Game.Time - BuffEndTime < 0.5f;
            }
        }
        public static bool IsRecentKick
        {
            get
            {
                return Game.Time - LastCastTime < 10f;
            }
        }

    }
}
