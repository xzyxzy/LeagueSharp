using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace SciasMightyAssistant
{
    class Minions
    {
        internal static float AttackRangeBuffer = ObjectManager.Player.AttackRange + 50;
        internal static void OnInit()
        {
            Game.OnGameUpdate += OnTick;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }

        internal static void OnTick(EventArgs args)
        {
            AttackRangeBuffer = ObjectManager.Player.AttackRange + 50;
        }
        static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            throw new NotImplementedException();
        }
    }
}
