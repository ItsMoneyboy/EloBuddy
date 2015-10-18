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
        public static bool IsDashing = false;
        private static MissileClient Missile = null;
        private static float LastCastTime = 0f;
        private static Obj_AI_Minion Smite_Target = null;
        public static void Init()
        {
            Game.OnUpdate += Game_OnTick;
            Obj_AI_Base.OnBuffGain += Obj_AI_Base_OnBuffGain;
            Obj_AI_Base.OnBuffLose += Obj_AI_Base_OnBuffLose;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            MissileClient.OnCreate += MissileClient_OnCreate;
            MissileClient.OnDelete += MissileClient_OnDelete;
        }

        private static void Game_OnTick(EventArgs args)
        {
            if (MissileIsValid)
            {
                SpellManager.Q1.SourcePosition = Missile.Position;
                SpellManager.Q1.RangeCheckSource = Missile.Position;
                SpellManager.Q1.AllowedCollisionCount = int.MaxValue;
                SpellManager.Q1.CastDelay = 0;
            }
            else
            {
                SpellManager.Q1.SourcePosition = Util.myHero.Position;
                SpellManager.Q1.RangeCheckSource = Util.myHero.Position;
                SpellManager.Q1.AllowedCollisionCount = 0;
                SpellManager.Q1.CastDelay = 250;
            }
            if (IsTryingToSmite)
            {
                if (IsWaitingMissile)//
                {
                    var CanSmite = false;
                    if (Game.Time - LastCastTime <= 0.25f)
                    {
                        if (SpellManager.Q1.Width + Smite_Target.BoundingRadius > Extensions.Distance(Util.myHero, Smite_Target))
                        {
                            CanSmite = true;
                        }
                    }
                    else if (WillHit(Smite_Target))
                    {
                        var pred = SpellManager.Q1.GetPrediction(Smite_Target);
                        var width = Smite_Target.BoundingRadius + SpellManager.Q1.Width;//
                        var TimeToArriveQ = (Extensions.Distance(Missile, pred.CastPosition) - width) / SpellManager.Q1.Speed - SpellManager.Smite_Delay + Game.Ping / 2000 - 0.1f;
                        if (TimeToArriveQ <= 0)
                        {
                            CanSmite = true;
                        }
                    }
                    if (CanSmite)
                    {
                        Util.myHero.Spellbook.CastSpell(SpellManager.Smite.Slot, Smite_Target);
                    }
                }
            }
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
            if (sender.IsMe)
            {
                if (args.SData.Name.Equals(SpellSlot.Q.GetSpellDataInst().SData.Name))
                {
                    if (args.SData.Name.ToLower().Contains("one"))
                    {
                        LastCastTime = Game.Time;
                    }
                    else
                    {
                        IsDashing = true;
                    }
                }
            }
        }

        public static bool HaveQ(this Obj_AI_Base unit)
        {
            return unit.IsValidTarget() && Target != null && unit.NetworkId == Target.NetworkId;
        }
        private static void MissileClient_OnCreate(GameObject sender, EventArgs args)
        {
            if (IsWaitingMissile)
            {
                if (sender != null && sender is MissileClient)
                {
                    var missile = sender as MissileClient;
                    if (missile.SpellCaster.IsMe)
                    {
                        if (missile.SData.Name.ToLower().Contains("blindmonkqone"))
                        {
                            Missile = missile;
                            Core.DelayAction(delegate { Missile = null; }, 1000 * (int)(2 * Extensions.Distance(Missile, Missile.EndPosition) / SpellManager.Q1.Speed));
                        }
                    }
                }
            }
        }

        private static void MissileClient_OnDelete(GameObject sender, EventArgs args)
        {
            if (MissileIsValid)
            {
                if (sender != null && sender is MissileClient)
                {
                    var missile = sender as MissileClient;
                    if (missile.SpellCaster.IsMe)
                    {
                        if (Missile.NetworkId == missile.NetworkId)
                        {
                            Smite_Target = null;
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
        public static void CheckSmite(Obj_AI_Base target)
        {
            if (target is AIHeroClient && SpellManager.CanCastQ1 && target.IsValidTarget())
            {
                SpellManager.Q1.AllowedCollisionCount = int.MaxValue;
                var pred = SpellManager.Q1.GetPrediction(target);
                if (pred.HitChancePercent >= SpellSlot.Q.HitChancePercent())
                {
                    var minions = pred.GetCollisionObjects<Obj_AI_Minion>().Where(m => m.IsValidTarget() && Extensions.Distance(Util.myHero, m, true) < Extensions.Distance(Util.myHero, target, true));
                    var CanSmite = (Combo.IsActive && AutoSmite.Menu.GetCheckBoxValue("Q.Combo")) || (Harass.IsActive && AutoSmite.Menu.GetCheckBoxValue("Q.Harass")) || (Insec.IsActive && AutoSmite.Menu.GetCheckBoxValue("Q.Insec"));
                    if (SpellManager.Smite_IsReady && minions.Count() == 1 && CanSmite)
                    {
                        var collision = minions.FirstOrDefault();
                        if (collision is Obj_AI_Minion && collision.IsInSmiteRange())
                        {
                            var minion = collision as Obj_AI_Minion;
                            int time = SpellManager.Q1.CastDelay + 1000 * (int)(Extensions.Distance(Util.myHero, minion) / SpellManager.Q1.Speed) + (int)SpellManager.Smite_Delay * 1000 - 70;
                            if (Prediction.Health.GetPrediction(minion, time) <= Util.myHero.GetSummonerSpellDamage(minion, DamageLibrary.SummonerSpells.Smite))
                            {
                                Smite_Target = minion;
                                SpellManager.Q1.Cast(pred.CastPosition);
                            }
                        }
                    }
                    else if (pred.CollisionObjects.Where(m => m.IsValidTarget() && Extensions.Distance(Util.myHero, m, true) < Extensions.Distance(Util.myHero, target, true)).Count() == 0)
                    {
                        SpellManager.Q1.Cast(pred.CastPosition);
                    }
                }
            }
        }
        public static bool WillHit(Obj_AI_Base target)
        {
            if (MissileIsValid && target.IsValidTarget())
            {
                var pred = SpellManager.Q1.GetPrediction(target);
                var info = pred.CastPosition.To2D().ProjectOn(Missile.StartPosition.To2D(), Missile.EndPosition.To2D());
                float hitchancepercent = (target is AIHeroClient) ? SpellSlot.Q.HitChancePercent() : 0;
                if (info.IsOnSegment && pred.HitChancePercent >= hitchancepercent && Extensions.Distance(info.SegmentPoint, pred.CastPosition.To2D(), true) <= Math.Pow(target.BoundingRadius + SpellManager.Q1.Width, 2))
                {
                    return true;
                }
            }
            return false;
        }
        private static BuffInstance Buff
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
        public static bool IsTryingToSmite
        {
            get
            {
                return Smite_Target != null && Smite_Target.IsValidTarget() && SpellManager.Smite_IsReady;
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
        public static bool IsValidTarget
        {
            get
            {
                return Target != null && Target.IsValidTarget();
            }
        }
        public static bool HasQ2Buff
        {
            get
            {
                return (SpellSlot.Q.IsReady() && !SpellSlot.Q.IsFirstSpell()) || IsValidTarget;
            }
        }
        private static bool MissileIsValid
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
                return MissileIsValid || Game.Time - LastCastTime <= 0.29f;
            }
        }

    }
}
