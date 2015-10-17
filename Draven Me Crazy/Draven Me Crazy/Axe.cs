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


namespace Draven_Me_Crazy
{

    public class Axe
    {
        public GameObject Reticle = null;
        public MissileClient Missile = null;
        public float StartTime;
        public bool MoveSent = false;
        public static float LimitTime = 1.20f;
        public static float Radius = 100f;
        public float Gravity
        {
            get
            {
                if (this.Missile != null)
                    return this.Missile.SData.MissileGravity;
                return 26;
            }
        }
        public float Speed
        {
            get
            {
                if (this.Missile != null)
                    return this.Missile.SData.MissileSpeed;
                return 700;
            }
        }
        public float TimeLeft
        {
            get
            {
                if (this.Missile != null)
                    return LimitTime - (Game.Time - this.StartTime);//2 * Extensions.Distance(this.Reticle.Position, this.Missile.Position) / this.Speed; //
                return float.MaxValue;
            }
        }
        public bool SourceInRadius
        {
            get
            {
                if (this.Position != Vector3.Zero)
                    return Extensions.Distance(Program.CatchSource, this.Position, true) < Program.CatchRadius * Program.CatchRadius;
                return false;
            }
        }
        public bool HeroInReticle
        {
            get
            {
                if (this.Position != Vector3.Zero)
                    return Extensions.Distance(Program.myHero.Position, this.Position, true) < Math.Pow(Radius, 2);
                return false;
            }
        }
        public bool InTime
        {
            get
            {
                return Game.Time - this.StartTime <= LimitTime + 0.2f;
            }
        }
        public bool InTurret
        {
            get
            {
                if (this.Position != Vector3.Zero)
                {
                    var turret = EntityManager.Turrets.Enemies.Where(m => m.Health > 0).OrderBy(m => Extensions.Distance(Program.myHero, m, true)).FirstOrDefault();
                    if (turret != null)
                    {
                        return turret.GetAutoAttackRange() + 750f >= Extensions.Distance(turret.Position, this.Position) && Program.SubMenu["Axes"]["Q"].Cast<CheckBox>().CurrentValue;
                    }
                }
                return false;
            }
        }
        public Vector3 Position
        {
            get
            {
                if (this.Reticle != null)
                    return this.Reticle.Position;
                return Vector3.Zero;
            }
        }
        public Axe(MissileClient missile)
        {
            this.Missile = missile;
            this.StartTime = Game.Time;
        }
        public void AddReticle(GameObject reticle)
        {
            this.Reticle = reticle;
            this.StartTime = Game.Time;
        }

    }
}
