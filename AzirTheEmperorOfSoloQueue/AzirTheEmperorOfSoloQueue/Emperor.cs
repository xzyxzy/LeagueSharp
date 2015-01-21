using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace AzirTheEmperorOfSoloQueue
{
    class Emperor
    {
        internal static Spell Q, W, E, R;
        internal static SpellSlot IgniteSlot;
        internal static Menu Config;
        internal static Obj_AI_Hero Player;
        internal static List<Spell> Spells = new List<Spell>();
        internal static Orbwalking.Orbwalker Orb;
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        internal static void Game_OnGameLoad(EventArgs args)
        {
            Config = new Menu("Azir - SoloQ God","azir",true);
            var ts = new Menu("Target Selector", "ts");
            TargetSelector.AddToMenu(ts);
            Config.AddSubMenu(ts);

            var orbz = new Menu("Orbwalker","orb");
            Orb = new Orbwalking.Orbwalker(orbz);
            Config.AddSubMenu(orbz);

            var sel = new Menu("Configuration", "Config");
            var useE = new Menu("Use E", "useeE");
            useE.AddItem(new MenuItem("useE", "Use E").SetValue(true));
            foreach (var minion in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsEnemy))
            {
                useE.AddItem(new MenuItem("useE_"+minion.BaseSkinName,"Use E on " + minion.BaseSkinName).SetValue(true));
            }
            sel.AddItem(new MenuItem("useR", "Use R").SetValue(true));
            sel.AddItem(new MenuItem("useAA", "Soldier AA in range").SetValue(true));
            sel.AddItem(new MenuItem("trainDelay", "Train (-) delay").SetValue(new Slider(0, 0, 50)));
            sel.AddSubMenu(useE);
            Config.AddSubMenu(sel);

            var draw = new Menu("Drawings", "draw");
            draw.AddItem(new MenuItem("drawInsec", "Draw Insec").SetValue(true));
            draw.AddItem(new MenuItem("drawQ", "Draw Q range").SetValue(true));
            draw.AddItem(new MenuItem("drawW", "Draw W range").SetValue(true));
            draw.AddItem(new MenuItem("drawE", "Draw E range").SetValue(true));
            draw.AddItem(new MenuItem("drawSoldier", "Draw Soldier range").SetValue(true));
            Config.AddSubMenu(draw);
            Config.AddItem(new MenuItem("trainMode", "Ride the train!").SetValue(new KeyBind('Z', KeyBindType.Press)));
            Config.AddItem(new MenuItem("insec", "Insec target").SetValue(new KeyBind('T', KeyBindType.Press)));
            Config.AddToMainMenu();
            Q = new Spell(SpellSlot.Q, 1250+345);
            W = new Spell(SpellSlot.W, 450);
            E = new Spell(SpellSlot.E, 1250);
            R = new Spell(SpellSlot.R, 500);

            Q.SetSkillshot(0.25f, 100, 500, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(0f, 425, float.MaxValue, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.2f, 700, 1300, false, SkillshotType.SkillshotLine);
            DrawingManager.SpellList.Add(Q);
            DrawingManager.SpellList.Add(W);
            DrawingManager.SpellList.Add(E);
            Game.OnGameUpdate += Game_OnGameUpdate;
            GameObject.OnCreate += VectorManager.GameObject_OnCreate;
            GameObject.OnDelete += VectorManager.GameObject_OnDelete;
            DrawingManager.Init();
        }
        
        internal static void Game_OnGameUpdate(EventArgs args)
        {
//            if (Player.InFountain() || Player.IsRecalling()) return;
            EscapeMode();
            FightMode();
            HarassMode();
            InSec();
            var myPos = ObjectManager.Player.Position;
            foreach (var minion in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsEnemy && h.IsVisible && !h.IsDead).OrderBy(h => h.Health))
            {
                if (Q.IsReady() && ObjectManager.Player.GetSpellDamage(minion, SpellSlot.Q) > minion.Health)
                {
                    Q.Cast(minion, true);
                }
                var nearest = VectorManager.GetSoldierNearPosition(minion.Position).Position;
                if (ObjectManager.Player.GetSpellDamage(minion, SpellSlot.E) > minion.Health && nearest.Distance(minion.Position) <= 450 && E.IsReady() && (myPos.Y * nearest.X - myPos.X * nearest.Y) - (myPos.Y * minion.Position.X - myPos.X * minion.Position.Y) <= 0)
                {
                    // target within radius
                    E.Cast(nearest, true);
                }
                if (Config.Item("useR").GetValue<bool>() &&
                    ObjectManager.Player.GetSpellDamage(minion, SpellSlot.R) > minion.Health)
                {
                    R.Cast(minion, true);
                }
            }
        }

        internal static void HarassMode()
        {
            if (Orbwalking.OrbwalkingMode.Mixed != Orb.ActiveMode) return;
            var target = TargetSelector.GetTarget(1250, TargetSelector.DamageType.Magical);
            if (target == null) return;
            if (VectorManager.IsWithinSoldierRange(target) && Config.Item("useAA").GetValue<bool>())
            {
                if (Orbwalking.CanAttack())
                {
                    Orbwalking.LastAATick = Environment.TickCount;
                    ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                }
            }
            if (VectorManager.AzirObjects.Count < 1)
            {
                W.Cast(
                    ObjectManager.Player.Distance(target) > 450
                        ? VectorManager.MaxSoldierPosition(target.Position)
                        : target.Position, true);
                Orbwalking.ResetAutoAttackTimer();
                ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, target);
            }
            if (VectorManager.IsWithinSoldierRange(target))
            {
                Orbwalking.ResetAutoAttackTimer();
                ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, target);
            }
            if (Q.IsReady())
            {
                if (!VectorManager.IsWithinSoldierRange(target) && ObjectManager.Player.Distance(target) >= 450 + 450)
                {
                    Q.Cast(target, true);
                }
            }

        }


        internal static void FightMode()
        {
            if (Orbwalking.OrbwalkingMode.Combo != Orb.ActiveMode) return;
            var target = TargetSelector.GetTarget(1250+450, TargetSelector.DamageType.Magical);
            if (target == null) return;
            if (VectorManager.IsWithinSoldierRange(target) && Config.Item("useAA").GetValue<bool>())
            {
                if (Orbwalking.CanAttack())
                {
                    Orbwalking.LastAATick = Environment.TickCount;
                    ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                }
            }
            if (W.IsReady())
            {
                if (VectorManager.AzirObjects.Count < 2 && target.Distance(VectorManager.MaxSoldierPosition(target.Position),true) <= 450)
                {
                    W.Cast(ObjectManager.Player.Distance(target) > 450 ? VectorManager.MaxSoldierPosition(target.Position) : target.Position, true);
                    Orbwalking.ResetAutoAttackTimer();
                    ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                }
                if (!Q.IsReady() && ObjectManager.Player.Distance(target) <= 450f+325f)
                    // we use double because azir soldier double our range.
                {
                    W.Cast(ObjectManager.Player.Distance(target) > 450 ? VectorManager.MaxSoldierPosition(target.Position) : target.Position,true);
                    Orbwalking.ResetAutoAttackTimer();
                    ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                }
            }
            if (Q.IsReady())
            {
                Q.Cast(target, true);
            }
            var myPos = ObjectManager.Player.Position;
            var nearest = VectorManager.GetSoldierNearPosition(target.Position).Position;
            if (Config.Item("useE").GetValue<bool>() && Config.Item("useE_"+target.BaseSkinName).GetValue<bool>() && nearest.Distance(target.Position) <= 450 && E.IsReady() && (myPos.Y*nearest.X - myPos.X*nearest.Y) - (myPos.Y*target.Position.X - myPos.X*target.Position.Y) <= 0)
            {
                // target within radius
                E.Cast(nearest, true);
            }
        }

        internal static void InSec()
        {
            if (Config.Item("insec").GetValue<KeyBind>().Active)
            {
                Orbwalking.Orbwalk(null, Game.CursorPos);
                var target = TargetSelector.GetTarget(1250 + 450, TargetSelector.DamageType.Magical);

                var pos1 = target.Position.Extend(Game.CursorPos, 450);
                var pos2 = target.Position.Extend(ObjectManager.Player.Position, -250);

                var myPos = ObjectManager.Player.Position;

                if ((myPos.Y*pos1.X - myPos.X*pos1.Y) - (myPos.Y*pos2.X - myPos.X*pos2.Y) > 0)
                {
                    if (W.IsReady()) W.Cast(VectorManager.MaxSoldierPosition(pos1), true);
                    if (E.IsReady())
                    {
                        E.Cast(pos1, true);
                    }
                    if (Q.IsReady())
                    {
                        Utility.DelayAction.Add(
                            1000 *
                            (int)
                                (ObjectManager.Player.Distance(pos1) /
                                 500) - Config.Item("trainDelay").GetValue<Slider>().Value - (Game.Ping / 2), () => { Q.Cast(pos2, true); });
                    }
                    if (R.IsReady() && !E.IsReady() && !Q.IsReady())
                    {
                        R.Cast(target, true);
                    }
                }
            }
        }
        internal static void EscapeMode()
        {
            if (!Config.Item("trainMode").GetValue<KeyBind>().Active || E.Level < 1 || Q.Level < 1) return;
            if (W.IsReady() && VectorManager.AzirObjects.Count < 1)
                W.Cast(VectorManager.MaxSoldierPosition(Game.CursorPos), true);
            var nearest = VectorManager.GetSoldierNearPosition(Game.CursorPos).Position;
            if (E.IsReady())
            {
                E.Cast(nearest, true);
            }
            // wtf did i just write there
            if (Q.IsReady() && ObjectManager.Player.Distance(Game.CursorPos) > 450f)
            {
                Utility.DelayAction.Add(
                    1000*
                    (int)
                        (ObjectManager.Player.Distance(nearest)/
                         500) - Config.Item("trainDelay").GetValue<Slider>().Value - (Game.Ping / 2), () => { Q.Cast(Game.CursorPos, true); });
            }
        }
    }
}
