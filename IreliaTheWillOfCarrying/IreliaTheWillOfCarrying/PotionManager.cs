using System;
using LeagueSharp;
using LeagueSharp.Common;

namespace IreliaTheWillOfCarrying
{
    internal class PotionManager
    {
        internal static Items.Item HealthPot = new Items.Item(ItemData.Health_Potion.Id);
        internal static Items.Item Cookie = new Items.Item((ItemData.Total_Biscuit_of_Rejuvenation.Id));
        internal static Items.Item ManaPot = new Items.Item(ItemData.Mana_Potion.Id);
        internal static Items.Item CrystallineFlask = new Items.Item(ItemData.Crystalline_Flask.Id);

        internal static void __init()
        {
            try
            {
                // FlaskOfCrystalWater - Mana Potion
                // RegenerationPotion - Health Potion
                // ItemCrystalFlask - Flask
                if (ObjectManager.Player.InFountain() || ObjectManager.Player.IsRecalling()) return;

                // Health Potion
                if (HealthPot.IsOwned() && HealthPot.IsReady() && !ObjectManager.Player.HasBuff("RegenerationPotion", true) &&
                    !ObjectManager.Player.HasBuff("ItemCrystalFlask", true))
                {
                    if (ObjectManager.Player.CountEnemysInRange((Irelia.R.Range + Irelia.Q.Range)*2) > 0 &&
                        ObjectManager.Player.Health + 150 < ObjectManager.Player.MaxHealth
                        || ObjectManager.Player.Health < ObjectManager.Player.MaxHealth*0.5)
                    {
                        HealthPot.Cast();
                    }
                }

                // Mana Potion
                if (ManaPot.IsOwned() && ManaPot.IsReady() && !ObjectManager.Player.HasBuff("FlaskOfCrystalWater", true) &&
                    !ObjectManager.Player.HasBuff("ItemCrystalFlask", true) && ManaPot.IsReady() &&
                    ObjectManager.Player.CountEnemysInRange((Irelia.R.Range + Irelia.Q.Range)*2) > 0 &&
                    ObjectManager.Player.Mana < ManaCost(SpellSlot.Q) + ManaCost(SpellSlot.E) + ManaCost(SpellSlot.R))
                {
                    ManaPot.Cast();
                }

                // Crystalline Flask
                if (CrystallineFlask.IsOwned() && !ObjectManager.Player.HasBuff("FlaskOfCrystalWater", true)
                    && !ObjectManager.Player.HasBuff("ItemCrystalFlask", true)
                    && !ObjectManager.Player.HasBuff("RegenerationPotion", true)
                    && CrystallineFlask.IsReady() &&
                    ObjectManager.Player.CountEnemysInRange((Irelia.R.Range + Irelia.Q.Range)*2) > 0 &&
                    (ObjectManager.Player.Mana < ManaCost(SpellSlot.W) + ManaCost(SpellSlot.E) + ManaCost(SpellSlot.R) ||
                     ObjectManager.Player.Health + 120 < ObjectManager.Player.MaxHealth))
                    CrystallineFlask.Cast();
            }
            catch (Exception ex)
            {
                Game.PrintChat("Potion manager failed!");   
            }
        }

        internal static float ManaCost(SpellSlot skill)
        {
            return ObjectManager.Player.Spellbook.GetSpell(skill).ManaCost;
        }
    }
}