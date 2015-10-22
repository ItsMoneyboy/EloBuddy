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
    public static class Champion
    {
        public static string Author = "iCreative";
        public static string AddonName = "Draven Me Crazy";
        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += SpellManager.Init;
            Loading.OnLoadingComplete += MenuManager.Init;
            Loading.OnLoadingComplete += DrawManager.Init;
            Loading.OnLoadingComplete += AxesManager.Init;
            Loading.OnLoadingComplete += ModeManager.Init;
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }


        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (Util.MyHero.Hero != EloBuddy.Champion.Draven) { return; }
            Chat.Print(AddonName + " made by " + Author + " loaded, have fun!.");
            TargetSelector.Init(1000f, DamageType.Physical);
            Interrupter.OnInterruptableSpell += Interrupter_OnInterruptableSpell;
            Gapcloser.OnGapcloser += Gapcloser_OnGapcloser;
        }

        private static void Gapcloser_OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            if (MenuManager.MiscMenu.GetCheckBoxValue("Gapcloser"))
            {
                SpellManager.CastE(sender);
            }
        }

        private static void Interrupter_OnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs e)
        {

            if (MenuManager.MiscMenu.GetCheckBoxValue("Interrupter"))
            {
                SpellManager.CastE(sender);
            }
        }
    }
}
