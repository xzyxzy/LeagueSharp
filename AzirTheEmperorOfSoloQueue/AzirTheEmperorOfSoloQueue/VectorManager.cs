using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace AzirTheEmperorOfSoloQueue
{
    class VectorManager
    {
        internal static List<GameObject> AzirObjects = new List<GameObject>();
        internal static void GameObject_OnCreate(GameObject obj, EventArgs args)
        {
            Game.PrintChat("Object created! "+obj.Name);
            if (obj.Name == "AzirSoldier")
            {
                AzirObjects.Add(obj);
            }
        }

        internal static GameObject GetSoldierNearMouse
        {
            get
            {
                return AzirObjects.FirstOrDefault(soldier => Game.CursorPos.Distance(soldier.Position) <= 450f);
            }
        }

        internal static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (sender.Name == "AzirSoldier")
            {
                AzirObjects.Remove(sender);
            }
        }

        internal static Vector2 NormalizePosition(Vector2 position)
        {
            var pos = ObjectManager.Player.Position.To2D();
            return pos.Distance(position) > 450 ? (pos - position).Normalized() * 450 : position;
        }

        internal static bool IsWithinSoldierRange(Obj_AI_Base unit)
        {
            return AzirObjects.Any(soldier => unit.Distance(soldier.Position) <= 450f);
        }

        internal static bool IsInFront(Vector3 unit, Vector3 direction)
        {
            var toTarget = (unit - direction).Normalized();
            /* True = Target is in front
             * False = Target is behind
             * */
            return Vector3.Dot(toTarget, direction) > 0;
        }
    }
}