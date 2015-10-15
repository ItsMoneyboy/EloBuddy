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
        public static float LastInsecTime = 0f;
        public static void Execute()
        {
            if (Orbwalker.CanMove)
            {
                Orbwalker.MoveTo(Util.mousePos);
            }
        }

        public static bool IsReady
        {
            get
            {
                return true;
            }
        }
        public static bool IsRecent
        {
            get
            {
                return false;
            }
        }
    }
}
