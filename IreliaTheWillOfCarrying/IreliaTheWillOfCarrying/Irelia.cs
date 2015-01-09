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

            Config = new Menu("Scias Irelia", "irelia", true);
            var ts = new Menu("Target Selector", "ts");
            TargetSelector.AddToMenu(ts);
            Config.AddSubMenu(ts);

            Walker = new Orbwalking.Orbwalker(Config);
            Config.AddToMainMenu();

            var lc = new Menu("Lane Clear/Last Hit", "lc");
            lc.AddItem(new MenuItem("useQLC", "Use (Q) to last hit"));
            Config.AddSubMenu(lc);

            var misc = new Menu("Settings", "sets");
            misc.AddItem(new MenuItem("packet", "Packet Casting").SetValue(true));
            misc.AddItem(new MenuItem("interrupt", "Interrupt spells with (Q minion)+(E stun)").SetValue(new StringList(new []{"Off", "If I won't die"},2)));
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
            var m = Config.Item("interrupt").GetValue<StringList>().SelectedIndex;
            if (m == 0 || !unit.IsEnemy || !E.IsReady() || !(unit.HealthPercentage() > ObjectManager.Player.HealthPercentage()) || !unit.IsChanneling)
                return;
            var minion = MinionManager.GetNearestMinionNearPosition(unit.Position);
            if ((m == 1 && unit.CountEnemysInRange(Q.Range) < (Walker.ActiveMode == Orbwalking.OrbwalkingMode.Combo ? 3 : 2)))
            {
                Q.Cast(minion ?? unit, true);
                E.Cast(unit, true);
            }
        }

        private static bool PacketCasting
        {
            get { return Config.Item("packet").GetValue<bool>(); }
        }
        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (ObjectManager.Player.InFountain() || ObjectManager.Player.IsRecalling()) return;
            PotionManager.__init();

            foreach (var unit in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsEnemy && h.IsValidTarget(Q.Range-150f) && h.IsVisible))
            {
                if (Config.Item("ignite").GetValue<bool>())
                    DamageManager.UseIgnite(unit);
                if (Q.IsReady() && DamageManager.GetSpellDamageQ(unit) > unit.Health)
                    Q.Cast(unit, PacketCasting);
                if (!W.IsReady() || !Q.IsReady() ||
                    !(unit.Health <
                      ObjectManager.Player.GetSpellDamage(unit, SpellSlot.Q) +
                      ObjectManager.Player.GetSpellDamage(unit, SpellSlot.W))) continue;
                W.Cast(PacketCasting);
                Q.Cast(unit, PacketCasting);
            }
            var target = TargetSelector.GetTarget(Q.Range*2, TargetSelector.DamageType.Physical);
            if (Walker.ActiveMode != Orbwalking.OrbwalkingMode.None) return;
            var nearestMinion = target == null ? null : MinionManager.GetNearestMinionNearPosition(target.Position);
            if (Q.IsReady())
            {
                if (nearestMinion != null &&
                    (Walker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed ||
                     Walker.ActiveMode == Orbwalking.OrbwalkingMode.Combo))
                {
                    if (DamageManager.GetSpellDamageQ(nearestMinion)*0.9 > nearestMinion.Health)
                    {
                        Q.Cast(nearestMinion, true);
                    }
                    // this will use W if we dont have enough dmg and then the loop will reload and use the above Q
                    if (W.IsReady() && !DamageManager.HasHitenBuff && ObjectManager.Player.GetSpellDamage(nearestMinion,SpellSlot.W) + DamageManager.GetSpellDamageQ(nearestMinion)*0.9 > nearestMinion.Health)
                    {
                        W.Cast(true);
                    }
                }
                else
                {
                    if (Config.Item("useQLC").GetValue<bool>() && (Walker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear ||
                        Orbwalking.OrbwalkingMode.LastHit == Walker.ActiveMode))
                    {
                        var mined = LeagueSharp.Common.MinionManager.GetMinions(Q.Range);
                        foreach (
                            var minion in mined.Where(minion => minion.Health > DamageManager.GetSpellDamageQ(minion)))
                        {
                            Q.Cast(minion);
                        }

                        var bigs =
                            ObjectManager.Get<Obj_AI_Minion>()
                                .Where(
                                    m =>
                                        m.Health < DamageManager.GetSpellDamageQ(m) && m.IsValidTarget(E.Range) &&
                                        (m.BaseSkinName.Contains("MinionSiege") || m.BaseSkinName.Contains("Dragon") ||
                                         m.BaseSkinName.Contains("Baron")));
                        foreach (var minionbig in bigs)
                        {
                            Q.Cast(minionbig);
                            break;
                        }
                    }
                    else
                    {
                        Q.Cast(target, true);
                    }
                }
            }
            if (target == null) return;
            if (W.IsReady() && target.IsValidTarget(250f) && Walker.ActiveMode != Orbwalking.OrbwalkingMode.LaneClear &&
                Walker.ActiveMode != Orbwalking.OrbwalkingMode.LastHit)
            {
                W.Cast(true);
            }
            if (E.IsReady() && target.IsValidTarget(425f) && Walker.ActiveMode != Orbwalking.OrbwalkingMode.LaneClear &&
                Walker.ActiveMode != Orbwalking.OrbwalkingMode.LastHit)
            {
                E.Cast(target, true);
            }
            if (Walker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && DamageManager.TranscendentBladesCount > 0 &&
                R.IsReady() && !target.IsValidTarget(300f))
            {
                R.Cast(target, true);
            }
        }

        private static void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (Orbwalking.OrbwalkingMode.Combo != Walker.ActiveMode) return;
            if (!target.IsEnemy || !unit.IsMe || !target.IsValidTarget(R.Range)) return;
            if (!E.IsReady() && !target.IsValidTarget(E.Range - 25f))
                R.Cast(target.Position, true);
        }
    }
}