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
    public static class WardManager
    {
        public static float WardRange = 600f;
        public static Item[] ItemWards = new Item[] { new Item((int)ItemId.Ruby_Sightstone, WardRange), new Item((int)ItemId.Sightstone, WardRange), new Item((int)ItemId.Warding_Totem_Trinket, WardRange), new Item((int)ItemId.Stealth_Ward, WardRange), new Item((int)ItemId.Vision_Ward, WardRange) };
        public static List<Obj_AI_Minion> WardsAvailable = new List<Obj_AI_Minion>();
        public static Vector3 LastWardJumpVector = Vector3.Zero;
        public static float LastWardJumpTime = 0f;
        public static void Init()
        {
            Game.OnTick += Game_OnTick;

            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;

            GameObject.OnCreate += Obj_Ward_OnCreate;
            GameObject.OnDelete += Obj_Ward_OnDelete;
            WardsAvailable = ObjectManager.Get<Obj_AI_Minion>().Where(m => m.IsWard() && m.IsValid && !m.IsDead).ToList();
        }
        public static void CastWardTo(Vector3 Position)
        {
            if (SpellManager.CanCastW1 && CanCastWard)
            {
                Vector3 EndPos = Util.myHero.Position + (Position - Util.myHero.Position).Normalized() * Math.Min(WardRange, Extensions.Distance(Util.myHero, Position));
                Item ward = GetItem;
                if (ward != null)
                {
                    ward.Cast(EndPos);
                    LastWardJumpVector = EndPos;
                    LastWardJumpTime = Game.Time;
                }
            }
        }
        public static void JumpToVector(Vector3 Position)
        {
            if (SpellManager.CanCastW1)
            {
                var ward = GetNearestTo(Position) as Obj_AI_Minion;
                if (ward != null && Extensions.Distance(Position.To2D(), ward.Position.To2D(), true) < Math.Pow(250f, 2))
                {
                    JumpToWard(ward);
                }
            }
        }
        public static void JumpToWard(Obj_AI_Minion ward)
        {
            if (SpellManager.CanCastW1)
            {
                Util.myHero.Spellbook.CastSpell(SpellSlot.W, ward);
            }
        }
        private static void Game_OnTick(EventArgs args)
        {
            if (IsTryingToJump)
            {
                JumpToVector(LastWardJumpVector);
            }
        }
        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (args.SData.Name.Equals(SpellSlot.W.GetSpellDataInst().SData.Name) && args.SData.Name.ToLower().Contains("one"))
            {
                LastWardJumpVector = Vector3.Zero;
            }
        }
        public static bool IsWard(this GameObject sender)
        {
            return sender is Obj_AI_Minion && (sender.Name.ToLower().Contains("sightward") || sender.Name.ToLower().Contains("visionward")) && sender.Team == Util.myHero.Team;
        }
        private static void Obj_Ward_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender.IsWard())
            {
                var ward = sender as Obj_AI_Minion;
                WardsAvailable.Add(ward);
                if (IsTryingToJump)
                {
                    if (Extensions.Distance(LastWardJumpVector.To2D(), ward.Position.To2D(), true) < Math.Pow(80, 2))
                    {
                        JumpToWard(ward);
                    }
                }
            }
        }
        private static void Obj_Ward_OnDelete(GameObject sender, EventArgs args)
        {
            if (sender.IsWard())
            {
                var ward = sender as Obj_AI_Minion;
                var ward2 = WardsAvailable.Where(m => m.NetworkId == ward.NetworkId).FirstOrDefault();
                if (ward2 != null)
                {
                    WardsAvailable.Remove(ward2);
                }
            }
        }
        public static Obj_AI_Minion GetNearestTo(Vector3 position)
        {
            return WardsAvailable.Where(m => m.IsValid && !m.IsDead && Extensions.Distance(Util.myHero, m, true) <= Math.Pow(SpellManager.W_Range + SpellManager.W_ExtraRange, 2)).OrderBy(m => Extensions.Distance(Util.myHero, position, true)).FirstOrDefault();
        }
        public static Obj_AI_Minion GetFurthestTo(Vector3 position)
        {
            return WardsAvailable.Where(m => m.IsValid && !m.IsDead && Extensions.Distance(Util.myHero, m, true) <= Math.Pow(SpellManager.W_Range + SpellManager.W_ExtraRange, 2)).OrderBy(m => Extensions.Distance(Util.myHero, position, true)).LastOrDefault();
        }
        public static bool IsTryingToJump
        {
            get
            {
                return LastWardJumpVector != Vector3.Zero && Game.Time - LastWardJumpTime < 1.25f;
            }
        }
        public static bool CanCastWard
        {
            get
            {
                return Game.Time - LastWardJumpTime > 1.25f && IsReady;
            }
        }
        public static bool IsReady
        {
            get
            {
                foreach (Item i in ItemWards)
                {
                    if (i.IsReady())
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public static Item GetItem
        {
            get
            {
                foreach (Item i in ItemWards)
                {
                    if (i.IsReady())
                    {
                        return i;
                    }
                }
                return null;
            }
        }

    }
}
