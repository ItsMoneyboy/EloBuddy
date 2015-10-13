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
    public static class Flee
    {
        public static void Execute()
        {
            if (SpellManager.CanCastW1)
            {
                var obj = Champion.GetBestObjectNearTo(Util.mousePos);
                if (obj != null && MenuManager.FleeMenu.GetCheckBoxValue("W"))
                {
                    Champion.JumpTo(obj);
                }
                else if (MenuManager.FleeMenu.GetCheckBoxValue("WardJump"))
                {
                    if (WardManager.CanCastWard)
                    {
                        WardManager.CastWardTo(Util.mousePos);
                    }
                }
            }
        }
    }
}
