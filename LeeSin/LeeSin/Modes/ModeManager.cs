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
    public static class ModeManager
    {
        public static bool IsInsec
        {
            get
            {
                return MenuManager.InsecMenu.GetKeyBindValue("Key");
            }
        }
        public static void Init()
        {
            Game.OnTick += Game_OnTick;
        }
        private static void Game_OnTick(EventArgs args)
        {
            Orbwalker.DisableAttacking = IsInsec;
            if (IsInsec)
            {
                Insec.Execute();
            }
            else if (IsCombo)
            {
                Combo.Execute();
            }
            else if (IsHarass)
            {
                Harass.Execute();
            }
            else if (IsClear)
            {
                if (IsLaneClear)
                {
                    LaneClear.Execute();
                }
                if (IsJungleClear)
                {
                    JungleClear.Execute();
                }
            }
            else if (IsLastHit)
            {
                LastHit.Execute();
            }
            if (IsFlee)
            {
                Flee.Execute();
            }

        }
        public static void KillSteal()
        {
            foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies.Where(m => m.IsValidTarget(1300f)))
            {

            }
        }
        public static bool IsCombo
        {
            get
            {
                return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo);
            }
        }
        public static bool IsHarass
        {
            get
            {
                return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass);
            }
        }
        public static bool IsClear
        {
            get
            {
                return IsLaneClear || IsJungleClear;
            }
        }
        public static bool IsLaneClear
        {
            get
            {
                return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear);
            }
        }
        public static bool IsJungleClear
        {
            get
            {
                return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear);
            }
        }
        public static bool IsLastHit
        {
            get
            {
                return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit);
            }
        }
        public static bool IsFlee
        {
            get
            {
                return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee);
            }
        }
        public static bool IsNone
        {
            get
            {
                return !IsFlee && !IsLastHit && !IsClear && !IsHarass && !IsCombo;
            }
        }
    }
}
