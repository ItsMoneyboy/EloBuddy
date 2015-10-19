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
    public static class Insec
    {
        // T O D O
        public static Menu Menu
        {
            get
            {
                return MenuManager.GetSubMenu("Insec");
            }
        }
        private static int Priority
        {
            get
            {
                return Menu.GetSliderValue("Priority");
            }
        }
        public static bool IsActive
        {
            get
            {
                return ModeManager.IsInsec;
            }
        }
        private static Obj_AI_Base AllySelected;
        private static Vector3 PositionSelected;
        private static float LastGapcloseAttempt = 0;
        private static float LastSetPositionTime = 0;
        private static float Offset = 80f;
        public static void Init()
        {
            Game.OnWndProc += Game_OnWndProc;
            Game.OnTick += Game_OnTick;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.SData.Name.Equals(SpellSlot.R.GetSpellDataInst().Name))
                {
                    if (Menu.GetCheckBoxValue("Flash") && SpellManager.Flash_IsReady && IsActive)
                    {
                        SpellManager.Flash.Cast(ExpectedEndPosition);
                    }
                }
            }
        }

        public static void Execute()
        {
            var target = TargetSelector.Target;
            if (Orbwalker.CanMove && Game.Time - LastGapcloseAttempt > 0.25f)
            {

                if (target.IsValidTarget() && Extensions.Distance(Util.myHero, ExpectedEndPosition, true) > Extensions.Distance(target, ExpectedEndPosition, true) && IsReady)
                {
                    var gapclosepos = target.Position + (target.Position - ExpectedEndPosition).Normalized() * DistanceBetween;
                    Orbwalker.MoveTo(gapclosepos);
                }
                else
                {
                    Orbwalker.MoveTo(Util.mousePos);
                }
            }
            if (target.IsValidTarget())
            {
                if (IsReady)
                {
                    if (IsActive)
                    {
                        if (SpellManager.CanCastQ1)
                        {
                            var predtarget = SpellManager.Q1.GetPrediction(target);
                            if (Menu.GetCheckBoxValue("Object") && predtarget.CollisionObjects.Count() > 1)
                            {
                                foreach (Obj_AI_Base minion in EntityManager.MinionsAndMonsters.Get(EntityManager.MinionsAndMonsters.EntityType.Both, EntityManager.UnitTeam.Enemy, Util.myHero.Position, SpellManager.Q2.Range).Where(m => m.IsValidTarget() && SpellSlot.Q.GetSpellDamage(m) < Prediction.Health.GetPrediction(m, SpellManager.Q1.CastDelay + 1000 * (int)(Extensions.Distance(Util.myHero, m) / SpellManager.Q1.Speed)) && Extensions.Distance(Util.myHero, target, true) > Extensions.Distance(m, target, true) && Extensions.Distance(m, target, true) < Math.Pow(WardManager.WardRange - DistanceBetween - Offset, 2)).OrderBy(m => Extensions.Distance(target, m, true)))
                                {
                                    var pred = SpellManager.Q1.GetPrediction(minion);
                                    if (pred.HitChancePercent >= SpellSlot.Q.HitChancePercent())
                                    {
                                        SpellManager.Q1.Cast(pred.CastPosition);
                                    }
                                }
                                foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies.Where(m => m.NetworkId != target.NetworkId && m.IsValidTarget(SpellManager.Q2.Range) && SpellSlot.Q.GetSpellDamage(m) < m.Health && Extensions.Distance(Util.myHero, target, true) > Extensions.Distance(m, target, true) && Extensions.Distance(m, target, true) < Math.Pow(WardManager.WardRange - DistanceBetween - Offset, 2)).OrderBy(m => Extensions.Distance(target, m, true)))
                                {
                                    SpellManager.CastQ1(enemy);
                                }
                            }
                            SpellManager.CastQ1(target);
                        }
                        if (Extensions.Distance(Util.myHero, target, true) > Math.Pow(WardManager.WardRange - DistanceBetween, 2))
                        {
                            if (_Q.HasQ2Buff)
                            {
                                if (_Q.IsValidTarget && Extensions.Distance(target, _Q.Target, true) < Math.Pow(WardManager.WardRange - DistanceBetween - Offset, 2))
                                {
                                    TargetSelector.ForcedTarget = target;
                                    Champion.ForceQ2(target);
                                }
                            }
                        }
                    }
                    if (Extensions.Distance(Util.myHero, target, true) < Math.Pow(WardManager.WardRange - DistanceBetween, 2) && !IsRecent)
                    {
                        switch (Priority)
                        {
                            case 0:
                                if (WardManager.CanWardJump)
                                {
                                    WardJump(target);
                                }
                                else if (SpellManager.Flash_IsReady)
                                {
                                    Flash(target);
                                }
                                break;
                            case 1:
                                if (SpellManager.Flash_IsReady)
                                {
                                    Flash(target);
                                }
                                else if (WardManager.CanWardJump)
                                {
                                    WardJump(target);
                                }
                                break;
                        }
                    }
                    CastR(target);
                }
                else
                {
                    NormalCombo.Execute();
                }
            }
        }
        private static void Flash(AIHeroClient target)
        {
            if (SpellManager.Flash_IsReady)
            {
                var gapclosepos = target.Position + (target.Position - ExpectedEndPosition).Normalized() * DistanceBetween;
                var flashendpos = Util.myHero.Position + (gapclosepos - Util.myHero.Position).Normalized() * SpellManager.Flash.Range;
                if (Extensions.Distance(gapclosepos, target, true) <= Math.Pow(SpellManager.R.Range, 2) && Extensions.Distance(target.Position, flashendpos, true) > Math.Pow(50, 2) && Extensions.Distance(flashendpos, target, true) < Extensions.Distance(flashendpos, ExpectedEndPosition, true) && Extensions.Distance(gapclosepos, target, true) < Extensions.Distance(gapclosepos, ExpectedEndPosition, true))
                {
                    if (Orbwalker.CanMove)
                    {
                        LastGapcloseAttempt = Game.Time;
                        //Orbwalker.MoveTo(gapclosepos + (gapclosepos - ExpectedEndPosition).Normalized() * (DistanceBetween + Util.myHero.BoundingRadius / 2));
                    }
                    AllySelected = null;
                    PositionSelected = EndPosition;
                    LastSetPositionTime = Game.Time;
                    TargetSelector.ForcedTarget = target;
                    Util.myHero.Spellbook.CastSpell(SpellManager.Flash.Slot, gapclosepos);
                }
            }
        }

        private static void WardJump(AIHeroClient target)
        {
            var pred = SpellManager.W1.GetPrediction(target);
            if (WardManager.CanWardJump && pred.HitChancePercent >= 30f)
            {
                var gapclosepos = pred.CastPosition + (pred.CastPosition - ExpectedEndPosition).Normalized() * DistanceBetween;
                if (Extensions.Distance(gapclosepos, Util.myHero, true) <= Math.Pow(WardManager.WardRange, 2) && Extensions.Distance(gapclosepos, target, true) <= Math.Pow(SpellManager.R.Range, 2) && Extensions.Distance(gapclosepos, target, true) < Extensions.Distance(gapclosepos, ExpectedEndPosition, true))
                {
                    if (Orbwalker.CanMove)
                    {
                        LastGapcloseAttempt = Game.Time;
                        Orbwalker.MoveTo(gapclosepos + (gapclosepos - ExpectedEndPosition).Normalized() * (DistanceBetween + Util.myHero.BoundingRadius / 2));
                    }
                    AllySelected = null;
                    PositionSelected = EndPosition;
                    LastSetPositionTime = Game.Time;
                    TargetSelector.ForcedTarget = target;
                    var obj = Champion.GetBestObjectNearTo(gapclosepos);
                    if (obj != null && Extensions.Distance(obj, target, true) < Extensions.Distance(obj, ExpectedEndPosition, true))
                    {
                        SpellManager.CastW1(obj);
                    }
                    else
                    {
                        WardManager.CastWardTo(gapclosepos);
                    }
                }
            }
        }
        private static void CastR(Obj_AI_Base target)
        {
            if (SpellSlot.R.IsReady() && target.IsValidTarget(SpellManager.R.Range) && Extensions.Distance(Util.myHero, ExpectedEndPosition, true) > Extensions.Distance(target, ExpectedEndPosition, true))
            {
                var extended = ExpectedEndPosition + (ExpectedEndPosition - target.Position).Normalized() * SpellManager.RKick.Range * 0.5f;
                var realendpos = target.Position + (target.Position - Util.myHero.Position).Normalized() * SpellManager.RKick.Range;
                var info = realendpos.To2D().ProjectOn(target.Position.To2D(), extended.To2D());
                if (info.IsOnSegment && Extensions.Distance(info.SegmentPoint, ExpectedEndPosition.To2D(), true) <= Math.Pow(SpellManager.RKick.Range * 0.5f, 2))
                {
                    SpellManager.CastR(target);
                }
            }
        }
        private static bool IsValidPosition(this Vector3 position)
        {
            var target = TargetSelector.Target;
            if (target.IsValidTarget())
            {
                return Extensions.Distance(target, position, true) <= Math.Pow(SpellManager.RKick.Range + 500f, 2);
            }
            return false;
        }
        private static float DistanceBetween
        {
            get
            {
                var target = TargetSelector.Target;
                if (target.IsValidTarget())
                {
                    return Math.Min((Util.myHero.BoundingRadius + target.BoundingRadius + 50f) * (100 + Menu.GetSliderValue("DistanceBetweenPercent")) / 100, SpellManager.R.Range);
                }
                return 0;
            }
        }
        public static Vector3 EndPosition
        {
            get
            {
                var target = TargetSelector.Target;
                if (target.IsValidTarget())
                {
                    return target.Position + (ExpectedEndPosition - target.Position).Normalized() * SpellManager.RKick.Range;
                }
                return Vector3.Zero;
            }
        }
        public static Vector3 ExpectedEndPosition
        {
            get
            {
                var target = TargetSelector.Target;
                if (target.IsValidTarget())
                {
                    if (AllySelected != null && AllySelected.IsValidAlly() && AllySelected.Position.IsValidPosition())
                    {
                        return AllySelected.Position + (target.Position - AllySelected.Position).Normalized().To2D().Perpendicular().To3D() * (AllySelected.AttackRange + AllySelected.BoundingRadius + target.BoundingRadius) / 2;
                    }
                    if (PositionSelected != Vector3.Zero && PositionSelected.IsValidPosition())
                    {
                        return PositionSelected;
                    }
                    switch (Menu.GetSliderValue("Position"))
                    {
                        case 1:
                            return Util.mousePos;
                        case 2:
                            return Util.myHero.Position;
                        default:
                            var turret = EntityManager.Turrets.Allies.Where(m => m.IsValidAlly()).OrderBy(m => Extensions.Distance(Util.myHero, m, true)).FirstOrDefault();
                            if (turret != null)
                            {
                                if (Extensions.Distance(turret, target) - SpellManager.RKick.Range < 750 + 200)
                                {
                                    return turret.Position;
                                }
                            }
                            var allies = EntityManager.Heroes.Allies.Where(m => m.IsValidAlly() && !m.IsMe && m.Position.IsValidPosition()).OrderBy(m => m.GetPriority());
                            if (allies.Count() > 0)
                            {
                                var ally = allies.LastOrDefault();
                                return ally.Position + (target.Position - ally.Position).Normalized().To2D().Perpendicular().To3D() * (ally.AttackRange + ally.BoundingRadius + target.BoundingRadius) / 2;
                            }
                            break;

                    }
                }
                return Util.myHero.Position;
            }
        }
        public static bool IsReady
        {
            get
            {
                return (WardManager.CanWardJump || SpellManager.Flash_IsReady || IsRecent) && SpellSlot.R.IsReady() && TargetSelector.Target != null && TargetSelector.Target.IsValidTarget();
            }
        }
        public static bool IsRecent
        {
            get
            {
                return Game.Time - SpellManager.W_LastCastTime < 5 || Game.Time - SpellManager.Flash_LastCastTime < 5 || Game.Time - WardManager.LastWardCreated < 5;
            }
        }
        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg == (uint)WindowMessages.LeftButtonDown)
            {
                if (IsReady)
                {
                    var target = EloBuddy.SDK.TargetSelector.GetTarget(200f, TargetSelector.damageType, Util.mousePos);
                    if (target.IsValidTarget())
                    {

                    }
                    else
                    {
                        var ally = AllyHeroManager.GetNearestTo(Util.mousePos);
                        if (ally != null && Extensions.Distance(ally, Util.mousePos) <= 200f)
                        {
                            AllySelected = ally;
                            PositionSelected = Vector3.Zero;
                            LastSetPositionTime = Game.Time;
                        }
                        else
                        {
                            AllySelected = null;
                            PositionSelected = new Vector3(Util.mousePos.X, Util.mousePos.Y, Util.mousePos.Z);
                            LastSetPositionTime = Game.Time;
                        }
                    }
                }
            }
        }
        private static void Game_OnTick(EventArgs args)
        {
            if (Game.Time - LastSetPositionTime > 10 && LastSetPositionTime > 0)
            {
                AllySelected = null;
                PositionSelected = Vector3.Zero;
                LastSetPositionTime = 0;
                TargetSelector.ForcedTarget = null;
            }
        }
    }
}
