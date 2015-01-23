using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace AzirTheEmperorOfSoloQueue
{
    class Emperor
    {
        internal static Spell Q, QTrain, W, E, R;
        internal static SpellSlot IgniteSlot;
        internal static Menu Config;
        internal static Obj_AI_Hero Player;
        internal static List<Spell> Spells = new List<Spell>();
        internal static Orbwalking.Orbwalker Orb;
        private static Vector3 _lastSoldierCastPosition = new Vector3();
        public static int LastCastDelay = 0;

		//Key binds
        public static MenuItem comboKey;
        public static MenuItem harassKey;
        public static MenuItem laneclearKey;
        public static MenuItem lanefreezeKey;
        
        //Items
        public static Items.Item DFG;

        //Orbwalker instance
        private static Orbwalking.Orbwalker _orbwalker;

        private static Menu Menu;
        private static Menu orbwalkerMenu;


        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        internal static void Game_OnGameLoad(EventArgs args)
        {
            Config = new Menu("Azir - SoloQ God","azir",true);
            orbwalkerMenu = new Menu("Orbwalker", "Orbwalker");
            //TargetSelector
            Menu.AddSubMenu(new Menu("TargetSelector", "TargetSelector"));
            TargetSelector.AddToMenu(Menu.SubMenu("TargetSelector"));

            //Orbwalker
            orbwalkerMenu.AddItem(new MenuItem("Orbwalker_Mode", "Regular Orbwalker").SetValue(false));
            Menu.AddSubMenu(orbwalkerMenu);
            ChooseOrbwalker(Menu.Item("Orbwalker_Mode").GetValue<bool>());

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
            Q = new Spell(SpellSlot.Q, 800);
            QTrain = new Spell(SpellSlot.Q, 800);
            W = new Spell(SpellSlot.W, 450);
            E = new Spell(SpellSlot.E, 2500);
            R = new Spell(SpellSlot.R, 580);

            Q.SetSkillshot(0.1f, 100, 1700, false, SkillshotType.SkillshotLine);
            QTrain.SetSkillshot(0.25f, 100, 500, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(0f, 325, float.MaxValue, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.5f, 700, 1400, false, SkillshotType.SkillshotLine);
            DrawingManager.SpellList.Add(Q);
            DrawingManager.SpellList.Add(W);
            DrawingManager.SpellList.Add(E);
            Game.OnGameUpdate += Game_OnGameUpdate;
            GameObject.OnCreate += VectorManager.GameObject_OnCreate;
            GameObject.OnDelete += VectorManager.GameObject_OnDelete;
            DrawingManager.Init();
        }
                private static void ChooseOrbwalker(bool mode)
        {
            if (mode)
            {
                _orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
                comboKey = Menu.Item("Orbwalk");
                harassKey = Menu.Item("Farm");
                laneclearKey = Menu.Item("LaneClear");
                lanefreezeKey = Menu.Item("LaneClear");
                Game.PrintChat("Regular Orbwalker Loaded");
            }
            else
            {
                xSLxOrbwalker.AddToMenu(orbwalkerMenu);
                comboKey = Menu.Item("Combo_Key");
                harassKey = Menu.Item("Harass_Key");
                laneclearKey = Menu.Item("LaneClear_Key");
                lanefreezeKey = Menu.Item("LaneFreeze_Key");
                Game.PrintChat("xSLx Orbwalker Loaded");
            }
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
        internal static void SaveLastSoldierCastPosition(Vector3 pos)
        {
            _lastSoldierCastPosition = pos;
        }

        private static void SaveLastCastDelay()
        {
            var spell = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E);
            var delay = 10 *
                        (int)
                            (ObjectManager.Player.ServerPosition.Distance(_lastSoldierCastPosition) /
                             spell.SData.MissileSpeed) - Config.Item("trainDelay").GetValue<Slider>().Value - (Game.Ping / 2);
            LastCastDelay = delay;
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
            if (Q.IsReady() && VectorManager.AzirObjects.Count > 0)
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
                    if (W.IsReady())
                    {
                        SaveLastSoldierCastPosition(pos1);
                        W.Cast(VectorManager.MaxSoldierPosition(pos1), true);
                    }
                    if (E.IsReady())
                    {
                        SaveLastCastDelay();
                        E.Cast(pos1, true);
                    }
                    if (QTrain.IsReady())
                    {
                        Utility.DelayAction.Add(
                            1000 *
                            (int)
                                (ObjectManager.Player.Distance(pos1) /
                                 500) - Config.Item("trainDelay").GetValue<Slider>().Value - (Game.Ping / 2), () => { QTrain.Cast(pos2, true); });
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
            {
                var whereToCast = Game.CursorPos;
                SaveLastSoldierCastPosition(whereToCast);
                W.Cast(VectorManager.MaxSoldierPosition(whereToCast), true);
            }
            if (E.IsReady())
            {
                SaveLastCastDelay();
                E.Cast(_lastSoldierCastPosition, true);
            }
            if (QTrain.IsReady())
            {
                Utility.DelayAction.Add(LastCastDelay, () => { QTrain.Cast(Game.CursorPos, true); });
            }
        }
    }
}
