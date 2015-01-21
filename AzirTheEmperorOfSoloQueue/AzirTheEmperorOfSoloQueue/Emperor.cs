using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;

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

            Config.AddItem(new MenuItem("trainMode", "Ride the train!").SetValue(new KeyBind('Z', KeyBindType.Press)));
            Config.AddItem(new MenuItem("insec", "Insec target").SetValue(new KeyBind('T', KeyBindType.Press)));
            Config.AddItem(new MenuItem("drawInsec", "Draw Insec").SetValue(true));

            Config.AddToMainMenu();
            Q = new Spell(SpellSlot.Q, 1250);
            W = new Spell(SpellSlot.W, 450);
            E = new Spell(SpellSlot.E, 1250);
            R = new Spell(SpellSlot.R, 500);

            Q.SetSkillshot(0.25f, 100, 500, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(0f, 425, float.MaxValue, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.2f, 700, 1300, false, SkillshotType.SkillshotLine);
            Game.OnGameUpdate += Game_OnGameUpdate;
            GameObject.OnCreate += VectorManager.GameObject_OnCreate;
            GameObject.OnDelete += VectorManager.GameObject_OnDelete;
            DrawingManager.Init();
        }
        
        internal static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.InFountain() || Player.IsRecalling()) return;
            EscapeMode();
            FightMode();
            HarassMode();
            InSec();
        }

        internal static void HarassMode()
        {
            if (Orbwalking.OrbwalkingMode.Mixed != Orb.ActiveMode) return;
            var target = TargetSelector.GetTarget(1250, TargetSelector.DamageType.Magical);
            if (target == null) return;
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
            if (W.IsReady())
            {
                if (VectorManager.AzirObjects.Count < 2 && target.Distance(VectorManager.MaxSoldierPosition(target.Position),true) <= 450)
                {
                    W.Cast(ObjectManager.Player.Distance(target) > 450 ? VectorManager.MaxSoldierPosition(target.Position) : target.Position, true);
                    Orbwalking.ResetAutoAttackTimer();
                    ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                }
                if (!Q.IsReady() && ObjectManager.Player.Distance(target, false) <= 450f+450f)
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
                Orbwalking.ResetAutoAttackTimer();
                ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, target);
            }
            var myPos = ObjectManager.Player.Position;
            var nearest = VectorManager.GetSoldierNearPosition(target.Position).Position;
            if (E.IsReady() && (myPos.Y*nearest.X - myPos.X*nearest.Y) - (myPos.Y*target.Position.X - myPos.X*target.Position.Y) <= 0)
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
                                 500) - (Game.Ping / 2), () => { Q.Cast(pos2, true); });
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
                         500) - (Game.Ping/2), () => { Q.Cast(Game.CursorPos, true); });
            }
        }
    }
}
