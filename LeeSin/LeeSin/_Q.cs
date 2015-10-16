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
        //  T O D O
        public static Obj_AI_Base Target = null;
        public static MissileClient Missile = null;
        public static float LastCastTime = 0f;
        public static bool IsDashing = false;
        public static void Init()
        {
            Game.OnTick += Game_OnTick;
            Obj_AI_Base.OnBuffGain += Obj_AI_Base_OnBuffGain;
            Obj_AI_Base.OnBuffLose += Obj_AI_Base_OnBuffLose;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            MissileClient.OnCreate += MissileClient_OnCreate;
            MissileClient.OnDelete += MissileClient_OnDelete;
        }

        private static void Game_OnTick(EventArgs args)
        {
            SpellManager.Q1.SourcePosition = Util.myHero.Position;
            SpellManager.Q1.RangeCheckSource = Util.myHero.Position;
            if (EndTime - Game.Time < 0.25f)
            {
                if (!ModeManager.IsNone)
                {
                    Champion.ForceQ2();
                }
            }
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
                if (!sender.IsMe)
                {
                    if (args.Buff.Name.ToLower().Contains("blindmonkqone"))
                    {
                        Target = sender;
                    }
                }
                else
                {
                    if (args.Buff.Name.ToLower().Contains("blindmonkqtwodash"))
                    {
                        IsDashing = true;
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
                    if (args.Buff.Name.ToLower().Contains("blindmonkqone"))
                    {
                        Target = null;
                    }
                }
                else
                {
                    if (args.Buff.Name.ToLower().Contains("blindmonkqtwodash"))
                    {
                        IsDashing = false;
                    }
                }
            }
        }
        public static bool WillHit(Obj_AI_Base target)
        {
            if (MissileIsValid && target.IsValidTarget())
            {
                SpellManager.Q1.SourcePosition = Missile.Position;
                SpellManager.Q1.RangeCheckSource = Missile.Position;
                var pred = SpellManager.Q1.GetPrediction(target);
                var info = pred.CastPosition.To2D().ProjectOn(Missile.Position.To2D(), Missile.EndPosition.To2D());
                if (info.IsOnSegment && pred.HitChancePercent >= SpellSlot.Q.HitChancePercent() && Extensions.Distance(info.SegmentPoint, pred.CastPosition.To2D(), true) <= Math.Pow(target.BoundingRadius + SpellManager.Q1.Width, 2))
                {
                    return true;
                }
            }
            return false;
        }
        public static BuffInstance Buff
        {
            get
            {
                if (Target != null)
                {
                    foreach (BuffInstance buff in Target.Buffs)
                    {
                        if (buff.Name.ToLower().Contains("blindmonkqone"))
                        {
                            return buff;
                        }
                    }
                }
                return null;
            }
        }
        public static float EndTime
        {
            get
            {
                if (Buff != null)
                {
                    return Buff.EndTime;
                }
                return 0f;
            }
        }
        public static bool HasQ2Buff
        {
            get
            {
                return SpellSlot.Q.IsReady() && !SpellSlot.Q.IsFirstSpell();
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
