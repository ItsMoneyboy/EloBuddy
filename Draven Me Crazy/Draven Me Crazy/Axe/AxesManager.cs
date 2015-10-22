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
namespace Draven_Me_Crazy
{
    public static class AxesManager
    {
        public static List<Axe> Axes = new List<Axe>();
        public static Menu Menu
        {
            get
            {
                return MenuManager.GetSubMenu("Axes");
            }
        }
        public static int AxesCount
        {
            get
            {
                if (Util.MyHero.HasBuff("dravenspinningattack"))
                {
                    return Util.MyHero.GetBuff("dravenspinningattack").Count + Axes.Count;
                }
                return Axes.Count;
            }
        }
        public static bool CatchEnabled { get { return Menu.GetKeyBindValue("Catch"); } }
        public static int CatchMode { get { return Menu.GetSliderValue("CatchMode"); } }
        public static bool CanCatch { get { return CatchEnabled && ((CatchMode == 0 && !ModeManager.IsNone) || CatchMode == 1); } }
        public static float CatchDelay { get { return Menu.GetSliderValue("Delay") / 100.0f; } }
        public static float CatchRadius
        {
            get
            {
                if (Combo.IsActive) { return Menu.GetSliderValue("Combo"); }
                else if (Harass.IsActive) { return Menu.GetSliderValue("Harass"); }
                else if (Clear.IsActive) { return Menu.GetSliderValue("Clear"); }
                else if (LastHit.IsActive) { return Menu.GetSliderValue("Harass"); }
                return Menu.GetSliderValue("Clear");
            }
        }
        public static int OrbwalkMode { get { return Menu.GetSliderValue("OrbwalkMode"); } }
        public static Vector3 CatchSource { get { return OrbwalkMode == 1 ? Util.MousePos : Util.MyHero.Position; } }
        public static void Init(EventArgs args)
        {
            Game.OnUpdate += Game_OnUpdate;
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
            Game.OnWndProc += Game_OnWndProc;
        }


        private static void AddReticleToAxe(GameObject obj)
        {
            var a = Axes.Where(m => m.MissileIsValid && m.Reticle == null).OrderBy(m => Extensions.Distance(obj, m.Missile.EndPosition, true)).FirstOrDefault();
            if (a != null)
            {
                a.AddReticle(obj);
            }
        }
        public static Axe FirstAxe
        {
            get
            {
                return Axes.Where(m => m.InTime).OrderBy(m => m.TimeLeft).FirstOrDefault();
            }
        }
        public static Axe FirstAxeInRadius
        {
            get
            {
                return Axes.Where(m => m.InTime && m.SourceInRadius).OrderBy(m => m.TimeLeft).FirstOrDefault();
            }
        }
        public static bool IsFirst(this Axe a)
        {
            return FirstAxe == a;
        }
        public static Axe AxeAfter(this Axe a)
        {
            return Axes.Where(m => m.InTime && m.TimeLeft > a.TimeLeft).OrderBy(m => m.TimeLeft).FirstOrDefault();
        }
        public static Axe AxeBefore(this Axe a)
        {
            return Axes.Where(m => m.InTime && m.TimeLeft < a.TimeLeft).OrderBy(m => m.TimeLeft).LastOrDefault();
        }
        private static void Game_OnUpdate(EventArgs args)
        {
            Axes.RemoveAll(a => a.Reticle != null && (!a.Reticle.IsValid || !a.InTime));
            bool CanMove = true;
            bool CanAttack = true;
            if (CanCatch)
            {
                foreach (Axe a in Axes)
                {
                    if (!a.CanOrbwalkWithUserDelay)
                    {
                        CanMove = false;
                    }
                    if (!a.CanAttack)
                    {
                        CanAttack = false;
                    }
                }
            }
            Orbwalker.DisableAttacking = !CanAttack;
            Orbwalker.DisableMovement = !CanMove;
            if (!CanMove)
            {
                var BestAxe = FirstAxeInRadius;
                if (BestAxe != null)
                {
                    Orbwalker.DisableMovement = false;
                    Orbwalker.MoveTo(BestAxe.EndPosition);
                    Orbwalker.DisableMovement = true;
                    if (Orbwalker.CanMove)
                    {
                        if (!BestAxe.MoveSent)
                        {
                            Player.IssueOrder(GameObjectOrder.MoveTo, BestAxe.EndPosition);
                            BestAxe.MoveSent = true;
                        }
                    }
                }
            }
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg == (uint)WindowMessages.LeftButtonDoubleClick && Menu.GetCheckBoxValue("Click"))
            {
                if (FirstAxeInRadius != null)
                {
                    Axes.Remove(FirstAxeInRadius);
                }
            }
        }
        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender is MissileClient)
            {
                var missile = sender as MissileClient;
                if (missile.SpellCaster.IsMe)
                {
                    var name = missile.SData.Name.ToLower();
                    if (name.Equals("dravenspinningreturncatch") || name.Equals("dravenspinningreturnleftaxe"))
                    {
                        Axes.Add(new Axe(missile));
                    }
                }
            }
            else if (sender is Obj_GeneralParticleEmitter)
            {
                var name = sender.Name.ToLower();
                if (name.Contains(Util.MyHero.ChampionName.ToLower()) && name.Contains("reticle"))
                {
                    if (name.Contains("q_reticle_self.troy"))
                    {
                        AddReticleToAxe(sender);
                    }
                }
            }
        }

        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (sender is Obj_GeneralParticleEmitter)
            {
                var name = sender.Name.ToLower();
                if (name.Contains(Util.MyHero.ChampionName.ToLower()) && name.Contains("reticle"))
                {
                    if (name.Contains("q_reticle_self.troy"))
                    {
                        Axes.RemoveAll(m => m.Reticle != null && m.Reticle.NetworkId == sender.NetworkId);
                    }
                }
            }
        }

    }
}
