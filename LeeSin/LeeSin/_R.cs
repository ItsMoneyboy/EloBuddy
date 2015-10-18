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
                                Champion.ForceQ2(sender);
                            }
                        }
                    }
                }
            }
        }

        public static Tuple<int, AIHeroClient> BestHitR(int minhits)
        {
            var BestCount = 0;
            AIHeroClient BestTarget = null;
            if (SpellSlot.R.IsReady() && EntityManager.Heroes.Enemies.Where(m => m.IsValidTarget(SpellManager.RKick.Range)).Count() >= minhits)
            {
                foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies.Where(m => m.IsValidTarget(SpellManager.R.Range)))
                {
                    var width = SpellManager.RKick.Width;//enemy.BoundingRadius;
                    var startpos = enemy.Position.To2D();
                    var endpos = startpos + (startpos - Util.myHero.Position.To2D()).Normalized() * SpellManager.RKick.Range;
                    List<Tuple<Vector2, float>> positions = new List<Tuple<Vector2, float>>();
                    SpellManager.RKick.SourcePosition = enemy.Position;
                    foreach (AIHeroClient enemy2 in EntityManager.Heroes.Enemies.Where(m => m.IsValidTarget(SpellManager.RKick.Range) && m.NetworkId != enemy.NetworkId))
                    {
                        var info = enemy2.Position.To2D().ProjectOn(startpos, endpos);
                        if (info.IsOnSegment && Extensions.Distance(info.SegmentPoint, enemy2.Position.To2D(), true) <= Math.Pow(1.8f * (enemy2.BoundingRadius + width), 2))
                        {
                            var pred = SpellManager.RKick.GetPrediction(enemy2);
                            if (pred.HitChancePercent >= 50)
                            {
                                positions.Add(new Tuple<Vector2, float>(pred.CastPosition.To2D(), enemy.BoundingRadius));
                            }
                        }
                    }
                    var Count = 1;
                    foreach (Tuple<Vector2, float> t in positions)
                    {
                        var v = t.Item1;
                        var info = v.ProjectOn(startpos, endpos);
                        if (info.IsOnSegment && Extensions.Distance(info.SegmentPoint, v, true) <= Math.Pow((t.Item2 + width), 2))
                        {
                            Count++;
                        }
                    }
                    if (BestCount == 0)
                    {
                        BestCount = Count;
                        BestTarget = enemy;
                    }
                    else if (BestCount < Count)
                    {
                        BestCount = Count;
                        BestTarget = enemy;
                    }
                }
            }
            return new Tuple<int, AIHeroClient>(BestCount, BestTarget);
        }
        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.SData.Name.Equals(SpellSlot.R.GetSpellDataInst().SData.Name))
                {
                    LastCastTime = Game.Time;
                }
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
