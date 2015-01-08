using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace IreliaTheWillOfCarrying
{
    enum sequence_R
    {
        On_Click,
        After_AA,
        Always,
        On_Clicking,
        Off
    }
    class Program
    {
        internal static Menu Config = new Menu("Irelia W.O.C","scias_irelia",true);
        private static Obj_AI_Hero Player = ObjectManager.Player;
        private static Orbwalking.Orbwalker Orb;
        private static Spell Q, W, E, R;
        private static SpellSlot IgniteSlot;
        private static List<Spell> SpellList = new List<Spell>();
        private static int[] offensive = { 1, 3, 2, 2, 2, 4, 2, 3, 2, 3, 4, 3, 3, 1, 1, 4, 1, 1 };
        private static int[] defensive = { 1, 3, 2, 3, 3, 4, 3, 2, 3, 2, 4, 2, 2, 1, 1, 4, 1, 1 };
        private static Items.Item healthPot = new Items.Item(ItemData.Health_Potion.Id);
        private static Items.Item manaPot = new Items.Item(ItemData.Mana_Potion.Id);
        private static Items.Item crystallineFlask = new Items.Item(ItemData.Crystalline_Flask.Id);
        private static bool TapKeyPressed;
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != "Irelia") return;
            Q = new Spell(SpellSlot.Q, 650f);
            W = new Spell(SpellSlot.W, 125f);
            E = new Spell(SpellSlot.E, 425f);
            R = new Spell(SpellSlot.R, 1000f);
            Q.SetTargetted(0.25f, 75f);
            E.SetTargetted(0.15f, 75f);
            R.SetSkillshot(0.15f, 80f, 1500f, false, SkillshotType.SkillshotLine);
            SpellList.Add(Q);
            SpellList.Add(E);
            SpellList.Add(R);
            IgniteSlot = Player.GetSpellSlot("SummonerDot");

            var ts = new Menu("Target Selector", "ts");
            TargetSelector.AddToMenu(ts);
            Config.AddSubMenu(ts);

            var clear = new Menu("Ultimate settings", "ult");
            foreach (var target in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsEnemy))
            {
                clear.AddItem(new MenuItem(target.NetworkId + "_use", "Use on " + target.ChampionName).SetValue(true));
            }
            Config.AddSubMenu(clear);
            Orb = new Orbwalking.Orbwalker(Config);
            Config.SubMenu("drawings").AddItem(new MenuItem("drawQ", "Draw Q Range").SetValue(new Circle(true, System.Drawing.Color.FromArgb(255, 0, 189, 22))));
            Config.SubMenu("drawings").AddItem(new MenuItem("drawE", "Draw E Range").SetValue(new Circle(true, System.Drawing.Color.FromArgb(255, 0, 112, 95))));
            Config.SubMenu("drawings").AddItem(new MenuItem("drawR", "Draw R Range").SetValue(new Circle(true, System.Drawing.Color.FromArgb(183, 0, 26, 173))));
            Config.SubMenu("drawings").AddItem(new MenuItem("possibleDamage", "Draw Possible Damage").SetValue(true));
            Config.SubMenu("drawings").AddItem(new MenuItem("drawMinion", "Draw gap-close minion").SetValue(true));
            Config.AddItem(new MenuItem("escape", "Flee").SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Press)));
            Config.AddItem(new MenuItem("clearToggle", "Q Clearing").SetValue(false));
            Config.AddItem(new MenuItem("alwaysBig", "Always Q big").SetValue(true));
            Config.AddItem(new MenuItem("secure", "Secure kills").SetValue(true));
            Config.AddItem(new MenuItem("1", "-----"));
            Config.AddItem(new MenuItem("stunGap", "Use (E) to stun gap closers").SetValue(true));
            Config.AddItem(new MenuItem("interrupt", "Use (Q) + (E) to interrupt spells").SetValue(true));
            Config.AddItem(new MenuItem("useR", "Use (R) mode").SetValue(new StringList(new[] { "Off", "On Press", "When available", "After AA", "On Clicking"})));
            Config.AddItem(new MenuItem("2", "-----"));
            Config.AddItem(new MenuItem("ignite", "Use Ignite").SetValue(true));
            Config.AddItem(new MenuItem("packets", "Use Packets").SetValue(true));
            Config.AddItem(new MenuItem("3", "-----"));
            Config.AddItem(new MenuItem("spellSeq", "Spell leveling sequence").SetValue(new StringList(new[] { "Off", "Offensive", "Defensive" })));


            if(Config.Item("spellSeq").GetValue<StringList>().SelectedIndex != 0)
            {
                var lev = new AutoLevel(Config.Item("spellSeq").GetValue<StringList>().SelectedIndex == 1 ? offensive : defensive);
                AutoLevel.Enabled(true);
            }
            Game.PrintChat("<font color=\"#ff00ff\">Irelia</font> - Carry me plz");
            CustomDamageIndicator.Initialize(getPossibleDamage);
            Config.AddToMainMenu();
            Game.OnGameUpdate += Game_OnGameUpdate;
            Game.OnWndProc += Game_OnWndProc;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg == (uint)WindowsMessages.WM_KEYUP)
            {
                if (sequence_R.On_Clicking == useRseq)
                {
                    if (getBladeCount > 0 && R.IsReady())
                    {
                        Obj_AI_Hero targetSelectorGetTarget = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
                        if (targetSelectorGetTarget != null)
                            R.Cast(targetSelectorGetTarget, packetCasting);
                    }
                }
                if (sequence_R.On_Click == useRseq)
                {
                    TapKeyPressed = true;
                }
            }
        }
        static sequence_R useRseq
        {
            get
            {
                if(Config.Item("useR").GetValue<StringList>().SelectedIndex == 0)
                        return sequence_R.Off;
                if(Config.Item("useR").GetValue<StringList>().SelectedIndex == 1)
                        return sequence_R.On_Click;
                if(Config.Item("useR").GetValue<StringList>().SelectedIndex == 2)
                    return sequence_R.Always;
                if (Config.Item("useR").GetValue<StringList>().SelectedIndex == 3)
                    return sequence_R.After_AA;
                if (Config.Item("useR").GetValue<StringList>().SelectedIndex == 4)
                    return sequence_R.On_Clicking;
                return sequence_R.Off;
            }
        }

        static void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if(args.Unit.IsMe && args.Target.IsEnemy)
            {
                if(Orb.ActiveMode == Orbwalking.OrbwalkingMode.Combo && ((TapKeyPressed && useRseq == sequence_R.On_Click) || useRseq == sequence_R.Always || useRseq == sequence_R.After_AA))
                {
                    if (R.IsReady() && args.Target.IsEnemy && Config.Item(args.Target.NetworkId+"_use").GetValue<bool>())
                    {
                        R.Cast(args.Target.Position, packetCasting);
                    }
                }
            }
        }

        internal static float getPossibleDamage(Obj_AI_Hero target)
        {
            var damage = 0d;
            if (Q.IsReady()) damage += Player.GetSpellDamage(target, SpellSlot.Q);
            if (W.IsReady()) damage += Player.GetSpellDamage(target, SpellSlot.W);
            if (E.IsReady()) damage += Player.GetSpellDamage(target, SpellSlot.E);
            if (R.IsReady()) damage += Player.GetSpellDamage(target, SpellSlot.R) * getBladeCount;
            if (IgniteSlot != SpellSlot.Unknown &&
                ObjectManager.Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                damage += ObjectManager.Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
            if (Player.HasBuff("Sheen")) damage += GetSheenDamage(target, false);
            return (float)damage;
        }
        static int getBladeCount
        {
            get
            {
                var blades = Player.Buffs.FirstOrDefault(b=> b.DisplayName == "IreliaTranscendentBlades");
                return blades == null ? 3 : blades.Count;
            }
        }
        static void Drawing_OnDraw(EventArgs args)
        {
            foreach(var spell in SpellList.Where(s => s.Level > 0 && Config.Item("draw"+s.Slot).GetValue<Circle>().Active))
            {
                Utility.DrawCircle(Player.Position, spell.Range, Config.Item("draw" + spell.Slot).GetValue<Circle>().Color, 5, 30);
            }
            if(Config.Item("drawMinion").GetValue<bool>())
            {
                var target = TargetSelector.GetTarget(Q.IsReady() ? Q.Range * 2 : Q.Range, TargetSelector.DamageType.Physical);
                if (target != null)
                {
                    var nearestMinion = getNearestMinion(target, Q.Range * 2);
                    if (nearestMinion != null && nearestMinion.Distance(Player, false) > 425)
                    {
                        if (nearestMinion.Distance(target, false) < 450)
                        {
                            Utility.DrawCircle(Player.Position, nearestMinion.AttackRange, System.Drawing.Color.Red);
                        }
                    }
                }
            }

        }

        static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!gapcloser.Sender.IsEnemy || !Config.Item("stunGap").GetValue<bool>()) return;
            if (gapcloser.Sender.IsValidTarget(E.Range) && E.IsReady())
                E.Cast(gapcloser.Sender, packetCasting);
        }

        static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (unit.IsMe || !unit.IsEnemy || !Config.Item("interrupt").GetValue<bool>() || Orb.ActiveMode == Orbwalking.OrbwalkingMode.Combo || Config.Item("escape").GetValue<bool>()) return;
            var nearestMinion = getNearestMinion(unit, E.Range);
            if (nearestMinion != null)
            {
                if (nearestMinion.Distance(unit, false) < E.Range)
                {
                    if (Q.IsReady() && E.IsReady())
                    {
                        Q.Cast(unit, packetCasting);
                        E.Cast(unit, packetCasting);
                    }
                }
            }
            else
            {
                if (unit.CountEnemysInRange(Q.Range) < 3)
                {
                    if (Q.IsReady() && E.IsReady())
                    {
                        Q.Cast(unit, packetCasting);
                        E.Cast(unit, packetCasting);
                    }
                }
            }
        }
        static double GetSheenDamage(Obj_AI_Base target, bool simulate = false)
        {
            if (simulate)
                return Items.HasItem(3057)
                    ? ObjectManager.Player.BaseAttackDamage
                    : (Items.HasItem(3078) ? ObjectManager.Player.BaseAttackDamage * 2 : 0);
            else if (Items.HasItem(3057) && ObjectManager.Player.HasBuff("Sheen")) // sheen
                return ObjectManager.Player.BaseAttackDamage;
            else if (Items.HasItem(3078) && ObjectManager.Player.HasBuff("Sheen")) // trinity
                return ObjectManager.Player.BaseAttackDamage * 2;
            else
                return 0;
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            PotionManager();
            if (Config.Item("secure").GetValue<bool>())
                secureKills();
            if (getBladeCount < 1 && TapKeyPressed) TapKeyPressed = false;
            switch(Orb.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                case Orbwalking.OrbwalkingMode.Mixed:
                    OnFight(Orb.ActiveMode);
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                case Orbwalking.OrbwalkingMode.LastHit:
                    OnClear(Orb.ActiveMode);
                    break;
                case Orbwalking.OrbwalkingMode.None:
                    if(Config.Item("escape").GetValue<KeyBind>().Active)
                        OnEscape();
                    break;
            }
        }
        private static void secureKills()
        {
            foreach ( var unit in ObjectManager.Get<Obj_AI_Hero>().Where(h=>h.IsEnemy && h.IsValidTarget(Q.Range) && h.IsVisible ) )
            {
                if (Config.Item("ignite").GetValue<bool>())
                    UseIgnite(unit);
                if (Q.IsReady() && getRealDamageQ(unit) > unit.Health)
                    Q.Cast(unit, packetCasting);
                if (W.IsReady() && Q.IsReady() && unit.Health < Player.GetSpellDamage(unit,SpellSlot.Q) + Player.GetSpellDamage(unit, SpellSlot.W))
                {
                    W.Cast(packetCasting);
                    Q.Cast(unit, packetCasting);
                }
            }
        }
        private static float getRealDamageQ(Obj_AI_Base unit)
        {
            return (float)Player.GetSpellDamage(unit, SpellSlot.Q) + (float)(Player.HasBuff("IreliaHitenStyleCharged") ? Player.GetSpellDamage(unit, SpellSlot.W) : 0) + (float)GetSheenDamage(unit);
        }
        private static void OnClear(Orbwalking.OrbwalkingMode mode)
        {
            if (Q.IsReady() && Config.Item("alwaysBig").GetValue<bool>())
            {
                var minions = ObjectManager.Get<Obj_AI_Minion>().Where(m => m.Health < getRealDamageQ(m) && m.IsValidTarget(E.Range) && (m.BaseSkinName.Contains("MinionSiege") || m.BaseSkinName.Contains("Dragon") || m.BaseSkinName.Contains("Baron")));
                foreach (var minion in minions)
                {
                    Q.Cast(minion);
                    break;
                }
            }
            if (Q.IsReady() && Config.Item("clearToggle").GetValue<bool>())
            {
                var minions = MinionManager.GetMinions(Q.Range);
                foreach (var minion in minions.Where(m => m.Health < getRealDamageQ(m)*0.8))
                {
                    Q.Cast(minion, packetCasting);
                }
            }
        }
        private static bool hasHitenBuff
        {
            get
            {
                return Player.HasBuff("IreliaHitenStyleCharged");
            }
        }
        private static void OnFight(Orbwalking.OrbwalkingMode mode)
        {
            var target = TargetSelector.GetTarget(Q.IsReady() ? Q.Range * 2 : Q.Range, TargetSelector.DamageType.Magical);
            if (target == null) return;
            var nearestMinion = getNearestMinion(target, Q.Range*2);
            if (nearestMinion != null && nearestMinion.Distance(Player,false) > 500)
            {
                if (nearestMinion.Distance(target, false) < 450)
                {
                    if (Q.IsReady() && nearestMinion.Health < getRealDamageQ(nearestMinion))
                    {
                        Q.Cast(nearestMinion, packetCasting);
                    }
                    else if (Q.IsReady() && (W.IsReady() || hasHitenBuff) && nearestMinion.Health < Player.GetSpellDamage(nearestMinion, SpellSlot.Q) + Player.GetSpellDamage(nearestMinion, SpellSlot.W))
                    {
                        Q.Cast(nearestMinion, packetCasting);
                        if(W.IsReady() && !hasHitenBuff)
                            W.Cast(packetCasting);
                    }
                        /* bugged
                    else if (Q.IsReady() && (W.IsReady() || hasHitenBuff) && R.IsReady() && nearestMinion.Health > Player.GetSpellDamage(nearestMinion, SpellSlot.Q) + Player.GetSpellDamage(nearestMinion, SpellSlot.W)
                        && nearestMinion.Health < Player.GetSpellDamage(nearestMinion, SpellSlot.Q) + Player.GetSpellDamage(nearestMinion, SpellSlot.W) + Player.GetSpellDamage(nearestMinion, SpellSlot.R))
                    {
                        R.CastOnUnit(nearestMinion, packetCasting);
                        if(W.IsReady() && !hasHitenBuff)
                            W.Cast(packetCasting);
                        Q.CastOnUnit(nearestMinion, packetCasting);
                    }*/
                    else
                    {
                        if (Q.IsReady() && Orbwalking.OrbwalkingMode.Mixed != Orb.ActiveMode)
                            Q.CastOnUnit(target, packetCasting);
                    }
                }
            }
            if (nearestMinion == null && target.Distance(Player, false) > 400 && Orbwalking.OrbwalkingMode.Mixed != Orb.ActiveMode)
            {
                if(Q.IsReady())
                    Q.CastOnUnit(target, packetCasting);
            }
            if(W.IsReady() && target.IsValidTarget(200))
            {
                W.Cast(packetCasting);
            }
            if (E.IsReady() && target.IsValidTarget(425))
            {
                E.Cast(target, packetCasting);
            }
            if (((TapKeyPressed && useRseq == sequence_R.On_Click) || useRseq == sequence_R.Always) && Config.Item(target.NetworkId+"_use").GetValue<bool>() && R.IsReady() && getBladeCount > 0 && Player.Distance(target,false) > 300)
                R.CastIfHitchanceEquals(target,HitChance.Medium, packetCasting);
        }
        private static bool isSlow(Obj_AI_Base unit)
        {
            return Player.HealthPercentage() > unit.HealthPercentage();
        }
        private static void OnEscape()
        {
            var target = TargetSelector.GetTarget(250f, TargetSelector.DamageType.Magical);
            Orbwalking.Orbwalk(target != null && E.IsReady() ? target : null, Game.CursorPos);
            Obj_AI_Base nearestMinion = null;
            var winions = MinionManager.GetMinions(Q.Range).OrderBy(m => m.Distance(Player, false)).ThenBy(m => m.Health < getRealDamageQ(m));
            foreach (var minion in winions)
            {
                nearestMinion = minion;
                break;
            }

            if (target != null && nearestMinion == null)
            {
                if (E.IsReady() && target.IsFacing(Player))
                    E.Cast(target, packetCasting);
            }
            if(nearestMinion != null)
            {
                if (Q.IsReady())
                    Q.Cast(nearestMinion, packetCasting);
            }
        }
        private static Obj_AI_Base getNearestMinion(Obj_AI_Base target, float range)
        {
            var minions = MinionManager.GetMinions(Q.Range+E.Range+50);
            foreach (var minion in minions.Where(m => m.Distance(target,false) < range))
            {
                return minion;
            }
            return null;
        }
        private static void UseIgnite(Obj_AI_Hero unit)
        {
            var damage = IgniteSlot == SpellSlot.Unknown || ObjectManager.Player.Spellbook.CanUseSpell(IgniteSlot) != SpellState.Ready ? 0 : ObjectManager.Player.GetSummonerSpellDamage(unit, Damage.SummonerSpell.Ignite);
            var targetHealth = unit.Health;
            var hasPots = Items.HasItem(ItemData.Health_Potion.Id) || Items.HasItem(ItemData.Crystalline_Flask.Id);
            if (hasPots || unit.HasBuff("RegenerationPotion", true))
            {
                if (damage * 0.5 > targetHealth)
                {
                    if (IgniteSlot.IsReady())
                    {
                        ObjectManager.Player.Spellbook.CastSpell(IgniteSlot, unit);
                    }
                }
            }
            else
            {
                if (IgniteSlot.IsReady() && damage > targetHealth)
                {
                    ObjectManager.Player.Spellbook.CastSpell(IgniteSlot, unit);
                }
            }
        }

        private static bool packetCasting
        {
            get { return Config.Item("packets").GetValue<bool>(); }
        }
        private static float manaCost(SpellSlot skill)
        {
            return ObjectManager.Player.Spellbook.GetSpell(skill).ManaCost;
        }
        private static void PotionManager()
        {
            // FlaskOfCrystalWater - Mana Potion
            // RegenerationPotion - Health Potion
            // ItemCrystalFlask - Flask
            if (ObjectManager.Player.InFountain() || ObjectManager.Player.IsRecalling()) return;

            // Health Potion
            if (healthPot.IsReady() && !ObjectManager.Player.HasBuff("RegenerationPotion", true) && !ObjectManager.Player.HasBuff("ItemCrystalFlask", true))
            {
                if (ObjectManager.Player.CountEnemysInRange((R.Range + Q.Range) * 2) > 0 &&
                        ObjectManager.Player.Health + 150 < ObjectManager.Player.MaxHealth
                            || ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * 0.5)
                {
                    healthPot.Cast();
                }
            }

            // Mana Potion
            if (!ObjectManager.Player.HasBuff("FlaskOfCrystalWater", true) && !ObjectManager.Player.HasBuff("ItemCrystalFlask", true) && manaPot.IsReady() && ObjectManager.Player.CountEnemysInRange((R.Range + Q.Range) * 2) > 0 && ObjectManager.Player.Mana < manaCost(SpellSlot.Q) + manaCost(SpellSlot.E) + manaCost(SpellSlot.R))
            {
                manaPot.Cast();
            }

            // Crystalline Flask
            if (!ObjectManager.Player.HasBuff("FlaskOfCrystalWater", true)
                    && !ObjectManager.Player.HasBuff("ItemCrystalFlask", true)
                        && !ObjectManager.Player.HasBuff("RegenerationPotion", true)
                            && crystallineFlask.IsReady() && ObjectManager.Player.CountEnemysInRange((R.Range + Q.Range) * 2) > 0 && (ObjectManager.Player.Mana < manaCost(SpellSlot.W) + manaCost(SpellSlot.E) + manaCost(SpellSlot.R) || ObjectManager.Player.Health + 120 < ObjectManager.Player.MaxHealth))
                crystallineFlask.Cast();
        }
    }
}
