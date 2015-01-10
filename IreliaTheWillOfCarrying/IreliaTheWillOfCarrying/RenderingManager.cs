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
        internal static List<Spell> Spells = new List<Spell>();
        internal static void Drawing_OnDraw(EventArgs args)
        {
            if (Spells.Count > 0)
            {
                foreach (var spell in Spells.Where(s => s.Level > 0))
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, spell.Range,
                        spell.IsReady() ? Color.Coral : Color.Crimson);
                }
            }

        }
    }
}