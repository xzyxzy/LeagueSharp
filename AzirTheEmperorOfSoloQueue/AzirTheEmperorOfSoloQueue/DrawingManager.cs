using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace AzirTheEmperorOfSoloQueue
{
    class DrawingManager
    {
        public static List<Spell> SpellList = new List<Spell>(); 
        public static void Init()
        {
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList.Where(d => Emperor.Config.Item("draw"+d.Slot).GetValue<bool>() && d.Level > 0))
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, spell.Range, spell.IsReady() ? System.Drawing.Color.AntiqueWhite : System.Drawing.Color.Red);
            }
            if (Emperor.Config.Item("drawSoldier").GetValue<bool>())
            {
                foreach (var soldier in VectorManager.AzirObjects)
                {
                    Render.Circle.DrawCircle(soldier.Position, 450f, System.Drawing.Color.RoyalBlue);
                }
            }
            if (Emperor.Config.Item("drawInsec").GetValue<bool>())
            {
                var target = TargetSelector.GetTarget(1250f, TargetSelector.DamageType.Magical);
                if (target != null)
                {
                    var newPos = Drawing.WorldToScreen(target.Position.Extend(Game.CursorPos, 450));
                    Drawing.DrawLine(Drawing.WorldToScreen(ObjectManager.Player.Position), Drawing.WorldToScreen(target.Position.Extend(Game.CursorPos, 450)), 3, System.Drawing.Color.White);
                    var extended = Drawing.WorldToScreen(target.Position.Extend(ObjectManager.Player.Position, -250));
                    Drawing.DrawLine(newPos, extended, 3, System.Drawing.Color.Cyan);
                    Render.Circle.DrawCircle(Drawing.ScreenToWorld(extended), 30, System.Drawing.Color.Red);
                }
            }
        }
    }
}
