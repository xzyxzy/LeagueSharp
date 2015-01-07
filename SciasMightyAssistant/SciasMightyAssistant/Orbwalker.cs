using System;
using System.Linq;
using SharpDX;
using Color = System.Drawing.Color;
using System.Collections.Generic;
using LeagueSharp;
using LeagueSharp.Common;

namespace SciasMightyAssistant
{
    /// <summary>
    ///     This class offers everything related to auto-attacks and orbwalking.
    /// </summary>
    public static class Assistant
    {
        public delegate void AfterAttackEvenH(AttackableUnit unit, AttackableUnit target);

        public delegate void BeforeAttackEvenH(BeforeAttackEventArgs args);

        public delegate void OnAttackEvenH(AttackableUnit unit, AttackableUnit target);

        public delegate void OnNonKillableMinionH(AttackableUnit minion);

        public delegate void OnTargetChangeH(AttackableUnit oldTarget, AttackableUnit newTarget);

        public enum Mode
        {
            LastHit,
            Mixed,
            LaneClear,
            LaneFreeze,
            Combo,
            None,
        }

        //Spells that reset the attack timer.
        private static readonly string[] AttackResets =
        {
            "dariusnoxiantacticsonh", "fioraflurry", "garenq",
            "hecarimrapidslash", "jaxempowertwo", "jaycehypercharge", "leonashieldofdaybreak", "luciane", "lucianq",
            "monkeykingdoubleattack", "mordekaisermaceofspades", "nasusq", "nautiluspiercinggaze", "netherblade",
            "parley", "poppydevastatingblow", "powerfist", "renektonpreexecute", "rengarq", "shyvanadoubleattack",
            "sivirw", "takedown", "talonnoxiandiplomacy", "trundletrollsmash", "vaynetumble", "vie", "volibearq",
            "xenzhaocombotarget", "yorickspectral", "reksaiq"
        };

        //Spells that are not attacks even if they have the "attack" word in their name.
        private static readonly string[] NoAttacks =
        {
            "jarvanivcataclysmattack", "monkeykingdoubleattack",
            "shyvanadoubleattack", "shyvanadoubleattackdragon", "zyragraspingplantattack", "zyragraspingplantattack2",
            "zyragraspingplantattackfire", "zyragraspingplantattack2fire"
        };

        //Spells that are attacks even if they dont have the "attack" word in their name.
        private static readonly string[] Attacks =
        {
            "caitlynheadshotmissile", "frostarrow", "garenslash2",
            "kennenmegaproc", "lucianpassiveattack", "masteryidoublestrike", "quinnwenhanced", "renektonexecute",
            "renektonsuperexecute", "rengarnewpassivebuffdash", "trundleq", "xenzhaothrust", "xenzhaothrust2",
            "xenzhaothrust3"
        };

        public static int LastAATick;

        public static bool Attack = true;
        public static bool DisableNextAttack = false;
        public static bool Move = true;
        public static int LastMoveCommandT = 0;
        public static Vector3 LastMoveCommandPosition = Vector3.Zero;
        private static AttackableUnit _lastTarget;
        private static readonly Obj_AI_Hero Player;
        private static int _delay = 80;
        private static float _minDistance = 400;
        private static readonly Random _random = new Random(DateTime.Now.Millisecond);
        private static int extraDelay;

        static Assistant()
        {
            Player = ObjectManager.Player;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;
            GameObject.OnCreate += Obj_SpellMissile_OnCreate;
            Obj_AI_Hero.OnInstantStopAttack += ObjAiHeroOnOnInstantStopAttack;
        }

