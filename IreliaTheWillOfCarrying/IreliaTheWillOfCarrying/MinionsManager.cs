using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace IreliaTheWillOfCarrying
{
    internal class MinionsManager
    {
        internal static Obj_AI_Base GetNearestMinionNearPosition(Vector3 pos)
        {
            var minions = MinionManager.GetMinions(Irelia.Q.Range);
            return minions.OrderBy(m => m.Distance(pos, false)).FirstOrDefault();
        }
    }
}