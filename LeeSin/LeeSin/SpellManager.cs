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
    public static class SpellManager
    {
        public static Spell.Skillshot Q1, W1, RKick, E1, E2;
        public static Spell.Targeted R;
        public static Spell.Active Q2, W2;
        public static Spell.Targeted Ignite, Smite;
        public static Spell.Skillshot Flash;
        public static float W_Range = 700f;
        public static float W_ExtraRange = 150f;
        public static void Init()
        {

            Q1 = new Spell.Skillshot(SpellSlot.Q, 1100, SkillShotType.Linear, 250, 1800, 60);
            Q1.AllowedCollisionCount = 0;
            Q2 = new Spell.Active(SpellSlot.Q, 1300);

            W1 = new Spell.Skillshot(SpellSlot.W, 900, SkillShotType.Linear, 0, 1500, 150);
            W1.AllowedCollisionCount = int.MaxValue;

            W2 = new Spell.Active(SpellSlot.W, 700);

            E1 = new Spell.Skillshot(SpellSlot.E, 350, SkillShotType.Linear, 250, 2500, 150);
            E1.AllowedCollisionCount = int.MaxValue;
            E2 = new Spell.Skillshot(SpellSlot.E, 675, SkillShotType.Linear, 250, 2500, 150);
            E2.AllowedCollisionCount = int.MaxValue;

            R = new Spell.Targeted(SpellSlot.R, 375);
            RKick = new Spell.Skillshot(SpellSlot.R, 650, SkillShotType.Linear, 250, 600, 100);
            RKick.AllowedCollisionCount = int.MaxValue;

            Ignite = new Spell.Targeted(Util.myHero.GetSpellSlotFromName("summonerdot"), 600);
            Smite = new Spell.Targeted(Util.myHero.GetSpellSlotFromName("smite"), 780);
            Flash = new Spell.Skillshot(Util.myHero.GetSpellSlotFromName("flash"), 400, SkillShotType.Circular);
            /*
            var slot = Util.myHero.GetSpellSlotFromName("summonerdot");
            if (slot != SpellSlot.Unknown)
            {
                Ignite = new Spell.Targeted(slot, 600);
            }
            slot = Util.myHero.GetSpellSlotFromName("flash");
            if (slot != SpellSlot.Unknown)
            {
                Flash = new Spell.Skillshot(slot, 400, SkillShotType.Circular);
            }
            slot = Util.myHero.GetSpellSlotFromName("smite");
            if (slot != SpellSlot.Unknown)
            {
                Smite = new Spell.Targeted(slot, 780);
            }*/
        }
        public static bool IsReady(this SpellSlot slot)
        {
            return slot.GetSpellDataInst().IsReady;
        }
        public static bool IsFirstSpell(this SpellSlot slot)
        {
            return slot.GetSpellDataInst().SData.Name.ToLower().Contains("one");
        }
        public static SpellDataInst GetSpellDataInst(this SpellSlot slot)
        {
            return Util.myHero.Spellbook.GetSpell(slot);
        }
        public static bool CanCastW1
        {
            get
            {
                return SpellSlot.W.IsReady() && SpellSlot.W.IsFirstSpell();
            }
        }
    }
}
