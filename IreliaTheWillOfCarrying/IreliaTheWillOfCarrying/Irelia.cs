using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;

namespace IreliaTheWillOfCarrying
{
    internal class Irelia
    {
        internal static Menu Config;
        internal static Spell Q, W, E, R;
        internal static SpellSlot IgniteSlot;
        internal static Orbwalking.Orbwalker Walker;

        static void Main(String[] args)
        {
            CustomEvents.Game.OnGameLoad += OnTick;
        }

        internal static void OnTick(EventArgs onTickArgs)
        {
            if (ObjectManager.Player.ChampionName != "Irelia") return;
            Q = new Spell(SpellSlot.Q, 650f);
            W = new Spell(SpellSlot.W, 125f);
            E = new Spell(SpellSlot.E, 425f);
            R = new Spell(SpellSlot.R, 1000f);
            Q.SetTargetted(0.25f, 75f);
            E.SetTargetted(0.15f, 75f);
            R.SetSkillshot(0.15f, 80f, 1500f, false, SkillshotType.SkillshotLine);
            IgniteSlot = ObjectManager.Player.GetSpellSlot("SummonerDot");
            RenderingManager.Spells.Add(Q);
            RenderingManager.Spells.Add(E);
            RenderingManager.Spells.Add(R);

            Config = new Menu("Scias Irelia", "irelia", true);
            var ts = new Menu("Target Selector", "ts");
            TargetSelector.AddToMenu(ts);
            Config.AddSubMenu(ts);

            Walker = new Orbwalking.Orbwalker(Config);
            Config.AddToMainMenu();

            var lc = new Menu("Lane Clear/Last Hit", "lc");
            lc.AddItem(new MenuItem("useQLC", "Use (Q) to last hit").SetValue(false));
            lc.AddItem(new MenuItem("alwaysBig", "Always Q big minions").SetValue(true));
            Config.AddSubMenu(lc);

            var misc = new Menu("Settings", "sets");
            misc.AddItem(new MenuItem("packet", "Packet Casting").SetValue(true));
            misc.AddItem(new MenuItem("ignite", "Use ignite").SetValue(true));
            misc.AddItem(new MenuItem("interrupt", "Interrupt spells with (Q minion)+(E stun)").SetValue(false));
            misc.AddItem(
                new MenuItem("gap", "Gap-Closers => ").SetValue(new StringList(new[] {"Off", "Stun", "Slow", "Any"}, 3)));
            misc.AddItem(new MenuItem("eusage", "Use (E) => ").SetValue(new StringList(new[] {"Stun", "Slow", "Any"}, 2)));

            misc.AddItem(new MenuItem("Draw_ComboDamage", "Draw Combo Damage", true).SetValue(true));
            misc.AddItem(new MenuItem("Draw_Fill", "Draw Combo Damage Fill", true).SetValue(new Circle(true, Color.FromArgb(90, 255, 169, 4))));
                
            Config.AddSubMenu(misc);

            Game.OnGameUpdate += Game_OnGameUpdate;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            Drawing.OnDraw += RenderingManager.Drawing_OnDraw;


            DamageIndicator.DamageToUnit = DamageManager.TotalDamageToUnit;
            DamageIndicator.Enabled = Config.Item("Draw_ComboDamage").GetValue<bool>();
            DamageIndicator.Fill = Config.Item("Draw_Fill").GetValue<Circle>().Active;
            DamageIndicator.FillColor = Config.Item("Draw_Fill").GetValue<Circle>().Color;

            Config.Item("Draw_ComboDamage").ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs eventArgs)
                {
                    DamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
                };
            Config.Item("Draw_Fill").ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs eventArgs)
                {
                    DamageIndicator.Fill = eventArgs.GetNewValue<Circle>().Active;
                    DamageIndicator.FillColor = eventArgs.GetNewValue<Circle>().Color;
                };
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!gapcloser.Sender.IsEnemy || !gapcloser.Sender.IsValidTarget(E.Range - 100f)) return;
            var gap = Config.Item("gap").GetValue<StringList>().SelectedIndex;
            if (gap == 0) return;
            if ((gap == 1 && gapcloser.Sender.HealthPercentage() > ObjectManager.Player.HealthPercentage())
                || (gap == 2 && gapcloser.Sender.HealthPercentage() < ObjectManager.Player.HealthPercentage())
                || gap == 3)
                E.Cast(gapcloser.Sender, true);
        }

        private static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!Config.Item("interrupt").GetValue<bool>() || !unit.IsEnemy || !E.IsReady() || !(unit.HealthPercentage() > ObjectManager.Player.HealthPercentage()) || !unit.IsChanneling)
                return;
            var minion = MinionsManager.GetNearestMinionNearPosition(unit.Position);
            if (unit.CountEnemysInRange(Q.Range) >= (Walker.ActiveMode == Orbwalking.OrbwalkingMode.Combo ? 3 : 2))
                return;
            Q.Cast(minion ?? unit, true);
            E.Cast(unit, true);
        }

        private static bool PacketCasting
        {
            get { return Config.Item("packet").GetValue<bool>(); }
        }

        private static void KillSteal()
        {
            try
            {
                if (ObjectManager.Get<Obj_AI_Hero>() == null) return;
                foreach (
                    var unit in
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(
                                h =>
                                    h.IsEnemy && h.IsValidTarget(Q.Range) && h.IsVisible &&
                                    (h.Health < DamageManager.GetSpellDamageQ(h) || h.Health < ObjectManager.Player.GetSpellDamage(h, SpellSlot.E) || h.Health < ObjectManager.Player.GetSpellDamage(h,SpellSlot.E)*DamageManager.TranscendentBladesCount)))
                {
                    if (unit.Health < DamageManager.GetSpellDamageQ(unit) && Q.IsReady())
                        Q.Cast(unit, PacketCasting);
                    if (unit.IsValidTarget(E.Range) && unit.Health < ObjectManager.Player.GetSpellDamage(unit, SpellSlot.E))
                        E.Cast(unit, PacketCasting);
                    if (DamageManager.TranscendentBladesCount > 0 &&
                        unit.Health <
                        ObjectManager.Player.GetSpellDamage(unit, SpellSlot.R)*
                        DamageManager.TranscendentBladesCount)
                        R.Cast(unit, PacketCasting);
                }
            }
            catch (Exception ex)
            {
                Game.PrintChat("Exception! "+ex);
            }
        }
        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead || ObjectManager.Player.InFountain() || ObjectManager.Player.IsRecalling()) return;
