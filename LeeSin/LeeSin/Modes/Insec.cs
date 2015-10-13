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
        public static void Execute()
        {
            if (Orbwalker.CanMove)
            {
                Orbwalker.MoveTo(Util.mousePos);
            }
        }
    }
}
