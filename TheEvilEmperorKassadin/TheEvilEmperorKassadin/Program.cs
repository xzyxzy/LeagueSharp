using System;
using System.Linq;
using System.Collections.Generic;
using LeagueSharp;
using LeagueSharp.Common;

namespace TheEvilEmperorKassadin
{
    internal class Program
    {
        internal static double Version = 0.3;
        internal static Spell Q, W, E, R;
        internal static SpellSlot IgniteSlot;
        internal static Orbwalking.Orbwalker Orb;
        internal static Menu Config;
        internal static Items.Item healthPot = new Items.Item(ItemData.Health_Potion.Id);
        internal static Items.Item manaPot = new Items.Item(ItemData.Mana_Potion.Id);
        internal static Items.Item crystallineFlask = new Items.Item(ItemData.Crystalline_Flask.Id);

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.ChampionName != "Kassadin")
                return;
            Q = new Spell(SpellSlot.Q, 650);
            W = new Spell(SpellSlot.W, 250);
            E = new Spell(SpellSlot.E, 700);
            R = new Spell(SpellSlot.R, 700);
            Q.SetTargetted(0.5f, 1400f);
            E.SetSkillshot(0.5f, 80f, float.MaxValue, false, SkillshotType.SkillshotCone);
            R.SetSkillshot(1f, 150f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            IgniteSlot = ObjectManager.Player.GetSpellSlot("SummonerDot");            
            MenuInit();

            Game.PrintChat("<font color='#ff00ff'>Kassadin</font> The Evil Emperor loaded!");


            Game.OnGameUpdate += Game_OnGameUpdate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (Config.Item("interruptSpells").GetValue<bool>() &&
                    Q.IsReady() && unit.IsValidTarget(Q.Range) && unit.IsChanneling)
                Q.Cast(unit, PacketCast);
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsRecalling()) return;
            PotionManager();
            if(Config.SubMenu("otherMenu").Item("Killsteal").GetValue<bool>())
                OnKillsteal();
            switch (Orb.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    OnCombo();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    ChargeW();
                    OnHarass();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                case Orbwalking.OrbwalkingMode.LastHit:
                case Orbwalking.OrbwalkingMode.None:
                    ChargeW();
                    break;
            }
        }
        private static void ChargeW()
        {
            if (!W.IsReady() && Config.Item("ChargeW").GetValue<Slider>().Value < GetEstacks) return;
            var target = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);
            var minions = MinionManager.GetMinions(W.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health);
            if (target != null) W.Cast(PacketCast);
            if (target == null && minions != null)
            {
                if(Orbwalking.OrbwalkingMode.None == Orb.ActiveMode)
                {
                    if (Utility.CountEnemysInRange((int)(Q.Range + R.Range)*2) < 1 && W.IsReady() && GetEstacks < 6)
                    {
                        W.Cast(PacketCast);
                    }
                    return;
                }
                if (Config.Item("ChargeW").GetValue<Slider>().Value > GetEstacks && GetEstacks < 6)
                {
                    foreach (var units in minions.Where(m => m.IsValidTarget(W.Range) && m.Health < ObjectManager.Player.GetSpellDamage(m, SpellSlot.W)))
                    {
                        W.Cast(PacketCast);
                        break;
                    }
                }
            }
        }
        private static bool spellKill(Obj_AI_Base unit, SpellSlot spell)
        {
            var spellDamage = ObjectManager.Player.GetSpellDamage(unit, spell);
            return spellDamage > unit.Health ? true : false; ;
        }
        private static double spellDamage(Obj_AI_Base unit, SpellSlot spell)
        {
            return ObjectManager.Player.GetSpellDamage(unit, spell);
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
                    if(IgniteSlot.IsReady())
                    {
                        ObjectManager.Player.Spellbook.CastSpell(IgniteSlot, unit);
                    }
                }
            }
            else
            {
                if(IgniteSlot.IsReady() && damage > targetHealth)
                {
                    ObjectManager.Player.Spellbook.CastSpell(IgniteSlot, unit);
                }
            }
        }
        private static bool DeathFireGraspNeed(Obj_AI_Hero unit)
        {
            var initDamage = unit.Health * 0.15;
            double totalDfgDamage = 0;
            double totalDamage = 0;
            if (Q.IsReady())
            {
                totalDfgDamage += spellDamage(unit, SpellSlot.Q) * 1.2;
                totalDamage += spellDamage(unit, SpellSlot.Q);
            }
            if (W.IsReady())
            {
                totalDfgDamage += spellDamage(unit, SpellSlot.W) * 1.2;
                totalDamage += spellDamage(unit, SpellSlot.W);
            }
            if (E.IsReady()) 
            {
                totalDfgDamage += spellDamage(unit, SpellSlot.E) * 1.2;
                totalDamage += spellDamage(unit, SpellSlot.E);
            }
            if (R.IsReady()) 
            {
                totalDfgDamage += spellDamage(unit, SpellSlot.R) * 1.2;
                totalDamage += spellDamage(unit, SpellSlot.R);
            }
            
            // Now let's compare the damage of non-dfg and dfg skills
            if (unit.Health < totalDamage) return false;
            if (unit.Health > totalDamage && unit.Health < totalDfgDamage + initDamage) return true;
            return false;
        }
        private static void OnKillsteal()
        {
            foreach(var c in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsEnemy && h.IsVisible && h.IsValidTarget(E.Range) && h.Health < spellDamage(h, SpellSlot.Q) + spellDamage(h, SpellSlot.E)))
            {
                if (Q.IsReady() && c.IsValidTarget(Q.Range) && c.Health < spellDamage(c, SpellSlot.Q))
                    Q.Cast(c, PacketCast);
                if (!Q.IsReady() && E.IsReady() && c.IsValidTarget(E.Range) && c.Health < spellDamage(c, SpellSlot.E)) // E is higher priority!!
                    E.Cast(c, PacketCast);
                if (Q.IsReady() && E.IsReady() && c.IsValidTarget(Q.Range) && c.Health < spellDamage(c,SpellSlot.Q))
                {
                    E.Cast(c, PacketCast);
                    Q.Cast(c, PacketCast);
                }
            }
        }
        private static void OnCombo()
        {
            var target = TargetSelector.GetTarget(1500, TargetSelector.DamageType.Magical);
            if (target == null)
                return;
            var mana = ObjectManager.Player.Mana;
            if (Config.SubMenu("otherMenu").Item("dfg").GetValue<bool>())
            {
                if (DeathFireGraspNeed(target) && Items.HasItem(ItemData.Deathfire_Grasp.Id) && Items.CanUseItem(ItemData.Deathfire_Grasp.Id))
                    Items.UseItem(ItemData.Deathfire_Grasp.Id, target);
            }
            if(Config.SubMenu("otherMenu").Item("ignite").GetValue<bool>())
                UseIgnite(target);
            if(R.IsReady() && target.IsValidTarget(Config.SubMenu("comboMenu").Item("minDistanceUlt").GetValue<Slider>().Value))
            {
                if (target.IsValidTarget(R.Range + 150))
                    R.Cast(target, PacketCast);
                if (target.IsValidTarget(Config.SubMenu("comboMenu").Item("maxDistanceGapClose").GetValue<Slider>().Value) && Config.SubMenu("comboMenu").Item("gapclose").GetValue<bool>())
                    R.Cast(target, PacketCast);
            }

            if(Q.IsReady() && mana > manaCost(SpellSlot.R) + manaCost(SpellSlot.E) && target.IsValidTarget(Q.Range))
                Q.Cast(target, PacketCast);
            if(E.IsReady() && mana > manaCost(SpellSlot.Q) + manaCost(SpellSlot.R) && target.IsValidTarget(E.Range))
                E.Cast(target, PacketCast);
            if (W.IsReady() && target.IsValidTarget(W.Range))
                W.Cast(PacketCast);
        }
        private static int GetEstacks
        {
            get
            {
                var data = ObjectManager.Player.Buffs.FirstOrDefault(b => b.DisplayName == "forcepulsecounter");
                return data != null ? data.Count : 0;
            }
        }

        private static bool PacketCast
        {
            get { return Config.Item("packetCast").GetValue<bool>(); }
        }

        private static void OnHarass()
        {
            var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            if (target == null)
                return;
            var mana = ObjectManager.Player.Mana;
            if (Q.IsReady() && mana > manaCost(SpellSlot.R) + manaCost(SpellSlot.E))
                Q.Cast(target, PacketCast);
            if (E.IsReady() && mana > manaCost(SpellSlot.Q) + manaCost(SpellSlot.R))
                E.Cast(target, PacketCast);
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Config.Item("noDraw").GetValue<bool>())
                return;
            if (Config.Item("drawQ").GetValue<bool>())
                Utility.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Cyan);
            if (Config.Item("drawW").GetValue<bool>())
                Utility.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Crimson);
            if (Config.Item("drawE").GetValue<bool>())
                Utility.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.ForestGreen);
            if (Config.Item("drawR").GetValue<bool>())
                Utility.DrawCircle(ObjectManager.Player.Position, R.Range, System.Drawing.Color.DarkCyan);
            if (Config.Item("drawRradius").GetValue<bool>())
                Utility.DrawCircle(ObjectManager.Player.Position, R.Range + 150, System.Drawing.Color.Gray);
            if (Config.Item("drawRQ").GetValue<bool>())
                Utility.DrawCircle(
                    ObjectManager.Player.Position, R.Range + Q.Range, System.Drawing.Color.MediumSpringGreen);
            if (Config.Item("drawMinDistanceUlt").GetValue<bool>())
                Utility.DrawCircle(
                    ObjectManager.Player.Position, Config.Item("minDistanceUlt").GetValue<Slider>().Value,
                    System.Drawing.Color.PowderBlue);
            if (Config.Item("drawMaxDistanceGapclose").GetValue<bool>())
                Utility.DrawCircle(
                    ObjectManager.Player.Position, Config.Item("maxDistanceGapclose").GetValue<Slider>().Value,
                    System.Drawing.Color.RoyalBlue);
            if (Config.Item("drawDamage").GetValue<bool>())
            {
                Utility.HpBarDamageIndicator.Color = System.Drawing.Color.Coral;
                Utility.HpBarDamageIndicator.DamageToUnit = CalculateDamageUnit;
                Utility.HpBarDamageIndicator.Enabled = Config.Item("drawDamage").GetValue<bool>();
            }
        }
        public static float manaCost(SpellSlot skill)
        {
            return ObjectManager.Player.Spellbook.GetSpell(skill).ManaCost;
            /*
            return SpellSlot.R == skill 
                    ? ObjectManager.Player.Spellbook.GetSpell(skill).ManaCost * ObjectManager.Player.GetSpell(skill).Ammo 
                        : ObjectManager.Player.Spellbook.GetSpell(skill).ManaCost;*/
        }
        public static void PotionManager()
        {
            if(ObjectManager.Player.InFountain()) return;
            if(healthPot.IsReady() && !ObjectManager.Player.HasBuff("RegenerationPotion",true))
            {
                if (Utility.CountEnemysInRange((int)(R.Range + Q.Range)*2) > 0 && ObjectManager.Player.Health + 150 < ObjectManager.Player.MaxHealth
                    || ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * 0.5)
                    healthPot.Cast();
            }
            if (manaPot.IsReady() && Utility.CountEnemysInRange((int)(R.Range + Q.Range)*2) > 0 && ObjectManager.Player.Mana < manaCost(SpellSlot.Q) + manaCost(SpellSlot.E) + manaCost(SpellSlot.R))
                manaPot.Cast();
            if (crystallineFlask.IsReady() && Utility.CountEnemysInRange((int)(R.Range + Q.Range) * 2) > 0 && (ObjectManager.Player.Mana < manaCost(SpellSlot.W) + manaCost(SpellSlot.E) + manaCost(SpellSlot.R) || ObjectManager.Player.Health + 120 < ObjectManager.Player.MaxHealth))
                crystallineFlask.Cast();
        }
        private static void UseItems()
        {
            var enemyCount = Utility.CountEnemysInRange((int)Q.Range);
            if (Config.SubMenu("itemMenu").Item("zh").GetValue<bool>())
            {
                // honya
                if(enemyCount > Config.SubMenu("itemMenu").Item("zhCount").GetValue<Slider>().Value && !HasSeraph)
                {
                    if(Items.HasItem(ItemData.Zhonyas_Hourglass.Id) && Items.CanUseItem(ItemData.Zhonyas_Hourglass.Id))
                    {
                        Items.UseItem(ItemData.Zhonyas_Hourglass.Id);
                    }
                }
            }
            if (Config.SubMenu("itemMenu").Item("se").GetValue<bool>() && !HasZhonya)
            {
                if (enemyCount > 1 && Items.HasItem(ItemData.Seraphs_Embrace.Id) && Items.CanUseItem(ItemData.Seraphs_Embrace.Id))
                    Items.UseItem(ItemData.Seraphs_Embrace.Id);
            }
        }

        static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
                return;
            UseItems();
            if (Q.IsReady() && sender.IsEnemy && !args.SData.IsAutoAttack())
            {
                var spell = sender.GetDamageSpell(ObjectManager.Player, args.SData.Name);
                if (spell.DamageType == Damage.DamageType.Magical && sender.IsValid<Obj_AI_Hero>())
                {
                    // Targetted & skillshots spells
                    if (args.Target.IsMe || ObjectManager.Player.Distance(args.Start) < 500 || ObjectManager.Player.Distance(args.End) < 500)
                    {
                        if(sender.IsValidTarget(Q.Range))
                            Q.Cast(sender, PacketCast);
                        else // if its not a valid lets do a more complicated method
                        {
                            var minions = MinionManager.GetMinions(Q.Range);
                            if (minions.Count > 0)
                            {
                                foreach (var minion in minions)
                                {
                                    Q.Cast(minion);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void MenuInit()
        {
            // Menu
            Config = new Menu("The Evil Emperor Kassadin","HMKassadin",true);

            // TS
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            // Orbwalker
            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orb = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            // Combo
            var combo = new Menu("Combo settings", "comboMenu");
            combo.AddItem(
                new MenuItem("minDistanceUlt", "Min distance to use ult in combo").SetValue(new Slider(350, 300, (int)R.Range)));
            combo.AddItem(new MenuItem("drawMinDistanceUlt", "Draw min R distance").SetValue(true));
            combo.AddItem(new MenuItem("1", "-------"));
            combo.AddItem(new MenuItem("gapclose", "Use (R) to gapclose").SetValue(true));
            combo.AddItem(
                new MenuItem("maxDistanceGapclose", "Max range to use ult for gapclose").SetValue(
                    new Slider((int) (R.Range + Q.Range - 100), 1000, (int) (R.Range + Q.Range))));
            combo.AddItem(
                new MenuItem("drawMaxDistanceGapclose", "Draw max range to use ult for gapclose").SetValue(true));
            Config.AddSubMenu(combo);

            // Harass
            var harass = new Menu("Harass settings", "harrasMenu");
            harass.AddItem(new MenuItem("useQ", "Use Q").SetValue(true));
            harass.AddItem(new MenuItem("useW", "Use W").SetValue(true));
            harass.AddItem(new MenuItem("useE", "Use E").SetValue(true));
            Config.AddSubMenu(harass);

            // Item
            var itemMenu = new Menu("Items", "itemMenu");
            itemMenu.AddItem(new MenuItem("zh", "Use Zhonya's Hourglass").SetValue(true));
            itemMenu.AddItem(new MenuItem("zhCount", "Zhonya at X enemy around you").SetValue(new Slider(2,1,5)));
            itemMenu.AddItem(new MenuItem("se", "Seraph's Embrace").SetValue(true));
            Config.AddSubMenu(itemMenu);

            // Others
            var otherMenu = new Menu("Other", "otherMenu");
            otherMenu.AddItem(new MenuItem("dfg", "Use DFG").SetValue(true));
            otherMenu.AddItem(new MenuItem("ignite", "Use Ignite").SetValue(true));
            otherMenu.AddItem(new MenuItem("Killsteal", "Steal kills baby").SetValue(true));
            otherMenu.AddItem(new MenuItem("99", "-------"));
            otherMenu.AddItem(new MenuItem("packetCast", "Packet Casting").SetValue(true));
            otherMenu.AddItem(new MenuItem("interruptSpells", "Interrupt channeled spells").SetValue(true));
            otherMenu.AddItem(new MenuItem("999", "-------"));
            otherMenu.AddItem(new MenuItem("ChargeW", "Use (W) to charge (E) to ").SetValue(new Slider(5, 1, 5)));
            Config.AddSubMenu(otherMenu);
            
            // Draw
            var draw = new Menu("Draw Menu", "draw");
            draw.AddItem(new MenuItem("noDraw","Disable all draw").SetValue(false));
            draw.AddItem(new MenuItem("5", "--- Skill Ranges ---"));
            draw.AddItem(new MenuItem("drawQ", "Draw Q range").SetValue(true));
            draw.AddItem(new MenuItem("drawW", "Draw W range").SetValue(false));
            draw.AddItem(new MenuItem("drawE", "Draw E range").SetValue(false));
            draw.AddItem(new MenuItem("drawR", "Draw R range").SetValue(false));
            draw.AddItem(new MenuItem("drawRradius", "Draw R radius").SetValue(false));
            draw.AddItem(new MenuItem("drawRQ", "Draw max range (gapcloser)").SetValue(false));
            draw.AddItem(new MenuItem("drawDamage", "Draw damage").SetValue(true));
            Config.AddSubMenu(draw);

            Config.AddToMainMenu();
        }
        private static float CalculateDamageUnit(Obj_AI_Base target)
        {
            if (target == null)
                return 0;
            var damage = 0d;
            if (Q.IsReady())
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q);
            if (W.IsReady())
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.W);
            if (E.IsReady())
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.E);
            if (R.IsReady())
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.R);
            if (Items.HasItem(ItemData.Deathfire_Grasp.Id) && Items.CanUseItem(ItemData.Deathfire_Grasp.Id))
                damage += target.MaxHealth * 0.15 + damage * 1.2;
            if (IgniteSlot != SpellSlot.Unknown &&
                ObjectManager.Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                damage += ObjectManager.Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
            return (float)damage;
        }
        private static bool HasZhonya
        {
            get { return ObjectManager.Player.HasBuff("zhonyasringshield"); }
        }

        private static bool HasSeraph
        {
            get { return ObjectManager.Player.HasBuff("ItemSeraphsEmbrace"); }
        }
    }
}
