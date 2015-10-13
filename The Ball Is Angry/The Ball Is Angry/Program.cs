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
using EloBuddy.SDK.Constants;
using SharpDX;

namespace The_Ball_Is_Angry
{
    class Program
    {
        static string Author = "iCreative";
        static string AddonName = "The ball is angry";
        static float RefreshTime = 0.4f;
        static Dictionary<int, DamageInfo> PredictedDamage = new Dictionary<int, DamageInfo>() { };
        static AIHeroClient myHero { get { return ObjectManager.Player; } }
        static Vector3 mousePos { get { return Game.CursorPos; } }
        static Menu menu;
        static Dictionary<string, Menu> SubMenu = new Dictionary<string, Menu>() { };
        static Spell.Skillshot Q, W, E, R;
        static Spell.Targeted Ignite;
        static List<MissileClient> missiles = new List<MissileClient>();
        static GameObject E_Target = null;
        static float Q_LastRequest = 0f;
        static float E_LastRequest = 0f;
        static GameObject BallObject;
        static float LastGapclose = 0f;
        static Vector3 Ball
        {
            get
            {
                if (BallObject != null && BallObject.IsValid)
                {
                    return BallObject.Position;
                }
                return myHero.Position;
            }
        }
        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += OnLoad;
        }
        private static void OnLoad(EventArgs args)
        {
            if (myHero.Hero != Champion.Orianna) { return; }
            Chat.Print(AddonName + " made by " + Author + " loaded, have fun!.");
            Q = new Spell.Skillshot(SpellSlot.Q, 815, SkillShotType.Circular, 0, 1200, 130);
            Q.AllowedCollisionCount = int.MaxValue;
            W = new Spell.Skillshot(SpellSlot.W, 255, SkillShotType.Linear, 250, int.MaxValue, 50);
            W.AllowedCollisionCount = int.MaxValue;
            E = new Spell.Skillshot(SpellSlot.E, 1095, SkillShotType.Circular, 0, 1800, 85);
            E.AllowedCollisionCount = int.MaxValue;
            R = new Spell.Skillshot(SpellSlot.R, 410, SkillShotType.Linear, 500, int.MaxValue, 50);
            R.AllowedCollisionCount = int.MaxValue;
            var slot = myHero.GetSpellSlotFromName("summonerdot");
            if (slot != SpellSlot.Unknown)
            {
                Ignite = new Spell.Targeted(slot, 600);
            }
            menu = MainMenu.AddMenu(AddonName, AddonName + " by " + Author + " v1.40");
            menu.AddLabel(AddonName + " made by " + Author);

            SubMenu["Prediction"] = menu.AddSubMenu("Prediction", "Prediction");
            SubMenu["Prediction"].AddGroupLabel("Q Settings");
            SubMenu["Prediction"].Add("QCombo", new Slider("Combo HitChancePercent", 60, 0, 100));
            SubMenu["Prediction"].Add("QHarass", new Slider("Harass HitChancePercent", 70, 0, 100));
            SubMenu["Prediction"].AddGroupLabel("W Settings");
            SubMenu["Prediction"].Add("WCombo", new Slider("Combo HitChancePercent", 60, 0, 100));
            SubMenu["Prediction"].Add("WHarass", new Slider("Harass HitChancePercent", 70, 0, 100));
            SubMenu["Prediction"].AddGroupLabel("E Settings");
            SubMenu["Prediction"].Add("ECombo", new Slider("Combo HitChancePercent", 45, 0, 100));
            SubMenu["Prediction"].Add("EHarass", new Slider("Harass HitChancePercent", 60, 0, 100));
            SubMenu["Prediction"].AddGroupLabel("R Settings");
            SubMenu["Prediction"].Add("RCombo", new Slider("Combo HitChancePercent", 60, 0, 100));
            SubMenu["Prediction"].Add("RHarass", new Slider("Harass HitChancePercent", 70, 0, 100));

            SubMenu["Combo"] = menu.AddSubMenu("Combo", "Combo");
            SubMenu["Combo"].Add("TF", new Slider("Use TeamFight Logic if enemies near >=", 3, 1, 5));
            SubMenu["Combo"].AddGroupLabel("Common Logic");
            SubMenu["Combo"].Add("Q", new CheckBox("Use Q On Target", true));
            SubMenu["Combo"].Add("W", new CheckBox("Use W On Target", true));
            SubMenu["Combo"].Add("Shield", new CheckBox("Use Shield On Enemy Missiles", true));
            SubMenu["Combo"].Add("E", new Slider("Use E If Hit", 1, 1, 5));
            SubMenu["Combo"].Add("E2", new Slider("Use E If HealthPercent <=", 50, 0, 100));
            SubMenu["Combo"].AddGroupLabel("1 vs 1 Logic");
            SubMenu["Combo"].Add("R", new CheckBox("Use R On Target If Killable", true));
            SubMenu["Combo"].AddGroupLabel("TeamFight Logic");
            SubMenu["Combo"].Add("Q2", new Slider("Use Q If Hit", 2, 1, 5));
            SubMenu["Combo"].Add("W2", new Slider("Use W If Hit", 2, 1, 5));
            SubMenu["Combo"].Add("R2", new Slider("Use R if Hit", 3, 1, 5));

            SubMenu["Harass"] = menu.AddSubMenu("Harass", "Harass");
            SubMenu["Harass"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["Harass"].Add("W", new CheckBox("Use W", true));
            SubMenu["Harass"].Add("Shield", new CheckBox("Use Shield On Enemy Missiles", true));
            SubMenu["Harass"].Add("E", new Slider("Use E If Hit", 1, 1, 5));
            SubMenu["Harass"].Add("E2", new Slider("Use E If HealthPercent <=", 40, 0, 100));
            SubMenu["Harass"].Add("Mana", new Slider("Min. Mana Percent:", 20, 0, 100));

            SubMenu["LaneClear"] = menu.AddSubMenu("LaneClear", "LaneClear");
            SubMenu["LaneClear"].AddGroupLabel("LaneClear Minions");
            SubMenu["LaneClear"].Add("Q", new Slider("Use Q If Hit", 4, 0, 10));
            SubMenu["LaneClear"].Add("W", new Slider("Use W If Hit", 3, 0, 10));
            SubMenu["LaneClear"].Add("E", new Slider("Use E If Hit", 6, 0, 10));
            SubMenu["LaneClear"].AddGroupLabel("Unkillable minions");
            SubMenu["LaneClear"].Add("Q2", new CheckBox("Use Q", true));
            SubMenu["LaneClear"].Add("Mana", new Slider("Min. Mana Percent:", 50, 0, 100));

            SubMenu["LastHit"] = menu.AddSubMenu("LastHit", "LastHit");
            SubMenu["LastHit"].AddGroupLabel("Unkillable minions");
            SubMenu["LastHit"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["LastHit"].Add("Mana", new Slider("Min. Mana Percent:", 50, 0, 100));

            SubMenu["JungleClear"] = menu.AddSubMenu("JungleClear", "JungleClear");
            SubMenu["JungleClear"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["JungleClear"].Add("W", new CheckBox("Use W", true));
            SubMenu["JungleClear"].Add("E", new CheckBox("Use E", true));
            SubMenu["JungleClear"].Add("Mana", new Slider("Min. Mana Percent:", 20, 0, 100));

            SubMenu["KillSteal"] = menu.AddSubMenu("KillSteal", "KillSteal");
            SubMenu["KillSteal"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["KillSteal"].Add("W", new CheckBox("Use W", true));
            SubMenu["KillSteal"].Add("E", new CheckBox("Use E", true));
            SubMenu["KillSteal"].Add("R", new CheckBox("Use R", false));
            SubMenu["KillSteal"].Add("Ignite", new CheckBox("Use Ignite", true));

            SubMenu["Flee"] = menu.AddSubMenu("Flee", "Flee");
            SubMenu["Flee"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["Flee"].Add("W", new CheckBox("Use W", true));
            SubMenu["Flee"].Add("E", new CheckBox("Use E", true));

            SubMenu["Draw"] = menu.AddSubMenu("Drawing", "Drawing");
            SubMenu["Draw"].Add("Ball", new CheckBox("Draw ball position", true));
            SubMenu["Draw"].Add("Q", new CheckBox("Draw Q Range", true));
            SubMenu["Draw"].Add("W", new CheckBox("Draw W Range", true));
            SubMenu["Draw"].Add("R", new CheckBox("Draw R Range", true));

            SubMenu["Misc"] = menu.AddSubMenu("Misc", "Misc");
            SubMenu["Misc"].Add("Overkill", new Slider("Overkill % for damage prediction", 10, 0, 100));
            SubMenu["Misc"].Add("BlockR", new CheckBox("Block R if will not hit", true));
            SubMenu["Misc"].Add("R", new CheckBox("Use R to Interrupt Channeling", true));
            SubMenu["Misc"].Add("E", new CheckBox("Use E to Initiate", true));
            SubMenu["Misc"].Add("Shield", new CheckBox("Use Shield On Enemy Missiles", false));
            SubMenu["Misc"].Add("W2", new Slider("Use W if Hit", 3, 1, 5));
            SubMenu["Misc"].Add("R2", new Slider("Use R if Hit", 4, 1, 5));

            Game.OnTick += OnTick;
            GameObject.OnCreate += OnCreate;
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;
            Obj_AI_Base.OnPlayAnimation += OnPlayAnimation;
            Interrupter.OnInterruptableSpell += OnInterruptableSpell;
            Gapcloser.OnGapcloser += OnGapcloser;
            Spellbook.OnCastSpell += OnCastSpell;
            Obj_AI_Base.OnBasicAttack += Obj_AI_Base_OnBasicAttack;
            BallObject = ObjectManager.Get<GameObject>().FirstOrDefault(obj => obj.Name != null && !obj.IsDead && obj.IsValid && obj.Name.ToLower().Contains("doomball"));
            MissileClient.OnCreate += MissileClient_OnCreate;
            MissileClient.OnDelete += MissileClient_OnDelete;
        }



        private static void Obj_AI_Base_OnBasicAttack(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.Team != myHero.Team && args.Target.IsMe && (GetCheckBox(SubMenu["Misc"], "Shield") || (IsCombo && GetCheckBox(SubMenu["Combo"], "Shield")) || (IsHarass && GetCheckBox(SubMenu["Harass"], "Shield"))))
            {
                if (sender is AIHeroClient)
                {
                    var hero = sender as AIHeroClient;
                    if (hero.IsMelee)
                    {
                        CastE(myHero);
                    }
                }
                else if (sender is Obj_AI_Turret)
                {
                    CastE(myHero);
                }
            }
        }

        private static void OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (sender.Owner.IsMe)
            {
                if (args.Slot == R.Slot && SubMenu["Misc"]["BlockR"].Cast<CheckBox>().CurrentValue && HitR() == 0)
                {
                    args.Process = false;
                }
                if (IsHarass && !Orbwalker.CanMove && Orbwalker.LastTarget.Type != myHero.Type)
                {
                    args.Process = false;
                }
            }
        }

        private static void OnTick(EventArgs args)
        {
            if (myHero.IsDead) { return; }
            Q.SourcePosition = Ball;
            Q.RangeCheckSource = myHero.Position;
            E.SourcePosition = Ball;
            E.RangeCheckSource = myHero.Position;
            W.SourcePosition = Ball;
            W.RangeCheckSource = Ball;
            R.SourcePosition = Ball;
            R.RangeCheckSource = Ball;
            if (GetCheckBox(SubMenu["Misc"], "Shield"))
            {
                CheckMissiles();
            }
            if (R.IsReady() && SubMenu["Misc"]["R2"].Cast<Slider>().CurrentValue <= HitR())
            {
                myHero.Spellbook.CastSpell(R.Slot);
            }
            if (W.IsReady() && SubMenu["Misc"]["W2"].Cast<Slider>().CurrentValue <= HitW(EntityManager.Heroes.Enemies.ToList<Obj_AI_Base>()))
            {
                myHero.Spellbook.CastSpell(W.Slot);
            }
            KillSteal();
            if (IsCombo)
            {
                Combo();
            }
            else if (IsHarass)
            {
                Harass();
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
        }

        private static void KillSteal()
        {
            foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies)
            {
                if (enemy.IsValidTarget(E.Range) && enemy.HealthPercent <= 40)
                {
                    var damageI = GetBestCombo(enemy);
                    if (damageI.Damage >= enemy.Health)
                    {
                        if (SubMenu["KillSteal"]["Q"].Cast<CheckBox>().CurrentValue && (Damage(enemy, Q.Slot) >= enemy.Health || damageI.Q)) { CastQ(enemy); }
                        if (SubMenu["KillSteal"]["W"].Cast<CheckBox>().CurrentValue && (Damage(enemy, W.Slot) >= enemy.Health || damageI.W)) { CastW(enemy); }
                        if (SubMenu["KillSteal"]["E"].Cast<CheckBox>().CurrentValue && (Damage(enemy, E.Slot) >= enemy.Health || damageI.E)) { CastE(enemy); }
                        if (SubMenu["KillSteal"]["R"].Cast<CheckBox>().CurrentValue && (Damage(enemy, R.Slot) >= enemy.Health || damageI.R)) { CastR(enemy); }
                        if ((SubMenu["KillSteal"]["E"].Cast<CheckBox>().CurrentValue || SubMenu["KillSteal"]["Q"].Cast<CheckBox>().CurrentValue) && ((Damage(enemy, Q.Slot) >= enemy.Health || damageI.Q) || (Damage(enemy, W.Slot) >= enemy.Health || damageI.W) || (Damage(enemy, R.Slot) >= enemy.Health || damageI.R))) { ThrowBall(enemy); }
                    }
                    if (Ignite != null && SubMenu["KillSteal"]["Ignite"].Cast<CheckBox>().CurrentValue && Ignite.IsReady() && myHero.GetSummonerSpellDamage(enemy, DamageLibrary.SummonerSpells.Ignite) >= enemy.Health)
                    {
                        Ignite.Cast(enemy);
                    }
                }
            }
        }

        private static void Combo()
        {
            if (E.IsReady() && GetCheckBox(SubMenu["Combo"], "Shield"))
            {
                CheckMissiles();
            }
            AIHeroClient target = TargetSelector.GetTarget(Q.Range + Q.Width, DamageType.Magical);
            if (target.IsValidTarget())
            {
                var damageI = GetBestCombo(target);
                if (myHero.CountEnemiesInRange(E.Range) >= SubMenu["Combo"]["TF"].Cast<Slider>().CurrentValue)
                {
                    if (Q.IsReady() && SubMenu["Combo"]["Q2"].Cast<Slider>().CurrentValue > 0)
                    {
                        List<Obj_AI_Base> list = EntityManager.Heroes.Enemies.Where<Obj_AI_Base>(o => o.IsValidTarget(Q.Range + Q.Width)).ToList();
                        if (list.Count >= SubMenu["Combo"]["Q2"].Cast<Slider>().CurrentValue)
                        {
                            var info = BestHitQ(list);
                            if (info.Item1 != Vector3.Zero && info.Item2 >= SubMenu["Combo"]["Q2"].Cast<Slider>().CurrentValue)
                            {
                                Q.Cast(info.Item1);
                            }
                        }
                    }
                    if (W.IsReady() && SubMenu["Combo"]["W2"].Cast<Slider>().CurrentValue > 0)
                    {
                        if (HitW(EntityManager.Heroes.Enemies.ToList<Obj_AI_Base>()) >= SubMenu["Combo"]["W2"].Cast<Slider>().CurrentValue)
                        {
                            myHero.Spellbook.CastSpell(W.Slot);
                        }

                    }
                    if (R.IsReady() && SubMenu["Combo"]["R2"].Cast<Slider>().CurrentValue > 0 && HitR() >= SubMenu["Combo"]["R2"].Cast<Slider>().CurrentValue)
                    {
                        myHero.Spellbook.CastSpell(R.Slot);
                    }
                    CastQR();
                    CastER();
                }
                else
                {
                    if (SubMenu["Combo"]["R"].Cast<CheckBox>().CurrentValue && damageI.R && damageI.Damage >= target.Health) { CastR(target); }
                }
                if (SubMenu["Combo"]["Q"].Cast<CheckBox>().CurrentValue)
                {
                    if (Game.Time - LastGapclose < 0.2f) { return; }
                    CastQ(target);
                }
                if (W.IsReady() && SubMenu["Combo"]["W"].Cast<CheckBox>().CurrentValue) { CastW(target); }
                if (E.IsReady() && SubMenu["Combo"]["E"].Cast<Slider>().CurrentValue > 0)
                {
                    List<Obj_AI_Base> list = EntityManager.Heroes.Enemies.Where<Obj_AI_Base>(o => o.IsValidTarget(E.Range)).ToList();
                    if (list.Count >= SubMenu["Combo"]["E"].Cast<Slider>().CurrentValue)
                    {
                        var info = BestHitE(list);
                        if (info.Item1 != null && info.Item2 > 0)
                        {
                            Obj_AI_Base bestAlly = info.Item1;
                            if (info.Item2 > SubMenu["Combo"]["E"].Cast<Slider>().CurrentValue && bestAlly.IsValid)
                            {
                                CastE(bestAlly);
                            }
                        }
                    }
                }
                if (E.IsReady() && SubMenu["Combo"]["E2"].Cast<Slider>().CurrentValue > myHero.HealthPercent && myHero.HealthPercent < target.HealthPercent)
                {
                    foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies.Where(o => o.IsValidTarget(E.Range)))
                    {
                        if (enemy.GetAutoAttackRange(myHero) < Extensions.Distance(myHero, enemy))
                        {
                            CastE(myHero);
                        }
                    }
                }
            }
        }

        private static void Harass()
        {
            AIHeroClient target = TargetSelector.GetTarget(Q.Range + Q.Width, DamageType.Magical);
            if (myHero.ManaPercent >= SubMenu["Harass"]["Mana"].Cast<Slider>().CurrentValue)
            {
                if (target.IsValidTarget())
                {
                    var damageI = GetBestCombo(target);
                    if (SubMenu["Harass"]["Q"].Cast<CheckBox>().CurrentValue) { CastQ(target); }
                    if (W.IsReady() && SubMenu["Harass"]["W"].Cast<CheckBox>().CurrentValue) { CastW(target); }
                    if (E.IsReady() && SubMenu["Harass"]["E"].Cast<Slider>().CurrentValue > 0)
                    {
                        List<Obj_AI_Base> list = EntityManager.Heroes.Enemies.Where<Obj_AI_Base>(o => o.IsValidTarget(E.Range)).ToList();
                        if (list.Count >= SubMenu["Harass"]["E"].Cast<Slider>().CurrentValue)
                        {
                            var info = BestHitE(list);
                            if (info.Item1 != null && info.Item2 > 0)
                            {
                                Obj_AI_Base bestAlly = info.Item1;
                                if (info.Item2 > SubMenu["Harass"]["E"].Cast<Slider>().CurrentValue && bestAlly.IsValid)
                                {
                                    CastE(bestAlly);
                                }
                            }
                        }
                    }
                    if (E.IsReady() && SubMenu["Harass"]["E2"].Cast<Slider>().CurrentValue > myHero.HealthPercent && myHero.HealthPercent < target.HealthPercent)
                    {
                        foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies.Where(o => o.IsValidTarget(E.Range)))
                        {
                            if (enemy.GetAutoAttackRange(myHero) < Extensions.Distance(myHero, enemy))
                            {
                                CastE(myHero);
                            }
                        }
                    }
                }
                if (E.IsReady() && GetCheckBox(SubMenu["Harass"], "Shield"))
                {
                    CheckMissiles();
                }
            }
        }
        private static void LaneClear()
        {
            if (myHero.ManaPercent >= SubMenu["LaneClear"]["Mana"].Cast<Slider>().CurrentValue)
            {
                if (E.IsReady() && SubMenu["LaneClear"]["E"].Cast<Slider>().CurrentValue > 0)
                {
                    var minions = EntityManager.MinionsAndMonsters.Get(EntityManager.MinionsAndMonsters.EntityType.Minion, EntityManager.UnitTeam.Enemy, myHero.Position, E.Range, true).ToList<Obj_AI_Base>();
                    if (minions.Count >= SubMenu["LaneClear"]["E"].Cast<Slider>().CurrentValue)
                    {
                        var info = BestHitE(minions);
                        if (info.Item1 != null)
                        {
                            Obj_AI_Base bestAlly = info.Item1;
                            int bestHit = info.Item2;
                            if (SubMenu["LaneClear"]["E"].Cast<Slider>().CurrentValue > 0 && bestHit >= SubMenu["LaneClear"]["E"].Cast<Slider>().CurrentValue && bestAlly.IsValid)
                            {
                                CastE(bestAlly);
                            }
                        }
                    }
                }
                if (Q.IsReady() && SubMenu["LaneClear"]["Q"].Cast<Slider>().CurrentValue > 0)
                {
                    var minions = EntityManager.MinionsAndMonsters.Get(EntityManager.MinionsAndMonsters.EntityType.Minion, EntityManager.UnitTeam.Enemy, myHero.Position, Q.Range + Q.Width).ToList<Obj_AI_Base>();
                    if (minions.Count >= SubMenu["LaneClear"]["Q"].Cast<Slider>().CurrentValue)
                    {
                        var info2 = BestHitQ(minions);
                        if (info2.Item1 != Vector3.Zero)
                        {
                            Vector3 bestPos = info2.Item1;
                            int bestHit = info2.Item2;
                            if (SubMenu["LaneClear"]["Q"].Cast<Slider>().CurrentValue > 0 && bestHit >= SubMenu["LaneClear"]["Q"].Cast<Slider>().CurrentValue)
                            {
                                Q.Cast(bestPos);
                            }
                        }
                    }
                }
                if (W.IsReady() && SubMenu["LaneClear"]["W"].Cast<Slider>().CurrentValue > 0 && HitW(EntityManager.MinionsAndMonsters.Get(EntityManager.MinionsAndMonsters.EntityType.Minion, EntityManager.UnitTeam.Enemy, Ball, W.Range, true).ToList<Obj_AI_Base>()) >= SubMenu["LaneClear"]["Q"].Cast<Slider>().CurrentValue)
                {
                    myHero.Spellbook.CastSpell(W.Slot);
                }
                if (SubMenu["LaneClear"]["Q2"].Cast<CheckBox>().CurrentValue)
                {
                    LastHitSpell(Q);
                }
            }
        }
        private static void LastHit()
        {
            if (myHero.ManaPercent >= SubMenu["LastHit"]["Mana"].Cast<Slider>().CurrentValue)
            {
                if (SubMenu["LastHit"]["Q"].Cast<CheckBox>().CurrentValue)
                {
                    LastHitSpell(Q);
                }
            }
        }
        private static void LastHitSpell(Spell.Skillshot s)
        {
            if (s.IsReady())
            {
                foreach (Obj_AI_Base minion in EntityManager.MinionsAndMonsters.Get(EntityManager.MinionsAndMonsters.EntityType.Minion, EntityManager.UnitTeam.Enemy, myHero.Position, s.Range + s.Width, true).Where(o => o.Health <= 2.0f * Damage(o, s.Slot)))
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
                            if (myHero.GetAutoAttackRange(minion) < Extensions.Distance(myHero, minion))
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
        private static void JungleClear()
        {
            if (myHero.ManaPercent >= SubMenu["JungleClear"]["Mana"].Cast<Slider>().CurrentValue)
            {
                foreach (Obj_AI_Base minion in EntityManager.MinionsAndMonsters.Get(EntityManager.MinionsAndMonsters.EntityType.Monster, EntityManager.UnitTeam.Enemy, myHero.Position, E.Range, true))
                {
                    if (minion.IsValidTarget() && myHero.ManaPercent >= SubMenu["JungleClear"]["Mana"].Cast<Slider>().CurrentValue)
                    {
                        if (SubMenu["JungleClear"]["E"].Cast<CheckBox>().CurrentValue) { CastE(minion); }
                        if (SubMenu["JungleClear"]["Q"].Cast<CheckBox>().CurrentValue) { CastQ(minion); }
                        if (SubMenu["JungleClear"]["W"].Cast<CheckBox>().CurrentValue) { CastW(minion); }
                    }
                }
            }
        }

        private static void Flee()
        {
            if (SubMenu["Flee"]["Q"].Cast<CheckBox>().CurrentValue)
            {
                if (Q.IsReady() && Extensions.Distance(myHero, Ball, true) > W.RangeSquared && !E.IsReady() && BallObject != null && !BallObject.Name.ToLower().Contains("missile"))
                {
                    myHero.Spellbook.CastSpell(Q.Slot, myHero.ServerPosition);
                }
            }
            if (SubMenu["Flee"]["W"].Cast<CheckBox>().CurrentValue)
            {
                if (W.IsReady() && Extensions.Distance(myHero, Ball, true) < W.RangeSquared)
                {
                    myHero.Spellbook.CastSpell(W.Slot);
                }
            }
            if (SubMenu["Flee"]["E"].Cast<CheckBox>().CurrentValue)
            {
                if (E.IsReady() && Extensions.Distance(myHero, Ball, true) > W.RangeSquared)
                {
                    CastE(myHero);
                }
            }
        }

        private static void CheckMissiles()
        {
            if (E.IsReady())
            {
                foreach (MissileClient m in missiles.Where(a => a.IsValidMissile()))
                {
                    var hero = myHero;
                    var CanCast = false;
                    if (m.Target != null)
                    {
                        CanCast = m.Target.IsMe;
                    }
                    if (m.EndPosition != null && m.SData.LineWidth > 0f)
                    {
                        var multiplier = 1.15f;
                        var width = (m.SData.LineWidth + hero.BoundingRadius) * multiplier;
                        var width_sqrt = width * width;
                        var startpos = m.StartPosition != null ? m.StartPosition : m.SpellCaster.Position;
                        var extendedendpos = m.EndPosition + (m.EndPosition - startpos).Normalized() * width;
                        var info = hero.Position.To2D().ProjectOn(startpos.To2D(), extendedendpos.To2D());
                        CanCast = info.IsOnSegment && Extensions.Distance(info.SegmentPoint, hero.Position.To2D(), true) <= width_sqrt;
                    }
                    if (CanCast)
                    {
                        CastE(hero);
                    }
                }
            }
        }
        private static void CastQ(Obj_AI_Base target, int minhits = 1)
        {
            if (Q.IsReady() && target.IsValidTarget(Q.Range + Q.Width))
            {
                if (E.IsReady() && myHero.Mana >= myHero.Spellbook.GetSpell(Q.Slot).SData.Mana + myHero.Spellbook.GetSpell(E.Slot).SData.Mana && target.Type == myHero.Type && Extensions.Distance(Ball, target, true) > Math.Pow(Q.Range * 1.2f, 2) && Extensions.Distance(myHero, target, true) < Extensions.Distance(Ball, target, true))
                {
                    var pred = Q.GetPrediction(target);
                    var damageI = GetBestCombo(target);
                    if (pred.HitChancePercent <= 5)
                    {
                        CastE(myHero);
                    }
                }
                List<Obj_AI_Base> list = new List<Obj_AI_Base>();
                if (target.Type == myHero.Type)
                {
                    list = EntityManager.Heroes.Enemies.Where(o => o.IsValidTarget(Q.Range + Q.Width)).ToList<Obj_AI_Base>();
                }
                else
                {
                    var enemyminions = EntityManager.MinionsAndMonsters.Get(EntityManager.MinionsAndMonsters.EntityType.Minion, EntityManager.UnitTeam.Enemy, myHero.Position, Q.Range + Q.Width).ToList<Obj_AI_Base>();
                    var jungleminions = EntityManager.MinionsAndMonsters.Get(EntityManager.MinionsAndMonsters.EntityType.Monster, EntityManager.UnitTeam.Enemy, myHero.Position, Q.Range + Q.Width).ToList<Obj_AI_Base>();
                    if (enemyminions.Count > 0)
                    {
                        list = enemyminions;
                    }
                    else if (jungleminions.Count > 0)
                    {
                        list = jungleminions.ToList<Obj_AI_Base>();
                    }
                }
                if (list.Count < minhits) { return; }
                var t = BestHitQ(list, target);
                if (t.Item1 != Vector3.Zero && t.Item2 >= minhits)
                {
                    Q.Cast(t.Item1);
                }
                /**
                var pred = Q.GetPrediction(target);
                if (pred.HitChancePercent >= 70)
                {
                    Q.Cast(pred.CastPosition);
                }**/
            }
        }

        private static void CastW(Obj_AI_Base target)
        {
            if (W.IsReady())
            {
                var pred = W.GetPrediction(target);
                if (pred.HitChancePercent >= HitChancePercent(W.Slot) && Extensions.Distance(Ball, pred.CastPosition, true) < W.RangeSquared)
                {
                    myHero.Spellbook.CastSpell(W.Slot);
                }
            }
        }
        private static void CastE(Obj_AI_Base target)
        {
            if (E.IsReady() && target != null && target.IsValid && Extensions.Distance(myHero, target, true) < E.RangeSquared)
            {
                if (target.Team == myHero.Team)
                {
                    myHero.Spellbook.CastSpell(E.Slot, target);
                }
                else
                {
                    List<Obj_AI_Base> list = new List<Obj_AI_Base>();
                    if (target.Type == myHero.Type)
                    {
                        list = EntityManager.Heroes.Enemies.ToList<Obj_AI_Base>();
                    }
                    else
                    {
                        var enemyminions = EntityManager.MinionsAndMonsters.Get(EntityManager.MinionsAndMonsters.EntityType.Minion, EntityManager.UnitTeam.Enemy, myHero.Position, Q.Range + Q.Width).ToList<Obj_AI_Base>();
                        var jungleminions = EntityManager.MinionsAndMonsters.Get(EntityManager.MinionsAndMonsters.EntityType.Monster, EntityManager.UnitTeam.Enemy, myHero.Position, Q.Range + Q.Width).ToList<Obj_AI_Base>();
                        if (enemyminions.Count > 0)
                        {
                            list = enemyminions;
                        }
                        else if (jungleminions.Count > 0)
                        {
                            list = jungleminions;
                        }
                    }
                    if (list.Count > 0)
                    {
                        var info = BestHitE(list);
                        if (info.Item1 != null)
                        {
                            Obj_AI_Base bestAlly = info.Item1;
                            int bestHit = info.Item2;
                            if (bestHit > 0 && bestAlly.IsValid)
                            {
                                CastE(bestAlly);
                            }
                        }
                    }
                }
            }
        }
        private static void ThrowBall(Obj_AI_Base target)
        {
            Obj_AI_Base eAlly = null;
            Vector3 predictedPos = Vector3.Zero;
            if (E.IsReady() && target.IsValidTarget() && Extensions.Distance(Ball, target, true) > R.RangeSquared)
            {
                var pred = E.GetPrediction(target);
                foreach (AIHeroClient ally in EntityManager.Heroes.Allies.Where(o => o.IsValid && Extensions.Distance(myHero, o, true) < E.RangeSquared && Extensions.Distance(Ball, o, true) > 0))
                {
                    var pred2 = E.GetPrediction(ally);
                    if (Extensions.Distance(pred.CastPosition, pred2.CastPosition, true) <= R.RangeSquared * 1.5f * 1.5f)
                    {
                        if (eAlly == null)
                        {
                            eAlly = ally;
                            predictedPos = pred2.CastPosition;
                        }
                        else if (Extensions.Distance(pred.CastPosition, predictedPos, true) > Extensions.Distance(pred.CastPosition, pred2.CastPosition, true))
                        {
                            eAlly = ally;
                            predictedPos = pred2.CastPosition;
                        }
                    }
                }
            }
            if (eAlly != null)
            {
                CastE(eAlly);
            }
            else
            {
                CastQ(target);
            }
        }
        private static int HitW(List<Obj_AI_Base> list)
        {
            int count = 0;
            if (W.IsReady())
            {
                foreach (Obj_AI_Base obj in list.Where(obj => obj.IsValidTarget() && Extensions.Distance(obj.ServerPosition, Ball, true) <= W.RangeSquared * 1.5f * 1.5f))
                {
                    var pred = W.GetPrediction(obj);
                    if (pred.HitChancePercent >= HitChancePercent(W.Slot) && Extensions.Distance(Ball, pred.CastPosition, true) <= W.RangeSquared)
                    {
                        count++;
                    }
                }
            }
            return count;
        }
        private static int HitR()
        {
            int count = 0;
            if (R.IsReady())
            {
                foreach (AIHeroClient obj in EntityManager.Heroes.Enemies.Where(o => o.IsValidTarget() && Extensions.Distance(Ball, o, true) <= R.RangeSquared * 1.5f * 1.5f))
                {
                    var pred = R.GetPrediction(obj);
                    if (pred.HitChancePercent >= HitChancePercent(R.Slot) && Extensions.Distance(Ball, pred.CastPosition, true) <= R.RangeSquared)
                    {
                        count++;
                    }
                }
            }
            return count;
        }
        private static void CastQR()
        {
            if (Q.IsReady() && R.IsReady())
            {
                var qWidth = Q.Width;
                var qDelay = Q.CastDelay;
                Q.CastDelay = R.CastDelay;
                Q.Width = (int)R.Range;
                List<Vector2> Positions = new List<Vector2>();
                foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies.Where(o => o.IsValidTarget(Q.Range + R.Range)))
                {
                    var pred = Q.GetPrediction(enemy);
                    if (pred.HitChancePercent >= HitChancePercent(R.Slot))
                    {
                        Positions.Add(pred.CastPosition.To2D());
                    }
                }
                Vector2 bestPos = Vector2.Zero;
                int bestCount = 0;
                foreach (Vector2 vec in Positions)
                {
                    int count = Positions.Where(v => Extensions.Distance(vec, v, true) < Math.Pow(R.Width * 1.4f, 2)).Count();
                    if (bestPos == Vector2.Zero)
                    {
                        bestPos = vec;
                        bestCount = count;
                    }
                    else if (bestCount < count)
                    {
                        bestPos = vec;
                        bestCount = count;
                    }
                }
                if (bestCount >= SubMenu["Combo"]["R2"].Cast<Slider>().CurrentValue)
                {
                    Q.Cast(bestPos.To3D());
                }
                Q.Width = qWidth;
                Q.CastDelay = qDelay;
            }
        }
        private static void CastER(AIHeroClient target = null)
        {
            if (E.IsReady() && R.IsReady())
            {
                int bestCount = -1;
                Obj_AI_Base bestAlly = null;
                foreach (Obj_AI_Base ally in EntityManager.Heroes.AllHeroes.Where(o => o.IsValid && o.Team == myHero.Team && Extensions.Distance(myHero, o, true) < E.RangeSquared))
                {
                    int count = ally.CountEnemiesInRange(R.Range * 1.5f);
                    if (bestCount == -1)
                    {
                        bestCount = count;
                        bestAlly = ally;
                    }
                    else if (bestCount < count)
                    {
                        bestCount = count;
                        bestAlly = ally;
                    }
                }

                if (bestCount >= SubMenu["Combo"]["R2"].Cast<Slider>().CurrentValue)
                {
                    CastE(bestAlly);
                }
            }
        }

        private static Tuple<int, Dictionary<int, bool>> CountHitQ(Vector3 StartPos, Vector3 EndPos, List<Obj_AI_Base> list, Obj_AI_Base target)
        {
            Dictionary<int, bool> counted = new Dictionary<int, bool>();
            counted[target.NetworkId] = true;
            if (Q.IsReady())
            {
                foreach (Obj_AI_Base obj in list.Where(o => o.IsValidTarget(Q.Range + Q.Width) && target.NetworkId != o.NetworkId))
                {
                    var extendedendpos = EndPos + (EndPos - StartPos).Normalized() * Q.Width;
                    var info = obj.ServerPosition.To2D().ProjectOn(StartPos.To2D(), extendedendpos.To2D());
                    if (info.IsOnSegment && Extensions.Distance(obj.ServerPosition.To2D(), info.SegmentPoint, true) <= Math.Pow(Q.Width * 1.5f + obj.BoundingRadius / 3, 2))
                    {
                        var hitchancepercent = obj.Type == myHero.Type ? HitChancePercent(Q.Slot) : 30;
                        var pred = Q.GetPrediction(obj);
                        if (pred.HitChancePercent >= hitchancepercent)
                        {
                            info = pred.CastPosition.To2D().ProjectOn(StartPos.To2D(), extendedendpos.To2D());
                            if (info.IsOnSegment && Extensions.Distance(pred.CastPosition.To2D(), info.SegmentPoint, true) <= Math.Pow(Q.Width + obj.BoundingRadius / 3, 2))
                            {
                                counted[obj.NetworkId] = true;
                            }
                        }
                    }
                }
            }
            return new Tuple<int, Dictionary<int, bool>>(counted.Count, counted);
        }
        private static Tuple<Vector3, int> BestHitQ(List<Obj_AI_Base> list, Obj_AI_Base target = null)
        {
            if (Game.Time < Q_LastRequest)
            {
                return new Tuple<Vector3, int>(Vector3.Zero, 0);
            }
            Q_LastRequest = Game.Time + (float)Math.Pow(list.Count, 3) / 1000;
            Vector3 BestPos = Vector3.Zero;
            int bestHit = -1;
            bool checktarget = target != null && target.IsValidTarget();
            if (Q.IsReady())
            {
                foreach (Obj_AI_Base obj in list.Where(o => o.IsValidTarget(Q.Range + Q.Width)))
                {
                    var pred = Q.GetPrediction(obj);
                    var hitchancepercent = obj.Type == myHero.Type ? HitChancePercent(Q.Slot) : 30;
                    if (pred.HitChancePercent >= hitchancepercent)
                    {
                        var t = CountHitQ(Ball, pred.CastPosition, list, obj);
                        var hit = t.Item1;
                        var counted = t.Item2;
                        bool b = true;
                        if (checktarget)
                        {
                            b = counted.ContainsKey(target.NetworkId);
                        }
                        if (hit > bestHit && b)
                        {
                            bestHit = hit;
                            BestPos = pred.CastPosition;
                            if (bestHit == list.Count)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            return new Tuple<Vector3, int>(BestPos, bestHit);
        }
        private static Tuple<int, Dictionary<int, bool>> CountHitE(Vector3 StartPos, Vector3 EndPos, List<Obj_AI_Base> list)
        {
            int count = 0;
            Dictionary<int, bool> counted = new Dictionary<int, bool>();
            if (E.IsReady())
            {
                foreach (Obj_AI_Base obj in list.Where(o => o.IsValidTarget(E.Range)))
                {
                    var info = obj.ServerPosition.To2D().ProjectOn(StartPos.To2D(), EndPos.To2D());
                    if (info.IsOnSegment && Extensions.Distance(obj.ServerPosition.To2D(), info.SegmentPoint, true) <= Math.Pow(E.Width * 1.5f + obj.BoundingRadius / 3, 2))
                    {
                        var pred = E.GetPrediction(obj);
                        var hitchancepercent = obj.Type == myHero.Type ? HitChancePercent(E.Slot) : 30;
                        if (pred.HitChancePercent >= hitchancepercent && Extensions.Distance(pred.CastPosition, myHero, true) <= E.RangeSquared)
                        {
                            info = pred.CastPosition.To2D().ProjectOn(StartPos.To2D(), EndPos.To2D());
                            if (info.IsOnSegment && Extensions.Distance(pred.CastPosition.To2D(), info.SegmentPoint, true) <= Math.Pow(E.Width + obj.BoundingRadius / 3, 2))
                            {
                                count++;
                                counted[obj.NetworkId] = true;
                            }
                        }
                    }
                }
            }
            return new Tuple<int, Dictionary<int, bool>>(count, counted);
        }
        private static Tuple<Obj_AI_Base, int> BestHitE(List<Obj_AI_Base> list, Obj_AI_Base target = null)
        {
            if (Game.Time < E_LastRequest)
            {
                return new Tuple<Obj_AI_Base, int>(null, 0);
            }
            E_LastRequest = Game.Time + (float)Math.Pow(list.Count, 3) / 1000;
            Obj_AI_Base bestAlly = null;
            int bestHit = 0;
            bool checktarget = target != null && target.IsValidTarget();
            if (E.IsReady())
            {
                foreach (Obj_AI_Base ally in EntityManager.Heroes.AllHeroes.Where(o => o.IsValid && o.Team == myHero.Team && Extensions.Distance(myHero, o, true) < E.RangeSquared))
                {
                    if (Extensions.Distance(Ball, ally, true) > 0)
                    {
                        var pred = E.GetPrediction(ally);
                        var info = CountHitE(Ball, pred.CastPosition, list);
                        var hit = info.Item1;
                        var counted = info.Item2;
                        bool b = true;
                        if (checktarget)
                        {
                            b = counted.ContainsKey(target.NetworkId);
                        }
                        if (hit > bestHit && b)
                        {
                            bestHit = hit;
                            bestAlly = ally;
                            if (bestHit == list.Count)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            return new Tuple<Obj_AI_Base, int>(bestAlly, bestHit);
        }
        private static void CastR(Obj_AI_Base target)
        {
            if (R.IsReady())
            {
                var pred = R.GetPrediction(target);
                if (pred.HitChancePercent >= HitChancePercent(R.Slot) && Extensions.Distance(Ball, pred.CastPosition, true) < R.RangeSquared)
                {
                    myHero.Spellbook.CastSpell(R.Slot);
                }
            }
        }


        private static void OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            if (sender.Team == myHero.Team)
            {
                var target = TargetSelector.GetTarget(E.Range, DamageType.Magical, myHero.Position);
                if (SubMenu["Misc"]["E"].Cast<CheckBox>().CurrentValue && target.IsValidTarget())
                {
                    CastE(sender);
                    LastGapclose = Game.Time;
                }
            }
        }

        private static void OnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs e)
        {
            if (sender.Team != myHero.Team)
            {
                if (SubMenu["Misc"]["R"].Cast<CheckBox>().CurrentValue)
                {
                    if (Extensions.Distance(Ball, sender, true) > R.RangeSquared)
                    {
                        ThrowBall(sender);
                    }
                    else
                    {
                        CastR(sender);
                    }
                }
            }
        }
        private static void OnPlayAnimation(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.Animation.ToLower().Equals("prop"))
                {
                    BallObject = sender;
                }
            }
        }

        private static void OnProcessSpell(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.SData.Name.ToLower() == myHero.Spellbook.GetSpell(SpellSlot.E).SData.Name.ToLower())
                {
                    E_Target = args.Target;
                }
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (myHero.IsDead) { return; }
            if (SubMenu["Draw"]["Ball"].Cast<CheckBox>().CurrentValue)
            {
                Circle.Draw(new ColorBGRA(0, 0, 255, 100), 120, Ball);
            }
            if (SubMenu["Draw"]["Q"].Cast<CheckBox>().CurrentValue && Q.IsReady())
            {
                Circle.Draw(new ColorBGRA(255, 255, 255, 100), Q.Range, myHero.Position);
            }
            if (SubMenu["Draw"]["W"].Cast<CheckBox>().CurrentValue && W.IsReady())
            {
                Circle.Draw(new ColorBGRA(255, 255, 255, 100), W.Range, Ball);
            }
            if (SubMenu["Draw"]["R"].Cast<CheckBox>().CurrentValue && R.IsReady())
            {
                Circle.Draw(new ColorBGRA(255, 255, 255, 100), R.Range, Ball);
            }
        }

        private static void OnCreate(GameObject sender, EventArgs args)
        {
            if (sender != null && sender.Name != null)
            {
                if (sender is Obj_GeneralParticleEmitter && sender.Name.ToLower().Contains(myHero.ChampionName.ToLower()))
                {
                    if (sender.Name.ToLower().Contains("yomu") && sender.Name.ToLower().Contains("green"))
                    {
                        BallObject = sender;
                    }
                }
            }
        }

        private static void MissileClient_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender is MissileClient)
            {
                var missile = sender as MissileClient;
                if (missile.SpellCaster.IsMe && (missile.SData.Name.ToLower().Contains("orianaizuna") || missile.SData.Name.ToLower().Contains("orianaredact")))
                {
                    BallObject = sender;
                }
                if (GetCheckBox(SubMenu["Combo"], "Shield") || GetCheckBox(SubMenu["Harass"], "Shield") || GetCheckBox(SubMenu["Misc"], "Shield"))
                {
                    var spellCaster = missile.SpellCaster as Obj_AI_Base;
                    if (!spellCaster.Name.ToLower().Contains("minion") && myHero.Team != missile.SpellCaster.Team && Extensions.Distance(myHero, sender, true) < E.RangeSquared * 1.5f)//(missile.SpellCaster.Type == GameObjectType.AIHeroClient && missile.SpellCaster.Type == GameObjectType.obj_AI_Turret) &&
                    {
                        missiles.Add(missile);
                        //Core.DelayAction(delegate { missiles.Remove(missile); }, 1000 * (int)(1.25f * Extensions.Distance(missile.Position, missile.EndPosition) / missile.SData.MissileSpeed));
                    }
                }
            }
        }
        private static void MissileClient_OnDelete(GameObject sender, EventArgs args)
        {
            if (sender is MissileClient)
            {
                var missile = sender as MissileClient;
                if (missile.SpellCaster.IsMe && missile.SData.Name.ToLower().Contains("orianaredact"))
                {
                    BallObject = E_Target;
                }
                if (GetCheckBox(SubMenu["Combo"], "Shield") || GetCheckBox(SubMenu["Harass"], "Shield") || GetCheckBox(SubMenu["Misc"], "Shield"))
                {
                    var spellCaster = missile.SpellCaster as Obj_AI_Base;
                    if (!spellCaster.Name.ToLower().Contains("minion") && myHero.Team != missile.SpellCaster.Team)//(missile.SpellCaster.Type == GameObjectType.AIHeroClient && missile.SpellCaster.Type == GameObjectType.obj_AI_Turret) &&
                    {
                        if (missiles.Count > 0)
                        {
                            foreach (MissileClient m in missiles)
                            {
                                if (m.NetworkId == missile.NetworkId || Extensions.Distance(m, missile, true) == 0f)
                                {
                                    missiles.Remove(m);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
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
        static float Damage(Obj_AI_Base target, SpellSlot slot)
        {
            if (target.IsValidTarget())
            {
                if (slot == SpellSlot.Q)
                {
                    return myHero.CalculateDamageOnUnit(target, DamageType.Magical, (float)30 * Q.Level + 30 + 0.5f * myHero.FlatMagicDamageMod);
                }
                else if (slot == SpellSlot.W)
                {
                    return myHero.CalculateDamageOnUnit(target, DamageType.Magical, (float)45 * W.Level + 25 + 0.7f * myHero.FlatMagicDamageMod);
                }
                else if (slot == SpellSlot.E)
                {
                    return myHero.CalculateDamageOnUnit(target, DamageType.Magical, (float)30 * E.Level + 30 + 0.3f * myHero.FlatMagicDamageMod);
                }
                else if (slot == SpellSlot.R)
                {
                    return myHero.CalculateDamageOnUnit(target, DamageType.Magical, (float)75 * R.Level + 75 + 0.7f * myHero.FlatMagicDamageMod);
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
                    ComboDamage += Damage(target, R.Slot);
                    ManaWasted += myHero.Spellbook.GetSpell(SpellSlot.R).SData.Mana;
                }
                if (Ignite != null && Ignite.IsReady())
                {
                    ComboDamage += myHero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite);
                }
                ComboDamage += myHero.GetAutoAttackDamage(target, true);
            }
            ComboDamage = ComboDamage * Overkill;
            return new DamageInfo(ComboDamage, ManaWasted);
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
                        foreach (bool r1 in r)
                        {
                            foreach (bool q1 in q)
                            {
                                foreach (bool w1 in w)
                                {
                                    foreach (bool e1 in e)
                                    {
                                        DamageInfo damageI2 = GetComboDamage(target, q1, w1, e1, r1);
                                        float d = damageI2.Damage;
                                        float m = damageI2.Mana;
                                        if (myHero.Mana >= m)
                                        {
                                            if (bestdmg >= target.Health)
                                            {
                                                if (d >= target.Health && (d < bestdmg || m < bestmana || (best[3] == true && damageI2.R == false)))
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
    
}
