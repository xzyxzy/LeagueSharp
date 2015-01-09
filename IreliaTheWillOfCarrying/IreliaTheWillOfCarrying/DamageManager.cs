using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace IreliaTheWillOfCarrying
{
    internal class DamageManager
    {
        internal static bool HasHitenBuff
        {
            get { return ObjectManager.Player.HasBuff("IreliaHitenStyleCharged"); }
        }

        internal static int TranscendentBladesCount
        {
            get
            {
                var blades = ObjectManager.Player.Buffs.FirstOrDefault(b => b.DisplayName == "IreliaTranscendentBlades");
                return blades != null ? blades.Count : 0;
            }
        }

        internal static float GetSpellDamageQ(Obj_AI_Base unit)
        {
            return (float) ObjectManager.Player.GetSpellDamage(unit, SpellSlot.Q)
                   + (float) (HasHitenBuff ? ObjectManager.Player.GetSpellDamage(unit, SpellSlot.W) : 0) +
                   GetSheenDamage(unit);
        }

        internal static float GetSheenDamage(Obj_AI_Base target)
        {
            return Items.HasItem(ItemData.Sheen.Id) && ObjectManager.Player.HasBuff("Sheen")
                ? ObjectManager.Player.BaseAttackDamage
                : (Items.HasItem(ItemData.Trinity_Force.Id) && ObjectManager.Player.HasBuff("Sheen")
                    ? ObjectManager.Player.BaseAttackDamage*2
                    : 0);
        }

        internal static float TotalDamageToUnit(Obj_AI_Base target)
        {
            var damage = 0d;
            if (Irelia.Q.IsReady()) damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q);
            if (Irelia.W.IsReady()) damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.W);
            if (Irelia.E.IsReady()) damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.E);
            if (Irelia.R.IsReady())
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.R)*
                          (TranscendentBladesCount > 0 ? TranscendentBladesCount : 3);
            if (Irelia.IgniteSlot != SpellSlot.Unknown &&
                ObjectManager.Player.Spellbook.CanUseSpell(Irelia.IgniteSlot) == SpellState.Ready)
                damage += ObjectManager.Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
            if (ObjectManager.Player.HasBuff("Sheen")) damage += GetSheenDamage(target);
            return (float) damage;
        }
        internal static void UseIgnite(Obj_AI_Hero unit)
        {
            var damage = Irelia.IgniteSlot == SpellSlot.Unknown || ObjectManager.Player.Spellbook.CanUseSpell(Irelia.IgniteSlot) != SpellState.Ready ? 0 : ObjectManager.Player.GetSummonerSpellDamage(unit, Damage.SummonerSpell.Ignite);
            var targetHealth = unit.Health;
            var hasPots = Items.HasItem(ItemData.Health_Potion.Id) || Items.HasItem(ItemData.Crystalline_Flask.Id);
            if (hasPots || unit.HasBuff("RegenerationPotion", true))
            {
                if (!(damage*0.5 > targetHealth)) return;
                if (Irelia.IgniteSlot.IsReady())
                {
                    ObjectManager.Player.Spellbook.CastSpell(Irelia.IgniteSlot, unit);
                }
            }
            else
            {
                if (Irelia.IgniteSlot.IsReady() && damage > targetHealth)
                {
                    ObjectManager.Player.Spellbook.CastSpell(Irelia.IgniteSlot, unit);
                }
            }
        }
    }
}