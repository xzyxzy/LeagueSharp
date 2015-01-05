using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SciasMightyAssistant;

namespace SciasMightyAssistantStandalone
{
    class Program
    {
        private static Assistant.Orbwalker Orbwalker;
        private static Menu Config;
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            Config = new Menu("SMA Standalone", "sma", true);

            Assistant.AddItem(ItemData.Ravenous_Hydra_Melee_Only);
            Assistant.AddItem(ItemData.Deathfire_Grasp);
            // Target Selector
            var ts = new Menu("Target Selector", "ts");
            TargetSelector.AddToMenu(ts);
            
            // Orbwalker
            var orb = new Menu("Scias Orbwalker", "sco");
            Orbwalker = new Assistant.Orbwalker(orb);

            Config.AddSubMenu(ts);
            Config.AddSubMenu(orb);
            Config.AddToMainMenu();
            Assistant.AfterAttack += Assistant_AfterAttack;
            Game.OnGameUpdate += Game_OnGameUpdate;
        }

        static void Assistant_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (ObjectManager.Player.IsMelee())
            {
                switch (Orbwalker.CurrentMode)
                {
                    case Assistant.Mode.LaneClear:
                        // Do something for LaneClear
                        break;
                    case Assistant.Mode.LaneFreeze:
                        // Do something for LaneFreeze
                        break;
                    case Assistant.Mode.LastHit:
                        // Do something for LastHit
                        break;
                }
            }
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            switch(Orbwalker.CurrentMode)
            {
                case Assistant.Mode.Combo:
                    // Do something for combo
                    OnCombo();
                    break;
                case Assistant.Mode.Mixed:
                    // Do something for mixed mode
                    break;
            }
            if(!ObjectManager.Player.IsMelee())
            {
                switch(Orbwalker.CurrentMode)
                {
                    case Assistant.Mode.LaneClear:
                        // Do something for LaneClear
                        break;
                    case Assistant.Mode.LaneFreeze:
                        // Do something for LaneFreeze
                        break;
                    case Assistant.Mode.LastHit:
                        // Do something for LastHit
                        break;
                }
            }
        }
        static void OnCombo()
        {
            var hydra = Assistant.GetItem(ItemData.Ravenous_Hydra_Melee_Only.Id);
            if (hydra.IsReady() && hydra.IsOwned())
                hydra.Cast();
        }
    }
}
