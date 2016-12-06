using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;

namespace UpdatedAhri
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Loading.OnLoadingComplete += delegate
            {
                if (Player.Instance.Hero != Champion.Ahri)
                {
                    return;
                }

                #region Menu

                var menu = MainMenu.AddMenu("Ahri", "Ahri");

                if (EntityManager.Heroes.Enemies.Count > 0)
                {
                    menu.AddSeparator();
                    menu.AddGroupLabel("Enabled targets");
                    var addedChamps = new List<string>();
                    foreach (var enemy in EntityManager.Heroes.Enemies.Where(enemy => !addedChamps.Contains(enemy.ChampionName)))
                    {
                        addedChamps.Add(enemy.ChampionName);
                        menu.Add(enemy.ChampionName, new CheckBox(string.Format("{0} ({1})", enemy.ChampionName, enemy.Name)));
                    }
                }

                #endregion

                var Q = new Spell.Skillshot(SpellSlot.Q, 880, SkillShotType.Linear, 250, 1600, 100)
                {
                    AllowedCollisionCount = int.MaxValue
                };
                var W = new Spell.Skillshot(SpellSlot.W, 600, SkillShotType.Circular, 0, 1400, 300)
                {
                    AllowedCollisionCount = int.MaxValue
                };
                var E = new Spell.Skillshot(SpellSlot.E, 975, SkillShotType.Linear, 250, 1550, 60)
                {
                    AllowedCollisionCount = 0
                };
                var predictedPositions = new Dictionary<int, Tuple<int, PredictionResult>>();

                #region Combo

                Game.OnTick += delegate
                {
                    if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                    {
                        foreach (var enemy in EntityManager.Heroes.Enemies.Where(enemy => ((TargetSelector.SeletedEnabled && TargetSelector.SelectedTarget == enemy) || menu[enemy.ChampionName].Cast<CheckBox>().CurrentValue) && enemy.IsValidTarget(E.Range + 150)))
                        {
                            var whitchance = W.GetPrediction(enemy);
                            if (W.IsReady() && whitchance.HitChancePercent >= 90)
                            {
                                W.Cast(whitchance.CastPosition);
                            }
                            var qhitchance = Q.GetPrediction(enemy);
                            if (Q.IsReady() && qhitchance.HitChancePercent >= 90)
                            {
                                Q.Cast(qhitchance.CastPosition);
                            }
                            var ehitchance = E.GetPrediction(enemy);
                            if (E.IsReady() && ehitchance.HitChancePercent >= 90 && !enemy.HasBuffOfType(BuffType.SpellShield))
                            {
                                E.Cast(ehitchance.CastPosition);
                            }
                        }
                    }
                };
                #endregion
                #region Drawings
                Drawing.OnEndScene += delegate
                {
                    foreach (var enemy in EntityManager.Heroes.Enemies.Where(u => u.IsValidTarget() && u.IsHPBarRendered))
                    {
                        var damage = 0d;
                        if (Q.IsReady()) { damage += (Player.Instance.GetSpellDamage(enemy, SpellSlot.Q) * 2); }
                        if (W.IsReady()) { damage += (Player.Instance.GetSpellDamage(enemy, SpellSlot.W) * 1.6); }
                        if (E.IsReady()) { damage += Player.Instance.GetSpellDamage(enemy, SpellSlot.E); }
                        //Chat.Print(damage);
                        var damagePercentage = ((enemy.TotalShieldHealth() - damage) > 0 ? (enemy.TotalShieldHealth() - damage) : 0) / (enemy.MaxHealth + enemy.AllShield + enemy.AttackShield + enemy.MagicShield);
                        var currentHealthPercentage = enemy.TotalShieldHealth() / (enemy.MaxHealth + enemy.AllShield + enemy.AttackShield + enemy.MagicShield);
                        //Chat.Print(damagePercentage);
                        //Chat.Print(currentHealthPercentage);
                        var start = new Vector2((int)(enemy.HPBarPosition.X + damagePercentage * 106), (int)enemy.HPBarPosition.Y - 5 + 14);
                        var end = new Vector2((int)(enemy.HPBarPosition.X + currentHealthPercentage * 106) + 1, (int)enemy.HPBarPosition.Y - 5 + 14);
                        Drawing.DrawLine(start, end, 10f, System.Drawing.Color.AliceBlue);
                    }
                };
                #endregion
            };
        }
    }
}
