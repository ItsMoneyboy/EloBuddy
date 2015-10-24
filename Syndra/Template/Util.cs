using System;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace Template
{
    public static class Util
    {
        public static float Extra_AA_Range = 120f;
        public static AIHeroClient MyHero { get { return ObjectManager.Player; } }
        public static Vector3 MousePos { get { return Game.CursorPos; } }
        public static bool IsValidAlly(this AttackableUnit unit, float range = float.MaxValue)
        {
            return unit != null && unit.IsValid && !unit.IsDead && Extensions.Distance(MyHero, unit, true) <= Math.Pow(range, 2);
        }
        public static bool IsInAutoAttackRange(this Obj_AI_Base source, Obj_AI_Base target)
        {
            if (Combo.IsActive || Harass.IsActive)
            {
                return source != null && target != null && source.IsValid && target.IsValid && !source.IsDead && !target.IsDead && Math.Pow(source.BoundingRadius + target.BoundingRadius + source.AttackRange + Extra_AA_Range, 2) >= Extensions.Distance(source, target, true);
            }
            return source != null && target != null && source.IsValid && target.IsValid && !source.IsDead && !target.IsDead && Math.Pow(source.BoundingRadius + target.BoundingRadius + source.AttackRange, 2) >= Extensions.Distance(source, target, true);
        }
        public static bool IsInEnemyTurret(this Obj_AI_Base unit)
        {
            if (unit != null && unit.IsValid && !unit.IsDead)
            {
                var turret = EntityManager.Turrets.Enemies.Where(m => m.IsValidTarget() && Extensions.Distance(unit, m, true) <= Math.Pow(750f + unit.BoundingRadius, 2)).FirstOrDefault();
                if (turret != null)
                {
                    return true;
                }
            }
            return false;
        }
        private static Vector3 Source(this Spell.Skillshot s)
        {
            return s.SourcePosition.HasValue ? s.SourcePosition.Value : Util.MyHero.Position;
        }
        public static Obj_AI_Base JungleClear(this Spell.Skillshot s, bool UseCast = true, int NumberOfHits = 1)
        {
            if (s.IsReady())
            {
                var minions = EntityManager.MinionsAndMonsters.GetJungleMonsters(s.Source(), s.Range + s.Width, true).OrderBy(m => m.MaxHealth);
                if (minions.Count() > 0 && minions.Count() >= NumberOfHits)
                {
                    switch (s.Type)
                    {
                        case SkillShotType.Linear:
                            var t = s.GetBestLineTarget(minions.ToList<Obj_AI_Base>());
                            if (t.Item1 >= NumberOfHits)
                            {
                                if (UseCast)
                                {
                                    s.Cast(t.Item2);
                                }
                                return t.Item2;
                            }
                            break;
                        case SkillShotType.Circular:
                            var t2 = s.GetBestCircularTarget(minions.ToList<Obj_AI_Base>());
                            if (t2.Item1 >= NumberOfHits)
                            {
                                if (UseCast)
                                {
                                    s.Cast(t2.Item2);
                                }
                                return t2.Item2;
                            }
                            break;
                    }
                }
            }
            return null;
        }
        public static Obj_AI_Base LaneClear(this Spell.Skillshot s, int NumberOfHits = 1, bool UseCast = true)
        {
            if (s.IsReady())
            {
                var minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, s.Source(), s.Range + s.Width, true);
                if (minions.Count() > 0 && minions.Count() >= NumberOfHits)
                {
                    switch (s.Type)
                    {
                        case SkillShotType.Linear:
                            var t = s.GetBestLineTarget(minions.ToList<Obj_AI_Base>());
                            if (t.Item1 >= NumberOfHits)
                            {
                                if (UseCast)
                                {
                                    s.Cast(t.Item2);
                                }
                                return t.Item2;
                            }
                            break;
                        case SkillShotType.Circular:
                            var t2 = s.GetBestCircularTarget(minions.ToList<Obj_AI_Base>());
                            if (t2.Item1 >= NumberOfHits)
                            {
                                if (UseCast)
                                {
                                    s.Cast(t2.Item2);
                                }
                                return t2.Item2;
                            }
                            break;
                    }
                }
            }
            return null;
        }
        private static int CountObjectsOnLineSegment(this Spell.Skillshot s, List<Obj_AI_Base> list, Vector3 EndPosition)
        {
            int Count = 0;
            foreach (Obj_AI_Base obj in list)
            {
                var info = obj.Position.To2D().ProjectOn(s.Source().To2D(), EndPosition.To2D());
                if (info.IsOnSegment && Extensions.Distance(info.SegmentPoint, obj.Position.To2D(), true) <= s.Width)
                {
                    Count++;
                }
            }
            return Count;
        }
        public static Tuple<int, Obj_AI_Base> GetBestLineTarget(this Spell.Skillshot s, List<Obj_AI_Base> list)
        {
            Obj_AI_Base BestTarget = null;
            int BestHit = -1;
            List<Obj_AI_Base> List = list.Where(m => m.IsValidTarget() && Extensions.Distance(s.Source(), m, true) <= Math.Pow(s.Range, 2)).ToList();
            foreach (Obj_AI_Base obj in List)
            {
                var EndPosition = s.Source() + (obj.Position - s.Source()).Normalized() * s.Range;
                var Hit = s.CountObjectsOnLineSegment(List, EndPosition);
                if (Hit > BestHit)
                {
                    BestHit = Hit;
                    BestTarget = obj;
                    if (BestHit == List.Count)
                    {
                        break;
                    }
                }
            }
            return new Tuple<int, Obj_AI_Base>(BestHit, BestTarget);
        }
        private static int CountObjectsNearTo(this Spell.Skillshot s, List<Obj_AI_Base> list, Obj_AI_Base target)
        {
            int Count = 0;
            foreach (Obj_AI_Base obj in list)
            {
                if (Extensions.Distance(target, obj, true) <= Math.Pow(s.Width, 2))
                {
                    Count++;
                }
            }
            return Count;
        }
        public static Tuple<int, Obj_AI_Base> GetBestCircularTarget(this Spell.Skillshot s, List<Obj_AI_Base> list)
        {
            Obj_AI_Base BestTarget = null;
            int BestHit = -1;
            List<Obj_AI_Base> List = list.Where(m => m.IsValidTarget() && Extensions.Distance(s.Source(), m, true) <= Math.Pow(s.Range + s.Width, 2)).ToList();
            foreach (Obj_AI_Base obj in List)
            {
                var Hit = s.CountObjectsNearTo(List, obj);
                if (Hit > BestHit)
                {
                    BestHit = Hit;
                    BestTarget = obj;
                    if (BestHit == List.Count)
                    {
                        break;
                    }
                }
            }
            return new Tuple<int, Obj_AI_Base>(BestHit, BestTarget);
        }
        public static Obj_AI_Base LastHit(this Spell.Skillshot s, bool UseCast = true)
        {
            if (s.IsReady())
            {
                var minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, s.Source(), s.Range + s.Width, true).Where(o => o.Health <= 2.0f * s.Slot.GetSpellDamage(o));
                if (minions.Count() > 0)
                {
                    foreach (Obj_AI_Base minion in minions)
                    {
                        bool CanCalculate = false;
                        if (minion.IsValidTarget())
                        {
                            if (!Orbwalker.CanAutoAttack)
                            {
                                if (Orbwalker.CanMove && Orbwalker.LastTarget != null && Orbwalker.LastTarget.NetworkId != minion.NetworkId)
                                {
                                    CanCalculate = true;
                                }
                            }
                            else
                            {
                                if (Util.MyHero.GetAutoAttackRange(minion) <= Extensions.Distance(Util.MyHero, minion))
                                {
                                    CanCalculate = true;
                                }
                                else
                                {
                                    var speed = Util.MyHero.BasicAttack.MissileSpeed;
                                    var time = (int)(1000 * Extensions.Distance(Util.MyHero, minion) / speed + Util.MyHero.AttackCastDelay * 1000 + Game.Ping - 100);
                                    var predHealth = Prediction.Health.GetPrediction(minion, time);
                                    if (predHealth <= 0)
                                    {
                                        CanCalculate = true;
                                    }
                                    /**
                                    if (!Orbwalker.CanBeLastHitted(minion))
                                    {
                                        CanCalculate = true;
                                    }**/
                                }
                            }
                        }
                        if (CanCalculate)
                        {
                            var dmg = s.Slot.GetSpellDamage(minion);
                            var time = (int)(1000 * Extensions.Distance(s.Source(), minion) / s.Speed + s.CastDelay - 70);
                            var predHealth = Prediction.Health.GetPrediction(minion, time);
                            if (time > 0 && predHealth == minion.Health)
                            {

                            }
                            else
                            {
                                if (dmg > predHealth && predHealth > 0)
                                {
                                    if (UseCast)
                                    {
                                        s.Cast(minion);
                                    }
                                    return minion;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }
        public static int GetPriority(this AIHeroClient hero)
        {
            string championName = hero.ChampionName;
            string[] p1 =
            {
                "Alistar", "Amumu", "Bard", "Blitzcrank", "Braum", "Cho'Gath", "Dr. Mundo", "Garen", "Gnar",
                "Hecarim", "Janna", "Jarvan IV", "Leona", "Lulu", "Malphite", "Nami", "Nasus", "Nautilus", "Nunu",
                "Olaf", "Rammus", "Renekton", "Sejuani", "Shen", "Shyvana", "Singed", "Sion", "Skarner", "Sona",
                "Soraka", "Taric", "Thresh", "Volibear", "Warwick", "MonkeyKing", "Yorick", "Zac", "Zyra"
            };

            string[] p2 =
            {
                "Aatrox", "Darius", "Elise", "Evelynn", "Galio", "Gangplank", "Gragas", "Irelia", "Jax",
                "Lee Sin", "Maokai", "Morgana", "Nocturne", "Pantheon", "Poppy", "Rengar", "Rumble", "Ryze", "Swain",
                "Trundle", "Tryndamere", "Udyr", "Urgot", "Vi", "XinZhao", "RekSai"
            };

            string[] p3 =
            {
                "Akali", "Diana", "Ekko", "Fiddlesticks", "Fiora", "Fizz", "Heimerdinger", "Jayce", "Kassadin",
                "Kayle", "Kha'Zix", "Lissandra", "Mordekaiser", "Nidalee", "Riven", "Shaco", "Vladimir", "Yasuo",
                "Zilean"
            };

            string[] p4 =
            {
                "Ahri", "Anivia", "Annie", "Ashe", "Azir", "Brand", "Caitlyn", "Cassiopeia", "Corki", "Draven",
                "Ezreal", "Graves", "Jinx", "Kalista", "Karma", "Karthus", "Katarina", "Kennen", "KogMaw", "Leblanc",
                "Lucian", "Lux", "Malzahar", "MasterYi", "MissFortune", "Orianna", "Quinn", "Sivir", "Syndra", "Talon",
                "Teemo", "Tristana", "TwistedFate", "Twitch", "Varus", "Vayne", "Veigar", "VelKoz", "Viktor", "Xerath",
                "Zed", "Ziggs"
            };

            if (p1.Contains(championName))
            {
                return 1;
            }
            if (p2.Contains(championName))
            {
                return 2;
            }
            if (p3.Contains(championName))
            {
                return 3;
            }
            return p4.Contains(championName) ? 4 : 1;
        }
    }
}