        private static void Obj_SpellMissile_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender.IsValid<Obj_SpellMissile>())
            {
                var missile = (Obj_SpellMissile)sender;
                if (missile.SpellCaster.IsValid<Obj_AI_Hero>() && IsAutoAttack(missile.SData.Name))
                {
                    FireAfterAttack(missile.SpellCaster, _lastTarget);
                }
            }
        }

        /// <summary>
        ///     This event is fired before the player auto attacks.
        /// </summary>
        public static event BeforeAttackEvenH BeforeAttack;

        /// <summary>
        ///     This event is fired when a unit is about to auto-attack another unit.
        /// </summary>
        public static event OnAttackEvenH OnAttack;

        /// <summary>
        ///     This event is fired after a unit finishes auto-attacking another unit (Only works with player for now).
        /// </summary>
        public static event AfterAttackEvenH AfterAttack;

        /// <summary>
        ///     Gets called on target changes
        /// </summary>
        public static event OnTargetChangeH OnTargetChange;

        //  <summary>
        //      Gets called if you can't kill a minion with auto attacks
        //  </summary>
        public static event OnNonKillableMinionH OnNonKillableMinion;

        private static void FireBeforeAttack(AttackableUnit target)
        {
            if (BeforeAttack != null)
            {
                BeforeAttack(new BeforeAttackEventArgs { Target = target });
            }
            else
            {
                DisableNextAttack = false;
            }
        }

        private static void FireOnAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (OnAttack != null)
            {
                OnAttack(unit, target);
            }
        }

        private static void FireAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (AfterAttack != null)
            {
                AfterAttack(unit, target);
            }
        }

        private static void FireOnTargetSwitch(AttackableUnit newTarget)
        {
            if (OnTargetChange != null && (!_lastTarget.IsValidTarget() || _lastTarget != newTarget))
            {
                OnTargetChange(_lastTarget, newTarget);
            }
        }

        private static void FireOnNonKillableMinion(AttackableUnit minion)
        {
            if (OnNonKillableMinion != null)
            {
                OnNonKillableMinion(minion);
            }
        }

        /// <summary>
        ///     Returns true if the spellname resets the attack timer.
        /// </summary>
        public static bool IsAutoAttackReset(string name)
        {
            return AttackResets.Contains(name.ToLower());
        }

        /// <summary>
        ///     Returns true if the unit is melee
        /// </summary>
        public static bool IsTargetMelee(this Obj_AI_Base unit)
        {
            return unit.CombatType == GameObjectCombatType.Melee;
        }

        /// <summary>
        ///     Returns true if the spellname is an auto-attack.
        /// </summary>
        public static bool IsAutoAttack(string name)
        {
            return (name.ToLower().Contains("attack") && !NoAttacks.Contains(name.ToLower())) || Attacks.Contains(name.ToLower());
        }

        public static float AutoAttackRange(AttackableUnit target)
        {
            var result = Player.AttackRange + Player.BoundingRadius;
            if (target.IsValidTarget())
            {
                return result + target.BoundingRadius;
            }
            return result;
        }

        /// <summary>
        ///     Returns true if the target is in auto-attack range.
        /// </summary>
        public static bool InAutoAttackRange(AttackableUnit target)
        {
            if (!target.IsValidTarget())
            {
                return false;
            }
            var myRange = AutoAttackRange(target);
            return
                Vector2.DistanceSquared(
                    (target is Obj_AI_Base) ? ((Obj_AI_Base)target).ServerPosition.To2D() : target.Position.To2D(),
                    Player.ServerPosition.To2D()) <= myRange * myRange;
        }
        public static float GetMyProjectileSpeed()
        {
            return IsTargetMelee(Player) ? float.MaxValue : Player.BasicAttack.MissileSpeed;
        }
        public static bool CanAttack()
        {
            if (LastAATick <= Environment.TickCount)
            {
                return Environment.TickCount + Game.Ping / 2 >= LastAATick + Player.AttackDelay * 1000 && Attack;
            }

            return false;
        }

        /// <summary>
        ///     Returns true if moving won't cancel the auto-attack.
        /// </summary>
        public static bool CanMove()//float extraWindup)
        {
            if (LastAATick <= Environment.TickCount)
            {
                return (Environment.TickCount + Game.Ping / 2 >= LastAATick + Player.AttackCastDelay * AttackSpeed * 1000 ) && Move;
            }

            return false;
        }
        internal static float getExtraDelay()
        {
            return extraDelay > 1 ? extraDelay : 1;
        }
        public static void setExtraDelay(int time)
        {
            extraDelay = time;
        }
        internal static float BaseWindUp
        {
            get
            {
                return 1 / (AttackSpeed * Player.AttackCastDelay);
            }
        }
        internal static float AttackSpeed
        {
            get
            {
                return 1 / Player.AttackDelay;
            }
        }
        public static void SetMovementDelay(int delay)
        {
            _delay = delay;
        }

        public static void SetMinimumOrbwalkDistance(float d)
        {
            _minDistance = d;
        }

        public static float GetLastMoveTime()
        {
            return LastMoveCommandT;
        }

        public static Vector3 GetLastMovePosition()
        {
            return LastMoveCommandPosition;
        }

        private static void MoveTo(Vector3 position)
        {
            if (Environment.TickCount - LastMoveCommandT < _delay)
            {
                return;
            }

            LastMoveCommandT = Environment.TickCount;

            if (Player.Distance(position) < 70)
            {
                if (Player.Path.Count() > 1)
                {
                    Player.IssueOrder(GameObjectOrder.HoldPosition, Player.Position);
                    LastMoveCommandPosition = Player.Position;
                }
                return;
            }
            var distance = Player.Distance(position) + Game.Ping / 10;
            var MoveSqr = Math.Sqrt(Math.Pow(position.X - Player.Position.X, 2) + Math.Pow(position.Y - Player.Position.Y, 2) + Math.Pow(position.Z - Player.Position.Z, 2));
            var MoveX = Player.Position.X + distance * ((position.X - Player.Position.X) / MoveSqr);
            var MoveY = Player.Position.Y + distance * ((position.Y - Player.Position.Y) / MoveSqr);
            var MoveZ = Player.Position.Z + distance * ((position.Z - Player.Position.Z) / MoveSqr);


            var newPosition = new Vector3((float)MoveX, (float)MoveY, (float)MoveZ);
//            Game.PrintChat("Distance: "+distance+" | MoveSqr: "+MoveSqr+" | newPosition: "+newPosition);
//            Drawing.DrawLine(new Vector2(Player.Position.X,Player.Position.Y), new Vector2(newPosition.X,newPosition.Y), 5, Color.Red);
            Player.IssueOrder(GameObjectOrder.MoveTo, newPosition);
            LastMoveCommandPosition = newPosition;
        }

        private static float GetRealDistance(GameObject target, Vector3 position = new Vector3())
        {
            return Player.Position.Distance((target == null ? position : target.Position)) + Player.BoundingRadius + (target == null ? target.BoundingRadius : 0);
        }

        /// <summary>
        ///     Orbwalk a target while moving to Position.
        /// </summary>
        public static void Orbwalk(AttackableUnit target, Vector3 position)
        {
            if (target.IsValidTarget() && CanAttack())
            {
                DisableNextAttack = false;
                FireBeforeAttack(target);
                if (!DisableNextAttack)
                {
//                    Game.PrintChat("Attacking!" + target.Name+" move ?"+CanMove());
                    Player.IssueOrder(GameObjectOrder.AttackUnit, target);

                    if (_lastTarget != null && _lastTarget.IsValid && _lastTarget != target)
                    {
                        LastAATick = Environment.TickCount + Game.Ping / 2;
                    }

                    _lastTarget = target;
                    return;
                }
            }
            
            if (IsTargetMelee(Player) && target!= null && GetRealDistance(target) < Orbwalker._config.SubMenu("melee").Item("MeleeStickyRange").GetValue<Slider>().Value)
            {
                MoveTo(target.Position);
            }

            if (CanMove())
                MoveTo(position);
        }

        public static void ResetAutoAttackTimer()
        {
            LastAATick = 0;
        }

        private static void ObjAiHeroOnOnInstantStopAttack(Obj_AI_Base sender, GameObjectInstantStopAttackEventArgs args)
        {
            if (sender.IsValid && sender.IsMe && (args.BitData & 1) == 0 && ((args.BitData >> 4) & 1) == 1)
            {
                ResetAutoAttackTimer();
            }
        }


        private static void OnProcessSpell(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs Spell)
        {
            var spellName = Spell.SData.Name;

            if (IsAutoAttackReset(spellName) && unit.IsMe)
            {
                Utility.DelayAction.Add(250, ResetAutoAttackTimer);
            }

            if (!IsAutoAttack(spellName))
            {
                return;
            }

            if (unit.IsMe && Spell.Target is Obj_AI_Base)
            {
                LastAATick = Environment.TickCount - Game.Ping / 2;
                var target = (Obj_AI_Base)Spell.Target;
                if (target.IsValid)
                {
                    FireOnTargetSwitch(target);
                    _lastTarget = target;
                }

                if (IsTargetMelee(unit))
                {
                    Utility.DelayAction.Add(
                        (int)(unit.AttackCastDelay * 1000 + 40), () => FireAfterAttack(unit, _lastTarget));
                }
            }

            FireOnAttack(unit, _lastTarget);
        }

        public class BeforeAttackEventArgs
        {
            public AttackableUnit Target;
            public Obj_AI_Base Unit = ObjectManager.Player;
            private bool _process = true;

            public bool Process
            {
                get { return _process; }
                set
                {
                    DisableNextAttack = !value;
                    _process = value;
                }
            }
        }
        public static List<ItemData.Item> ItemList = new List<ItemData.Item>();
        public static void AddItem(ItemData.Item item)
        {
            ItemList.Add(item);
        }
        public static Items.Item GetItem(int id)
        {
            var item = ItemList.FirstOrDefault(i => i.Id == id);
            return new Items.Item(item.Id,item.Range);
        }
        public static bool IsItemEnabled(Items.Item item)
        {
            return Orbwalker._config.Item(item.Id + "_item").GetValue<bool>();
        }
        public class Orbwalker
        {
            private const float LaneClearWaitTimeMod = 2f;
            internal static Menu _config;
            private readonly Obj_AI_Hero Player;

            private Obj_AI_Base _forcedTarget;
            private Mode _mode = Mode.None;
            private Vector3 _orbwalkingPoint;

            private Obj_AI_Minion _prevMinion;

            public Orbwalker(Menu attachToMenu)
            {
			    Game.PrintChat(" ");
			    Game.PrintChat(" ");
			    Game.PrintChat(" ");
			    Game.PrintChat("<font color='#D859CD'>¸¸.•*¨*••*¨*•.¸¸¸¸.•*¨*••*¨*•.¸¸¸¸.•*¨*••*¨*•.¸¸</font>");
			    Game.PrintChat("<font color='#adec00'>            Scias Mighty Assistant</font>");
                Game.PrintChat("<font color='#adec00'>                   Has been loaded!</font>");
			    Game.PrintChat("<font color='#D859CD'>¸¸.•*¨*••*¨*•.¸¸¸¸.•*¨*••*¨*•.¸¸¸¸.•*¨*••*¨*•.¸¸</font>");
                _config = attachToMenu;
                // combo menu
                var combo = new Menu("Combo Menu", "combo");
                combo.AddItem(new MenuItem("1", "-- Settings --"));
                combo.AddItem(new MenuItem("LeftClick", "Left Click Mode").SetValue(false));
                combo.AddItem(new MenuItem("Active", "Combo key").SetValue(new KeyBind(32, KeyBindType.Press, false)));
                if (ItemList != null)
                {
                    combo.AddItem(new MenuItem("2", "-- Items --"));
                    foreach (var item in ItemList)
                        combo.AddItem(new MenuItem(item.Id + "_item", "Use " + item.Name).SetValue(true));
                }

                // last hit menu
                var lasthit = new Menu("Last Hit Menu", "lasthit");
                lasthit.AddItem(new MenuItem("1", "-- Settings --"));
                lasthit.AddItem(new MenuItem("Active", "Last Hit key").SetValue(new KeyBind("X".ToCharArray()[0], KeyBindType.Press, false)));
                
                // freeze
                var freeze = new Menu("Last Freeze Menu", "freeze");
                freeze.AddItem(new MenuItem("1", "-- Settings --"));
                freeze.AddItem(new MenuItem("Active", "Lane Freeze key").SetValue(new KeyBind("X".ToCharArray()[0], KeyBindType.Press, false)));

                // Mixed Mode
                var mixed = new Menu("Mixed Mode Menu", "mixed");
                mixed.AddItem(new MenuItem("1", "-- Settings --"));
                mixed.AddItem(new MenuItem("Active", "Mixed key").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press, false)));
                mixed.AddItem(new MenuItem("MinionPriority", "Prioritise Last Hit Over Harass").SetValue(true));

                // Lane Clear Menu
                var lc = new Menu("Lane Clear Menu", "laneclear");
                lc.AddItem(new MenuItem("1", "-- Settings --"));
                lc.AddItem(new MenuItem("Active", "Combo key").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press, false)));
                lc.AddItem(new MenuItem("AttackEnemies", "Attack Enemies").SetValue(true));
                lc.AddItem(new MenuItem("MinionPriority", "Prioritise Last Hit Over Harass").SetValue(true));

                // Melee menu
                var melee = new Menu("Melee", "melee");
                melee.AddItem(new MenuItem("MeleeStickyRange", "Stick to target extra range").SetValue(new Slider(0, 0, 300)));

                // drawing menu
                var draw = new Menu("Drawing", "drawings");
                draw.AddItem(new MenuItem("AACircle","Champion Range Circle").SetShared().SetValue(new Circle(true, Color.FromArgb(255, 0, 189, 22))));
                draw.AddItem(new MenuItem("AACircle2", "Enemy Range Circle").SetShared().SetValue(new Circle(true, Color.FromArgb(255, 0, 112, 95))));
                draw.AddItem(new MenuItem("1", "-----"));
                draw.AddItem(new MenuItem("MinionCircle", "Killable minion").SetShared().SetValue(new Circle(true, Color.FromArgb(183, 0, 26, 173))));
                draw.AddItem(new MenuItem("AlmostMinionCircle", "Almost Killable Minion").SetShared().SetValue(new Circle(true, Color.FromArgb(255, 0, 189, 22))));

                _config.AddSubMenu(combo);
                _config.AddSubMenu(mixed);
                _config.AddSubMenu(lasthit);
                _config.AddSubMenu(lc);
                _config.AddSubMenu(freeze);
                _config.AddSubMenu(melee);
                _config.AddSubMenu(draw);
                /*
                _config.AddItem(new MenuItem("setExtraDelay", "Extra movement delay").SetShared().SetValue(new Slider(1, 1, 30)));

                _config.Item("setExtraDelay").ValueChanged += Orbwalker_ValueChanged;*/


                Player = ObjectManager.Player;
                Game.OnGameUpdate += GameOnOnGameUpdate;
                Drawing.OnDraw += DrawingOnOnDraw;
            }
            /*
            void Orbwalker_ValueChanged(object sender, OnValueChangeEventArgs e)
            {
//                Game.PrintChat("New: " + e.GetNewValue<Slider>().Value);
                setExtraDelay(e.GetNewValue<Slider>().Value);
            }*/

            public Mode CurrentMode
            {
                get
                {
                    return _config.SubMenu("combo").Item("Active").GetValue<KeyBind>().Active ? Mode.Combo :
                                (_config.SubMenu("lasthit").Item("Active").GetValue<KeyBind>().Active ? Mode.LastHit :
                                    (_config.SubMenu("mixed").Item("Active").GetValue<KeyBind>().Active ? Mode.Mixed :
                                        (_config.SubMenu("laneclear").Item("Active").GetValue<KeyBind>().Active ? Mode.LaneClear : 
                                        (_config.SubMenu("freeze").Item("Active").GetValue<KeyBind>().Active ? Mode.LaneFreeze : Mode.None))));
                }
                set { _mode = value; }
            }
            public void SetAttack(bool b)
            {
                Attack = b;
            }
            public void SetMovement(bool b)
            {
                Move = b;
            }
            public void ForceTarget(Obj_AI_Base target)
            {
                _forcedTarget = target;
            }
            public void SetOrbwalkingPoint(Vector3 point)
            {
                _orbwalkingPoint = point;
            }

            private bool ShouldWait()
            {
                return
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Any(
                            minion =>
                                minion.IsValidTarget() && minion.Team != GameObjectTeam.Neutral &&
                                InAutoAttackRange(minion) &&
                                HealthPrediction.LaneClearHealthPrediction(
                                    minion, (int)((Player.AttackDelay * 1000) * LaneClearWaitTimeMod)) <=
                                Player.GetAutoAttackDamage(minion));
            }

            public AttackableUnit GetTarget()
            {
                AttackableUnit result = null;
                var r = float.MaxValue;
                /*
                if ((CurrentMode == Mode.Mixed && !_config.SubMenu("mixed").Item("MinionPriority").GetValue<bool>()) 
                    || (CurrentMode == Mode.LaneClear && !_config.SubMenu("laneclear").Item("MinionPriority").GetValue<bool>()))
                    return null;*/
                
                /*Killable Minion*/
                var target = TargetSelector.GetTarget(AutoAttackRange(Player), TargetSelector.DamageType.Physical);
                if (CurrentMode == Mode.LaneClear || CurrentMode == Mode.Mixed || CurrentMode == Mode.LastHit || CurrentMode == Mode.LaneFreeze)
                {
                    foreach (var minion in
                        ObjectManager.Get<Obj_AI_Minion>()
                            .Where(
                                minion =>
                                    minion.IsValidTarget() && InAutoAttackRange(minion) &&
                                    minion.Health <
                                    2 *
                                    (ObjectManager.Player.BaseAttackDamage + ObjectManager.Player.FlatPhysicalDamageMod))
                        )
                    {
                        var t = (int)(Player.AttackCastDelay * 1000) - 100 + Game.Ping / 2 +
                                1000 * (int)Player.Distance(minion,false) / (int)GetMyProjectileSpeed();
                        var predHealth = HealthPrediction.GetHealthPrediction(minion, t, CurrentMode == Mode.LaneFreeze ? 85 : 70);
                        if (minion.Team != GameObjectTeam.Neutral && MinionManager.IsMinion(minion, true))
                        {
                            if(CurrentMode == Mode.LaneClear)
                            {
                                if(_config.Item("AttackEnemies").GetValue<bool>())
                                {
                                    if (_config.SubMenu("laneclear").Item("MinionPriority").GetValue<bool>() && predHealth > 0 && predHealth <= Player.GetAutoAttackDamage(minion, true))
                                    {
                                        return minion;
                                    }
                                    else
                                    {
                                        return target;
                                    }
                                }
                                if(predHealth > 0 && predHealth <= Player.GetAutoAttackDamage(minion, true) && _config.Item("MinionPriority").GetValue<bool>())
                                {
                                    return minion;
                                }
                            }
                            if (predHealth <= 0)
                            {
                                FireOnNonKillableMinion(minion);
                            }

                            if (predHealth > 0 && predHealth <= Player.GetAutoAttackDamage(minion, true))
                            {
                                return minion;
                            }
                        }
                    }
                }

                //Forced target
                if (_forcedTarget.IsValidTarget() && InAutoAttackRange(_forcedTarget))
                {
                    return _forcedTarget;
                }

                /* turrets / inhibitors / nexus */
                if (CurrentMode == Mode.LaneClear || CurrentMode == Mode.Mixed)
                {
                    /* turrets */
                    foreach (var turret in
                        ObjectManager.Get<Obj_AI_Turret>().Where(t => t.IsValidTarget() && InAutoAttackRange(t)))
                    {
                        return turret;
                    }

                    /* inhibitor */
                    foreach (var turret in
                        ObjectManager.Get<Obj_BarracksDampener>()
                            .Where(t => t.IsValidTarget() && InAutoAttackRange(t)))
                    {
                        return turret;
                    }

                    /* nexus */
                    foreach (var nexus in
                        ObjectManager.Get<Obj_HQ>()
                            .Where(t => t.IsValidTarget() && InAutoAttackRange(t)))
                    {
                        return nexus;
                    }
                }

                /*Champions*/
                if (CurrentMode != Mode.LastHit && CurrentMode != Mode.LaneFreeze)
                {
                    target = TargetSelector.GetTarget(-1, TargetSelector.DamageType.Physical);
                    if (target.IsValidTarget())
                    {
                        return target;
                    }
                }

                /*Jungle minions*/
                if (CurrentMode == Mode.LaneClear || CurrentMode == Mode.Mixed)
                {
                    foreach (var mob in
                        ObjectManager.Get<Obj_AI_Minion>()
                            .Where(
                                mob =>
                                    mob.IsValidTarget() && InAutoAttackRange(mob) && mob.Team == GameObjectTeam.Neutral)
                            .Where(mob => mob.MaxHealth >= r || Math.Abs(r - float.MaxValue) < float.Epsilon))
                    {
                        // Todo: prioritize big mobs
                        result = mob;
                        r = mob.MaxHealth;
                    }
                }

                /*Lane Clear minions*/
                r = float.MaxValue;
                if (CurrentMode == Mode.LaneClear)
                {
                    if (!ShouldWait())
                    {
                        if (_prevMinion.IsValidTarget() && InAutoAttackRange(_prevMinion))
                        {
                            var predHealth = HealthPrediction.LaneClearHealthPrediction(
                                _prevMinion, (int)((Player.AttackDelay * 1000) * LaneClearWaitTimeMod));
                            if (predHealth >= 2 * Player.GetAutoAttackDamage(_prevMinion, false) ||
                                Math.Abs(predHealth - _prevMinion.Health) < float.Epsilon)
                            {
                                return _prevMinion;
                            }
                        }

                        foreach (var minion in
                            ObjectManager.Get<Obj_AI_Minion>()
                                .Where(minion => minion.IsValidTarget() && InAutoAttackRange(minion)))
                        {
                            var predHealth = HealthPrediction.LaneClearHealthPrediction(
                                minion, (int)((Player.AttackDelay * 1000) * LaneClearWaitTimeMod));
                            if (predHealth >= 2 * Player.GetAutoAttackDamage(minion) ||
                                Math.Abs(predHealth - minion.Health) < float.Epsilon)
                            {
                                if (minion.Health >= r || Math.Abs(r - float.MaxValue) < float.Epsilon)
                                {
                                    // Prioritize big minion
                                    result = minion;
                                    r = minion.Health;
                                    _prevMinion = minion;
                                }
                            }
                        }
                    }
                }

                return result;
            }

            private void GameOnOnGameUpdate(EventArgs args)
            {
                try
                {
                    if (CurrentMode == Mode.None)
                    {
                        return;
                    }

                    //Prevent canceling important channeled spells like Miss Fortunes R.
                    if (Player.IsChannelingImportantSpell())
                    {
                        return;
                    }

                    var target = GetTarget();
                    Orbwalk(target, (_orbwalkingPoint.To2D().IsValid()) ? _orbwalkingPoint : Game.CursorPos);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            private void DrawingOnOnDraw(EventArgs args)
            {
                if (_config.Item("AACircle").GetValue<Circle>().Active)
                {
                    Utility.DrawCircle(
                        Player.Position, AutoAttackRange(null),
                        _config.Item("AACircle").GetValue<Circle>().Color);
                }

                if (_config.Item("AACircle2").GetValue<Circle>().Active)
                {
                    foreach (var target in
                        ObjectManager.Get<Obj_AI_Hero>().Where(target => target.IsValidTarget(1175)))
                    {
                        Utility.DrawCircle(
                            target.Position, AutoAttackRange(target),
                            _config.Item("AACircle2").GetValue<Circle>().Color);
                    }
                }
                if (_config.SubMenu("drawings").Item("MinionCircle").GetValue<Circle>().Active || _config.SubMenu("drawings").Item("AlmostMinionCircle").GetValue<Circle>().Active)
                {

                }
            }
        }
    }
}
