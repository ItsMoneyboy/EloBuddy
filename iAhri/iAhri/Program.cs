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

namespace iAhri
{
    class Program
    {
        private static string Author = "iCreative";
        private static string AddonName = "iAhri";
        private static float RefreshTime = 0.4f;
        private static Dictionary<int, DamageInfo> PredictedDamage = new Dictionary<int, DamageInfo>() { };
        private static AIHeroClient myHero { get { return ObjectManager.Player; } }
        private static Vector3 mousePos { get { return Game.CursorPos; } }
        private static Menu menu;
        private static Dictionary<string, Menu> SubMenu = new Dictionary<string, Menu>() { };
        private static Spell.Skillshot Q, W, E, R;
        private static Spell.Targeted Ignite;
        static float Overkill
        {
            get
            {
                return (float)((100 + SubMenu["Misc"]["Overkill"].Cast<Slider>().CurrentValue) / 100);
            }
        }
        private static Dictionary<string, object> _Q = new Dictionary<string, object>() { { "MinSpeed", 400 }, { "MaxSpeed", 2500 }, { "Acceleration", -3200 }, { "Speed1", 1400 }, { "Delay1", 250 }, { "Range1", 880 }, { "Delay2", 0 }, { "Range2", int.MaxValue }, { "IsReturning", false }, { "Target", null }, { "Object", null }, { "LastObjectVector", null }, { "LastObjectVectorTime", null }, { "CatchPosition", null } };
        private static Dictionary<string, object> _E = new Dictionary<string, object>() { { "LastCastTime", 0f }, { "Object", null }, };
        private static Dictionary<string, object> _R = new Dictionary<string, object>() { { "EndTime", 0f }, };
        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += OnLoad;
        }
        private static void OnLoad(EventArgs args)
        {
            if (myHero.Hero != Champion.Ahri) { return; }
            Chat.Print(AddonName + " made by " + Author + " loaded, have fun!.");
            Q = new Spell.Skillshot(SpellSlot.Q, 880, SkillShotType.Linear, 250, 1500, 100);
            Q.AllowedCollisionCount = int.MaxValue;
            W = new Spell.Skillshot(SpellSlot.W, 600, SkillShotType.Circular, 0, 1400, 300);
            W.AllowedCollisionCount = int.MaxValue;
            E = new Spell.Skillshot(SpellSlot.E, 975, SkillShotType.Linear, 250, 1550, 60);
            E.AllowedCollisionCount = 0;
            R = new Spell.Skillshot(SpellSlot.R, 800, SkillShotType.Circular, 0, 1400, 300);
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
            SubMenu["Combo"].Add("R", new CheckBox("Use R", true));
            SubMenu["Combo"].Add("CatchQR", new CheckBox("Catch the Q with R", true));
            SubMenu["Combo"].Add("CatchQRPriority", new CheckBox("Give Priority to Catch the Q with R", true));

            SubMenu["Harass"] = menu.AddSubMenu("Harass", "Harass");
            SubMenu["Harass"].Add("Q", new CheckBox("Use Q", true));
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
            SubMenu["Flee"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["Flee"].Add("R", new CheckBox("Use R", true));

            SubMenu["Draw"] = menu.AddSubMenu("Drawing", "Drawing");
            SubMenu["Draw"].Add("Line", new CheckBox("Draw line for Q orb", true));

            SubMenu["Misc"] = menu.AddSubMenu("Misc", "Misc");
            SubMenu["Misc"].Add("Overkill", new Slider("Overkill % for damage prediction", 10, 0, 100));
            SubMenu["Misc"].Add("CatchQMovement", new CheckBox("Catch the Q with movement", false));
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

        public static bool IsWall(Vector3 v)
        {
            var v2 = v.To2D();
            return NavMesh.GetCollisionFlags(v2.X, v2.Y).HasFlag(CollisionFlags.Wall);
        }

        static void OnTick(EventArgs args)
        {
            if (myHero.IsDead) { return; }
            if (_Q["Object"] != null)
            {
                //Q.Range = (uint)_Q["Range2"];
                Q.CastDelay = (int)_Q["Delay2"];
                Q.SourcePosition = ((GameObject)_Q["Object"]).Position;
                if (_Q["LastObjectVector"] != null)
                {
                    Q.Speed = (int)(Extensions.Distance((Vector3)Q.SourcePosition, (Vector3)_Q["LastObjectVector"])
                        / (Game.Time - (float)_Q["LastObjectVectorTime"]));
                }
                _Q["LastObjectVector"] = new Vector3(((Vector3)Q.SourcePosition).X, ((Vector3)Q.SourcePosition).Y, ((Vector3)Q.SourcePosition).Z);
                _Q["LastObjectVectorTime"] = Game.Time;
            }
            else
            {
                //Q.Range = (float)_Q["Range1"];
                Q.CastDelay = (int)_Q["Delay1"];
                Q.Speed = (int)_Q["Speed1"];
                Q.SourcePosition = myHero.Position;
            }
            CatchQ();
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

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee))
            {
                Flee();
            }
        }

