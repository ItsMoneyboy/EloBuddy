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
    public static class Champion
    {
        //Falta agregar laneclear, lasthit.
        public static string Author = "iCreative";
        public static string AddonName = "Master the enemy";
        public static int PassiveStack
        {
            get
            {
                if (Util.myHero.HasBuff("blindmonkpassive_cosmetic"))
                {
                    return Util.myHero.GetBuff("blindmonkpassive_cosmetic").Count;
                }
                return 0;
            }
        }
        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (Util.myHero.Hero != EloBuddy.Champion.LeeSin) { return; }
            Chat.Print(AddonName + " made by " + Author + " loaded, have fun!.");
            SpellManager.Init();
            MenuManager.Init();
            ModeManager.Init();
            WardManager.Init();
            _Q.Init();
            _R.Init();
            Insec.Init();
            AutoSmite.Init();
            DrawManager.Init();
            TargetSelector.Init(SpellManager.Q2.Range + 200, DamageType.Physical);
            LoadCallbacks();
        }
        private static void LoadCallbacks()
        {
            Game.OnTick += Game_OnTick;

            Interrupter.OnInterruptableSpell += Interrupter_OnInterruptableSpell;

        }
        public static void GapCloseWithWard(Obj_AI_Base target)
        {
            if (SpellManager.CanCastW1)
            {
                var obj = GetBestObjectNearTo(target.Position);
                if (obj != null && Extensions.Distance(Util.myHero, target, true) > Extensions.Distance(obj, target, true))
                {
                    SpellManager.CastW1(obj);
                }
                else if (WardManager.CanCastWard)
                {
                    var pred = SpellManager.W1.GetPrediction(target);
                    if (pred.HitChancePercent >= 50f)
                    {
                        WardManager.CastWardTo(pred.CastPosition);
                    }
                }
            }
        }
        public static void GapCloseWithoutWard(Obj_AI_Base target)
        {
            if (SpellManager.CanCastW1)
            {
                var obj = GetBestObjectNearTo(target.Position);
                if (obj != null && Extensions.Distance(Util.myHero, target, true) > Extensions.Distance(obj, target, true))
                {
                    SpellManager.CastW1(obj);
                }
            }
        }
        public static Obj_AI_Base GetBestObjectFarFrom(Vector3 Position)
        {
            var minion = AllyMinionManager.GetFurthestTo(Position);
            var ally = AllyHeroManager.GetFurthestTo(Position);
            var ward = WardManager.GetFurthestTo(Position);
            var miniondistance = minion != null ? Extensions.Distance(Position, minion, true) : 0;
            var allydistance = ally != null ? Extensions.Distance(Position, ally, true) : 0;
            var warddistance = ward != null ? Extensions.Distance(Position, ward, true) : 0;
            var best = Math.Max(miniondistance, Math.Max(allydistance, warddistance));
            if (best > 0f)
            {
                if (best == allydistance)
                {
                    return ally;
                }
                else if (best == miniondistance)
                {
                    return minion;
                }
                else if (best == warddistance)
                {
                    return ward;
                }
            }
            return null;
        }
        public static Obj_AI_Base GetBestObjectNearTo(Vector3 Position)
        {
            var minion = AllyMinionManager.GetNearestTo(Position);
            var ally = AllyHeroManager.GetNearestTo(Position);
            var ward = WardManager.GetNearestTo(Position);
            var miniondistance = minion != null ? Extensions.Distance(Position, minion, true) : 999999999f;
            var allydistance = ally != null ? Extensions.Distance(Position, ally, true) : 999999999f;
            var warddistance = ward != null ? Extensions.Distance(Position, ward, true) : 999999999f;
            var best = Math.Min(miniondistance, Math.Min(allydistance, warddistance));
            if (best <= Math.Pow(250f, 2))
            {
                if (best == allydistance)
                {
                    return ally;
                }
                else if (best == miniondistance)
                {
                    return minion;
                }
                else if (best == warddistance)
                {
                    return ward;
                }
            }
            return null;
        }

        public static void ForceQ2(Obj_AI_Base target = null)
        {
            if (SpellSlot.Q.IsReady() && !SpellSlot.Q.IsFirstSpell())
            {
                if (target == null)
                {
                    target = TargetSelector.Target;
                }
                if (_Q.IsValidTarget && target.IsValidTarget())
                {
                    if (Extensions.Distance(target, _Q.Target, true) < Extensions.Distance(Util.myHero, _Q.Target, true))
                    {
                        SpellManager.CastQ2(_Q.Target);
                    }
                }
            }
            /*
            if (SpellSlot.Q.IsReady() && !SpellSlot.Q.IsFirstSpell())
            {
                Core.DelayAction(ForceQ2, 100);
            }*/
        }

        private static void Game_OnTick(EventArgs args)
        {
            TargetSelector.Range = 1000f;
            if (SpellSlot.Q.IsReady() && SpellSlot.Q.IsFirstSpell())
            {
                TargetSelector.Range = 1300f;
            }
            if (SpellSlot.Q.IsReady() && !SpellSlot.Q.IsFirstSpell() && _Q.Target != null)
            {
                TargetSelector.Range = 1500f;
            }
            if (!Insec.IsActive)
            {
                var t = _R.BestHitR(MenuManager.MiscMenu.GetSliderValue("R.Hit"));
                if (MenuManager.MiscMenu.GetSliderValue("R.Hit") <= t.Item1)
                {
                    SpellManager.CastR(t.Item2);
                }
            }
        }

        private static void Interrupter_OnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs e)
        {
            if (sender.IsValidTarget(TargetSelector.Range) && sender.IsEnemy && sender is AIHeroClient)
            {
                if (MenuManager.MiscMenu.GetCheckBoxValue("Interrupter"))
                {
                    if (SpellSlot.R.IsReady())
                    {
                        SpellManager.CastQ(sender);
                        SpellManager.CastR(sender);
                        GapCloseWithWard(sender);
                    }
                }
            }
        }


    }
}
