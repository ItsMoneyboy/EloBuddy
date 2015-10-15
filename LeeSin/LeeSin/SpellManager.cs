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
        public static float E_Range
        {
            get
            {
                if (!SpellSlot.E.IsFirstSpell())
                {
                    return E2.Range;
                }
                return E1.Range;
            }
        }
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

        public static void CastQ(Obj_AI_Base target)
        {
            if (SpellSlot.Q.IsReady())
            {
                if (SpellSlot.Q.IsFirstSpell())
                {
                    CastQ1(target);
                }
                else
                {
                    CastQ2(target);
                }
            }
        }

        public static void CastQ1(Obj_AI_Base target, float hitchancepercent = -1)
        {
            if (SpellSlot.Q.IsReady() && SpellSlot.Q.IsFirstSpell() && target.IsValidTarget(Q1.Range))
            {
                var pred = Q1.GetPrediction(target);
                if (hitchancepercent == -1) { hitchancepercent = SpellSlot.Q.HitChancePercent(); }
                if (pred.HitChancePercent >= hitchancepercent)
                {
                    Q1.Cast(pred.CastPosition);
                }
            }
        }
        public static void CastQ2(Obj_AI_Base target)
        {
            if (SpellSlot.Q.IsReady() && !SpellSlot.Q.IsFirstSpell() && target.IsValidTarget(Q2.Range) && target.HaveQ())
            {
                Q1.Cast();
            }
        }
        public static void CastW(Obj_AI_Base target)
        {
            if (SpellSlot.W.IsReady())
            {
                if (SpellSlot.W.IsFirstSpell())
                {
                    CastQ1(target);
                }
                else
                {
                    CastQ2(target);
                }
            }
        }
        public static void CastW1(Obj_AI_Base target)
        {
            if (SpellSlot.W.IsReady() && SpellSlot.W.IsFirstSpell() && target.IsValid && !target.IsDead)
            {
                Util.myHero.Spellbook.CastSpell(W1.Slot, target);
            }
        }

        public static void CastW2()
        {
            if (SpellSlot.W.IsReady() && !SpellSlot.W.IsFirstSpell())
            {
                W2.Cast();
            }
        }

        public static void CastE(Obj_AI_Base target)
        {
            if (SpellSlot.E.IsReady())
            {
                if (SpellSlot.E.IsFirstSpell())
                {
                    CastE1(target);
                }
                else
                {
                    CastE2(target);
                }
            }
        }

        public static void CastE1(Obj_AI_Base target)
        {
            if (SpellSlot.E.IsReady() && SpellSlot.E.IsFirstSpell() && target.IsValidTarget(E1.Range))
            {
                var pred = E1.GetPrediction(target);
                if (pred.HitChance == HitChance.High)
                {
                    Util.myHero.Spellbook.CastSpell(E1.Slot);
                }
            }
        }
        public static void CastE2(Obj_AI_Base target)
        {
            if (SpellSlot.E.IsReady() && !SpellSlot.E.IsFirstSpell() && target.IsValidTarget(E2.Range))
            {
                var pred = E2.GetPrediction(target);
                if (pred.HitChance == HitChance.High)
                {
                    Util.myHero.Spellbook.CastSpell(E2.Slot);
                }
            }
        }
        public static void CastR(Obj_AI_Base target)
        {
            if (SpellSlot.R.IsReady() && target.IsValidTarget(R.Range))
            {
                R.Cast(target);
            }
        }
        public static float HitChancePercent(this SpellSlot s)
        {
            string slot;
            switch (s)
            {
                case SpellSlot.Q:
                    slot = "Q";
                    break;
                case SpellSlot.W:
                    slot = "W";
                    break;
                case SpellSlot.E:
                    slot = "E";
                    break;
                case SpellSlot.R:
                    slot = "R";
                    break;
                default:
                    slot = "Q";
                    break;
            }
            if (ModeManager.IsHarass)
            {
                return MenuManager.PredictionMenu.GetSliderValue(slot + "Harass");
            }
            return MenuManager.PredictionMenu.GetSliderValue(slot + "Combo");
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
