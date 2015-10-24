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



namespace Template
{
    public class Ball
    {
        public Obj_AI_Minion Object = null;
        public string LastAnimation = "Idle1";
        public Ball(GameObject obj)
        {
            Object = obj as Obj_AI_Minion;
        }
        public Vector3 Position
        {
            get
            {
                if (ObjectIsValid)
                {
                    return Object.Position;
                }
                return Vector3.Zero;
            }
        }
        public bool ObjectIsValid
        {
            get
            {
                return Object != null && Object.IsValid && !Object.IsDead;
            }
        }
        public bool IsIdle
        {
            get
            {
                return ObjectIsValid && LastAnimation.ToLower().Contains("idle") && Object.IsTargetable;
            }
        }
        public bool IsWObject
        {
            get
            {
                return ObjectIsValid && !Object.IsTargetable;
            }
        }
        public bool IsOnETime
        {
            get
            {
                return ObjectIsValid && Game.Time - SpellManager.E_LastCastTime <= SpellManager.E.CastDelay / 1000 + 1.5f * Extensions.Distance(Util.MyHero, Position) / SpellManager.E.Speed;
            }
        }
        public bool IsOnERange
        {
            get
            {
                return ObjectIsValid && Extensions.Distance(Util.MyHero, Position, true) <= Math.Pow(SpellManager.E.Range + SpellManager.E_ExtraWidth, 2);
            }
        }
        public Vector3 E_EndPosition
        {
            get
            {
                return Util.MyHero.Position + (Position - Util.MyHero.Position).Normalized() * ((Extensions.Distance(Util.MyHero, Position, true) >= Math.Pow(200, 2)) ? SpellManager.QE.Range : 1000);
            }
        }
    }
}