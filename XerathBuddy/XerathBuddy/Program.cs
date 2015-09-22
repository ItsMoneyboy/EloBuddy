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

namespace XerathBuddy
{
    class Program
    {
        private static string Author = "iCreative";
        private static string AddonName = "XerathBuddy";
        private static float RefreshTime = 0.4f;
        private static Dictionary<int, DamageInfo> PredictedDamage = new Dictionary<int, DamageInfo>() { };
        private static AIHeroClient myHero { get { return ObjectManager.Player; } }
        private static Vector3 mousePos { get { return Game.CursorPos; } }
        private static Menu menu;
        private static Dictionary<string, Menu> SubMenu = new Dictionary<string, Menu>() { };
        //private static Spell.Chargeable Q;
        private static Spell.Skillshot Q;
        private static Spell.Skillshot W, E, R;
        private static Spell.Targeted Ignite;
        private static bool Q_IsCharging = false;
        private static float Q_LastCastTime = 0f;
        private static float E_LastCastTime = 0f;
        private static GameObject E_GameObject = null;
        private static int R_Stack
        {
            get
            {
                if (myHero.HasBuff("xerathrshots"))
                    return myHero.GetBuff("xerathrshots").Count;
                if (R.IsReady())
                    return 3;
                return 0;
            }
        }
        private static bool R_IsCasting = false;
        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += OnLoad;
        }
        private static void OnLoad(EventArgs args)
        {
            if (myHero.Hero != Champion.Xerath) { return; }
            Chat.Print(AddonName + " made by " + Author + " loaded, have fun!.");
            //Q = new Spell.Chargeable(SpellSlot.Q, 750, 1550, 1500, 600, int.MaxValue, 100);
            Q = new Spell.Skillshot(SpellSlot.Q, 1550, SkillShotType.Linear, 600, int.MaxValue, 100);
            Q.AllowedCollisionCount = int.MaxValue;
            W = new Spell.Skillshot(SpellSlot.W, 1100, SkillShotType.Circular, 790, int.MaxValue, 100);
            W.AllowedCollisionCount = int.MaxValue;
            E = new Spell.Skillshot(SpellSlot.E, 1050, SkillShotType.Linear, 250, 1400, 60);
            E.AllowedCollisionCount = 0;
            R = new Spell.Skillshot(SpellSlot.R, 3200, SkillShotType.Circular, 650, int.MaxValue, 120);
            R.AllowedCollisionCount = int.MaxValue;
            var slot = myHero.GetSpellSlotFromName("summonerdot");
            if (slot != SpellSlot.Unknown)
            {
                Ignite = new Spell.Targeted(slot, 600);
            }

            menu = MainMenu.AddMenu(AddonName, AddonName + " by " + Author + "v1.0");
            menu.AddLabel(AddonName + " made by " + Author);

            SubMenu["Combo"] = menu.AddSubMenu("Combo", "Combo");
            SubMenu["Combo"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["Combo"].Add("W", new CheckBox("Use W", true));
            SubMenu["Combo"].Add("E", new CheckBox("Use E", true));

            SubMenu["Ultimate"] = menu.AddSubMenu("Ultimate", "Ultimate");

            SubMenu["Harass"] = menu.AddSubMenu("Harass", "Harass");
            SubMenu["Harass"].Add("Q", new CheckBox("Use Q", false));
            SubMenu["Harass"].Add("W", new CheckBox("Use W", false));
            SubMenu["Harass"].Add("E", new CheckBox("Use E", false));
            SubMenu["Harass"].Add("Mana", new Slider("Min. Mana Percent:", 20, 0, 100));

            SubMenu["JungleClear"] = menu.AddSubMenu("JungleClear", "JungleClear");
            SubMenu["JungleClear"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["JungleClear"].Add("W", new CheckBox("Use W", true));
            SubMenu["JungleClear"].Add("E", new CheckBox("Use E", true));
            SubMenu["JungleClear"].Add("Mana", new Slider("Min. Mana Percent:", 20, 0, 100));

            SubMenu["KillSteal"] = menu.AddSubMenu("KillSteal", "KillSteal");
            SubMenu["KillSteal"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["KillSteal"].Add("W", new CheckBox("Use W", true));
            SubMenu["KillSteal"].Add("E", new CheckBox("Use E", true));
            SubMenu["KillSteal"].Add("Ignite", new CheckBox("Use Ignite", true));

            SubMenu["Flee"] = menu.AddSubMenu("Flee", "Flee");
            SubMenu["Flee"].Add("E", new CheckBox("Use E", true));

            SubMenu["Draw"] = menu.AddSubMenu("Drawing", "Drawing");
            SubMenu["Draw"].Add("Killable", new CheckBox("Draw text if killable with R", true));

            SubMenu["Misc"] = menu.AddSubMenu("Misc", "Misc");
            SubMenu["Misc"].Add("Overkill", new Slider("Overkill % for damage prediction", 10, 0, 100));
            SubMenu["Misc"].Add("Gapclose", new CheckBox("Use E on gapclose spells", true));
            SubMenu["Misc"].Add("Channeling", new CheckBox("Use E on channeling spells", true));

            Game.OnTick += OnTick;
            GameObject.OnCreate += OnCreateObj;
            GameObject.OnDelete += OnDeleteObj;
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;
            Obj_AI_Base.OnBuffGain += OnApplyBuff;
            Obj_AI_Base.OnBuffLose += OnRemoveBuff;
            Gapcloser.OnGapcloser += OnGapCloser;
            Interrupter.OnInterruptableSpell += OnInterruptableSpell;
        }

        private static void OnTick(EventArgs args)
        {
            R = new Spell.Skillshot(R.Slot, (uint)(2000 + 1200 * R.Level), R.Type, R.CastDelay, R.Speed, R.Width);
            R.AllowedCollisionCount = int.MaxValue;
            if (Q_IsCharging)
            {
                Q = new Spell.Skillshot(Q.Slot, (uint)Math.Min((1550 - 750) * (Game.Time - Q_LastCastTime) / 1.5f + 750, 1550), Q.Type, Q.CastDelay, Q.Speed, Q.Width);
            }
            else
            {
                Q = new Spell.Skillshot(Q.Slot, 1550, Q.Type, Q.CastDelay, Q.Speed, Q.Width);
            }
            Q.AllowedCollisionCount = int.MaxValue;
            if (R_IsCasting)
            {
                Orbwalker.DisableMovement = true;
            }
            else
            {
                Orbwalker.DisableMovement = false;
            }
            if (Q_IsCharging || R_IsCasting)
            {
                Orbwalker.DisableAttacking = true;
            }
            else
            {
                Orbwalker.DisableAttacking = false;
            }
            if (R_IsCasting)
            {
                return;
            }
            KillSteal();
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Combo();
            }
            else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                Harass();
            }
            else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
                {
                    JungleClear();
                }
            }
        }