        static void KillSteal()
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
        static void Combo()
        {
            var target = TargetSelector.GetTarget(E.Range, EloBuddy.DamageType.Magical);
            if (target.IsValidTarget())
            {
                if (SubMenu["Combo"]["R"].Cast<CheckBox>().CurrentValue) { CastR(target); }
                if (SubMenu["Combo"]["E"].Cast<CheckBox>().CurrentValue) { CastE(target); }
                if ((Game.Time - (float)_E["LastCastTime"] <= (float)(E.CastDelay / 1000 * 1.1)) || (_E["Object"] != null && myHero.Position.Distance(target.Position) > myHero.Position.Distance(((GameObject)_E["Object"]).Position)))
                {
                    return;
                }
                if (SubMenu["Combo"]["Q"].Cast<CheckBox>().CurrentValue) { CastQ(target); }
                if (SubMenu["Combo"]["W"].Cast<CheckBox>().CurrentValue) { CastW(target); }
            }
        }

        static void Harass()
        {
            var target = TargetSelector.GetTarget(E.Range, EloBuddy.DamageType.Magical);
            if (target.IsValidTarget() && myHero.ManaPercent >= SubMenu["Harass"]["Mana"].Cast<Slider>().CurrentValue)
            {
                if (SubMenu["Harass"]["E"].Cast<CheckBox>().CurrentValue) { CastE(target); }
                if ((Game.Time - (float)_E["LastCastTime"] <= (float)(E.CastDelay / 1000 * 1.1)) || (_E["Object"] != null && myHero.Position.Distance(target.Position) > myHero.Position.Distance(((GameObject)_E["Object"]).Position)))
                {
                    return;
                }
                if (SubMenu["Harass"]["Q"].Cast<CheckBox>().CurrentValue) { CastQ(target); }
                if (SubMenu["Harass"]["W"].Cast<CheckBox>().CurrentValue) { CastW(target); }
            }
        }
        static void Flee()
        {
            if (SubMenu["Flee"]["R"].Cast<CheckBox>().CurrentValue && R.IsReady())
            {
                R.Cast(mousePos);
            }
            if (SubMenu["Flee"]["Q"].Cast<CheckBox>().CurrentValue && Q.IsReady())
            {
                Q.Cast(mousePos);
            }
        }

