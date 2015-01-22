using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;
namespace Scias_Jax
{
    class MenuHandler
    {
        internal static Menu Config;
        internal static void Load()
        {
            Config = new Menu("Scias Jax", "Scias_Jax", true);
            Menu targetSelector = new Menu("Target selector", "ts");
            TargetSelector.AddToMenu(targetSelector);
            Config.AddSubMenu(targetSelector);

            Config.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            GameHandler.Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalker"));

            Config.AddSubMenu(new Menu("Auto Carry", "ac"));
            Config.SubMenu("ac").AddSubMenu(new Menu("Use BotRK on", "botrk_menu"));

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
            {
                Config.SubMenu("ac").SubMenu("botrk_menu").AddItem(new MenuItem("botrk_"+enemy.BaseSkinName, enemy.BaseSkinName).SetValue(true));
            }

            Config.SubMenu("ac").AddSubMenu(new Menu("Use Q", "q_menu"));
            Config.SubMenu("ac").SubMenu("q_menu").AddItem(new MenuItem("acQ_useIfWorth", "Use F+Q+W if worth").SetValue(true));
            Config.SubMenu("ac").SubMenu("q_menu").AddItem(new MenuItem("acQ_useIfWorthEnemy", "Maximum enemies in range: ").SetValue(new Slider(2, 1, 5)));
            Config.SubMenu("ac").AddItem(new MenuItem("force_sheen", "Force Sheen proc before W").SetValue(true));

            Config.AddSubMenu(new Menu("Mixed", "mx"));
            Config.SubMenu("mx").AddItem(new MenuItem("about", "This is automatic"));
            Config.SubMenu("mx").AddItem(new MenuItem("about1", "Hold mixed key and if"));
            Config.SubMenu("mx").AddItem(new MenuItem("about3", "you dont have lvl 6 it will"));
            Config.SubMenu("mx").AddItem(new MenuItem("about4", "use E+W+Q or if you have lvl 6"));
            Config.SubMenu("mx").AddItem(new MenuItem("about5", "then you need 2 auto's and then"));
            Config.SubMenu("mx").AddItem(new MenuItem("about6", "hold mixed button for high burst dmg"));

            Config.AddSubMenu(new Menu("Lane/Jungle clear", "cl"));
            Config.SubMenu("cl").AddItem(new MenuItem("clear_w", "Use W").SetValue(true));
            Config.SubMenu("cl").AddItem(new MenuItem("clear_e", "Use E").SetValue(true));

            Config.AddSubMenu(new Menu("Advanced", "advanced"));
            Config.SubMenu("advanced").AddSubMenu(new Menu("Smart E", "e_menu"));
            Config.SubMenu("advanced").SubMenu("e_menu").AddItem(new MenuItem("interruptE", "Interrupt (E) enemy cast").SetValue(true));
            Config.SubMenu("advanced").SubMenu("e_menu").AddItem(new MenuItem("gapclose_E", "Prevent gap-closing").SetValue(true));
            Config.SubMenu("advanced").SubMenu("e_menu").AddItem(new MenuItem("gapcloseRange_E", "Gap-close range").SetValue(new Slider(250, 200, 400)));

            Config.SubMenu("advanced").AddSubMenu(new Menu("Smart R", "r_menu"));
            Config.SubMenu("advanced").SubMenu("r_menu").AddItem(new MenuItem("useR_under", "Use if under HP %").SetValue(new Slider(50, 10)));
            Config.SubMenu("advanced").SubMenu("r_menu").AddItem(new MenuItem("useR_when", "Use when X enemy around").SetValue(new Slider(2, 1, 5)));
            Config.SubMenu("advanced").SubMenu("r_menu").AddItem(new MenuItem("useR", "Enabled").SetValue(true));
            Config.SubMenu("advanced").SubMenu("r_menu").AddSubMenu(new Menu("Modes", "modes"));
            Config.SubMenu("advanced").SubMenu("r_menu").SubMenu("modes").AddItem(new MenuItem("useR_combo", "Use in combo mode").SetValue(true));
            Config.SubMenu("advanced").SubMenu("r_menu").SubMenu("modes").AddItem(new MenuItem("useR_mixed", "Use in mixed mode").SetValue(true));
            Config.SubMenu("advanced").SubMenu("r_menu").SubMenu("modes").AddItem(new MenuItem("useR_flee", "Use in flee mode").SetValue(true));

            Config.SubMenu("advanced").AddItem(new MenuItem("Ward", "Ward Jump")).SetValue(new KeyBind('T', KeyBindType.Press));
            Config.SubMenu("advanced").AddItem(new MenuItem("Flee", "Flee mode")).SetValue(new KeyBind('G', KeyBindType.Press));
            Config.SubMenu("advanced").AddItem(new MenuItem("ks_enabled", "Kill-Steal").SetValue(true));
            Config.SubMenu("advanced").AddItem(new MenuItem("packetCast", "Packet Casting").SetValue(true));
            Config.SubMenu("advanced").AddItem(new MenuItem("debug", "Debug passive").SetValue(true));

            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("rangeQ", "Q range").SetValue(new Circle(true, Color.FromArgb(100, Color.Red))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("rangeE", "E range").SetValue(new Circle(true, Color.FromArgb(100, Color.BlueViolet))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("drawHp", "Draw combo damage").SetValue(true));
            Config.SubMenu("Drawings").AddItem(new MenuItem("drawWard", "Draw jumping radius").SetValue(true));
            Config.AddToMainMenu();
        }
    }
}
