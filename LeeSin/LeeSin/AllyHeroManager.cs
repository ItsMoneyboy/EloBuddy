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



namespace LeeSin
{
    public static class AllyHeroManager
    {
        public static Obj_AI_Base GetNearestTo(Vector3 position)
        {
            return EntityManager.Heroes.Allies.Where(m => m.IsValid && !m.IsDead && !m.IsMe && Extensions.Distance(Util.myHero, m, true) <= Math.Pow(SpellManager.W1_Range + SpellManager.W_ExtraRange, 2)).OrderBy(m => Extensions.Distance(m, position, true)).FirstOrDefault();
        }
        public static Obj_AI_Base GetFurthestTo(Vector3 position)
        {
            return EntityManager.Heroes.Allies.Where(m => m.IsValid && !m.IsDead && !m.IsMe && Extensions.Distance(Util.myHero, m, true) <= Math.Pow(SpellManager.W1_Range + SpellManager.W_ExtraRange, 2)).OrderBy(m => Extensions.Distance(m, position, true)).LastOrDefault();
        }
    }
}
