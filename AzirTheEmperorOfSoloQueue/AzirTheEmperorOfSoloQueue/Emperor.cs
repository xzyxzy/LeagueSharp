using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;

namespace AzirTheEmperorOfSoloQueue
{
    class Emperor
    {
        internal static Spell Q, W, E, R;
        internal static SpellSlot IgniteSlot;
        internal static Menu Config;
        internal static Obj_AI_Hero Player;
        internal static List<Spell> Spells = new List<Spell>();
        internal static Orbwalking.Orbwalker Orb;
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        internal static void Game_OnGameLoad(EventArgs args)
        {
            Config = new Menu("Azir - SoloQ God","azir",true);
            var ts = new Menu("Target Selector", "ts");
            TargetSelector.AddToMenu(ts);
            Config.AddSubMenu(ts);

            var orbz = new Menu("Orbwalker","orb");
            Orb = new Orbwalking.Orbwalker(orbz);
            Config.AddSubMenu(orbz);

            Config.AddItem(new MenuItem("trainMode", "Chu chuuu!!!").SetValue(new KeyBind('Z', KeyBindType.Press)));

            Config.AddToMainMenu();
            Q = new Spell(SpellSlot.Q, 2500);
            W = new Spell(SpellSlot.W, 450);
            E = new Spell(SpellSlot.E, 1250);
            R = new Spell(SpellSlot.R, 500);

            Q.SetSkillshot(0.25f, 100, 500, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(0f, 425, float.MaxValue, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.2f, 700, 1300, false, SkillshotType.SkillshotLine);
            Game.OnGameUpdate += Game_OnGameUpdate;
            GameObject.OnCreate += VectorManager.GameObject_OnCreate;
            GameObject.OnDelete += VectorManager.GameObject_OnDelete;
            DrawingManager.Init();
        }
        
        internal static void Game_OnGameUpdate(EventArgs args)
        {
//            if (Player.InFountain() || Player.IsRecalling()) return;
            EscapeMode();
            FightMode();
        }

        internal static void FightMode()
        {
        }

        internal static void EscapeMode()
        
        {
            if (!Config.Item("trainMode").GetValue<KeyBind>().Active) return;
            if (!W.IsReady() || !Q.IsReady()) return;

            if (W.IsReady() && Player.Distance(VectorManager.GetSoldierNearMouse.Position) > 450f) W.Cast();
            if (Q.IsReady()) Q.Cast(Game.CursorPos, true);
        }
    }
}