        static void JungleClear()
        {
            if (myHero.ManaPercent >= SubMenu["JungleClear"]["Mana"].Cast<Slider>().CurrentValue)
            {
                foreach (Obj_AI_Base minion in EntityManager.MinionsAndMonsters.GetJungleMonsters(myHero.Position, 1000f))
                {
                    if (minion.IsValidTarget() && myHero.ManaPercent >= SubMenu["JungleClear"]["Mana"].Cast<Slider>().CurrentValue)
                    {
                        if (SubMenu["JungleClear"]["E"].Cast<CheckBox>().CurrentValue) { CastE(minion); }
                        if ((Game.Time - (float)_E["LastCastTime"] <= (float)(E.CastDelay / 1000 * 1.1)) || (_E["Object"] != null && myHero.Position.Distance(minion.Position) > myHero.Position.Distance(((GameObject)_E["Object"]).Position)))
                        {
                            return;
                        }
                        if (SubMenu["JungleClear"]["Q"].Cast<CheckBox>().CurrentValue) { CastQ(minion); }
                        if (SubMenu["JungleClear"]["W"].Cast<CheckBox>().CurrentValue) { CastW(minion); }
                    }
                }

            }
        }
        static void CastQ(Obj_AI_Base target)
        {
            if (Q.IsReady() && target.IsValidTarget())
            {
                int maxspeed = (int)_Q["MaxSpeed"];
                int acc = (int)_Q["Acceleration"];
                var speed = Math.Sqrt(Math.Pow(maxspeed, 2) + 2 * acc * Extensions.Distance(myHero, target));
                var tf = (speed - maxspeed) / acc;
                Q.Speed = (int)(Extensions.Distance(myHero, target) / tf);
                var r = Q.GetPrediction(target);
                if (r.HitChance >= HitChance.High)
                {
                    Q.Cast(r.CastPosition);
                    _Q["Target"] = target;
                }
            }
        }

        static void CastW(Obj_AI_Base target)
        {
            if (W.IsReady() && target.IsValidTarget())
            {
                var r = W.GetPrediction(target);
                if (r.HitChance >= HitChance.High)
                {
                    if (target.Type == myHero.Type)
                    {
                        if (_Q["Object"] != null || Orbwalker.LastTarget.NetworkId == target.NetworkId)
                        {
                            myHero.Spellbook.CastSpell(W.Slot);
                        }
                    }
                    else
                    {
                        myHero.Spellbook.CastSpell(W.Slot);
                    }
                }
            }
        }
        static void CastE(Obj_AI_Base target)
        {
            if (E.IsReady() && target.IsValidTarget())
            {
                var r = E.GetPrediction(target);
                if (r.HitChance >= HitChance.High && target.IsEnemy)
                {
                    E.Cast(r.CastPosition);
                }
            }
        }

        static void CastR(Obj_AI_Base target)
        {
            if (R.IsReady() && target.IsValidTarget())
            {
                var damageI = GetBestCombo(target);
                if (SubMenu["Combo"]["CatchQRPriority"].Cast<CheckBox>().CurrentValue)
                {
                    if ((float)_R["EndTime"] > ((float)0))
                    { //have active r
                        if (_Q["Object"] != null)
                        {
                            if ((bool)_Q["IsReturning"] && Extensions.Distance(myHero, (GameObject)_Q["Object"]) < Extensions.Distance(myHero, (Obj_AI_Base)_Q["Target"]))
                            {
                                R.Cast(mousePos);
                            }
                            else
                            {
                                return;
                            }
                        }
                        if (!Q.IsReady() && (float)_R["EndTime"] - Game.Time <= myHero.Spellbook.GetSpell(R.Slot).Cooldown)
                        {
                            R.Cast(mousePos);
                        }
                    }
                    if (damageI.Damage >= target.Health && Extensions.Distance(mousePos, target) < Extensions.Distance(myHero, target))
                    {
                        if (damageI.R)
                        {
                            if (Extensions.Distance(myHero, target) > 400)
                            {
                                R.Cast(mousePos);
                            }

                        }
                    }
                }
                else
                {
                    if ((float)_R["EndTime"] > ((float)0))
                    {
                        if (!Q.IsReady() && (float)_R["EndTime"] - Game.Time <= myHero.Spellbook.GetSpell(R.Slot).Cooldown)
                        {
                            R.Cast(mousePos);
                        }
                    }
                    if (damageI.Damage >= target.Health && Extensions.Distance(mousePos, target) < Extensions.Distance(myHero, target))
                    {
                        if (damageI.R)
                        {
                            if (Extensions.Distance(myHero, target) > 400)
                            {
                                R.Cast(mousePos);
                            }

                        }
                    }
                }
            }
        }

