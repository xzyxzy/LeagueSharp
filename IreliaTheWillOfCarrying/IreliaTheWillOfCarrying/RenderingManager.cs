using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;

namespace IreliaTheWillOfCarrying
{
    internal class RenderingManager
    {
        private const float BarWidth = 104;
        internal static List<Spell> Spells = new List<Spell>();
        private static readonly Line Line = new Line(Drawing.Direct3DDevice) {Width = 9};
        private static Utility.HpBarDamageIndicator.DamageToUnitDelegate _damageToUnit;
        private static Vector2 _barOffset = new Vector2(10, 20);

        internal static ColorBGRA ColorBgra(Color c)
        {
            var color = ColorBGRA.FromRgba(c.ToArgb());
            return new ColorBGRA(color.B, color.G, color.R, 90);
        }

        internal static void Drawing_OnDraw(EventArgs args)
        {
            if (Spells != null)
            {
                foreach (var spell in Spells.Where(s => s.Level > 0))
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, spell.Range,
                        spell.IsReady() ? Color.Coral : Color.Crimson);
                }
            }
            var target = TargetSelector.GetTarget(Irelia.Q.Range*2, TargetSelector.DamageType.Physical);
            if (target != null)
            {
                var nearestMinion = MinionsManager.GetNearestMinionNearPosition(target.Position);
                if (nearestMinion != null)
                {
                    var canKill = Irelia.W.IsReady() ? nearestMinion.Health < ObjectManager.Player.GetSpellDamage(nearestMinion, SpellSlot.W) + DamageManager.GetSpellDamageQ(nearestMinion) : nearestMinion.Health < DamageManager.GetSpellDamageQ(nearestMinion);
                    Render.Circle.DrawCircle(nearestMinion.Position, 75f, canKill ? Color.CadetBlue : Color.IndianRed);
                }
            }
            foreach (var unit in ObjectManager.Get<Obj_AI_Hero>().Where(u => u.IsValidTarget()))
            {
                var damage = DamageManager.TotalDamageToUnit(unit);
                if (damage < 1) continue;
                var damagePercentage = ((unit.Health - damage) > 0 ? (unit.Health - damage) : 0)/unit.MaxHealth;
                var currentHealthPercentage = unit.Health/unit.MaxHealth;
                var startPoint = new Vector2((int) (unit.HPBarPosition.X + _barOffset.X + damagePercentage*BarWidth),
                    (int) (unit.HPBarPosition.Y + _barOffset.Y) + 4);
                var endPoint =
                    new Vector2((int) (unit.HPBarPosition.X + _barOffset.X + currentHealthPercentage*BarWidth) + 1,
                        (int) (unit.HPBarPosition.Y + _barOffset.Y) + 4);

                // Draw the DirectX line
                Line.Begin();
                Line.Draw(new[] {startPoint, endPoint}, ColorBgra(Color.DodgerBlue));
                Line.End();
            }
        }

        internal static void Drawing_OnPreReset(EventArgs args)
        {
            Line.OnLostDevice();
        }

        internal static void Drawing_OnOnPostReset(EventArgs args)
        {
            Line.OnResetDevice();
        }

        internal static void OnProcessExit(object sender, EventArgs eventArgs)
        {
            Line.Dispose();
        }
    }
}