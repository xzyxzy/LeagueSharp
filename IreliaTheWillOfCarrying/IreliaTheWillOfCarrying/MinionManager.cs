using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace IreliaTheWillOfCarrying
{
    internal class MinionManager
    {
        internal static Obj_AI_Base GetNearestMinionNearPosition(Vector3 pos)
        {
            var minions = LeagueSharp.Common.MinionManager.GetMinions(Irelia.Q.Range);
            return minions.OrderBy(m => m.Distance(pos, false)).ThenBy(m => m.Health).FirstOrDefault();
        }
    }
}