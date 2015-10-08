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
        static AIHeroClient myHero { get { return ObjectManager.Player; } }
        static Vector3 mousePos { get { return Game.CursorPos; } }
        static Menu menu;
        static Dictionary<string, Menu> SubMenu = new Dictionary<string, Menu>() { };
        static Spell.Skillshot E, R;
        static Spell.Active Q, W;
        static Spell.Targeted Ignite;
        static float LimitTime = 1.2f;
        static List<Axe> Axes = new List<Axe>();
        //hacer bool cancatch
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
        static bool CanCatch { get { return GetKeyBind(SubMenu["Axes"], "Catch") && ((GetSlider(SubMenu["Axes"], "CatchMode") == 0 && !IsNone) || GetSlider(SubMenu["Axes"], "CatchMode") == 1); } }
        static float CatchDelay { get { return GetSlider(SubMenu["Axes"], "Delay") / 100.0f; } }
        static float CatchRadius
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
        static Vector3 CatchSource { get { return GetSlider(SubMenu["Axes"], "OrbwalkMode") == 1 ? mousePos : myHero.Position; } }
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
            menu = MainMenu.AddMenu(AddonName, AddonName + " by " + Author + " v1.0");
            menu.AddLabel(AddonName + " made by " + Author);

            SubMenu["Axes"] = menu.AddSubMenu("Axes", "Axes");
            SubMenu["Axes"].AddGroupLabel("Keys");
            SubMenu["Axes"].Add("Catch", new KeyBind("Catch Axes (Toggle)", true, KeyBind.BindTypes.PressToggle, (uint)'Z'));
            SubMenu["Axes"].AddGroupLabel("Settings");
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

            SubMenu["Prediction"] = menu.AddSubMenu("Prediction", "Prediction");
            SubMenu["Prediction"].AddGroupLabel("E Settings");
            SubMenu["Prediction"].Add("ECombo", new Slider("Combo HitChancePercent", 45, 0, 100));
            SubMenu["Prediction"].Add("EHarass", new Slider("Harass HitChancePercent", 60, 0, 100));

            SubMenu["Combo"] = menu.AddSubMenu("Combo", "Combo");
            SubMenu["Combo"].Add("Q", new Slider("Use Q to have X spinning axes", 2, 0, 2));
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
            SubMenu["KillSteal"].Add("R", new CheckBox("Use R", false));
            SubMenu["KillSteal"].Add("Ignite", new CheckBox("Use Ignite", true));

            SubMenu["Flee"] = menu.AddSubMenu("Flee", "Flee");
            SubMenu["Flee"].Add("W", new CheckBox("Use W", true));
            SubMenu["Flee"].Add("E", new CheckBox("Use E", true));

            SubMenu["Misc"] = menu.AddSubMenu("Misc", "Misc");
            SubMenu["Misc"].Add("Overkill", new Slider("Overkill % for damage prediction", 10, 0, 100));
            SubMenu["Misc"].Add("GapCloser", new CheckBox("Use E to Interrupt GapClosers", true));
            SubMenu["Misc"].Add("Interrupter", new CheckBox("Use E to Interrupt Channeling Spells", true));

            Game.OnTick += Game_OnTick;
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
            Drawing.OnDraw += Drawing_OnDraw;
            Obj_AI_Base.OnBasicAttack += Obj_AI_Base_OnBasicAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Interrupter.OnInterruptableSpell += Interrupter_OnInterruptableSpell;
            Gapcloser.OnGapcloser += Gapcloser_OnGapcloser; ;
        }


        private static void Game_OnTick(EventArgs args)
        {

        }
        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender != null && sender.Name != null)
            {
                if (sender.Name.ToLower().Contains(myHero.ChampionName.ToLower()))
                {
                    var name = sender.Name.ToLower();
                    if (name.Contains("q_reticle_self.troy"))
                    {
                        Axes.Add(new Axe(sender, Game.Time - Game.Ping / 2000f));
                        //Core.DelayAction(delegate { RemoveAxe(sender); }, (int)(LimitTime * 1000 + 200));
                    }
                    else if (name.Contains("reticlecatchsuccess.troy"))
                    {
                        RemoveAxe(sender);
                    }
                    else if (name.Contains("q_tar.troy"))
                    {
                        //missile object
                    }
                    Chat.Print("Created " + sender.Name);
                }
            }
        }
        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (sender != null && sender.Name != null)
            {
                if (sender.Name.ToLower().Contains(myHero.ChampionName.ToLower()))
                {
                    var name = sender.Name.ToLower();
                    if (name.Contains("q_reticle_self.troy"))
                    {
                        RemoveAxe(sender);
                    }
                    Chat.Print("Deleted " + sender.Name);
                }
            }
        }
        private static void RemoveAxe(GameObject sender)
        {
            if (Axes.Count > 0)
            {
                foreach (Axe a in Axes)
                {
                    if (Extensions.Distance(a.Reticle, sender, true) < 900)
                    {
                        Axes.Remove(a);
                        break;
                    }
                }
            }
        }
        private static void Obj_AI_Base_OnBasicAttack(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {

        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (myHero.IsDead) { return; }
            if (GetCheckBox(SubMenu["Axes"], "Draw") && CanCatch)
            {
                foreach (Axe a in Axes)
                {
                    Circle.Draw(new ColorBGRA(0, 0, 255, 100), CatchRadius, a.Position);
                }
            }
        }

        private static void Gapcloser_OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {

        }

        private static void Interrupter_OnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs e)
        {

        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {

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
                return (float)((100 + SubMenu["Misc"]["Overkill"].Cast<Slider>().CurrentValue) / 100);
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
    public class Axe
    {
        public GameObject Reticle;
        public GameObject Missile;
        public float StartTime;
        public Vector3 Position
        {
            get { return this.Reticle.Position; }
        }
        public Axe(GameObject o, float s)
        {
            this.Reticle = o;
            this.StartTime = s;
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
