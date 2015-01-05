using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;
/*
 * http://leagueoflegends.wikia.com/wiki/Gold_efficiency
 * http://leagueoflegends.wikia.com/wiki/Assist
 * http://leagueoflegends.wikia.com/wiki/Kill
 * */
namespace Obama_TeamPower
{
    internal class Program
    {
        private static Menu _menu;
        public static Obj_AI_Hero Player;
        internal static int[] BountyKills = { 300, 350, 408, 475, 500 }; // >= 5 kills is 500 gold.
        internal static int[] BountyDeaths = { 300, 275, 220, 176, 141, 112, 90, 72, 58 }; // 50 gold after 9 deaths
        internal static Dictionary<int, int> PlayerData = new Dictionary<int, int>();
        internal static bool FirstBlood;

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
            Game.OnGameNotifyEvent += argsx =>
            {
//                Game.PrintChat("EventId: "+argsx.EventId+" Champion: "+GetChampionData((int)argsx.NetworkId).ChampionName);
                /* Update killstreak by +1 for player :P */
                //
                if (argsx.EventId == GameEventId.OnChampionDie)//OnChampionKill
                {
                    //Game.PrintChat("Adding kill value to "+GetChampionData((int)argsx.NetworkId).ChampionName);
                    PlayerData[(int) argsx.NetworkId] += 1;
                }
                /* Reset killstreak whenever a player dies.
                 * No use to count killstreak..
                 * */
                if (argsx.EventId == GameEventId.OnChampionKillPre)//OnChampionDie
                {
                    //Game.PrintChat("Resetting " + GetChampionData((int)argsx.NetworkId).ChampionName+" killstreak");
                    PlayerData[(int)argsx.NetworkId] = 0;
                }

                // first blood event
                if (argsx.EventId == GameEventId.OnFirstBlood)
                {
                    FirstBlood = true;
                }
            };
        }

        static void LoadPlayers()
        {
            foreach (var player in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.ChampionName != ""))
                PlayerData.Add(player.NetworkId,0);
        }
        static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;
            FirstBlood = false;

            _menu = new Menu("Team Power","obama_teampower",true);
            var worthIt = new Menu("Champion Gold", "champion_gold");
            worthIt.AddItem(new MenuItem("enabled", "Enabled").SetValue(true));
            worthIt.AddItem(new MenuItem("drawInRange", "Draw in range").SetValue(true));
//            var sharedExp = new Menu("Shared Experience", "shared_experience");
 //           var teamItemPower = new Menu("Team Power", "team_power");

            var shit = new Menu("Drawings", "draw");
            shit.AddItem(new MenuItem("drawSightRange", "Draw enemy sight range").SetValue(false));


            LoadPlayers();
            _menu.AddItem(new MenuItem("debug", "Debug").SetValue(false));
            _menu.AddSubMenu(worthIt);
            _menu.AddSubMenu(shit);
//            _menu.AddSubMenu(sharedExp);
//            _menu.AddSubMenu(teamItemPower);
            _menu.AddToMainMenu();
            Game.PrintChat("[!] Obama's Team Power loaded!");
            Drawing.OnDraw += Drawing_OnDraw;
        }

        static bool DrawInRange
        {
            get { return _menu.Item("drawInRange").GetValue<bool>(); }
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            if (_menu.Item("enabled").GetValue<bool>())
            {
                foreach (var champion in
                    PlayerData.Select(players => GetChampionData(players.Key))
                        .Where(champion => !champion.IsDead && champion.IsVisible))
                {
                    if (champion.IsValidTarget(1500) && DrawInRange)
                        continue;
                    Drawing.DrawText(
                        champion.HPBarPosition.X + 5, champion.HPBarPosition.Y - 10, Color.Cyan,
                        "K/A/S: " + GoldWorthKill(champion.NetworkId) + "/" + GoldWorthAssist(champion) + "/" +
                        PlayerData[champion.NetworkId]);
                }
            }
            if (_menu.Item("drawSightRange").GetValue<bool>())
                foreach (var c in ObjectManager.Get<Obj_AI_Hero>().Where(c => c.IsVisible && !c.IsDead))
                    Utility.DrawCircle(c.Position, 1200, Color.RoyalBlue);
            TeamPower();
            SharedExperience();
        }

        static Obj_AI_Hero GetChampionData (int networkId)
        {
            return ObjectManager.Get<Obj_AI_Hero>().FirstOrDefault(champion => champion.NetworkId == networkId);
        }

        static int CountGoldDeath(Obj_AI_Hero player)
        {
            var x = (int)(player.GoldEarned / 1000);
            var tempValue = player.Deaths > 8 ? 50 : BountyDeaths[(player.Deaths-x) < 0 ? 0 : (player.Deaths-x)];
            return tempValue;
        }

        static int GoldWorthKill(int networkId)
        {
            var kills = PlayerData[networkId];
            var charData = GetChampionData(networkId);

            var tempValue = kills > 5
                ? 500
                : (PlayerData[networkId] > 1 ? BountyKills[kills-1] : CountGoldDeath(charData));

            if (Game.ClockTime < 120 && FirstBlood) // firstblood is not affected by this
                tempValue = (tempValue * 75) / 100;

            return tempValue;
        }

        static int GoldWorthAssist(GameObject target)
        {
            return (GoldWorthKill(target.NetworkId) / 2);
        }
        /*
         * TLDR: Alert's when a enemy champion is sharing experience with another enemy in fog of war or a bush.
         * Why: Prevent's ganks from lane bushes.
         * How: Need to Calculate exp from each creep and when it dies compare with how muth exp a "on creep exp range" visible champion gained from that creep to determinate how many champions shared that exp.
         * Also option to store how many champions are in your lane in the past 10/15 seconds so it only alert's when a "new" champion is sharing exp ie. not the support
         * Drawing: When a creep dies and share's exp with a "new" champion alert the user by writing it above that creep and/or pinging and/or drawing a circle from the creep to exp range with on/off options for each.*/

        static void SharedExperience()
        {
            
        }
        /*
         * TLDR: Draws text on screen of how muth "item power" each team have based on gold eficiency by leagueoflegends.wikia.com
         * Why: To better decide if you should force a team fight.
         * How: Need to calculate each team's item gold eficiency.
         * Why not raw gold: Because each item's purchase price in the in-game shop is not a real representation of the amount of gold that item is actually worth, based on the stats it provides. Meaning there are item's that cost less for more stat's then others.*/
        static void TeamPower()
        {
            
        }
    }
}
