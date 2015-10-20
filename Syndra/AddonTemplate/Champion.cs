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


namespace AddonTemplate
{
    public static class Champion
    {
        public static string Author = "iCreative";
        public static string AddonName = "";
        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (Util.myHero.Hero != EloBuddy.Champion.Syndra) { return; }
            Chat.Print(AddonName + " made by " + Author + " loaded, have fun!.");
            SpellManager.Init();
            MenuManager.Init();
            DrawManager.Init();
            TargetSelector.Init(1000f, DamageType.Physical);


        }

    }
}
