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


namespace Template
{
    public static class Champion
    {
        public static string Author = "iCreative";
        public static string AddonName = "Limitless Potential";
        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }


        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (Util.MyHero.Hero != EloBuddy.Champion.Syndra) { return; }
            Chat.Print(AddonName + " made by " + Author + " loaded, have fun!.");
            TargetSelector.Init(1200f, DamageType.Physical);
            SpellManager.Init(args);
            MenuManager.Init(args);
            DrawManager.Init(args);
            ModeManager.Init(args);
            BallManager.Init(args);
            Interrupter.OnInterruptableSpell += Interrupter_OnInterruptableSpell;
            Gapcloser.OnGapcloser += Gapcloser_OnGapcloser;
        }
        
        private static void Gapcloser_OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            if (MenuManager.MiscMenu.GetCheckBoxValue("Gapcloser"))
            {

            }
        }

        private static void Interrupter_OnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs e)
        {

            if (MenuManager.MiscMenu.GetCheckBoxValue("Interrupter"))
            {

            }
        }
    }
}
