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
        static Spell.Skillshot Q, W, E = null;
        static Spell.Targeted Ignite, R = null;
        static _Spell _W, _R;
        static Obj_AI_Minion wFound, rFound;
        static GameObject IsDeadObject = null;
        static Dictionary<int, bool> PassiveUsed = new Dictionary<int, bool>();
        static List<Obj_AI_Minion> Shadows = new List<Obj_AI_Minion>();
        static bool IsWaitingShadow
        {
            get
            {
                return Game.Time - _W.LastCastTime < 0.1f && wShadow == null && IsW1 && W.IsReady();
                //return Game.Time - _W.LastCastTime < 1.5f && wShadow == null;
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
        /*
        static bool Combo1Pressed, Combo2Pressed, Harass1Pressed, Harass2Pressed = false;
        static int HarassType
        {
            get
            {
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
                {
                    if (Harass1Pressed)
                    {
                        return 1;
                    }
                    else if (Harass2Pressed)
                    {
                        return 2;
                    }
                }
                return -1;
            }
        }
        static int ComboType
        {
            get
            {
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                {
                    if (Combo1Pressed)
                    {
                        return 1;
                    }
                    else if (Combo2Pressed)
                    {
                        return 2;
                    }
                }
                return -1;
            }
        }*/
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
                    foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies)
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
                    foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies.Where(o => o.NetworkId != t.NetworkId && o.IsValidTarget(TS_Range)))
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
                rFound = Shadows.FirstOrDefault(obj => !obj.IsDead && obj.Team == myHero.Team && Extensions.Distance(_R.End, obj) < 60);
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
                    wFound = Shadows.FirstOrDefault(obj => !obj.IsDead && obj.Team == myHero.Team && Extensions.Distance(_W.End, obj) < 100 && Extensions.Distance(rShadow, obj) > 0);
                    return wFound;
                }
                wFound = Shadows.Where(obj => !obj.IsDead && obj.Team == myHero.Team && Extensions.Distance(_W.End, obj) < 550).OrderBy(o => Extensions.Distance(_W.End, o)).FirstOrDefault();
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
            Q = new Spell.Skillshot(SpellSlot.Q, 925, SkillShotType.Linear, 250, 1700, 50);
            Q.AllowedCollisionCount = int.MaxValue;
            W = new Spell.Skillshot(SpellSlot.W, 1500, SkillShotType.Linear, 0, 1600, 50);
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
            foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies)
            {
                PassiveUsed.Add(enemy.NetworkId, false);
            }

            menu = MainMenu.AddMenu(AddonName, AddonName + " by " + Author + "v1.10");
            menu.AddLabel(AddonName + " made by " + Author);

            SubMenu["Prediction"] = menu.AddSubMenu("Prediction", "Prediction");
            SubMenu["Prediction"].AddGroupLabel("Q Settings");
            SubMenu["Prediction"].Add("QCombo", new Slider("Combo HitChancePercent", 60, 0, 100));
            SubMenu["Prediction"].Add("QHarass", new Slider("Harass HitChancePercent", 75, 0, 100));

            SubMenu["Combo"] = menu.AddSubMenu("Combo", "Combo");
            SubMenu["Combo"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["Combo"].Add("W", new CheckBox("Use W", true));
            SubMenu["Combo"].Add("E", new CheckBox("Use E", true));
            SubMenu["Combo"].Add("R", new CheckBox("Use R", true));
            SubMenu["Combo"].Add("SwapDead", new CheckBox("Use W2/R2 if target will die", true));
            SubMenu["Combo"].Add("SwapHP", new Slider("Use W2/R2 if my HealthPercent is less than", 10, 0, 100));
            SubMenu["Combo"].Add("SwapGapclose", new CheckBox("Use W2/R2 to get close to target", true));
            SubMenu["Combo"].Add("Prevent", new KeyBind("Don't use spells before R", true, KeyBind.BindTypes.PressToggle, (uint)'L'));
            SubMenu["Combo"].AddGroupLabel("Don't use R on");
            foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies)
            {
                SubMenu["Combo"].Add(enemy.ChampionName, new CheckBox(enemy.ChampionName, false));
            }

            SubMenu["Harass"] = menu.AddSubMenu("Harass", "Harass");
            SubMenu["Harass"].Add("Collision", new CheckBox("Check collision with Q", false));
            SubMenu["Harass"].Add("SwapGapclose", new CheckBox("Use W2 if target is killable", true));
            SubMenu["Harass"].AddGroupLabel("Harass 1");
            SubMenu["Harass"].Add("Q", new CheckBox("Use Q on Harass 1", true));
            SubMenu["Harass"].Add("W", new CheckBox("Use W on Harass 1", false));
            SubMenu["Harass"].Add("E", new CheckBox("Use E on Harass 1", true));
            SubMenu["Harass"].Add("Mana", new Slider("Min. Energy Percent:", 20, 0, 100));
            SubMenu["Harass"].AddGroupLabel("Harass 2");
            SubMenu["Harass"].Add("Q2", new CheckBox("Use Q on Harass 2", true));
            SubMenu["Harass"].Add("W2", new CheckBox("Use W on Harass 2", true));
            SubMenu["Harass"].Add("E2", new CheckBox("Use E on Harass 2", true));
            SubMenu["Harass"].Add("Mana2", new Slider("Min. Energy Percent:", 20, 0, 100));

            SubMenu["LaneClear"] = menu.AddSubMenu("LaneClear", "LaneClear");
            SubMenu["LaneClear"].Add("E", new Slider("Use E if Hit >= ", 3, 0, 10));
            SubMenu["LaneClear"].AddGroupLabel("Unkillable minions");
            SubMenu["LaneClear"].Add("Q2", new CheckBox("Use Q", true));
            SubMenu["LaneClear"].Add("Mana", new Slider("Min. Energy Percent:", 50, 0, 100));

            SubMenu["LastHit"] = menu.AddSubMenu("LastHit", "LastHit");
            SubMenu["LastHit"].AddGroupLabel("Unkillable minions");
            SubMenu["LastHit"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["LastHit"].Add("Mana", new Slider("Min. Energy Percent:", 50, 0, 100));

            SubMenu["JungleClear"] = menu.AddSubMenu("JungleClear", "JungleClear");
            SubMenu["JungleClear"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["JungleClear"].Add("W", new CheckBox("Use W", true));
            SubMenu["JungleClear"].Add("E", new CheckBox("Use E", true));
            SubMenu["JungleClear"].Add("Mana", new Slider("Min. Energy Percent:", 20, 0, 100));

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
            SubMenu["Draw"].Add("IsDead", new CheckBox("Draw Text if target will die", true));

            SubMenu["Misc"] = menu.AddSubMenu("Misc", "Misc");
            SubMenu["Misc"].Add("Overkill", new Slider("Overkill % for damage prediction", 10, 0, 100));
            SubMenu["Misc"].Add("AutoE", new CheckBox("Use Auto E", false));
            SubMenu["Misc"].Add("SwapDead", new CheckBox("Use Auto W2/R2 if target will die", false));
            SubMenu["Misc"].AddSeparator();
            SubMenu["Misc"].Add("EvadeR1", new CheckBox("Use R1 to Evade", true));
            foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies)
            {
                SubMenu["Misc"].AddGroupLabel(enemy.ChampionName);
                SubMenu["Misc"].Add(enemy.ChampionName + "Q", new CheckBox("Q", false));
                SubMenu["Misc"].Add(enemy.ChampionName + "W", new CheckBox("W", false));
                SubMenu["Misc"].Add(enemy.ChampionName + "E", new CheckBox("E", false));
                SubMenu["Misc"].Add(enemy.ChampionName + "R", new CheckBox("R", false));
            }
            /*
            if (Orbwalker.Menu["Combo"].Cast<KeyBind>().Keys.Item2 == KeyBind.UnboundKey)
            {
                //Orbwalker.Menu["Combo"].Cast<KeyBind>().Keys = new Tuple<uint, uint>(Orbwalker.Menu["Combo"].Cast<KeyBind>().Keys.Item1, (uint)'A');
            }
            if (Orbwalker.Menu["Harass"].Cast<KeyBind>().Keys.Item2 == KeyBind.UnboundKey)
            {
                Orbwalker.Menu["Harass"].Cast<KeyBind>().Keys = new Tuple<uint, uint>(Orbwalker.Menu["Harass"].Cast<KeyBind>().Keys.Item2, (uint)'S');
            }
            */
            Game.OnTick += OnTick;
            GameObject.OnCreate += OnCreateObj;
            GameObject.OnDelete += OnDeleteObj;
            Game.OnWndProc += OnWndProc;
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;
            Obj_AI_Base.OnPlayAnimation += Obj_AI_Base_OnPlayAnimation;
        }

        private static void Obj_AI_Base_OnPlayAnimation(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args)
        {
            if (args.Animation.ToLower().Contains("death"))
            {
                if (Shadows.Count > 0)
                {
                    foreach (Obj_AI_Minion o in Shadows)
                    {
                        if (o.NetworkId == sender.NetworkId)
                        {
                            Shadows.Remove(o);
                        }
                    }
                }
            }
        }

        private static void OnWndProc(WndEventArgs args)
        {
            /*
            switch (args.Msg)
            {
                case (uint)WindowMessages.KeyDown:
                    if (Orbwalker.["Combo"].Cast<KeyBind>().Keys.Item1 == args.WParam)
                    {
                        Combo1Pressed = true;
                    }
                    if (Orbwalker.Menu["Combo"].Cast<KeyBind>().Keys.Item2 == args.WParam)
                    {
                        Combo2Pressed = true;
                    }
                    if (Orbwalker.Menu["Harass"].Cast<KeyBind>().Keys.Item1 == args.WParam)
                    {
                        Harass1Pressed = true;
                    }
                    if (Orbwalker.Menu["Harass"].Cast<KeyBind>().Keys.Item2 == args.WParam)
                    {
                        Harass2Pressed = true;
                    }
                    break;
                case (uint)WindowMessages.KeyUp:
                    if (Orbwalker.Menu["Combo"].Cast<KeyBind>().Keys.Item1 == args.WParam)
                    {
                        Combo1Pressed = false;
                    }
                    if (Orbwalker.Menu["Combo"].Cast<KeyBind>().Keys.Item2 == args.WParam)
                    {
                        Combo2Pressed = false;
                    }
                    if (Orbwalker.Menu["Harass"].Cast<KeyBind>().Keys.Item1 == args.WParam)
                    {
                        Harass1Pressed = false;
                    }
                    if (Orbwalker.Menu["Harass"].Cast<KeyBind>().Keys.Item2 == args.WParam)
                    {
                        Harass2Pressed = false;
                    }
                    break;
            }
            */
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
            if (IsHarass && SubMenu["Harass"]["Collision"].Cast<CheckBox>().CurrentValue)
            {
                Q.AllowedCollisionCount = 0;
                W.AllowedCollisionCount = 0;
            }
            else
            {
                Q.AllowedCollisionCount = int.MaxValue;
                W.AllowedCollisionCount = int.MaxValue;
            }
            if (IsCombo)
            {
                Combo();
            }
            else if (IsHarass)
            {
                Harass();
                /*
                if (HarassType == 1)
                {
                    Harass();
                }
                else if (HarassType == 2)
                {
                    Harass2();
                }*/
            }
            else if (IsClear)
            {
                if (IsJungleClear)
                {
                    JungleClear();
                }
                if (IsLaneClear)
                {
                    LaneClear();
                }
            }
            else if (IsLastHit)
            {
                LastHit();
            }

            if (IsFlee)
            {
                Flee();
            }
            if (SubMenu["Misc"]["AutoE"].Cast<CheckBox>().CurrentValue)
            {
                foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies)
                {
                    if (enemy.IsValidTarget(TS_Range))
                    {
                        CastE(enemy);
                    }
                }
            }
        }
        static void KillSteal()
        {
            foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies)
            {
                if (enemy.IsValidTarget(TS_Range) && enemy.HealthPercent <= 40f && !IsDead(enemy))
                {
                    var damageI = GetBestCombo(enemy);
                    if (damageI.Damage >= enemy.Health)
                    {
                        if (SubMenu["KillSteal"]["Q"].Cast<CheckBox>().CurrentValue && (Damage(enemy, Q.Slot) >= enemy.Health || damageI.Q)) { CastQ(enemy); }
                        if (SubMenu["KillSteal"]["W"].Cast<CheckBox>().CurrentValue && enemy.HealthPercent < 25f && (Damage(enemy, W.Slot) >= enemy.Health || damageI.W)) { CastW(enemy); }
                        if (SubMenu["KillSteal"]["E"].Cast<CheckBox>().CurrentValue && (Damage(enemy, E.Slot) >= enemy.Health || damageI.E)) { CastE(enemy); }
                    }
                    if (Ignite != null && SubMenu["KillSteal"]["Ignite"].Cast<CheckBox>().CurrentValue)
                    {
                        if (Ignite.IsReady() && myHero.GetSummonerSpellDamage(enemy, DamageLibrary.SummonerSpells.Ignite) >= enemy.Health)
                        {
                            Ignite.Cast(enemy);
                        }
                    }
                }
            }
        }
        static void Combo()
        {
            var target = TS_Target;
            if (target.IsValidTarget())
            {
                if (SubMenu["Combo"]["R"].Cast<CheckBox>().CurrentValue && !SubMenu["Combo"][target.ChampionName].Cast<CheckBox>().CurrentValue) { CastR(target); }
                if (SubMenu["Combo"]["Prevent"].Cast<KeyBind>().CurrentValue && R.IsReady() && IsR1 && !SubMenu["Combo"][target.ChampionName].Cast<CheckBox>().CurrentValue)
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
                if (IsDeadObject != null && SubMenu["Misc"]["SwapDead"].Cast<KeyBind>().CurrentValue)
                {
                    var HeroCount = myHero.CountEnemiesInRange(400);
                    var wCount = (wShadow != null && W.IsReady()) ? wShadow.CountEnemiesInRange(400) : 1000;
                    var rCount = (rShadow != null && R.IsReady()) ? rShadow.CountEnemiesInRange(400) : 1000;
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
                if (IsCombo)
                {
                    if (SubMenu["Combo"]["SwapGapclose"].Cast<CheckBox>().CurrentValue && Extensions.Distance(myHero, target) > E.Range * 1.3f)
                    {
                        var heroDistance = Extensions.Distance(myHero, target);
                        var wShadowDistance = (wShadow != null && W.IsReady()) ? Extensions.Distance(target, wShadow) : 999999f;
                        var rShadowDistance = (rShadow != null && R.IsReady()) ? Extensions.Distance(target, rShadow) : 999999f;
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
                    if (SubMenu["Combo"]["SwapHP"].Cast<Slider>().CurrentValue >= myHero.HealthPercent)
                    {
                        if (damageI.Damage <= target.Health || myHero.HealthPercent < target.HealthPercent)
                        {
                            var HeroCount = myHero.CountEnemiesInRange(400);
                            var wCount = (wShadow != null && W.IsReady()) ? wShadow.CountEnemiesInRange(400) : 1000;
                            var rCount = (rShadow != null && R.IsReady()) ? rShadow.CountEnemiesInRange(400) : 1000;
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
                    if (IsDeadObject != null && SubMenu["Combo"]["SwapDead"].Cast<KeyBind>().CurrentValue)
                    {
                        var HeroCount = myHero.CountEnemiesInRange(400);
                        var wCount = (wShadow != null && W.IsReady()) ? wShadow.CountEnemiesInRange(400) : 1000;
                        var rCount = (rShadow != null && R.IsReady()) ? rShadow.CountEnemiesInRange(400) : 1000;
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
                else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
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
                foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies)
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
            var m = SubMenu["LaneClear"];
            if (myHero.ManaPercent >= GetSlider(m, "Mana"))
            {
                if (GetCheckBox(m, "Q2"))
                {
                    LastHitSpell(Q);
                }
                if (GetSlider(m, "E") > 0 && E.IsReady())
                {
                    if (GetSlider(m, "E") <= EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myHero.Position, E.Range).Count())
                    {
                        myHero.Spellbook.CastSpell(E.Slot);
                    }
                }
            }
        }
        static void LastHit()
        {
            var m = SubMenu["LastHit"];
            if (myHero.ManaPercent >= GetSlider(m, "Mana"))
            {
                if (GetCheckBox(m, "Q"))
                {
                    LastHitSpell(Q);
                }
            }
        }

        static void JungleClear()
        {
            if (myHero.ManaPercent >= SubMenu["JungleClear"]["Mana"].Cast<Slider>().CurrentValue)
            {
                var jungleminions = EntityManager.MinionsAndMonsters.GetJungleMonsters(myHero.Position, 1000f);
                if (jungleminions.Count() > 0)
                {
                    foreach (Obj_AI_Base minion in jungleminions)
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
        }
        static void CastQ(Obj_AI_Base target)
        {
            if (Q.IsReady() && target.IsValidTarget() && !IsWaitingShadow)
            {
                var heroDistance = Extensions.Distance(myHero, target);
                var wShadowDistance = wShadow != null ? Extensions.Distance(myHero, wShadow) : 999999f;
                var rShadowDistance = rShadow != null ? Extensions.Distance(myHero, rShadow) : 999999f;
                var min = Math.Min(Math.Min(rShadowDistance, wShadowDistance), heroDistance);
                if (min == heroDistance)
                {
                    Q.SourcePosition = myHero.Position;
                }
                else if (min == rShadowDistance)
                {
                    Q.SourcePosition = rShadow.Position;
                }
                else
                {
                    Q.SourcePosition = wShadow.Position;
                }
                Q.RangeCheckSource = Q.SourcePosition;
                var pred = Q.GetPrediction(target);
                var hitchance = HitChancePercent(Q.Slot);
                if (pred.HitChancePercent >= hitchance)
                {
                    Q.Cast(pred.CastPosition);
                }
            }
        }

        static void CastW(Obj_AI_Base target)
        {
            if (W.IsReady() && IsW1 && target.IsValidTarget())
            {
                _W.LastCastTime = Game.Time;
                var r = W.GetPrediction(target);
                if (r.HitChancePercent >= 50 && Game.Time - _W.LastSentTime > 0.25f)
                {
                    _W.LastSentTime = Game.Time;
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

        static void CastR(AIHeroClient target)
        {
            if (R.IsReady() && target.IsValidTarget() && IsR1)
            {
                R.Cast(target);
            }
        }

        private static void LastHitSpell(Spell.Skillshot s)
        {
            if (s.IsReady())
            {
                var enemyminions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myHero.Position, s.Range + s.Width, true).Where(o => o.Health <= 2.0f * Damage(o, s.Slot));
                if (enemyminions.Count() > 0)
                {
                    foreach (Obj_AI_Base minion in enemyminions)
                    {
                        bool CanCalculate = false;
                        if (minion.IsValidTarget())
                        {
                            if (!Orbwalker.CanAutoAttack)
                            {
                                if (Orbwalker.CanMove && Orbwalker.LastTarget != null && Orbwalker.LastTarget.NetworkId != minion.NetworkId)
                                {
                                    CanCalculate = true;
                                }
                            }
                            else
                            {
                                if (myHero.GetAutoAttackRange(minion) <= Extensions.Distance(myHero, minion))
                                {
                                    CanCalculate = true;
                                }
                                else
                                {
                                    var speed = myHero.BasicAttack.MissileSpeed;
                                    var time = (int)(1000 * Extensions.Distance(myHero, minion) / speed + myHero.AttackCastDelay * 1000 + Game.Ping - 100);
                                    var predHealth = Prediction.Health.GetPrediction(minion, time);
                                    if (predHealth <= 0)
                                    {
                                        CanCalculate = true;
                                    }
                                    /**
                                    if (!Orbwalker.CanBeLastHitted(minion))
                                    {
                                        CanCalculate = true;
                                    }**/
                                }
                            }
                        }
                        if (CanCalculate)
                        {
                            var dmg = Damage(minion, s.Slot);
                            var time = (int)(1000 * Extensions.Distance(s.SourcePosition.Value, minion) / s.Speed + s.CastDelay - 70);
                            var predHealth = Prediction.Health.GetPrediction(minion, time);
                            if (time > 0 && predHealth == minion.Health) { return; }
                            if (dmg > predHealth && predHealth > 0)
                            {
                                CastQ(minion);
                            }
                        }
                    }
                }
            }
        }
        static void OnCreateObj(GameObject sender, EventArgs args)
        {
            if (sender is Obj_AI_Minion)
            {
                if (sender.Name.ToLower().Equals("shadow") && sender.Team == myHero.Team)
                {
                    var s = sender as Obj_AI_Minion;
                    Shadows.Add(s);
                }
            }
            if (sender is Obj_GeneralParticleEmitter && sender.Name.ToLower().Contains(myHero.ChampionName.ToLower()))
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
            if (sender is Obj_AI_Minion)
            {
                if (sender.Name.ToLower().Equals("shadow") && sender.Team == myHero.Team)
                {
                    var s2 = Shadows.Where(m => m.NetworkId == sender.NetworkId).FirstOrDefault();
                    if (s2 != null)
                    {
                        Shadows.Remove(s2);
                    }
                }
            }
            if (sender is Obj_GeneralParticleEmitter && sender.Name.ToLower().Contains(myHero.ChampionName.ToLower()) && sender.Name.ToLower().Contains("base_r") && sender.Name.ToLower().Contains("buf_tell"))
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
            if (IsDeadObject != null && SubMenu["Draw"]["IsDead"].Cast<CheckBox>().CurrentValue)
            {
                Drawing.DrawText(Drawing.WorldToScreen(IsDeadObject.Position), System.Drawing.Color.Red, "TARGET DEAD", 200);
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
            if (sender.Type == myHero.Type && sender.Team != myHero.Team && SubMenu["Misc"]["EvadeR1"].Cast<CheckBox>().CurrentValue)
            {
                if (Extensions.Distance(myHero, sender) < 1000)
                {
                    AIHeroClient unit = (AIHeroClient)sender;
                    if (SubMenu["Misc"]["EvadeR1"].Cast<CheckBox>().CurrentValue)
                    {
                        if (SpellIsActive(unit, args.SData.Name))
                        {
                            var target = EntityManager.Heroes.Enemies.Where(o => o.IsValidTarget(R.Range)).OrderByDescending(o => TargetSelector.GetPriority(o)).First();
                            if (target.IsValidTarget())
                            {
                                CastR(target);
                            }
                        }
                    }

                }
            }
        }
        static bool SpellIsActive(AIHeroClient unit, string name)
        {
            string slot = "Q";
            if (name.Equals(unit.Spellbook.GetSpell(SpellSlot.Q).SData.Name))
            {
                slot = "Q";
            }
            else if (name.Equals(unit.Spellbook.GetSpell(SpellSlot.W).SData.Name))
            {
                slot = "W";
            }
            else if (name.Equals(unit.Spellbook.GetSpell(SpellSlot.E).SData.Name))
            {
                slot = "E";
            }
            else if (name.Equals(unit.Spellbook.GetSpell(SpellSlot.R).SData.Name))
            {
                slot = "R";
            }
            return SubMenu["Misc"][unit.ChampionName + slot].Cast<CheckBox>().CurrentValue;
        }
        static void ResetR()
        {
            _R.End = Vector3.Zero;
            rFound = null;
            IsDeadObject = null;
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
                if (Extensions.Distance(myHero, target) < 550 && (myHero.Mana < myHero.Spellbook.GetSpell(SpellSlot.W).SData.Mana + myHero.Spellbook.GetSpell(SpellSlot.Q).SData.Mana || myHero.Mana < myHero.Spellbook.GetSpell(SpellSlot.W).SData.Mana + myHero.Spellbook.GetSpell(SpellSlot.E).SData.Mana))
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
                if (PassiveUsed.ContainsKey(target.NetworkId))
                {
                    if (PassiveUsed[target.NetworkId])
                    {
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
        static float HitChancePercent(SpellSlot s)
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
            if (IsHarass)
            {
                return SubMenu["Prediction"][slot + "Harass"].Cast<Slider>().CurrentValue;
            }
            return SubMenu["Prediction"][slot + "Combo"].Cast<Slider>().CurrentValue;
        }
        static int GetSlider(Menu m, string s)
        {
            return m[s].Cast<Slider>().CurrentValue;
        }
        static bool GetCheckBox(Menu m, string s)
        {
            return m[s].Cast<CheckBox>().CurrentValue;
        }
        static bool GetKeyBind(Menu m, string s)
        {
            return m[s].Cast<KeyBind>().CurrentValue;
        }
        static float Overkill
        {
            get
            {
                return (float)((100 + GetSlider(SubMenu["Misc"], "Overkill")) / 100);
            }
        }
        static bool IsCombo
        {
            get
            {
                return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo);
            }
        }
        static bool IsHarass
        {
            get
            {
                return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass);
            }
        }
        static bool IsClear
        {
            get
            {
                return IsLaneClear || IsJungleClear;
            }
        }
        static bool IsLaneClear
        {
            get
            {
                return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear);
            }
        }
        static bool IsJungleClear
        {
            get
            {
                return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear);
            }
        }
        static bool IsLastHit
        {
            get
            {
                return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit);
            }
        }
        static bool IsFlee
        {
            get
            {
                return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee);
            }
        }
        static bool IsNone
        {
            get
            {
                return !IsFlee && !IsLastHit && !IsClear && !IsHarass && !IsCombo;
            }
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
    public class _Spell
    {
        public float LastCastTime = 0;
        public float LastSentTime = 0;
        public Vector3 End = Vector3.Zero;
    }
}
