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

//agregar check con deletemissile
namespace Draven_Me_Crazy
{
    class Program
    {
        static string Author = "iCreative";
        static string AddonName = "Draven Me Crazy";
        static float RefreshTime = 0.4f;
        static Dictionary<int, DamageInfo> PredictedDamage = new Dictionary<int, DamageInfo>() { };
        public static AIHeroClient myHero { get { return ObjectManager.Player; } }
        static Vector3 mousePos { get { return Game.CursorPos; } }
        static Menu menu;
        public static Dictionary<string, Menu> SubMenu = new Dictionary<string, Menu>() { };
        static Spell.Skillshot E, R;
        static Spell.Active Q, W;
        static Spell.Targeted Ignite;
        static List<Axe> Axes = new List<Axe>();
        public static List<Obj_AI_Turret> Turrets = new List<Obj_AI_Turret>();
        static bool TryingToCatch = false;
        static int AxesCount
        {
            get
            {
                if (myHero.HasBuff("dravenspinningattack"))
                {
                    return myHero.GetBuff("dravenspinningattack").Count + Axes.Count;
                }
                return Axes.Count;
            }
        }
        static bool CatchEnabled { get { return GetKeyBind(SubMenu["Axes"], "Catch"); } }
        static bool CanCatch { get { return CatchEnabled && ((GetSlider(SubMenu["Axes"], "CatchMode") == 0 && !IsNone) || GetSlider(SubMenu["Axes"], "CatchMode") == 1); } }
        static float CatchDelay { get { return GetSlider(SubMenu["Axes"], "Delay") / 100.0f; } }
        public static float CatchRadius
        {
            get
            {
                if (IsCombo) { return GetSlider(SubMenu["Axes"], "Combo"); }
                else if (IsHarass) { return GetSlider(SubMenu["Axes"], "Harass"); }
                else if (IsClear) { return GetSlider(SubMenu["Axes"], "Clear"); }
                else if (IsLastHit) { return GetSlider(SubMenu["Axes"], "Harass"); }
                return GetSlider(SubMenu["Axes"], "Clear");
            }
        }
        public static Vector3 CatchSource { get { return GetSlider(SubMenu["Axes"], "OrbwalkMode") == 1 ? mousePos : myHero.Position; } }
        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (myHero.Hero != Champion.Draven) { return; }
            Chat.Print(AddonName + " made by " + Author + " loaded, have fun!.");
            Q = new Spell.Active(SpellSlot.Q, 1075);
            W = new Spell.Active(SpellSlot.W, 950);
            E = new Spell.Skillshot(SpellSlot.E, 1050, SkillShotType.Linear, 250, 1600, 130);
            E.AllowedCollisionCount = int.MaxValue;
            R = new Spell.Skillshot(SpellSlot.R, 20000, SkillShotType.Linear, 500, 2000, 155);
            R.AllowedCollisionCount = int.MaxValue;
            var slot = myHero.GetSpellSlotFromName("summonerdot");
            if (slot != SpellSlot.Unknown)
            {
                Ignite = new Spell.Targeted(slot, 600);
            }
            menu = MainMenu.AddMenu(AddonName, AddonName + " by " + Author + " v1.10");
            menu.AddLabel(AddonName + " made by " + Author);

