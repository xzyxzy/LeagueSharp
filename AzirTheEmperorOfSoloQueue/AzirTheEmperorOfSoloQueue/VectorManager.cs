using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace AzirTheEmperorOfSoloQueue
{
    class VectorManager
    {
        internal static GameObject AzirObject;
        internal static void GameObject_OnCreate(GameObject obj, EventArgs args)
        {
            Game.PrintChat("Object created! "+obj.Name);
            if (obj.Name == "AzirSoldier")
            {
                AzirObject = obj;
            }
        }

        internal static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (sender.Name == "AzirSoldier")
            {
                AzirObject = null;
            }
        }

        internal static Vector2 NormalizePosition(Vector2 position)
        {
            var pos = ObjectManager.Player.Position.To2D();
            return pos.Distance(position) > 450 ? (pos - position).Normalized() * 450 : position;
        }

        internal static bool IsWithinSoldierRange(Obj_AI_Base unit)
        {
            return unit.Distance(AzirObject.Position) <= 450;
        }

        internal static bool IsInFront(Vector3 unit, Vector3 direction)
        {
            double infront = (ObjectManager.Player.Position.X - unit.X)*direction.X
                             + (ObjectManager.Player.Position.Y - unit.Y)*direction.Y
                             + (ObjectManager.Player.Position.Z - unit.Z)*direction.Z;
            return infront > 0.0;
        }
    }
}