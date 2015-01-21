using System;
using LeagueSharp;
using LeagueSharp.Common;

namespace AzirTheEmperorOfSoloQueue
{
    class DrawingManager
    {
        public static void Init()
        {
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var target = TargetSelector.GetTarget(2000f, TargetSelector.DamageType.Magical);
            if (target != null)
            {
                Drawing.DrawLine(ObjectManager.Player.Position.To2D(), Game.CursorPos.To2D(), 15, System.Drawing.Color.Blue);
                Drawing.DrawLine(Game.CursorPos.To2D(), target.Position.To2D(), 15, System.Drawing.Color.Blue);
            }
        }
    }
}