            SubMenu["Axes"] = menu.AddSubMenu("Axes", "Axes");
            SubMenu["Axes"].AddGroupLabel("Keys");
            SubMenu["Axes"].Add("Catch", new KeyBind("Catch Axes (Toggle)", true, KeyBind.BindTypes.PressToggle, (uint)'L'));
            SubMenu["Axes"].AddGroupLabel("Settings");
            SubMenu["Axes"].Add("Click", new CheckBox("Use left click to disable catching the current axe", true));
            SubMenu["Axes"].AddSeparator(5);
            SubMenu["Axes"].Add("W", new CheckBox("Use W to Catch (Smart)", true));
            SubMenu["Axes"].Add("Turret", new CheckBox("Don't catch under turret", true));
            SubMenu["Axes"].Add("Delay", new Slider("% of delay to catch the axe", 100, 0, 100));
            var mode = SubMenu["Axes"].Add("CatchMode", new Slider("Catch Condition", 0, 0, 1));
            var catchmodes = new[] { "When Orbwalking", "AutoCatch" };
            mode.DisplayName = catchmodes[mode.CurrentValue];
            mode.OnValueChange += delegate (ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args2)
            {
                sender.DisplayName = catchmodes[args2.NewValue];
            };
            var mode2 = SubMenu["Axes"].Add("OrbwalkMode", new Slider("Catch Mode", 0, 0, 1));
            var orbwalkmodes = new[] { "My Hero in radius", "Mouse in radius" };
            mode2.DisplayName = orbwalkmodes[mode2.CurrentValue];
            mode2.OnValueChange += delegate (ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args2)
            {
                sender.DisplayName = orbwalkmodes[args2.NewValue];
            };
            SubMenu["Axes"].AddGroupLabel("Catch radius");
            SubMenu["Axes"].Add("Combo", new Slider("Combo radius", 250, 150, 600));
            SubMenu["Axes"].Add("Harass", new Slider("Harass radius", 350, 150, 600));
            SubMenu["Axes"].Add("Clear", new Slider("Clear radius", 400, 150, 800));
            SubMenu["Axes"].Add("LastHit", new Slider("LastHit radius", 400, 150, 800));
            SubMenu["Axes"].AddGroupLabel("Drawings");
            SubMenu["Axes"].Add("Draw", new CheckBox("Draw catch radius", true));

            SubMenu["Combo"] = menu.AddSubMenu("Combo", "Combo");
            SubMenu["Combo"].Add("Q", new Slider("Use Q to have X spinning axes", 3, 0, 3));
            SubMenu["Combo"].Add("W", new CheckBox("Use W", true));
            SubMenu["Combo"].Add("E", new CheckBox("Use E", true));
            SubMenu["Combo"].Add("R", new CheckBox("Use R if killable", true));
            SubMenu["Combo"].Add("R2", new Slider("Use R if hit X enemies", 3, 1, 5));

            SubMenu["Harass"] = menu.AddSubMenu("Harass", "Harass");
            SubMenu["Harass"].Add("AA", new CheckBox("Use AA if will be lose the spin", true));
            SubMenu["Harass"].Add("Q", new Slider("Use Q to have X spinning axes", 1, 0, 2));
            SubMenu["Harass"].Add("W", new CheckBox("Use W", false));
            SubMenu["Harass"].Add("E", new CheckBox("Use E", false));
            SubMenu["Harass"].Add("Mana", new Slider("Min. Mana Percent:", 20, 0, 100));

            SubMenu["LaneClear"] = menu.AddSubMenu("LaneClear", "LaneClear");
            SubMenu["LaneClear"].Add("Q", new Slider("Use Q to have X spinning axes", 1, 0, 2));
            SubMenu["LaneClear"].Add("Mana", new Slider("Min. Mana Percent:", 20, 0, 100));

            SubMenu["LastHit"] = menu.AddSubMenu("LastHit", "LastHit");
            SubMenu["LastHit"].Add("Q", new Slider("Use Q to have X spinning axes", 1, 0, 2));
            SubMenu["LastHit"].Add("Mana", new Slider("Min. Mana Percent:", 20, 0, 100));

            SubMenu["JungleClear"] = menu.AddSubMenu("JungleClear", "JungleClear");
            SubMenu["JungleClear"].Add("Q", new Slider("Use Q to have X spinning axes", 1, 0, 2));
            SubMenu["JungleClear"].Add("W", new CheckBox("Use W", true));
            SubMenu["JungleClear"].Add("E", new CheckBox("Use E", true));
            SubMenu["JungleClear"].Add("Mana", new Slider("Min. Mana Percent:", 20, 0, 100));

