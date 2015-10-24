using System;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;



namespace Template
{
    public static class BallManager
    {
        public static List<Ball> Balls = new List<Ball>();
        public static void Init(EventArgs args)
        {
            Game.OnTick += Game_OnTick;
            GameObject.OnCreate += GameObject_OnCreate;
            Obj_AI_Base.OnPlayAnimation += Obj_AI_Base_OnPlayAnimation;
        }

        private static void Game_OnTick(EventArgs args)
        {
            Balls.RemoveAll(m => !m.ObjectIsValid);
        }

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender.IsBall())
            {
                Balls.Add(new Ball(sender));
            }
        }
        private static void Obj_AI_Base_OnPlayAnimation(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args)
        {
            if (sender.IsBall())
            {
                foreach (Ball b in Balls.Where(m => m.ObjectIsValid && m.Object.NetworkId == sender.NetworkId))
                {
                    b.LastAnimation = args.Animation;
                }
                if (args.Animation.Equals("Death"))
                {
                    Balls.RemoveAll(m => m.Object.NetworkId == sender.NetworkId);
                }
            }
        }
        public static bool IsBall(this GameObject obj)
        {
            return obj != null && obj.Name != null && obj is Obj_AI_Minion && obj.IsAlly && obj.Name.Equals("Seed");
        }
        public static bool IsBall(this Obj_AI_Base obj)
        {
            return obj != null && obj.Name != null && obj is Obj_AI_Minion && obj.IsAlly && obj.Name.Equals("Seed");
        }
    }
}
