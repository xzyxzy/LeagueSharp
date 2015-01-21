using System;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Matrix = SharpDX.Matrix;

namespace AzirTheEmperorOfSoloQueue
{
    class DrawingManager
    {
        public static void Init()
        {
            Drawing.OnDraw += Drawing_OnDraw;
        }

        public static double ToRadians(double val)
        {
            return (Math.PI / 180) * val;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Emperor.Config.Item("drawInsec").GetValue<bool>())
            {
                var target = TargetSelector.GetTarget(1250f, TargetSelector.DamageType.Magical);
                if (target != null)
                {
                    var newPos = Drawing.WorldToScreen(target.Position.Extend(Game.CursorPos, 450));
                    Drawing.DrawLine(Drawing.WorldToScreen(ObjectManager.Player.Position), Drawing.WorldToScreen(target.Position.Extend(Game.CursorPos, 450)), 3, System.Drawing.Color.White);
                    var extended = Drawing.WorldToScreen(target.Position.Extend(ObjectManager.Player.Position, -250));
                    Drawing.DrawLine(newPos, extended, 3, System.Drawing.Color.Cyan);
                    Utility.DrawCircle(Drawing.ScreenToWorld(extended), 30, System.Drawing.Color.Red);
                }
            }
        }
    }
}
