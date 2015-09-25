﻿using System;
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

namespace Project_Zed
{
    class Program
    {
        static string Author = "iCreative";
        static string AddonName = "Project Zed";
        static float RefreshTime = 0.4f;
        static Dictionary<int, DamageInfo> PredictedDamage = new Dictionary<int, DamageInfo>() { };
        static AIHeroClient myHero { get { return ObjectManager.Player; } }
        static Vector3 mousePos { get { return Game.CursorPos; } }
        static Menu menu;
        static Dictionary<string, Menu> SubMenu = new Dictionary<string, Menu>() { };
        static Spell.Skillshot Q, W, E;
        static Spell.Targeted Ignite, R;
        static _Spell _W, _R;
        static Obj_AI_Minion wFound, rFound;
        static GameObject IsDeadObject = null;
        static Dictionary<int, bool> PassiveUsed = new Dictionary<int, bool>();
        static float Overkill
        {
            get
            {
                return (float)((100 + SubMenu["Misc"]["Overkill"].Cast<Slider>().CurrentValue) / 100);
            }
        }
        static bool IsWaitingShadow
        {
            get
            {
                return Game.Time - _W.LastCastTime < 0.1 && wShadow == null && IsW1 && W.IsReady();
                //return Game.Time - _W.LastCastTime < 0.5 && wShadow == null;
            }
        }
        static bool IsW1
        {
            get { return myHero.Spellbook.GetSpell(W.Slot).SData.Name.ToLower() != "zedw2"; }
        }
        static bool IsR1
        {
            get { return myHero.Spellbook.GetSpell(R.Slot).SData.Name.ToLower() != "zedr2"; }
        }
        static bool IsHarass
        {
            get { return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass) || SubMenu["Harass"]["Key1"].Cast<KeyBind>().CurrentValue || SubMenu["Harass"]["Key2"].Cast<KeyBind>().CurrentValue; }
        }
        static int TS_Range
        {
            get
            {
                if (wShadow != null && rShadow != null)
                {
                    return (int)(Q.Range + Math.Max(Extensions.Distance(myHero, rShadow), Extensions.Distance(myHero, wShadow)));
                }
                else if (IsW1 && W.IsReady() && rShadow != null)
                {
                    return (int)(Q.Range + Math.Max(Extensions.Distance(myHero, rShadow), 550));
                }
                else if (wShadow != null)
                {
                    return (int)(Q.Range + Extensions.Distance(myHero, wShadow));
                }
                else if (IsW1 && W.IsReady())
                {
                    return (int)(Q.Range + 550);
                }
                return (int)Q.Range;
            }
        }
        static AIHeroClient TS_Target
        {

            get
            {
                if (!IsR1)
                {
                    foreach (AIHeroClient enemy in HeroManager.Enemies)
                    {
                        if (enemy.IsValidTarget(TS_Range) && TargetHaveR(enemy))
                        {
                            return enemy;
                        }
                    }
                }
                var t = TargetSelector.GetTarget(TS_Range, EloBuddy.DamageType.Physical);
                if (IsDead(t))
                {
                    AIHeroClient t2 = null;
                    foreach (AIHeroClient enemy in HeroManager.Enemies.Where(o => o.NetworkId != t.NetworkId && o.IsValidTarget(TS_Range)))
                    {
                        if (t2 == null) { t2 = enemy; }
                        else if (TargetSelector.GetPriority(enemy) > TargetSelector.GetPriority(t2)) { t2 = enemy; }
                    }
                    if (t2 != null && t2.IsValidTarget(TS_Range))
                    {
                        return t2;
                    }
                }
                return t;
            }
        }
        static Obj_AI_Minion rShadow
        {
            get
            {
                if (IsR1 && R.IsReady() || _R.End == Vector3.Zero)
                {
                    return null;
                }
                if (rFound != null && !rFound.IsDead)
                {
                    return rFound;
                }
                rFound = ObjectManager.Get<Obj_AI_Minion>().FirstOrDefault(obj => obj.Name.ToLower() == "shadow" && !obj.IsDead && obj.Team == myHero.Team && Extensions.Distance(_R.End, obj) < 60);
                return rFound;
            }
        }
        static Obj_AI_Minion wShadow
        {
            get
            {
                if (IsW1 && W.IsReady() || _W.End == Vector3.Zero)
                {
                    return null;
                }
                if (wFound != null && !wFound.IsDead)
                {
                    return wFound;
                }
                if (rShadow != null)
                {
                    wFound = ObjectManager.Get<Obj_AI_Minion>().FirstOrDefault(obj => obj.Name.ToLower() == "shadow" && !obj.IsDead && obj.Team == myHero.Team && Extensions.Distance(_W.End, obj) < 100 && Extensions.Distance(rShadow, obj) > 0);
                    return wFound;
                }
                wFound = ObjectManager.Get<Obj_AI_Minion>().FirstOrDefault(obj => obj.Name.ToLower() == "shadow" && !obj.IsDead && obj.Team == myHero.Team && Extensions.Distance(_W.End, obj) < 250);
                return wFound;
            }
        }
        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += OnLoad;
        }
        private static void OnLoad(EventArgs args)
        {
            if (myHero.Hero != Champion.Zed) { return; }
            Chat.Print(AddonName + " made by " + Author + " loaded, have fun!.");
            Q = new Spell.Skillshot(SpellSlot.Q, 925, SkillShotType.Linear, 250, 1700, 46);
            Q.AllowedCollisionCount = int.MaxValue;
            W = new Spell.Skillshot(SpellSlot.W, 1500, SkillShotType.Linear, 0, 1600, 300);
            W.AllowedCollisionCount = int.MaxValue;
            E = new Spell.Skillshot(SpellSlot.E, 280, SkillShotType.Circular, 0, int.MaxValue, 100);
            E.AllowedCollisionCount = int.MaxValue;
            R = new Spell.Targeted(SpellSlot.R, 625);
            var slot = myHero.GetSpellSlotFromName("summonerdot");
            if (slot != SpellSlot.Unknown)
            {
                Ignite = new Spell.Targeted(slot, 600);
            }
            _W = new _Spell();
            _R = new _Spell();
            foreach (AIHeroClient enemy in HeroManager.Enemies)
            {
                PassiveUsed.Add(enemy.NetworkId, false);
            }

            menu = MainMenu.AddMenu(AddonName, AddonName + " by " + Author + "v1");
            menu.AddLabel(AddonName + " made by " + Author);

            SubMenu["Combo"] = menu.AddSubMenu("Combo", "Combo");
            SubMenu["Combo"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["Combo"].Add("W", new CheckBox("Use W", true));
            SubMenu["Combo"].Add("E", new CheckBox("Use E", true));
            SubMenu["Combo"].Add("R", new CheckBox("Use R", true));
            SubMenu["Combo"].Add("SwapDead", new CheckBox("Use W2/R2 if target will die", true));
            SubMenu["Combo"].Add("SwapHP", new Slider("Use W2/R2 if the % of my health is less than ", 15, 0, 100));
            SubMenu["Combo"].Add("SwapGapclose", new CheckBox("Use W2/R2 to get close to target", true));
            SubMenu["Combo"].Add("Prevent", new KeyBind("Don't use spells before R", true, KeyBind.BindTypes.PressToggle, (uint)'L'));

            SubMenu["Harass"] = menu.AddSubMenu("Harass", "Harass");
            SubMenu["Harass"].Add("SwapGapclose", new CheckBox("Use W2 if target is killable", true));
            SubMenu["Harass"].AddGroupLabel("Harass 1");
            SubMenu["Harass"].Add("Q", new CheckBox("Use Q on Harass 1", true));
            SubMenu["Harass"].Add("W", new CheckBox("Use W on Harass 1", false));
            SubMenu["Harass"].Add("E", new CheckBox("Use E on Harass 1", true));
            SubMenu["Harass"].Add("Mana", new Slider("Min. Mana Percent:", 20, 0, 100));
            SubMenu["Harass"].AddGroupLabel("Harass 2");
            SubMenu["Harass"].Add("Q2", new CheckBox("Use Q on Harass 2", true));
            SubMenu["Harass"].Add("W2", new CheckBox("Use W on Harass 2", true));
            SubMenu["Harass"].Add("E2", new CheckBox("Use E on Harass 2", true));
            SubMenu["Harass"].Add("Mana2", new Slider("Min. Mana Percent:", 20, 0, 100));
            SubMenu["Harass"].AddGroupLabel("Harass Keys");
            SubMenu["Harass"].Add("Key1", new KeyBind("Harass 1 Key", false, KeyBind.BindTypes.HoldActive, (uint)'C'));
            SubMenu["Harass"].Add("Key2", new KeyBind("Harass 2 Key", false, KeyBind.BindTypes.HoldActive, (uint)'K'));

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
            SubMenu["Flee"].Add("W", new CheckBox("Use W", true));
            SubMenu["Flee"].Add("E", new CheckBox("Use E", true));

            SubMenu["Draw"] = menu.AddSubMenu("Drawing", "Drawing");
            SubMenu["Draw"].Add("W", new CheckBox("Draw W Shadow", true));
            SubMenu["Draw"].Add("R", new CheckBox("Draw R Shadow", true));

            SubMenu["Misc"] = menu.AddSubMenu("Misc", "Misc");
            SubMenu["Misc"].Add("Overkill", new Slider("Overkill % for damage prediction", 10, 0, 100));


            Game.OnTick += OnTick;
            GameObject.OnCreate += OnCreateObj;
            GameObject.OnDelete += OnDeleteObj;
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;
        }

        static bool IsWall(Vector3 v)
        {
            var v2 = v.To2D();
            return NavMesh.GetCollisionFlags(v2.X, v2.Y).HasFlag(CollisionFlags.Wall);
        }

        static void OnTick(EventArgs args)
        {
            if (myHero.IsDead) { return; }
            KillSteal();
            Swap();
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Combo();
            }
            else if (IsHarass)
            {
                if (SubMenu["Harass"]["Key1"].Cast<KeyBind>().CurrentValue)
                {
                    Harass();
                }
                else if (SubMenu["Harass"]["Key2"].Cast<KeyBind>().CurrentValue)
                {
                    Harass2();
                }
            }
            else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
                {
                    JungleClear();
                }
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee))
            {
                Flee();
            }

        }
        static void KillSteal()
        {
            foreach (AIHeroClient enemy in HeroManager.Enemies)
            {
                if (enemy.IsValidTarget(TS_Range) && enemy.HealthPercent <= 40 && !IsDead(enemy))
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
        static void Combo()
        {
            var target = TS_Target;
            if (target.IsValidTarget())
            {
                var damageI = GetBestCombo(target);
                if (SubMenu["Combo"]["SwapHP"].Cast<Slider>().CurrentValue >= myHero.HealthPercent)
                {
                    if (damageI.Damage <= target.Health || myHero.HealthPercent < target.HealthPercent)
                    {
                        var HeroCount = myHero.CountEnemiesInRange(400);
                        var wCount = (wShadow != null && W.IsReady() && !IsW1) ? wShadow.CountEnemiesInRange(400) : 1000;
                        var rCount = (rShadow != null && R.IsReady() && !IsR1) ? rShadow.CountEnemiesInRange(400) : 1000;
                        var min = Math.Min(rCount, wCount);
                        if (HeroCount > min)
                        {
                            if (min == wCount)
                            {
                                myHero.Spellbook.CastSpell(W.Slot);
                            }
                            else if (min == rCount)
                            {
                                myHero.Spellbook.CastSpell(R.Slot);
                            }
                        }
                    }
                }
                if (IsDead(target) && SubMenu["Combo"]["SwapDead"].Cast<KeyBind>().CurrentValue)
                {
                    var HeroCount = myHero.CountEnemiesInRange(400);
                    var wCount = (wShadow != null && W.IsReady() && !IsW1) ? wShadow.CountEnemiesInRange(400) : 1000;
                    var rCount = (rShadow != null && R.IsReady() && !IsR1) ? rShadow.CountEnemiesInRange(400) : 1000;
                    var min = Math.Min(rCount, wCount);
                    if (HeroCount > min)
                    {
                        if (min == wCount)
                        {
                            myHero.Spellbook.CastSpell(W.Slot);
                        }
                        else if (min == rCount)
                        {
                            myHero.Spellbook.CastSpell(R.Slot);
                        }
                    }
                }
                if (SubMenu["Combo"]["R"].Cast<CheckBox>().CurrentValue) { CastR(target); }
                if (SubMenu["Combo"]["Prevent"].Cast<KeyBind>().CurrentValue && R.IsReady() && IsR1)
                {
                    return;
                }
                if (SubMenu["Combo"]["W"].Cast<CheckBox>().CurrentValue && NeedsW(target)) { CastW(target); }
                if (SubMenu["Combo"]["E"].Cast<CheckBox>().CurrentValue) { CastE(target); }
                if (SubMenu["Combo"]["Q"].Cast<CheckBox>().CurrentValue) { CastQ(target); }
            }
        }

        static void Harass()
        {
            var target = TS_Target;
            if (target.IsValidTarget() && myHero.ManaPercent >= SubMenu["Harass"]["Mana"].Cast<Slider>().CurrentValue)
            {
                if (SubMenu["Harass"]["W"].Cast<CheckBox>().CurrentValue) { CastW(target); }
                if (SubMenu["Harass"]["E"].Cast<CheckBox>().CurrentValue) { CastE(target); }
                if (SubMenu["Harass"]["Q"].Cast<CheckBox>().CurrentValue) { CastQ(target); }
            }
        }
        static void Harass2()
        {
            var target = TS_Target;
            if (target.IsValidTarget() && myHero.ManaPercent >= SubMenu["Harass"]["Mana2"].Cast<Slider>().CurrentValue)
            {
                if (SubMenu["Harass"]["W2"].Cast<CheckBox>().CurrentValue) { CastW(target); }
                if (SubMenu["Harass"]["E2"].Cast<CheckBox>().CurrentValue) { CastE(target); }
                if (SubMenu["Harass"]["Q2"].Cast<CheckBox>().CurrentValue) { CastQ(target); }
            }
        }
        static void Swap()
        {
            var target = TS_Target;
            if (target.IsValidTarget() && !IsDead(target))
            {
                var damageI = GetBestCombo(target);
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                {
                    if (SubMenu["Combo"]["SwapGapclose"].Cast<CheckBox>().CurrentValue && Extensions.Distance(myHero, target) > E.Range * 1.3f)
                    {
                        var heroDistance = Extensions.Distance(myHero, target);
                        var wShadowDistance = wShadow != null ? Extensions.Distance(myHero, wShadow) : 999999f;
                        var rShadowDistance = rShadow != null ? Extensions.Distance(myHero, rShadow) : 999999f;
                        var min = Math.Min(Math.Min(wShadowDistance, rShadowDistance), heroDistance);
                        if (min <= 500 && min < heroDistance)
                        {
                            if (min == wShadowDistance)
                            {
                                myHero.Spellbook.CastSpell(W.Slot);
                            }
                            else if (min == rShadowDistance)
                            {
                                myHero.Spellbook.CastSpell(R.Slot);
                            }
                        }
                    }
                }
                else if (IsHarass)
                {
                    if (SubMenu["Harass"]["SwapGapclose"].Cast<CheckBox>().CurrentValue && W.IsReady() && !IsW1 && wShadow != null && target.HealthPercent <= 50 && Passive(target, target.Health) > 0f && damageI.Damage / Overkill >= target.Health && Extensions.Distance(myHero, target) > Extensions.Distance(wShadow, target) && Extensions.Distance(wShadow, target) < E.Range)
                    {
                        myHero.Spellbook.CastSpell(W.Slot);
                    }
                }
            }
        }
        static void Flee()
        {
            if (SubMenu["Flee"]["W"].Cast<CheckBox>().CurrentValue && W.IsReady())
            {
                if (IsW1)
                    W.Cast(mousePos);
                else
                    myHero.Spellbook.CastSpell(W.Slot);
            }
            if (SubMenu["Flee"]["E"].Cast<CheckBox>().CurrentValue && E.IsReady())
            {
                foreach (AIHeroClient enemy in HeroManager.Enemies)
                {
                    if (enemy.IsValidTarget(TS_Range) && !enemy.IsValidTarget(E.Range))
                    {
                        CastE(enemy);
                    }
                }

            }

        }
        static void LaneClear()
        {
            foreach (Obj_AI_Base minion in EntityManager.GetLaneMinions(EntityManager.UnitTeam.Enemy, myHero.Position.To2D(), 1000f))
            {

            }
        }

        static void JungleClear()
        {
            if (myHero.ManaPercent >= SubMenu["JungleClear"]["Mana"].Cast<Slider>().CurrentValue)
            {
                foreach (Obj_AI_Base minion in EntityManager.GetJungleMonsters(myHero.Position.To2D(), 1000f))
                {
                    if (minion.IsValidTarget() && myHero.ManaPercent >= SubMenu["JungleClear"]["Mana"].Cast<Slider>().CurrentValue)
                    {
                        if (SubMenu["JungleClear"]["W"].Cast<CheckBox>().CurrentValue) { CastW(minion); }
                        if (SubMenu["JungleClear"]["Q"].Cast<CheckBox>().CurrentValue) { CastQ(minion); }
                        if (SubMenu["JungleClear"]["E"].Cast<CheckBox>().CurrentValue) { CastE(minion); }
                    }
                }

            }
        }
        static void CastQ(Obj_AI_Base target)
        {
            if (Q.IsReady() && target.IsValidTarget() && !IsWaitingShadow)
            {
                if (Extensions.Distance(myHero, target) < Q.Range * 0.9f)
                {
                    Q.SourcePosition = myHero.Position;
                }
                else
                {
                    var wShadowDistance = wShadow != null ? Extensions.Distance(myHero, wShadow) : 999999f;
                    var rShadowDistance = rShadow != null ? Extensions.Distance(myHero, rShadow) : 999999f;
                    var min = Math.Min(rShadowDistance, wShadowDistance);
                    if (min == rShadowDistance)
                    {
                        Q.SourcePosition = rShadow.Position;
                    }
                    else
                    {
                        Q.SourcePosition = wShadow.Position;
                    }
                }
                var pred = Q.GetPrediction(target);
                if (pred.HitChance == HitChance.High)
                {
                    Q.Cast(pred.CastPosition);
                }
            }
        }

        static void CastW(Obj_AI_Base target)
        {
            if (W.IsReady() && IsW1 && target.IsValidTarget() && Game.Time - _W.LastSentTime > 0.12f)
            {
                _W.LastSentTime = Game.Time;
                _W.LastCastTime = Game.Time;
                var r = W.GetPrediction(target);
                if (r.HitChance == HitChance.High)
                {
                    var wPos = Vector3.Zero;
                    if (rShadow != null)
                    {
                        wPos = myHero.Position + (r.CastPosition - rShadow.Position).Normalized() * 550;
                    }
                    else
                    {
                        wPos = myHero.Position + (r.CastPosition - myHero.Position).Normalized() * 550;
                    }
                    /**
                    if (IsWall(wPos))
                    {
                        for (float i = Extensions.Distance(myHero, wPos); i > 0; i = i - 10)
                        {
                            var notwall = myHero.Position + (wPos - myHero.Position).Normalized() * i;
                            if (!IsWall(notwall))
                            {
                                wPos = notwall;
                                break;
                            }
                        }
                    }**/
                    W.Cast(wPos);
                }
            }
        }
        static void CastE(Obj_AI_Base target)
        {
            if (E.IsReady() && target.IsValidTarget() && !IsWaitingShadow)
            {
                var pred = E.GetPrediction(target);
                var heroDistance = Extensions.Distance(myHero, pred.CastPosition);
                var wShadowDistance = wShadow != null ? Extensions.Distance(pred.CastPosition, wShadow) : 999999f;
                var rShadowDistance = rShadow != null ? Extensions.Distance(pred.CastPosition, rShadow) : 999999f;
                var min = Math.Min(Math.Min(rShadowDistance, wShadowDistance), heroDistance);
                if (min <= E.Range)
                {
                    myHero.Spellbook.CastSpell(E.Slot);
                }
            }
        }

        static void CastR(Obj_AI_Base target)
        {
            if (R.IsReady() && target.IsValidTarget() && IsR1)
            {
                R.Cast(target);
            }
        }


        static void OnCreateObj(GameObject sender, EventArgs args)
        {
            if (sender.Name.ToLower().Contains(myHero.ChampionName.ToLower()))
            {
                if (sender.Name.ToLower().Contains("base_r") && sender.Name.ToLower().Contains("buf_tell") && TS_Target != null && Extensions.Distance(TS_Target, sender) < 200)
                {
                    IsDeadObject = sender;
                }
                if (sender.Name.ToLower().Contains("passive") && sender.Name.ToLower().Contains("proc") && sender.Name.ToLower().Contains("target"))
                {
                    if (Orbwalker.LastTarget != null)
                    {
                        if (Extensions.Distance(Orbwalker.LastTarget, sender) < 100 && PassiveUsed.ContainsKey(Orbwalker.LastTarget.NetworkId))
                        {
                            var target = Orbwalker.LastTarget;
                            PassiveUsed[Orbwalker.LastTarget.NetworkId] = true;
                            Core.DelayAction(delegate { PassiveUsed[target.NetworkId] = false; }, 10 * 1000);
                        }
                    }
                }
            }
        }
        static void OnDeleteObj(GameObject sender, EventArgs args)
        {
            if (sender.Name.ToLower().Contains(myHero.ChampionName.ToLower()) && sender.Name.ToLower().Contains("base_r") && sender.Name.ToLower().Contains("buf_tell"))
            {
                IsDeadObject = null;
            }
        }

        static void OnDraw(EventArgs args)
        {
            if (myHero.IsDead) { return; }
            if (wShadow != null && SubMenu["Draw"]["W"].Cast<CheckBox>().CurrentValue)
            {
                Circle.Draw(Color.Blue, 100, wShadow.Position);
            }
            if (rShadow != null && SubMenu["Draw"]["R"].Cast<CheckBox>().CurrentValue)
            {
                Circle.Draw(Color.Orange, 100, rShadow.Position);
            }
        }


        static void OnProcessSpell(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.SData.Name.ToLower() == myHero.Spellbook.GetSpell(SpellSlot.W).SData.Name.ToLower())
                {
                    if (args.SData.Name.ToLower() == "zedw2")
                    {
                        _W.End = args.End;
                    }
                    else
                    {
                        var pos = args.End;
                        if (IsWall(pos))
                        {
                            for (float i = Extensions.Distance(myHero, args.End); i > 0; i = i - 10)
                            {
                                var notwall = myHero.Position + (pos - myHero.Position).Normalized() * i;
                                if (!IsWall(notwall))
                                {
                                    pos = notwall;
                                    break;
                                }
                            }
                        }
                        _W.End = pos;
                        _W.LastCastTime = Game.Time;
                        Core.DelayAction(() => ResetW(), (int)(Extensions.Distance(myHero, _W.End) / W.Speed + 6) * 1000);
                    }
                }
                else if (args.SData.Name.ToLower() == myHero.Spellbook.GetSpell(SpellSlot.R).SData.Name.ToLower())
                {
                    if (args.SData.Name.ToLower() == "zedr2")
                    {
                        _R.End = args.End;
                    }
                    else
                    {
                        _R.End = myHero.Position;
                        _R.LastCastTime = Game.Time;
                        Core.DelayAction(() => ResetR(), 8 * 1000);
                    }
                }
            }
        }

        static void ResetR()
        {
            _R.End = Vector3.Zero;
            rFound = null;
        }
        static void ResetW()
        {
            _W.End = Vector3.Zero;
            wFound = null;
        }

        static bool NeedsW(Obj_AI_Base target)
        {
            if (target.IsValidTarget())
            {
                var damageI = GetBestCombo(target);
                if (Extensions.Distance(myHero, target) < 550 && damageI.Damage >= target.Health && (myHero.Mana < myHero.Spellbook.GetSpell(SpellSlot.W).SData.Mana + myHero.Spellbook.GetSpell(SpellSlot.Q).SData.Mana || myHero.Mana < myHero.Spellbook.GetSpell(SpellSlot.W).SData.Mana + myHero.Spellbook.GetSpell(SpellSlot.E).SData.Mana))
                {
                    return false;
                }
            }
            return true;
        }
        static bool TargetHaveR(Obj_AI_Base target)
        {
            return target.HasBuff("zedrtargetmark");
        }
        static bool IsDead(Obj_AI_Base target)
        {
            if (!IsR1 && target.IsValidTarget() && TargetHaveR(target))
            {
                if (IsDeadObject != null)
                {
                    return Extensions.Distance(IsDeadObject, target) < 200;
                }
            }
            return false;
        }
        static float Passive(Obj_AI_Base target, float health)
        {
            float damage = 0f;
            if (100 * health / target.MaxHealth <= 50)
            {
                if (PassiveUsed.ContainsKey(target.NetworkId)){
                    if (PassiveUsed[target.NetworkId]) {
                        return 0f;
                    }
                }
                return myHero.CalculateDamageOnUnit(target, DamageType.Physical, (float)(4 + 2 * R.Level) / target.MaxHealth);
            }
            return damage;
        }
        static float Damage(Obj_AI_Base target, SpellSlot slot)
        {
            if (target.IsValidTarget())
            {
                if (slot == SpellSlot.Q)
                {
                    return myHero.CalculateDamageOnUnit(target, DamageType.Physical, (float)40 * Q.Level + 35 + 1f * myHero.FlatPhysicalDamageMod);
                }
                else if (slot == SpellSlot.W)
                {
                    return 0;
                }
                else if (slot == SpellSlot.E)
                {
                    return myHero.CalculateDamageOnUnit(target, DamageType.Physical, (float)30 * E.Level + 30 + 0.8f * myHero.FlatPhysicalDamageMod);
                }
                else if (slot == SpellSlot.R)
                {
                    return myHero.CalculateDamageOnUnit(target, DamageType.Physical, (float)1f * myHero.FlatPhysicalDamageMod);
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
                if (w && IsW1 && W.IsReady())
                {
                    ManaWasted += myHero.Spellbook.GetSpell(SpellSlot.W).SData.Mana;
                }
                if (w && e)
                {
                    ComboDamage += Damage(target, E.Slot);
                }
                if (w && q)
                {
                    ComboDamage += 0.5f * Damage(target, Q.Slot);
                }
                if (r && q)
                {
                    ComboDamage += 0.5f * Damage(target, Q.Slot);
                }

                if (q)
                {
                    ComboDamage += Damage(target, Q.Slot);
                    ManaWasted += myHero.Spellbook.GetSpell(SpellSlot.Q).SData.Mana;
                }
                if (e)
                {
                    ComboDamage += Damage(target, E.Slot);
                    ManaWasted += myHero.Spellbook.GetSpell(SpellSlot.E).SData.Mana;
                }
                if (Ignite != null && Ignite.IsReady())
                {
                    ComboDamage += myHero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite);
                }
                if (r)
                {
                    ComboDamage += Damage(target, R.Slot);
                    ComboDamage += ComboDamage * (5f + R.Level * 15) / 100f;
                    ManaWasted += myHero.Spellbook.GetSpell(SpellSlot.R).SData.Mana;
                }
                if (TargetHaveR(target))
                {
                    ComboDamage += ComboDamage * (5f + R.Level * 15) / 100f;
                }
                ComboDamage += myHero.GetAutoAttackDamage(target, true);
            }
            ComboDamage += Passive(target, target.Health - ComboDamage);
            ComboDamage = ComboDamage * Overkill;
            return new DamageInfo(ComboDamage, ManaWasted);
        }

        static DamageInfo GetBestCombo(Obj_AI_Base target)
        {
            var q = Q.IsReady() ? new bool[] { false, true } : new bool[] { false };
            var w = ((W.IsReady() && IsW1) || wShadow != null) ? new bool[] { false, true } : new bool[] { false };
            var e = E.IsReady() ? new bool[] { false, true } : new bool[] { false };
            var r = (R.IsReady() && IsR1) ? new bool[] { false, true } : new bool[] { false };
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

    class DamageInfo
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
    public class _Spell
    {
        public float LastCastTime = 0;
        public float LastSentTime = 0;
        public Vector3 End = Vector3.Zero;
    }
}