        private static void CastQ(Obj_AI_Base target)
        {
            if (Q.IsReady() && target.IsValidTarget(Q.Range * 1.2f))
            {
                var pred = Q.GetPrediction(target);
                //Chat.Print(pred.HitChance + " " + Q.Range + " " + Q_IsCharging + " " + Q.Width + " " + Q.CastDelay);
                if (Q_IsCharging)
                {
                    if (pred.HitChance == HitChance.Medium)
                    {
                        myHero.Spellbook.CastSpell(Q.Slot, pred.CastPosition);
                        myHero.Spellbook.UpdateChargeableSpell(Q.Slot, pred.CastPosition, true);
                    }
                }
                else
                {
                    if (pred.HitChance == HitChance.AveragePoint)
                    {
                        myHero.Spellbook.CastSpell(Q.Slot);
                        //myHero.Spellbook.CastSpell(Q.Slot, mousePos);
                    }
                }
            }
        }
        private static void CastW(Obj_AI_Base target)
        {
            if (W.IsReady() && target.IsValidTarget())
            {
                var pred = W.GetPrediction(target);
                if (pred.HitChance == HitChance.High)
                {
                    W.Cast(pred.CastPosition);
                }
            }
        }
        private static void CastE(Obj_AI_Base target)
        {
            if (E.IsReady() && target.IsValidTarget())
            {
                var pred = E.GetPrediction(target);
                if (pred.HitChance == HitChance.High)
                {
                    E.Cast(pred.CastPosition);
                }
            }
        }
        private static void KillSteal()
        {
            foreach (AIHeroClient enemy in HeroManager.Enemies)
            {
                if (enemy.IsValidTarget(E.Range) && enemy.HealthPercent <= 40)
                {
                    var damageI = GetBestCombo(enemy);
                    if (damageI.Damage >= enemy.Health)
                    {
                        if (SubMenu["KillSteal"]["Q"].Cast<CheckBox>().CurrentValue && (Damage(enemy, Q.Slot) >= enemy.Health || damageI.Q)) { CastQ(enemy); }
                        if (SubMenu["KillSteal"]["W"].Cast<CheckBox>().CurrentValue && (Damage(enemy, W.Slot) >= enemy.Health || damageI.W)) { CastW(enemy); }
                        if (SubMenu["KillSteal"]["E"].Cast<CheckBox>().CurrentValue && (Damage(enemy, E.Slot) >= enemy.Health || damageI.E)) { CastE(enemy); }
                    }
                    if (Ignite != null && SubMenu["KillSteal"]["Ignite"].Cast<CheckBox>().CurrentValue && Ignite.IsReady() && myHero.GetSummonerSpellDamage(enemy, DamageLibrary.SummonerSpells.Ignite) >= enemy.Health)
                    {
                        Ignite.Cast(enemy);
                    }
                }
            }
        }

