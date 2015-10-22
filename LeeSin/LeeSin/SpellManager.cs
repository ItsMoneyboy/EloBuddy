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
        public static Spell.Skillshot Q1, W1, RKick, E1, E2 = null;
        public static Spell.Targeted R = null;
        public static Spell.Active Q2, W2 = null;
        public static Spell.Targeted Ignite, Smite = null;
        public static Spell.Skillshot Flash = null;
        public static float W1_Range = 700f;
        public static float W_ExtraRange = 150f;
        public static float Smite_Delay = 0f;
        public static float W_LastCastTime, Flash_LastCastTime = 0f;
        public static void Init()
        {
            Q1 = new Spell.Skillshot(SpellSlot.Q, 1100, SkillShotType.Linear, 250, 1800, 60);
            Q1.AllowedCollisionCount = 0;
            Q2 = new Spell.Active(SpellSlot.Q, 1300);

            W1 = new Spell.Skillshot(SpellSlot.W, 900, SkillShotType.Linear, 50, 1500, 150);
            W1.AllowedCollisionCount = int.MaxValue;

            W2 = new Spell.Active(SpellSlot.W, 700);

            E1 = new Spell.Skillshot(SpellSlot.E, 350, SkillShotType.Linear, 250, 2500, 150);
            E1.AllowedCollisionCount = int.MaxValue;
            E2 = new Spell.Skillshot(SpellSlot.E, 675, SkillShotType.Linear, 250, 2500, 150);
            E2.AllowedCollisionCount = int.MaxValue;

            R = new Spell.Targeted(SpellSlot.R, 375);
            RKick = new Spell.Skillshot(SpellSlot.R, 275 + 550, SkillShotType.Linear, 400, 600, 75);
            RKick.AllowedCollisionCount = int.MaxValue;
            var slot = Util.myHero.SpellSlotFromName("summonerdot");
            if (slot != SpellSlot.Unknown)
            {
                Ignite = new Spell.Targeted(slot, 600);
            }
            slot = Util.myHero.SpellSlotFromName("smite");
            if (slot != SpellSlot.Unknown)
            {
                Smite = new Spell.Targeted(slot, 500);
            }
            slot = Util.myHero.SpellSlotFromName("flash");
            if (slot != SpellSlot.Unknown)
            {
                Flash = new Spell.Skillshot(slot, 400, SkillShotType.Circular);
            }

            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }
        public static bool IsInSmiteRange(this Obj_AI_Base target)
        {
            return target.IsValidTarget(Smite.Range + Util.myHero.BoundingRadius + target.BoundingRadius);
        }
        public static SpellSlot SpellSlotFromName(this AIHeroClient hero, string name)
        {
            foreach (SpellDataInst s in hero.Spellbook.Spells)
            {
                if (s.Name.ToLower().Contains(name.ToLower()))
                {
                    return s.Slot;
                }
            }
            return SpellSlot.Unknown;
        }
        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.SData.Name.Equals(SpellSlot.W.GetSpellDataInst().SData.Name) && args.SData.Name.ToLower().Contains("one"))
                {
                    W_LastCastTime = Game.Time;
                }
                else if (args.SData.Name.ToLower().Contains("flash"))
                {
                    Flash_LastCastTime = Game.Time;
                }
            }
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

        public static void CastQ1(Obj_AI_Base target)
        {
            if (SpellSlot.Q.IsReady() && SpellSlot.Q.IsFirstSpell() && target.IsValidTarget(Q1.Range))
            {
                if (target is AIHeroClient)
                {
                    _Q.CheckSmite(target);
                }
                else
                {
                    var pred = Q1.GetPrediction(target);
                    if (pred.HitChancePercent >= SpellSlot.Q.HitChancePercent())
                    {
                        Q1.Cast(pred.CastPosition);
                    }
                }
            }
        }
        public static void CastQ2(Obj_AI_Base target)
        {
            if (SpellSlot.Q.IsReady() && !SpellSlot.Q.IsFirstSpell() && target.IsValidTarget(Q2.Range))
            {
                if (target.HaveQ())
                {
                    Util.myHero.Spellbook.CastSpell(SpellSlot.Q, true);
                }
                else if (_Q.EndTime - Game.Time < 0.5f)
                {
                    Champion.ForceQ2(target);
                }
            }
        }
        public static void CastW(Obj_AI_Base target)
        {
            if (SpellSlot.W.IsReady() && target.IsValidAlly())
            {
                if (SpellSlot.W.IsFirstSpell())
                {
                    CastW1(target);
                }
                else
                {
                    CastW2();
                }
            }
        }
        public static void CastW1(Obj_AI_Base target)
        {
            if (SpellSlot.W.IsReady() && SpellSlot.W.IsFirstSpell() && target.IsValidAlly())
            {
                Util.myHero.Spellbook.CastSpell(W1.Slot, target, true);
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
                    Util.myHero.Spellbook.CastSpell(E1.Slot, true);
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
                    Util.myHero.Spellbook.CastSpell(E2.Slot, true);
                }
            }
        }
        public static void CastR(Obj_AI_Base target)
        {
            if (SpellSlot.R.IsReady() && target.IsValidTarget(R.Range))
            {
                Util.myHero.Spellbook.CastSpell(R.Slot, target, true);
            }
        }
        public static float HitChancePercent(this SpellSlot s)
        {
            string slot = s.ToString().Trim();
            if (Harass.IsActive)
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
                return SpellSlot.W.IsReady() && SpellSlot.W.IsFirstSpell() && Util.myHero.Mana >= SpellSlot.W.GetSpellDataInst().SData.ManaCostArray[SpellSlot.W.GetSpellDataInst().Level - 1];
            }
        }
        public static bool CanCastQ1
        {
            get
            {
                return SpellSlot.Q.IsReady() && SpellSlot.Q.IsFirstSpell() && Util.myHero.Mana >= SpellSlot.Q.GetSpellDataInst().SData.ManaCostArray[SpellSlot.Q.GetSpellDataInst().Level - 1];
            }
        }
        public static bool Smite_IsReady
        {
            get
            {
                return Smite != null && Smite.IsReady();
            }
        }
        public static bool CanUseSmiteOnHeroes
        {
            get
            {
                if (Smite_IsReady)
                {
                    var name = Smite.Slot.GetSpellDataInst().SData.Name.ToLower();
                    if (name.Contains("smiteduel") || name.Contains("smiteplayerganker"))
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        public static float SmiteDamage(this Obj_AI_Base target)
        {
            if (target.IsValidTarget() && Smite_IsReady)
            {
                if (target is AIHeroClient)
                {
                    if (CanUseSmiteOnHeroes)
                    {
                        return Util.myHero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Smite);
                    }
                }
                else
                {
                    var level = Util.myHero.Level;
                    return (new[] { 20 * level + 370, 30 * level + 330, 40 * level + 240, 50 * level + 100 }).Max();
                }
            }
            return 0;
        }
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
        public static float Q_Range
        {
            get
            {
                if (!SpellSlot.Q.IsFirstSpell())
                {
                    return Q2.Range;
                }
                return Q1.Range;
            }
        }
        public static float W_Range
        {
            get
            {
                if (!SpellSlot.W.IsFirstSpell())
                {
                    return W2.Range;
                }
                return W1_Range;
            }
        }
        public static bool Ignite_IsReady
        {
            get
            {
                return Ignite != null && Ignite.IsReady();
            }
        }
        public static bool Flash_IsReady
        {
            get
            {
                return Flash != null && Flash.IsReady();
            }
        }
    }
}
