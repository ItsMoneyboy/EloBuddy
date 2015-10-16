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
    public class DamageResult
    {
        public bool Q = false;
        public bool W = false;
        public bool E = false;
        public bool R = false;
        public float Damage = 0f;
        public float Mana = 0f;
        public float Time = 0f;
        public Obj_AI_Base Target = null;

        public DamageResult(Obj_AI_Base target, float Damage, float Mana, bool Q, bool W, bool E, bool R, float Time)
        {
            this.Q = Q;
            this.W = W;
            this.E = E;
            this.R = R;
            this.Damage = Damage;
            this.Mana = Mana;
            this.Time = Time;
        }
        public DamageResult(float Damage, float Mana)
        {
            this.Damage = Damage;
            this.Mana = Mana;
        }
        public bool IsKillable
        {
            get
            {
                return Target.Health <= Damage;
            }
        }
    }
}
