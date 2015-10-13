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
        public static string Author = "iCreative";
        public static string AddonName = "Master the enemy";
        public static Obj_AI_Base QTarget = null;
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
            TargetSelector.Init(SpellManager.Q2.Range, DamageType.Physical);
            LoadCallbacks();
        }

        private static void LoadCallbacks()
        {
            Game.OnTick += Game_OnTick;

            Obj_AI_Base.OnBuffGain += Obj_AI_Base_OnBuffGain;
            Obj_AI_Base.OnBuffLose += Obj_AI_Base_OnBuffLose;

            Drawing.OnDraw += Drawing_OnDraw;

            Interrupter.OnInterruptableSpell += Interrupter_OnInterruptableSpell;
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
        public static void JumpTo(Obj_AI_Base target)
        {
            if (SpellManager.CanCastW1)
            {
                Util.myHero.Spellbook.CastSpell(SpellSlot.W, target);
            }
        }
        private static void Obj_AI_Base_OnBuffLose(Obj_AI_Base sender, Obj_AI_BaseBuffLoseEventArgs args)
        {
            if (args.Buff.Caster.IsMe)
            {
                if (args.Buff.Name.ToLower().Contains("blindmonkqone"))
                {
                    QTarget = sender;
                }
            }
        }

        private static void Obj_AI_Base_OnBuffGain(Obj_AI_Base sender, Obj_AI_BaseBuffGainEventArgs args)
        {

            if (args.Buff.Caster.IsMe)
            {
                if (args.Buff.Name.ToLower().Contains("blindmonkqone"))
                {
                    QTarget = null;
                }
            }
        }

        public static bool HaveQ(this Obj_AI_Base unit)
        {
            return unit.IsValidTarget() && QTarget != null && unit.NetworkId == QTarget.NetworkId;
        }

        private static void Game_OnTick(EventArgs args)
        {
        }

        private static void Interrupter_OnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs e)
        {

        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Util.myHero.IsDead) { return; }
            //Draw current combo mode;
        }


    }
}