        static void CatchQ()
        {

            if (_Q["Object"] != null)
            {
                _Q["CatchPosition"] = null;
                var target = _Q["Target"] != null ? (Obj_AI_Base)_Q["Target"] : TargetSelector.GetTarget(Q.Range, EloBuddy.DamageType.Magical);
                if (target != null && target.IsValidTarget())
                {

                    var r = Q.GetPrediction(target);
                    if (Extensions.Distance(myHero, r.CastPosition) <= Extensions.Distance(myHero, (GameObject)_Q["Object"]))
                    {
                        //Chat.Print("2");
                        var TimeLeft = Extensions.Distance(myHero, target) / Q.Speed;
                        var qObject = ((GameObject)_Q["Object"]).Position;
                        var ExtendedPos = qObject + (r.CastPosition - qObject).Normalized() * 1500;
                        var ClosestToTargetLine = myHero.Position.To2D().ProjectOn(qObject.To2D(), ExtendedPos.To2D());
                        var ClosestToHeroLine = r.CastPosition.To2D().ProjectOn(qObject.To2D(), myHero.Position.To2D());
                        if (ClosestToTargetLine.IsOnSegment && ClosestToHeroLine.IsOnSegment && ClosestToTargetLine.SegmentPoint.Distance(qObject.To2D()) < r.CastPosition.To2D().Distance(qObject.To2D()))
                        {
                            //Chat.Print("3");
                            if (ClosestToTargetLine.SegmentPoint.Distance(myHero.Position.To2D()) < myHero.MoveSpeed * TimeLeft)
                            {
                                if (SubMenu["Misc"]["CatchQMovement"].Cast<CheckBox>().CurrentValue)
                                {
                                    //Chat.Print("4");
                                    if (ClosestToHeroLine.SegmentPoint.Distance(r.CastPosition.To2D()) > Q.Width)
                                    {
                                        Orbwalker.OrbwalkTo(ClosestToTargetLine.SegmentPoint.To3D());
                                    }
                                }
                            }
                            else if (ClosestToTargetLine.SegmentPoint.Distance(myHero.Position.To2D()) < 450 + myHero.MoveSpeed * TimeLeft)
                            {
                                if (SubMenu["Combo"]["CatchQR"].Cast<CheckBox>().CurrentValue && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                                {
                                    if (ClosestToHeroLine.SegmentPoint.Distance(r.CastPosition.To2D()) > Q.Width)
                                    {
                                        var rPos = myHero.Position + (ClosestToTargetLine.SegmentPoint.To3D() - myHero.Position).Normalized() * Extensions.Distance(myHero, ClosestToTargetLine.SegmentPoint.To3D()) * 1.2f;
                                        R.Cast(rPos);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        static void OnCreateObj(EloBuddy.GameObject sender, EventArgs args)
        {
            if (sender.Name.ToLower().Contains("missile"))
            {
                var missile = (MissileClient)sender;
                if (missile == null || !missile.IsValid || missile.SpellCaster == null || !missile.SpellCaster.IsValid)
                {
                    return;
                }
                var unit = (Obj_AI_Base)missile.SpellCaster;
                if (missile.SpellCaster.IsMe)
                {
                    var name = missile.SData.Name.ToLower();
                    if (name.Contains("ahriorbmissile"))
                    {
                        _Q["Object"] = sender;
                        _Q["IsReturning"] = false;
                    }
                    else if (name.Contains("ahriorbreturn"))
                    {
                        _Q["Object"] = sender;
                        _Q["IsReturning"] = true;
                    }
                    else if (name.Contains("ahriseducemissile"))
                    {
                        _E["Object"] = sender;
                    }
                }
            }
        }
        static void OnDeleteObj(GameObject sender, EventArgs args)
        {
            if (sender.Name.ToLower().Contains("missile"))
            {
                var missile = (MissileClient)sender;
                if (missile == null || !missile.IsValid || missile.SpellCaster == null || !missile.SpellCaster.IsValid)
                {
                    return;
                }
                var unit = (Obj_AI_Base)missile.SpellCaster;
                if (missile.SpellCaster.IsMe)
                {
                    var name = missile.SData.Name.ToLower();
                    if (name.Contains("ahriorbreturn"))
                    {
                        _Q["Object"] = null;
                        _Q["IsReturning"] = false;
                        _Q["Target"] = null;
                        _Q["LastObjectVector"] = null;
                    }
                    else if (name.Contains("ahriseducemissile"))
                    {
                        _E["Object"] = null;
                    }
                }
            }
        }

        static void OnDraw(EventArgs args)
        {
            if (myHero.IsDead) { return; }
            if (_Q["Object"] != null && SubMenu["Draw"]["Line"].Cast<CheckBox>().CurrentValue)
            {
                var asd = (GameObject)_Q["Object"];
                var p1 = Drawing.WorldToScreen(myHero.Position);
                var p2 = Drawing.WorldToScreen(asd.Position);
                Drawing.DrawLine(p1, p2, Q.Width, System.Drawing.Color.FromArgb(100, 255, 255, 255));
            }
        }

        static void OnGapCloser(Obj_AI_Base sender, Gapcloser.GapcloserEventArgs args)
        {
            if (SubMenu["Misc"]["Gapclose"].Cast<CheckBox>().CurrentValue)
            {
                CastE(args.Sender);
            }
        }

        static void OnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs args)
        {
            if (SubMenu["Misc"]["Channeling"].Cast<CheckBox>().CurrentValue)
            {
                CastE(args.Sender);
            }
        }

        static void OnProcessSpell(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.SData.Name.ToLower().Contains(myHero.Spellbook.GetSpell(SpellSlot.Q).SData.Name.ToLower()))
                {
                    _Q["IsReturning"] = false;
                    _Q["Object"] = null;
                }
                else if (args.SData.Name.ToLower().Contains(myHero.Spellbook.GetSpell(SpellSlot.W).SData.Name.ToLower()))
                {
                    _E["Object"] = null;
                    _E["LastCastTime"] = Game.Time;
                }
                else if (args.SData.Name.ToLower().Contains(myHero.Spellbook.GetSpell(SpellSlot.R).SData.Name.ToLower()))
                {

                }
            }
        }
        static void OnApplyBuff(Obj_AI_Base sender, Obj_AI_BaseBuffGainEventArgs args)
        {
            if (sender.IsMe)
            {
                var buff = args.Buff;
                if (buff.Name.ToLower() == "ahritumble")
                {
                    _R["EndTime"] = (float)(Game.Time + buff.EndTime - buff.StartTime);
                }
            }
        }

        static void OnRemoveBuff(Obj_AI_Base sender, Obj_AI_BaseBuffLoseEventArgs args)
        {
            if (sender.IsMe)
            {
                var buff = args.Buff;
                if (buff.Name.ToLower() == "ahritumble")
                {
                    _R["EndTime"] = (float)0f;
                }
            }
        }

        static Obj_AI_Base LastHit(Spell.SpellBase spell)
        {
            return null;
        }

        static float Damage(Obj_AI_Base target, SpellSlot slot)
        {
            if (target.IsValidTarget())
            {
                if (slot == SpellSlot.Q)
                {
                    return myHero.CalculateDamageOnUnit(target, DamageType.Magical, (float)25 * Q.Level + 15 + 0.35f * myHero.FlatMagicDamageMod) + myHero.CalculateDamageOnUnit(target, DamageType.True, (float)25 * Q.Level + 15 + 0.35f * myHero.FlatMagicDamageMod);
                }
                else if (slot == SpellSlot.W)
                {
                    return 1.6f * myHero.CalculateDamageOnUnit(target, DamageType.Magical, (float)25 * W.Level + 15 + 0.4f * myHero.FlatMagicDamageMod);
                }
                else if (slot == SpellSlot.E)
                {
                    return myHero.CalculateDamageOnUnit(target, DamageType.Magical, (float)35 * E.Level + 25 + 0.5f * myHero.FlatMagicDamageMod);
                }
                else if (slot == SpellSlot.R)
                {
                    return 3 * myHero.CalculateDamageOnUnit(target, DamageType.Magical, (float)40 * R.Level + 30 + 0.3f * myHero.FlatMagicDamageMod);
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
}