        private static void JungleClear()
        {
            if (myHero.ManaPercent >= SubMenu["JungleClear"]["Mana"].Cast<Slider>().CurrentValue)
            {
                foreach (Obj_AI_Base minion in EntityManager.GetJungleMonsters(myHero.Position.To2D(), 1000f))
                {
                    if (minion.IsValidTarget() && myHero.ManaPercent >= SubMenu["JungleClear"]["Mana"].Cast<Slider>().CurrentValue)
                    {
                        if (SubMenu["JungleClear"]["E"].Cast<CheckBox>().CurrentValue) { CastE(minion); }
                        if ((Game.Time - E_LastCastTime <= (float)(E.CastDelay / 1000 * 1.1)) || (E_GameObject != null && Extensions.Distance(myHero, minion) > Extensions.Distance(myHero, E_GameObject)))
                        {
                            return;
                        }
                        if (SubMenu["JungleClear"]["Q"].Cast<CheckBox>().CurrentValue) { CastQ(minion); }
                        if (SubMenu["JungleClear"]["W"].Cast<CheckBox>().CurrentValue) { CastW(minion); }
                    }
                }

            }
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(Q.Range, EloBuddy.DamageType.Magical);
            if (target.IsValidTarget() && myHero.ManaPercent >= SubMenu["Harass"]["Mana"].Cast<Slider>().CurrentValue)
            {
                if (SubMenu["Harass"]["E"].Cast<CheckBox>().CurrentValue) { CastE(target); }
                if ((Game.Time - E_LastCastTime <= (float)(E.CastDelay / 1000 * 1.1)) || (E_GameObject != null && Extensions.Distance(myHero, target) > Extensions.Distance(myHero, E_GameObject)))
                {
                    return;
                }
                if (SubMenu["Harass"]["Q"].Cast<CheckBox>().CurrentValue) { CastQ(target); }
                if (SubMenu["Harass"]["W"].Cast<CheckBox>().CurrentValue) { CastW(target); }
            }
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(Q.Range, EloBuddy.DamageType.Magical);
            if (target.IsValidTarget())
            {
                if (SubMenu["Combo"]["E"].Cast<CheckBox>().CurrentValue) { CastE(target); }
                if ((Game.Time - E_LastCastTime <= (float)(E.CastDelay / 1000 * 1.1)) || (E_GameObject != null && Extensions.Distance(myHero, target) > Extensions.Distance(myHero, E_GameObject)))
                {
                    return;
                }
                if (SubMenu["Combo"]["Q"].Cast<CheckBox>().CurrentValue) { CastQ(target); }
                if (SubMenu["Combo"]["W"].Cast<CheckBox>().CurrentValue) { CastW(target); }
            }
        }

