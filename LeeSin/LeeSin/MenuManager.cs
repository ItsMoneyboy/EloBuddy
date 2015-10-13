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
    public static class MenuManager
    {
        public static Menu AddonMenu;
        public static Dictionary<string, Menu> SubMenu = new Dictionary<string, Menu>() { };
        public static void Init()
        {
            var AddonName = Champion.AddonName;
            var Author = Champion.Author;
            AddonMenu = MainMenu.AddMenu(AddonName, AddonName + " by " + Author + " v1.0");
            AddonMenu.AddLabel(AddonName + " made by " + Author);

            SubMenu["Prediction"] = AddonMenu.AddSubMenu("Prediction", "Prediction");
            SubMenu["Prediction"].AddGroupLabel("Q Settings");
            SubMenu["Prediction"].Add("QCombo", new Slider("Combo HitChancePercent", 60, 0, 100));
            SubMenu["Prediction"].Add("QHarass", new Slider("Harass HitChancePercent", 70, 0, 100));

            //Combo
            SubMenu["Combo"] = AddonMenu.AddSubMenu("Combo", "Combo");
            SubMenu["Combo"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["Combo"].Add("W", new CheckBox("Use W", true));
            SubMenu["Combo"].Add("E", new CheckBox("Use E", true));
            SubMenu["Combo"].Add("Items", new CheckBox("Use Offensive Items", true));
            SubMenu["Combo"].Add("Ward", new CheckBox("Use Ward", false));
            SubMenu["Combo"].Add("Stack", new Slider("Use x passive before using another spell", 1, 0, 2));
            SubMenu["Combo"].AddStringList("StarMode", "Star Combo Mode", new[] { "Q1 R Q2", "R Q1 Q2" }, 0);
            SubMenu["Combo"].AddStringList("Mode", "Combo Mode", new[] { "Without R", "Star Combo", "Gank Combo" }, 0);

            //Insec
            SubMenu["Insec"] = AddonMenu.AddSubMenu("Insec", "Insec");
            SubMenu["Insec"].Add("Key", new KeyBind("Insec Key", false, EloBuddy.SDK.Menu.Values.KeyBind.BindTypes.HoldActive, (uint)'T'));
            SubMenu["Combo"].AddStringList("Priority", "Priority", new[] { "WardJump > Flash", "Flash > WardJump" }, 0);
            SubMenu["Combo"].AddStringList("Position", "Insec End Position", new[] { "Turrets and Allies", "Mouse Position", "Current Position", "Clicked Position" }, 0);
            SubMenu["Insec"].Add("DistanceBetweenPercent", new Slider("% of distance between ward an target", 20, 0, 100));
            SubMenu["Insec"].Add("Flash", new CheckBox("Use flash to return", false));

            SubMenu["Drawings"] = AddonMenu.AddSubMenu("Drawings", "Drawings");

            SubMenu["Flee"] = AddonMenu.AddSubMenu("Flee", "Flee");
            SubMenu["Flee"].Add("WardJump", new CheckBox("Use WardJump", true));
        }
        public static int GetSliderValue(this Menu m, string s)
        {
            return m[s].Cast<Slider>().CurrentValue;
        }
        public static bool GetCheckBoxValue(this Menu m, string s)
        {
            return m[s].Cast<CheckBox>().CurrentValue;
        }
        public static bool GetKeyBindValue(this Menu m, string s)
        {
            return m[s].Cast<KeyBind>().CurrentValue;
        }
        public static void AddStringList(this Menu m, string uniqueID, string DisplayName, string[] values, int defaultValue)
        {
            m.AddGroupLabel(DisplayName);
            var mode = m.Add(uniqueID, new Slider(DisplayName, defaultValue, 0, values.Length - 1));
            mode.DisplayName = values[mode.CurrentValue];
            mode.OnValueChange += delegate (ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
            {
                sender.DisplayName = values[args.NewValue];
            };
        }
        public static Menu GetSubMenu(string s)
        {
            return SubMenu[s];
        }
        public static Menu InsecMenu
        {
            get
            {
                return GetSubMenu("Insec");
            }
        }
        public static Menu FleeMenu
        {
            get
            {
                return GetSubMenu("Flee");
            }
        }
        public static Menu ComboMenu
        {
            get
            {
                return GetSubMenu("Combo");
            }
        }
        public static Menu HarassMenu
        {
            get
            {
                return GetSubMenu("Harass");
            }
        }
        public static Menu MiscMenu
        {
            get
            {
                return GetSubMenu("Misc");
            }
        }
        public static Menu PredictionMenu
        {
            get
            {
                return GetSubMenu("Prediction");
            }
        }
        public static Menu DrawingsMenu
        {
            get
            {
                return GetSubMenu("Drawings");
            }
        }
    }
}