//            PotionManager.__init();
            KillSteal();
            /* Below goes logic of buttons */
            if (Walker.ActiveMode == Orbwalking.OrbwalkingMode.None) return;
                var useE = Config.Item("eusage").GetValue<StringList>().SelectedIndex;
                var target = TargetSelector.GetTarget(Q.Range*2, TargetSelector.DamageType.Physical);
                // for some reason it bugsplats, i don't know why, whenever i press a button it does that.
                if (target != null)
                {
                    DamageManager.UseIgnite(target);
                    var nearestMinion = MinionsManager.GetNearestMinionNearPosition(target.Position);
                    if (Walker.ActiveMode == Orbwalking.OrbwalkingMode.Combo ||
                        Walker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                    {
                        if (Q.IsReady())
                        {
                            if (nearestMinion.Distance(target, false) < ObjectManager.Player.Distance(target, false) &&
                                nearestMinion != null)
                            {
                                if (DamageManager.GetSpellDamageQ(nearestMinion) > nearestMinion.Health + 35)
                                {
                                    Q.Cast(nearestMinion, PacketCasting);
                                }
                                if (W.IsReady() &&
                                    ObjectManager.Player.GetSpellDamage(nearestMinion, SpellSlot.W) +
                                    DamageManager.GetSpellDamageQ(nearestMinion) > nearestMinion.Health + 35)
                                {
                                    W.Cast(PacketCasting);
                                    Q.Cast(nearestMinion, PacketCasting);
                                }
                            }
                            else
                            {
                                if (!target.IsValidTarget((E.IsReady() ? E.Range : 300)))
                                    Q.Cast(target, PacketCasting);
                            }
                        }
                        if (E.IsReady() && target.IsValidTarget(E.Range))
                        {
                            if ((useE == 0 && target.HealthPercentage() > ObjectManager.Player.HealthPercentage())
                                || (useE == 2 && target.HealthPercentage() < ObjectManager.Player.HealthPercentage()) ||
                                useE == 3)
                                E.Cast(target, PacketCasting);
                        }
                        if (W.IsReady() && target.IsValidTarget(200f))
                        {
                            W.Cast(PacketCasting);
                        }
                        if (R.IsReady())
                        {
                            if (ObjectManager.Player.Distance(target, false) > E.Range && !E.IsReady())
                                R.Cast(PacketCasting);
                        }
                    }
                }
            Farming();
        }

        internal static void Farming()
        {
            try
            {
                if (Walker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear ||
                    Walker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
                {
                    var mined = MinionManager.GetMinions(Q.Range);
                    if (Config.Item("useQLC").GetValue<bool>() && Q.IsReady())
                    {
                        foreach (
                            var minion in
                                mined.Where(minion => minion.Health + 35 < DamageManager.GetSpellDamageQ(minion))
                                    .Where(minion => minion.Health + 35 > ObjectManager.Player.GetAutoAttackDamage(minion)))
                        {
                            Q.Cast(minion, PacketCasting);
                        }
                    }
                    if (Config.Item("alwaysBig").GetValue<bool>() && Q.IsReady())
                    {
                        foreach (var minionBig in mined.Where(m => m.Health + 35 < DamageManager.GetSpellDamageQ(m)
                                                                   &&
                                                                   (m.BaseSkinName.Contains("MinionSiege") ||
                                                                    m.BaseSkinName.Contains("Dragon") ||
                                                                    m.BaseSkinName.Contains("Baron"))))
                        {
                            Q.Cast(minionBig, PacketCasting);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Game.PrintChat("Exception at farming!");
            }
        }

        private static void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (Orbwalking.OrbwalkingMode.Combo != Walker.ActiveMode) return;
            if (!target.IsValidTarget(R.Range) || !unit.IsMe || !target.IsEnemy || !target.IsVisible || target.IsDead)
                return;
            if (!E.IsReady())
            {
                R.Cast(target.Position, PacketCasting);
            }
        }
    }
}