        private static void OnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs e)
        {
            if (SubMenu["Misc"]["Channeling"].Cast<CheckBox>().CurrentValue)
            {
                CastE(e.Sender);
            }
        }

        private static void OnGapCloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            if (SubMenu["Misc"]["Gapclose"].Cast<CheckBox>().CurrentValue)
            {
                CastE(e.Sender);
            }
        }

        private static void OnApplyBuff(Obj_AI_Base sender, Obj_AI_BaseBuffGainEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.Buff.Name.ToLower().Contains("xeratharcanopulsechargeup"))
                {
                    Q_IsCharging = true;
                }
                else if (args.Buff.Name.ToLower().Contains("xerathlocusofpower2"))
                {
                    R_IsCasting = true;
                }
            }
        }
        private static void OnRemoveBuff(Obj_AI_Base sender, Obj_AI_BaseBuffLoseEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.Buff.Name.ToLower().Contains("xeratharcanopulsechargeup"))
                {
                    Q_IsCharging = false;
                }
                else if (args.Buff.Name.ToLower().Contains("xerathlocusofpower2"))
                {
                    R_IsCasting = false;
                }
            }
        }


        private static void OnProcessSpell(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.SData.Name.ToLower() == myHero.Spellbook.GetSpell(SpellSlot.Q).SData.Name.ToLower())
                {
                    if (args.SData.Name.ToLower().Contains("locus2"))
                    {
                        Q_IsCharging = false;
                        Orbwalker.DisableAttacking = false;
                    }
                    else
                    {
                        Q_IsCharging = true;
                        Orbwalker.DisableAttacking = true;
                        Q_LastCastTime = Game.Time;
                    }
                }
                else if (args.SData.Name.ToLower() == myHero.Spellbook.GetSpell(SpellSlot.E).SData.Name.ToLower())
                {
                    E_LastCastTime = Game.Time;
                }
                else if (args.SData.Name.ToLower() == myHero.Spellbook.GetSpell(SpellSlot.R).SData.Name.ToLower())
                {
                    if (args.SData.Name.ToLower().Contains("locusofpower2"))
                    {
                        R_IsCasting = true;
                    }
                    else
                    {

                    }
                }
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (myHero.IsDead)
                return;
            if (SubMenu["Draw"]["Killable"].Cast<CheckBox>().CurrentValue && R.IsReady() && myHero.Mana >= myHero.Spellbook.GetSpell(R.Slot).SData.Mana)
            {
                var count = 0;
                foreach (AIHeroClient enemy in HeroManager.Enemies)
                {
                    if (enemy.IsValidTarget(R.Range))
                    {
                        if (Damage(enemy, R.Slot) * R_Stack >= enemy.Health)
                        {
                            if (enemy.VisibleOnScreen)
                            {
                                var p = Drawing.WorldToScreen(enemy.Position);
                                Drawing.DrawText(p, System.Drawing.Color.Red, "R Killable", 200);
                            }

                            Drawing.DrawText(new Vector2(100, 50 + count * 50), System.Drawing.Color.Red, enemy.ChampionName.ToUpper() + " KILLABLE", 250);
                            count++;
                        }
                    }
                }

            }
        }


        private static void OnCreateObj(GameObject sender, EventArgs args)
        {
            if (sender.Name.ToLower().Contains(myHero.ChampionName.ToLower()))
            {
                var name = sender.Name.ToLower();
                if (Extensions.Distance(sender, myHero) < 150)
                {
                    if (name.Contains("_r") && name.Contains("_buf"))
                    {
                        R_IsCasting = true;
                    }
                    else if (name.Contains("_q") && name.Contains("_cas") && name.Contains("_charge"))
                    {
                        Q_IsCharging = true;
                    }
                }
            }
            var missile = (MissileClient)sender;
            if (missile == null || !missile.IsValid || missile.SpellCaster == null || !missile.SpellCaster.IsValid)
            {
                return;
            }
            var unit = (Obj_AI_Base)missile.SpellCaster;
            if (missile.SpellCaster.IsMe)
            {
                var name = missile.SData.Name.ToLower();
                if (name.Contains("xerathmagespear"))
                {
                    E_GameObject = sender;
                }
            }
        }
        private static void OnDeleteObj(GameObject sender, EventArgs args)
        {
            if (sender.Name.ToLower().Contains(myHero.ChampionName.ToLower()))
            {
                var name = sender.Name.ToLower();
                if (Extensions.Distance(sender, myHero) < 150)
                {
                    if (name.Contains("_r") && name.Contains("_buf"))
                    {
                        R_IsCasting = false;
                    }
                    else if (name.Contains("_q") && name.Contains("_cas") && name.Contains("_charge"))
                    {
                        Q_IsCharging = false;
                    }
                }
            }
            var missile = (MissileClient)sender;
            if (missile == null || !missile.IsValid || missile.SpellCaster == null || !missile.SpellCaster.IsValid)
            {
                return;
            }
            var unit = (Obj_AI_Base)missile.SpellCaster;
            if (missile.SpellCaster.IsMe)
            {
                var name = missile.SData.Name.ToLower();
                if (name.Contains("xerathmagespear"))
                {
                    E_GameObject = null;
                }
            }
        }

        static float Damage(Obj_AI_Base target, SpellSlot slot)
        {
            if (target.IsValidTarget())
            {
                if (slot == SpellSlot.Q)
                {
                    return myHero.CalculateDamageOnUnit(target, DamageType.Magical, (float)40 * Q.Level + 40 + 0.75f * myHero.FlatMagicDamageMod);
                }
                else if (slot == SpellSlot.W)
                {
                    return myHero.CalculateDamageOnUnit(target, DamageType.Magical, (float)45 * W.Level + 45 + 0.9f * myHero.FlatMagicDamageMod);
                }
                else if (slot == SpellSlot.E)
                {
                    return myHero.CalculateDamageOnUnit(target, DamageType.Magical, (float)30 * E.Level + 50 + 0.45f * myHero.FlatMagicDamageMod);
                }
                else if (slot == SpellSlot.R)
                {
                    return myHero.CalculateDamageOnUnit(target, DamageType.Magical, (float)55 * R.Level + 135 + 0.43f * myHero.FlatMagicDamageMod);
                }
            }
            return myHero.GetSpellDamage(target, slot);
        }

        static DamageInfo GetComboDamage(Obj_AI_Base target, bool q, bool w, bool e, bool r)
        {
            var ComboDamage = 0f;
            var ManaWasted = 0f;
            if (target.IsValidTarget())
            {
                if (q)
                {
                    ComboDamage += Damage(target, Q.Slot);
                    ManaWasted += myHero.Spellbook.GetSpell(SpellSlot.Q).SData.Mana;
                }
                if (w)
                {
                    ComboDamage += Damage(target, W.Slot);
                    ManaWasted += myHero.Spellbook.GetSpell(SpellSlot.W).SData.Mana;
                }
                if (e)
                {
                    ComboDamage += Damage(target, E.Slot);
                    ManaWasted += myHero.Spellbook.GetSpell(SpellSlot.E).SData.Mana;
                }
                if (r)
                {
                    ComboDamage += R_Stack * Damage(target, R.Slot);
                    ManaWasted += myHero.Spellbook.GetSpell(SpellSlot.R).SData.Mana;
                }
                if (Ignite != null && Ignite.IsReady())
                {
                    ComboDamage += myHero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite);
                }
                ComboDamage += myHero.GetAutoAttackDamage(target, true);
            }

            return new DamageInfo(ComboDamage, ManaWasted);
        }

        static float GetOverkill()
        {
            return (float)((100 + SubMenu["Misc"]["Overkill"].Cast<Slider>().CurrentValue) / 100);
        }
        static DamageInfo GetBestCombo(Obj_AI_Base target)
        {
            var q = Q.IsReady() ? new bool[] { false, true } : new bool[] { false };
            var w = W.IsReady() ? new bool[] { false, true } : new bool[] { false };
            var e = E.IsReady() ? new bool[] { false, true } : new bool[] { false };
            var r = R.IsReady() ? new bool[] { false, true } : new bool[] { false };
            if (target.IsValidTarget())
            {
                if (PredictedDamage.ContainsKey(target.NetworkId))
                {
                    var damageI = PredictedDamage[target.NetworkId];
                    if (Game.Time - damageI.Time <= RefreshTime)
                    {
                        return damageI;
                    }
                    else
                    {
                        bool[] best = new bool[] {
							Q.IsReady (),
							W.IsReady (),
							E.IsReady (),
							R.IsReady ()
						};
                        var bestdmg = 0f;
                        var bestmana = 0f;
                        foreach (bool q1 in q)
                        {
                            foreach (bool w1 in w)
                            {
                                foreach (bool e1 in e)
                                {
                                    foreach (bool r1 in r)
                                    {
                                        DamageInfo damageI2 = GetComboDamage(target, q1, w1, e1, r1);
                                        float d = damageI2.Damage;
                                        float m = damageI2.Mana;
                                        if (myHero.Mana >= m)
                                        {
                                            if (bestdmg >= target.Health)
                                            {
                                                if (d >= target.Health && (d < bestdmg || m < bestmana))
                                                {
                                                    bestdmg = d;
                                                    bestmana = m;
                                                    best = new bool[] { q1, w1, e1, r1 };
                                                }
                                            }
                                            else
                                            {
                                                if (d >= bestdmg)
                                                {
                                                    bestdmg = d;
                                                    bestmana = m;
                                                    best = new bool[] { q1, w1, e1, r1 };
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        PredictedDamage[target.NetworkId] = new DamageInfo(best[0], best[1], best[2], best[3], bestdmg, bestmana, Game.Time);
                        return PredictedDamage[target.NetworkId];
                    }
                }
                else
                {
                    var damageI2 = GetComboDamage(target, Q.IsReady(), W.IsReady(), E.IsReady(), R.IsReady());
                    PredictedDamage[target.NetworkId] = new DamageInfo(false, false, false, false, damageI2.Damage, damageI2.Mana, Game.Time - Game.Ping * 2);
                    return GetBestCombo(target);
                }
            }
            return new DamageInfo(false, false, false, false, 0, 0, 0);
        }
    }

    public class DamageInfo
    {
        public bool Q;
        public bool W;
        public bool E;
        public bool R;
        public float Damage;
        public float Mana;
        public float Time;

        public DamageInfo(bool Q, bool W, bool E, bool R, float Damage, float Mana, float Time)
        {
            this.Q = Q;
            this.W = W;
            this.E = E;
            this.R = R;
            this.Damage = Damage;
            this.Mana = Mana;
            this.Time = Time;
        }
        public DamageInfo(float Damage, float Mana)
        {
            this.Damage = Damage;
            this.Mana = Mana;
        }
    }
}
