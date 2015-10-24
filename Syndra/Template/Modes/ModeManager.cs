using System;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace Template
{
    public static class ModeManager
    {
        public static float Extra_AA_Range = 120f;
        public static void Init(EventArgs args)
        {
            Game.OnTick += Game_OnUpdate;
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            KillSteal.Execute();
            Orbwalker.DisableMovement = IsFlee && Flee.Menu.GetCheckBoxValue("Movement");
            if (IsCombo)
            {
                Combo.Execute();
            }
            else if (IsHarass)
            {
                Harass.Execute();
            }
            else if (IsClear)
            {
                Clear.Execute();
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
                return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear) && EntityManager.MinionsAndMonsters.GetJungleMonsters(Util.MyHero.Position, Util.MyHero.GetAutoAttackRange() + 200, false).Count() > 0;
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
                return !IsLastHit && !IsClear && !IsHarass && !IsCombo;
            }
        }
    }
}