            SubMenu["KillSteal"] = menu.AddSubMenu("KillSteal", "KillSteal");
            SubMenu["KillSteal"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["KillSteal"].Add("E", new CheckBox("Use E", true));
            SubMenu["KillSteal"].Add("R", new CheckBox("Use R", true));
            SubMenu["KillSteal"].Add("Ignite", new CheckBox("Use Ignite", true));

            SubMenu["Flee"] = menu.AddSubMenu("Flee", "Flee");
            SubMenu["Flee"].Add("W", new CheckBox("Use W", true));
            SubMenu["Flee"].Add("E", new CheckBox("Use E", true));

            SubMenu["Misc"] = menu.AddSubMenu("Misc", "Misc");
            SubMenu["Misc"].Add("GapCloser", new CheckBox("Use E to Interrupt GapClosers", true));
            SubMenu["Misc"].Add("Interrupter", new CheckBox("Use E to Interrupt Channeling Spells", true));
            SubMenu["Misc"].Add("Overkill", new Slider("Overkill % for damage prediction", 10, 0, 100));
            SubMenu["Misc"].Add("RRange", new Slider("R Range", 1800, 300, 6000));

            Turrets = ObjectManager.Get<Obj_AI_Turret>().Where(m => m.HealthPercent > 0 && m.IsEnemy).ToList();

            Game.OnTick += Game_OnTick;
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
            Drawing.OnDraw += Drawing_OnDraw;
            Interrupter.OnInterruptableSpell += Interrupter_OnInterruptableSpell;
            Gapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
            Game.OnWndProc += Game_OnWndProc;
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg == (uint)WindowMessages.LeftButtonDown && GetCheckBox(SubMenu["Axes"], "Click"))
            {
                var axe = Axes.Where(m => m.SourceInRadius).OrderBy(m => m.TimeLeft).FirstOrDefault();
                //var axe = Axes.Where(a => Extensions.Distance(mousePos, a.Position, true) < CatchRadius * CatchRadius).OrderBy(a => Extensions.Distance(a.Position, mousePos, true)).FirstOrDefault();
                if (axe != null)
                {
                    Axes.Remove(axe);
                }
            }
        }

        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {

            if (sender.Owner.IsMe)
            {
                if (TryingToCatch && !IsNone && (args.Slot == R.Slot || args.Slot == E.Slot))
                {
                    //args.Process = false;
                }
            }
        }

