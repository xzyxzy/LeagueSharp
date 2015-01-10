using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

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
            misc.AddItem(new MenuItem("gap", "Gap-Closers => ").SetValue(new StringList(new[] {"Off", "Stun", "Slow", "Any"}, 3)));
            Config.AddSubMenu(misc);

            Game.OnGameUpdate += Game_OnGameUpdate;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            Drawing.OnDraw += RenderingManager.Drawing_OnDraw;
            Drawing.OnPreReset += RenderingManager.Drawing_OnPreReset;
            Drawing.OnPostReset += RenderingManager.Drawing_OnOnPostReset;
            AppDomain.CurrentDomain.DomainUnload += RenderingManager.OnProcessExit;
            AppDomain.CurrentDomain.ProcessExit += RenderingManager.OnProcessExit;
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
            var m = Config.Item("interrupt").GetValue<bool>();
            if (!unit.IsEnemy || !E.IsReady() || !(unit.HealthPercentage() > ObjectManager.Player.HealthPercentage()) || !unit.IsChanneling)
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
        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead || ObjectManager.Player.InFountain() || ObjectManager.Player.IsRecalling()) return;
            PotionManager.__init();
            try
            {
                // null exception
                if (ObjectManager.Get<Obj_AI_Hero>() != null && Q.IsReady())
                {
                    foreach (
                        var unit in
                            ObjectManager.Get<Obj_AI_Hero>()
                                .Where(
                                    h =>
                                        h.IsEnemy && h.IsValidTarget(Q.Range - 150f) && h.IsVisible &&
                                        h.Health <
                                        DamageManager.GetSpellDamageQ(h)))
                    {
                        Orbwalking.ResetAutoAttackTimer();
                        Q.Cast(unit, PacketCasting);
                    }
                }
            }
            catch (Exception ex)
            {
                Game.PrintChat("Kill-Steal failed to initialize!");
            }

            /* Below goes logic of buttons */
            if (Walker.ActiveMode == Orbwalking.OrbwalkingMode.None) return;

            var target = TargetSelector.GetTarget(Q.Range*2, TargetSelector.DamageType.Physical);
            if (target != null)
            {
                DamageManager.UseIgnite(target);
                var nearestMinion = MinionsManager.GetNearestMinionNearPosition(target.Position);
                if (Walker.ActiveMode == Orbwalking.OrbwalkingMode.Combo ||
                    Walker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                {
                    // todo: bug doesnt use q ?
                    if (Q.IsReady() && nearestMinion.Distance(target, false) < ObjectManager.Player.Distance(target, false) &&
                        nearestMinion != null)
                    {
                        if (DamageManager.GetSpellDamageQ(nearestMinion)*0.9 > nearestMinion.Health)
                        {
                            Orbwalking.ResetAutoAttackTimer();
                            Q.Cast(nearestMinion, PacketCasting);
                        }
                        if (W.IsReady() && !DamageManager.HasHitenBuff &&
                            ObjectManager.Player.GetSpellDamage(nearestMinion, SpellSlot.W) +
                            DamageManager.GetSpellDamageQ(nearestMinion)*0.9 > nearestMinion.Health)
                        {
                            Orbwalking.ResetAutoAttackTimer();
                            W.Cast(PacketCasting);
                            Q.Cast(nearestMinion, PacketCasting);
                        }
                    }
                    if (E.IsReady() && target.IsValidTarget(E.Range))
                    {
                        E.Cast(target, PacketCasting);
                    }
                    if (W.IsReady() && target.IsValidTarget(250f))
                    {
                        W.Cast(PacketCasting);
                    }
                    if (R.IsReady())
                    {
                        if (ObjectManager.Player.Distance(target,false) > E.Range && !E.IsReady())
                            R.Cast(PacketCasting);
                    }
                }
            }

            if (Walker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear ||
                Walker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
            {
                var mined = MinionManager.GetMinions(Q.Range);
                if (Config.Item("useQLC").GetValue<bool>() && Q.IsReady())
                {
                    foreach (var minion in mined.Where(minion => minion.Health < DamageManager.GetSpellDamageQ(minion)).Where(minion => minion.Health > ObjectManager.Player.GetAutoAttackDamage(minion)))
                    {
                        Orbwalking.ResetAutoAttackTimer();
                        Q.Cast(minion, PacketCasting);
                    }
                }
                if (Config.Item("alwaysBig").GetValue<bool>() && Q.IsReady())
                {
                    foreach (var minionBig in mined.Where(m => m.Health < DamageManager.GetSpellDamageQ(m)
                                                                &&
                                                                (m.BaseSkinName.Contains("MinionSiege") ||
                                                                m.BaseSkinName.Contains("Dragon") ||
                                                                m.BaseSkinName.Contains("Baron"))))
                    {
                        Orbwalking.ResetAutoAttackTimer();
                        Q.Cast(minionBig, PacketCasting);
                    }
                }
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