        private static void Game_OnTick(EventArgs args)
        {
            R = new Spell.Skillshot(R.Slot, (uint)GetSlider(SubMenu["Misc"], "RRange"), R.Type, R.CastDelay, R.Speed, R.Width);
            R.AllowedCollisionCount = int.MaxValue;
            foreach (Axe a in Axes)
            {
                if (!a.InTime)
                {
                    Axes.Remove(a);
                }
            }
            CatchReticles();
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
                    var menu = SubMenu["KillSteal"];
                    if (damageI.Damage >= enemy.Health)
                    {
                        if (GetCheckBox(menu, "Q") && (Damage(enemy, Q.Slot) >= enemy.Health || damageI.Q)) { CastQ(enemy); }
                        if (GetCheckBox(menu, "W") && (Damage(enemy, W.Slot) >= enemy.Health || damageI.W)) { CastW(enemy); }
                        if (GetCheckBox(menu, "E") && (Damage(enemy, E.Slot) >= enemy.Health || damageI.E)) { CastE(enemy); }
                        if (GetCheckBox(menu, "R") && (Damage(enemy, R.Slot) >= enemy.Health || damageI.R)) { CastR(enemy); }
                    }
                    if (Ignite != null && GetCheckBox(menu, "Ignite") && Ignite.IsReady() && myHero.GetSummonerSpellDamage(enemy, DamageLibrary.SummonerSpells.Ignite) >= enemy.Health)
                    {
                        Ignite.Cast(enemy);
                    }
                }
            }
        }
        static void Combo()
        {
            var menu = SubMenu["Combo"];
            var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);
            if (target.IsValidTarget())
            {
                var damageI = GetBestCombo(target);
                CastQ(target, GetSlider(menu, "Q"));
                if (GetCheckBox(menu, "R") && damageI.Damage >= target.Health && damageI.R) { CastR(target); }
                if (GetCheckBox(menu, "E")) { CastE(target); }
                if (GetCheckBox(menu, "W")) { CastW(target); }
            }
        }
        static void Harass()
        {
            var menu = SubMenu["Harass"];
            if (GetSlider(menu, "Mana") <= myHero.ManaPercent)
            {
                var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);
                if (target.IsValidTarget())
                {
                    CastQ(target, GetSlider(menu, "Q"));
                    var minion = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myHero.Position, myHero.AttackRange + myHero.BoundingRadius, true).FirstOrDefault();
                    if (minion != null && minion.IsValidTarget())
                    {
                        CastQ(minion, GetSlider(menu, "Q"));
                    }
                    if (GetCheckBox(menu, "E")) { CastE(target); }
                    if (GetCheckBox(menu, "W")) { CastW(target); }
                    if (GetCheckBox(menu, "AA"))
                    {

                        if (myHero.HasBuff("dravenspinningattack"))
                        {
                            var buff = myHero.GetBuff("dravenspinningattack");
                            if (Orbwalker.CanAutoAttack)
                            {
                                if (buff.EndTime - Game.Time <= 0.8f + myHero.AttackCastDelay)
                                {
                                    Obj_AI_Base BestTarget = null;
                                    AIHeroClient target2 = TargetSelector.GetTarget(myHero.GetAutoAttackRange() + 60, DamageType.Physical);
                                    if (target2 != null && target2.IsValidTarget())
                                    {
                                        BestTarget = target2;
                                    }
                                    else
                                    {
                                        Obj_AI_Base BestMinion = EntityManager.MinionsAndMonsters.EnemyMinions.Where(m => m.IsValidTarget() && myHero.IsInAutoAttackRange(m) && (Prediction.Health.GetPrediction(m, 2 * 1000 * (int)(myHero.AttackDelay + myHero.AttackCastDelay + Extensions.Distance(myHero, m) / myHero.BasicAttack.MissileSpeed - 0.07f)) > 2 * myHero.GetAutoAttackDamage(m) || Prediction.Health.GetPrediction(m, 1000 * (int)(myHero.AttackCastDelay + Extensions.Distance(myHero, m) / myHero.BasicAttack.MissileSpeed - 0.07f)) == m.Health)).OrderBy(m => m.HealthPercent).LastOrDefault();
                                        if (BestMinion != null && BestMinion.IsValidTarget())
                                        {
                                            BestTarget = BestMinion;
                                        }
                                        else
                                        {
                                            BestMinion = EntityManager.MinionsAndMonsters.EnemyMinions.Where(m => m.IsValidTarget() && myHero.IsInAutoAttackRange(m)).OrderBy(m => m.HealthPercent).LastOrDefault();
                                            if (BestMinion != null && BestMinion.IsValidTarget())
                                            {
                                                BestTarget = BestMinion;
                                            }
                                        }
                                    }
                                    Orbwalker.ForcedTarget = BestTarget;
                                }
                                else
                                {
                                    Orbwalker.ForcedTarget = null;
                                }
                            }
                        }
                    }
                }
            }
        }
        private static void LaneClear()
        {
            var menu = SubMenu["LaneClear"];
            if (myHero.ManaPercent >= GetSlider(menu, "Mana"))
            {
                foreach (Obj_AI_Base minion in EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myHero.Position, 1000f))
                {
                    if (minion.IsValidTarget() && myHero.ManaPercent >= GetSlider(menu, "Mana"))
                    {
                        CastQ(minion, GetSlider(menu, "Q"));
                    }
                }
            }
        }
        private static void JungleClear()
        {
            var menu = SubMenu["JungleClear"];
            if (myHero.ManaPercent >= GetSlider(menu, "Mana"))
            {
                foreach (Obj_AI_Base minion in EntityManager.MinionsAndMonsters.GetJungleMonsters(myHero.Position, 1000f))
                {
                    if (minion.IsValidTarget() && myHero.ManaPercent >= GetSlider(menu, "Mana"))
                    {
                        CastQ(minion, GetSlider(menu, "Q"));
                        if (GetCheckBox(menu, "E")) { CastE(minion); }
                        if (GetCheckBox(menu, "W")) { CastW(minion); }
                    }
                }
            }
        }
        private static void LastHit()
        {
            var menu = SubMenu["LastHit"];
            if (myHero.ManaPercent >= GetSlider(menu, "Mana"))
            {
                foreach (Obj_AI_Base minion in EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myHero.Position, 1000f))
                {
                    if (minion.IsValidTarget() && myHero.ManaPercent >= GetSlider(menu, "Mana"))
                    {
                        CastQ(minion, GetSlider(menu, "Q"));
                    }
                }
            }
        }
        private static void Flee()
        {
            if (W.IsReady() && GetCheckBox(SubMenu["Flee"], "W"))
            {
                myHero.Spellbook.CastSpell(W.Slot);
            }
            if (E.IsReady() && GetCheckBox(SubMenu["Flee"], "E"))
            {
                var target = EntityManager.Heroes.Enemies.Where(e => e.IsValidTarget(E.Range)).OrderBy(d => Extensions.Distance(myHero, d)).FirstOrDefault();
                if (target.IsValidTarget())
                {
                    CastE(target);
                }
            }
        }
        private static void CastQ(Obj_AI_Base target, int count = 2)
        {
            if (Q.IsReady() && target.IsValidTarget(Q.Range) && Orbwalker.CanAutoAttack)
            {
                if (AxesCount < count)
                {
                    myHero.Spellbook.CastSpell(Q.Slot);
                }
            }
        }
        private static void CastW(Obj_AI_Base target)
        {
            if (W.IsReady() && target.IsValidTarget(W.Range) && !myHero.HasBuff("dravenfurybuff"))
            {
                var pred = E.GetPrediction(target);
                var damageI = GetBestCombo(target);
                if (pred.HitChancePercent >= 20f && ((damageI.IsKillable(target) && Extensions.Distance(myHero, target, true) < Extensions.Distance(myHero, pred.CastPosition, true)) || Extensions.Distance(myHero, target) >= 0.75f))
                {
                    myHero.Spellbook.CastSpell(W.Slot);
                }
            }
        }
        private static void CastE(Obj_AI_Base target)
        {
            if (E.IsReady() && target.IsValidTarget())
            {
                var pred = E.GetPrediction(target);
                if (pred.HitChancePercent >= 70)
                {
                    E.Cast(pred.CastPosition);
                }
            }
        }
        private static void CastR(Obj_AI_Base target)
        {
            if (R.IsReady() && target.IsValidTarget())
            {
                var pred = R.GetPrediction(target);
                if (pred.HitChancePercent >= 65)
                {
                    R.Cast(pred.CastPosition);
                }
            }
        }
        private static Tuple<bool, bool> GetAxeStatus(Axe a)
        {
            var CanMove = false;
            var CanAttack = false;
            if (a.InTime && a.SourceInRadius && !a.InTurret)
            {
                var AxeCatchPositionFromHero = a.Position + (myHero.Position - a.Position).Normalized() * Math.Min(Axe.Radius, Extensions.Distance(myHero, a.Position));
                var AxeCatchPositionFromMouse = a.Position + (mousePos - a.Position).Normalized() * Math.Min(Axe.Radius, Extensions.Distance(mousePos, a.Position));
                //var OrbwalkPosition = myHero.Position + (mousePos - a.Position).Normalized() * Axe.Radius;
                var TimeAttack = a.TimeLeft - ((Extensions.Distance(myHero.Position, AxeCatchPositionFromHero) / myHero.MoveSpeed) + (myHero.AttackCastDelay + 0.07f));
                var TimeMoveWithDelay = a.TimeLeft * CatchDelay - (Extensions.Distance(myHero.Position, AxeCatchPositionFromHero) + 110f) / myHero.MoveSpeed;
                var TimeMove = a.TimeLeft - (Extensions.Distance(myHero.Position, AxeCatchPositionFromHero) + 110f) / myHero.MoveSpeed;
                var TimeMove2 = a.TimeLeft - (Extensions.Distance(myHero.Position, AxeCatchPositionFromHero)) / myHero.MoveSpeed;
                var TimeLefts = 0f;
                foreach (Axe a1 in Axes.Where(m => m.TimeLeft < a.TimeLeft && m.SourceInRadius))
                {
                    TimeLefts += a1.TimeLeft;
                }
                if (a.HeroInReticle)
                {
                    CanAttack = true;
                }
                else if (TimeAttack > 0f)
                {
                    CanAttack = true;
                }
                else
                {
                    CanAttack = false;
                }

                if (TimeMoveWithDelay > 0f)
                {
                    CanMove = true;
                }
                else
                {
                    CanMove = false;
                }
                if (TimeLefts > 0f)
                {
                    if (CanAttack)
                    {
                        if (TimeAttack - TimeLefts <= 0)
                        {
                            CanAttack = false;
                        }
                    }
                    if (CanMove)
                    {

                        if (TimeMove - TimeLefts <= 0)
                        {
                            CanMove = false;
                            if (TimeMove2 - TimeLefts <= 0)
                            {
                                if (GetCheckBox(SubMenu["Axes"], "W") && !myHero.HasBuff("dravenfurybuff"))
                                {
                                    if (W.IsReady()) { myHero.Spellbook.CastSpell(W.Slot); }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                CanMove = true;
                CanAttack = true;
            }
            return new Tuple<bool, bool>(CanMove, CanAttack);
        }
        private static void CatchReticles()
        {
            TryingToCatch = false;
            if (Axes.Count > 0)
            {
                if (CanCatch)
                {
                    bool CanMove = true;
                    bool CanAttack = true;
                    foreach (Axe a in Axes)
                    {
                        if (a.InTime)
                        {
                            var status = GetAxeStatus(a);
                            if (!status.Item1) { CanMove = false; }
                            if (!status.Item2) { CanAttack = false; }
                        }
                    }

                    Orbwalker.DisableAttacking = !CanAttack;
                    Orbwalker.DisableMovement = !CanMove;
                    if (!CanMove)
                    {
                        var BestAxe = Axes.Where(m => m.SourceInRadius).OrderBy(m => m.TimeLeft).FirstOrDefault();
                        if (BestAxe != null)
                        {
                            if (Orbwalker.CanMove)
                            {
                                TryingToCatch = true;
                                Orbwalker.DisableMovement = false;
                                if (!BestAxe.MoveSent)
                                {
                                    Player.IssueOrder(GameObjectOrder.MoveTo, BestAxe.Position);
                                    BestAxe.MoveSent = true;
                                }
                                Orbwalker.MoveTo(BestAxe.Position);
                                Orbwalker.DisableMovement = true;
                            }
                        }
                    }
                }
                else
                {
                    Orbwalker.DisableAttacking = false;
                    Orbwalker.DisableMovement = false;
                }
            }
            else
            {
                Orbwalker.DisableAttacking = false;
                Orbwalker.DisableMovement = false;
            }
        }
        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender != null && sender.Name != null)
            {
                if (sender is Obj_GeneralParticleEmitter && sender.Name.ToLower().Contains(myHero.ChampionName.ToLower()))
                {
                    var name = sender.Name.ToLower();
                    if (name.Contains("q_reticle_self.troy"))
                    {
                        AddAxe(sender);
                    }
                    else if (name.Contains("reticlecatchsuccess.troy"))
                    {
                        RemoveAxe(sender);
                    }
                    //Chat.Print("Created " + sender.Name);
                }
                if (sender is MissileClient)
                {
                    var missile = sender as MissileClient;
                    if (missile.SpellCaster.IsMe && missile.SData.Name.ToLower().Contains("dravenspinningreturncatch"))
                    {
                        Axes.Add(new Axe(missile));
                        Core.DelayAction(delegate { RemoveAxe(sender); }, (int)(Axe.LimitTime * 1000 + 600));
                    }
                }
            }
        }
        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (sender != null && sender.Name != null)
            {
                if (sender is Obj_GeneralParticleEmitter && sender.Name.ToLower().Contains(myHero.ChampionName.ToLower()))
                {
                    var name = sender.Name.ToLower();
                    if (name.Contains("q_reticle_self.troy"))
                    {
                        RemoveAxe(sender);
                    }
                    //Chat.Print("Deleted " + sender.Name);
                }
            }
        }
        private static void AddAxe(GameObject sender)
        {

            if (Axes.Count > 0)
            {
                var a = Axes.Where(m => Game.Time - m.StartTime < 0.35f && m.Reticle == null).OrderBy(m => Extensions.Distance(sender.Position.To2D(), m.Missile.EndPosition.To2D(), true)).FirstOrDefault();
                if (a != null)
                {
                    a.AddReticle(sender);
                }
            }
        }
        private static void RemoveAxe(GameObject sender)
        {
            if (Axes.Count > 0)
            {
                foreach (Axe a in Axes.OrderBy(m => Extensions.Distance(m.Position, sender, true)))
                {
                    if (Extensions.Distance(a.Reticle.Position.To2D(), sender.Position.To2D(), true) < 30 * 30)
                    {
                        Axes.Remove(a);
                        break;
                    }
                }
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (myHero.IsDead) { return; }
            if (GetCheckBox(SubMenu["Axes"], "Draw") && CatchEnabled)
            {
                foreach (Axe a in Axes)
                {
                    var color = new ColorBGRA(255, 0, 0, 255);
                    if (a.InTime)
                    {
                        var AxeCatchPositionFromHero = a.Position + (myHero.Position - a.Position).Normalized() * Math.Min(Axe.Radius, Extensions.Distance(myHero, a.Position));
                        var Time = a.TimeLeft - ((Extensions.Distance(myHero.Position, AxeCatchPositionFromHero) / myHero.MoveSpeed) + (myHero.AttackCastDelay));
                        if (Time > 0 || a.HeroInReticle)
                        {
                            color = new ColorBGRA(0, 180, 0, 255);
                        }
                        Circle.Draw(color, CatchRadius, 5, a.Position);
                    }
                    //Circle.Draw(new ColorBGRA(0, 0, 255, 100), 150, a.Missile.Position);
                }
            }
        }

        private static void Gapcloser_OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            if (GetCheckBox(SubMenu["Misc"], "Gapcloser"))
            {
                if (e.Sender.IsEnemy && e.Sender.IsValidTarget())
                {
                    CastE(e.Sender);
                }
            }
        }

        private static void Interrupter_OnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs e)
        {

            if (GetCheckBox(SubMenu["Misc"], "Interrupter"))
            {
                if (e.Sender.IsEnemy && e.Sender.IsValidTarget())
                {
                    CastE(e.Sender);
                }
            }
        }


        static float Damage(Obj_AI_Base target, SpellSlot slot)
        {
            if (target.IsValidTarget())
            {
                if (slot == SpellSlot.W)
                {
                    return myHero.GetAutoAttackDamage(target, true) * 2;
                }
                else if (slot == SpellSlot.E)
                {
                    return myHero.CalculateDamageOnUnit(target, DamageType.Physical, (float)35 * E.Level + 35 + 0.5f * myHero.FlatPhysicalDamageMod);
                }
                else if (slot == SpellSlot.R)
                {
                    return 2 * myHero.CalculateDamageOnUnit(target, DamageType.Physical, (float)100 * R.Level + 75 + 1.1f * myHero.FlatPhysicalDamageMod);
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
        public bool IsKillable(Obj_AI_Base target)
        {
            return this.Damage >= target.Health;
        }
    }
}
