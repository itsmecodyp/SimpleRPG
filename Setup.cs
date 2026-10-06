using BepInEx;
using BepInEx.Bootstrap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SimpleRPG
{
    public partial class SimpleRPG : BaseUnityPlugin
    {
        public static BaseUnityPlugin messagesMod;
        public static MethodInfo actorSayMethod;
        public static Action<Actor, string, float> ActorSay;
        public static Func<string, bool> HasActorStartPhrase;
        public static Func<string, string> GetRandomActorStartPhrase;

        public static void setup()
        {
            setupMessages();
            setupTextureIcons();
        }

        public static void setupMessages()
        {
            messagesMod = Chainloader.PluginInfos.TryGetValue("cody.worldbox.simple.messages", out var info) ? info.Instance : null;
            if (messagesMod != null)
            {
                Debug.Log("detected messages mod");
                var type = messagesMod.GetType();
                MethodInfo method = type.GetMethod(
                "ActorSay",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                if (method != null)
                {
                    ActorSay = (Action<Actor, string, float>)
                        Delegate.CreateDelegate(typeof(Action<Actor, string, float>), method);
                }
                FieldInfo enableField = type.GetField(
                    "enable",
                    BindingFlags.Public | BindingFlags.Static);

                enableField?.SetValue(null, true);

                FieldInfo isEnabledField = type.GetField(
                    "isEnabled",
                    BindingFlags.Public | BindingFlags.Static);

                isEnabledField?.SetValue(null, true);

                MethodInfo hasPhraseMethod = type.GetMethod(
                "HasActorStartPhrase",
                BindingFlags.Public | BindingFlags.Static);
                if (hasPhraseMethod != null)
                {
                    HasActorStartPhrase = (Func<string, bool>)
                        Delegate.CreateDelegate(typeof(Func<string, bool>), hasPhraseMethod);
                }

                MethodInfo getPhraseMethod = type.GetMethod(
                    "GetRandomActorStartPhrase",
                    BindingFlags.Public | BindingFlags.Static);

                if (getPhraseMethod != null)
                {
                    GetRandomActorStartPhrase = (Func<string, string>)
                        Delegate.CreateDelegate(typeof(Func<string, string>), getPhraseMethod);
                }
                hasMessagesMod = true;
                //Messages.enable = true;
                //Messages.isEnabled = true;
            }
        }

        public static void setupTextureIcons()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Hammer = LoadTexture(assembly, "SimpleRPG.Resources.cursorHammer.png");
            Axe = LoadTexture(assembly, "SimpleRPG.Resources.cursorAxe.png");
            Pickaxe = LoadTexture(assembly, "SimpleRPG.Resources.cursorPickaxe.png");
            Shovel = LoadTexture(assembly, "SimpleRPG.Resources.cursorShovel.png");
            Sickle = LoadTexture(assembly, "SimpleRPG.Resources.cursorSickle.png");
            Speak = LoadTexture(assembly, "SimpleRPG.Resources.cursorSpeak.png");
            Sword = LoadTexture(assembly, "SimpleRPG.Resources.cursorSword.png");
        }

        private static Texture2D LoadTexture(Assembly assembly, string resourceName)
        {
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    Debug.LogError($"Couldn't find embedded resource: {resourceName}");
                    return null;
                }

                byte[] data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);

                Texture2D tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                tex.LoadImage(data);
                tex.filterMode = FilterMode.Point; // optional
                tex.wrapMode = TextureWrapMode.Clamp; // optional

                return tex;
            }
        }

        public static string possessionFormationType => ModSettings.formationType != null ? ModSettings.formationType.Value : "circle";
        public static float possessionFormationSpacing => ModSettings.formationSpacing != null ? ModSettings.formationSpacing.Value : 2f;

        public static Vector2 GetPossessionFormationOffset(int index, int totalCount, string fType = null, float? fSpacing = null, Vector2 moveVector = default)
        {
            string currentType = fType ?? possessionFormationType;
            float currentSpacing = fSpacing ?? possessionFormationSpacing;

            if (index <= 0 || totalCount <= 1)
            {
                return Vector2.zero;
            }

            int followerIndex = index - 1;
            int followerCount = totalCount - 1;
            Vector2 rawOffset = Vector2.zero;

            switch (currentType)
            {
                case "wedge":
                    int row = (followerIndex / 2) + 1;
                    int side = (followerIndex % 2 == 0) ? 1 : -1;
                    rawOffset = new Vector2(side * row * currentSpacing, -row * currentSpacing);
                    break;

                case "line":
                case "rect":
                    int cols = Mathf.CeilToInt(Mathf.Sqrt(followerCount));
                    int col = followerIndex % cols;
                    int r = followerIndex / cols;
                    float offsetX = (col - (cols - 1) / 2f) * currentSpacing;
                    float offsetY = -(r + 1) * currentSpacing;
                    rawOffset = new Vector2(offsetX, offsetY);
                    break;

                case "circle":
                default:
                    float angle = followerIndex * (2f * Mathf.PI / followerCount);
                    float radius = currentSpacing + (followerCount / 10f);
                    return new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            }

            if (moveVector != Vector2.zero)
            {
                Vector2 forward = moveVector.normalized;
                Vector2 right = new Vector2(forward.y, -forward.x);
                return right * rawOffset.x + forward * rawOffset.y;
            }

            return rawOffset;
        }

        public void PatchPossessionLogic()
        {
            StatusAsset possessedAsset = AssetManager.status.get("possessed");
            possessedAsset.action = new WorldAction(possessedAction);
            AssetManager.status.add(possessedAsset);
        }

        //minimal changes, mark when we add/change original behaviour
        private bool possessedAction(BaseSimObject pTarget, WorldTile _)
        {
            Actor tActor = pTarget.a;
            if (tActor.asset.id == "crabzilla")
            {
                return false;
            }
            if (tActor.is_unconscious)
            {
                return false;
            }
            this.checkPossessedMovement(tActor); // this needs modified and replaced to add formations
            //AssetManager.status.checkPossessedAttackLeft(tActor);
            //AssetManager.status.checkPossessedAttackRight(tActor);//dont we have attacks handled somewhere already?
            AssetManager.status.checkPossessedFlip(tActor);
            AssetManager.status.checkBonusActions(tActor);
            return true;
        }

        public static void ExecuteDoubleTapAction()
        {
            if (!ControllableUnit.isControllingUnit())
            {
                return;
            }

            HashSet<Actor> unitsSet = ControllableUnit._units ?? ControllableUnit.getCotrolledUnits();
            if (unitsSet == null || unitsSet.Count == 0)
            {
                return;
            }

            List<Actor> toRemove = new List<Actor>();
            foreach (var kvp in assignedFollowerPositions)
            {
                Actor actor = kvp.Key;
                if (actor != null && unitsSet.Contains(actor))
                {
                    toRemove.Add(actor);
                }
            }

            foreach (Actor actor in toRemove)
            {
                assignedFollowerPositions.Remove(actor);
            }
        }

        public static void ExecuteTripleTapAction()
        {
            HashSet<Actor> unitsSet = null;
            if (ControllableUnit.isControllingUnit())
            {
                unitsSet = ControllableUnit._units ?? ControllableUnit.getCotrolledUnits();
            }

            List<Actor> toRemove = new List<Actor>();
            foreach (var kvp in assignedFollowerPositions)
            {
                Actor actor = kvp.Key;
                if (actor == null || !actor.isAlive())
                {
                    toRemove.Add(actor);
                    continue;
                }

                if (unitsSet == null || !unitsSet.Contains(actor))
                {
                    toRemove.Add(actor);
                }
            }

            StatusAsset possessedAsset = AssetManager.status.get("possessed");
            foreach (Actor actor in toRemove)
            {
                assignedFollowerPositions.Remove(actor);
                if (actor != null && actor.isAlive())
                {
                    actor.finishStatusEffect("possessed");
                }
            }
        }
        public static void FlashTile(WorldTile tile)
        {
            MapBox.instance.flash_effects.flashPixel(tile, 20, ColorType.White);
        }

        public static void HandleDirectUnitInput()
        {
            // Execute pending double tap if multi-tap window expired without a 3rd tap
            if (pendingDoubleTap && Time.time - doubleTapTime > 0.30f)
            {
                pendingDoubleTap = false;
                ctrlTapCount = 0;
                ExecuteDoubleTapAction();
            }

            // Detect multi-tap of Control key
            if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
            {
                if (Time.time - lastCtrlTapTime <= 0.35f && lastClickTime < lastCtrlTapTime)
                {
                    ctrlTapCount++;
                }
                else
                {
                    ctrlTapCount = 1;
                }
                lastCtrlTapTime = Time.time;

                if (ctrlTapCount == 2)
                {
                    pendingDoubleTap = true;
                    doubleTapTime = Time.time;
                }
                else if (ctrlTapCount >= 3)
                {
                    pendingDoubleTap = false;
                    ctrlTapCount = 0;
                    ExecuteTripleTapAction();
                }
            }

            if (!ControllableUnit.isControllingUnit())
            {
                return;
            }

            bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            Actor leadActor = ControllableUnit.getControllableUnit();

            if (ctrlHeld && leadActor != null)
            {
                HashSet<Actor> unitsSet = ControllableUnit._units ?? ControllableUnit.getCotrolledUnits();
                WorldTile hoveredTile = MapBox.instance.getMouseTilePos();
                if (hoveredTile != null)
                {
                    FlashTile(hoveredTile);
                }
                if (Input.GetMouseButtonDown(0))
                {
                    if (Time.time - lastClickTime <= 0.35f)
                    {
                        isCtrlDoubleClicked = true;
                        if (hoveredTile != null && unitsSet != null)
                        {
                            AssignNextFollowerToTile(unitsSet, leadActor, hoveredTile);
                            lastDragTile = hoveredTile;
                        }
                    }
                    else
                    {
                        isCtrlDoubleClicked = false;
                    }
                    lastClickTime = Time.time;
                }
                else if (Input.GetMouseButton(0) && isCtrlDoubleClicked)
                {
                    if (hoveredTile != null && hoveredTile != lastDragTile && unitsSet != null)
                    {
                        AssignNextFollowerToTile(unitsSet, leadActor, hoveredTile);
                        lastDragTile = hoveredTile;
                    }
                }
            }
            else if (!ctrlHeld)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    if (Time.time - lastTileClickTime <= 0.35f)
                    {
                        isTileDoubleClicked = true;
                        WorldTile hoveredTile = MapBox.instance.getMouseTilePos();
                        if (hoveredTile != null)
                        {
                            ProcessTileUnitInclusion(hoveredTile);
                            lastTileDragTile = hoveredTile;
                        }
                    }
                    else
                    {
                        isTileDoubleClicked = false;
                    }
                    lastTileClickTime = Time.time;
                }
                else if (Input.GetMouseButton(0) && isTileDoubleClicked)
                {
                    WorldTile hoveredTile = MapBox.instance.getMouseTilePos();
                    if (hoveredTile != null && hoveredTile != lastTileDragTile)
                    {
                        ProcessTileUnitInclusion(hoveredTile);
                        lastTileDragTile = hoveredTile;
                    }
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                isCtrlDoubleClicked = false;
                lastDragTile = null;
                isTileDoubleClicked = false;
                lastTileDragTile = null;
            }
        }

        private static void ProcessTileUnitInclusion(WorldTile tile)
        {
            if (tile == null || !tile.hasUnits())
            {
                return;
            }

            HashSet<Actor> unitsSet = ControllableUnit._units ?? ControllableUnit.getCotrolledUnits();
            foreach (Actor pActor in tile._units)
            {
                if (pActor != null && pActor.isAlive())
                {
                    if (assignedFollowerPositions.ContainsKey(pActor))
                    {
                        assignedFollowerPositions.Remove(pActor);
                    }
                    if (unitsSet == null || !unitsSet.Contains(pActor))
                    {
                        if (ControllableUnit._units == null)
                        {
                            ControllableUnit._units = unitsSet ?? new HashSet<Actor>();
                        }
                        ControllableUnit._units.Add(pActor);
                        ControllableUnit.addStatus(pActor);
                    }
                }
            }
        }

        private static void AssignNextFollowerToTile(HashSet<Actor> unitsSet, Actor leadActor, WorldTile tile)
        {
            foreach (Actor u in unitsSet)
            {
                if (u != null && u.isAlive() && u != leadActor && !assignedFollowerPositions.ContainsKey(u))
                {
                    assignedFollowerPositions[u] = tile;
                    break;
                }
            }
        }

        public static void UpdateAssignedPositions()
        {
            StatusAsset possessedAsset = AssetManager.status.get("possessed");

            // Continuously refresh possessed status effect on actively controlled squad members so they don't wander off after 5s
            if (ControllableUnit.isControllingUnit())
            {
                HashSet<Actor> activeUnits = ControllableUnit._units ?? ControllableUnit.getCotrolledUnits();
                if (activeUnits != null)
                {
                    foreach (Actor u in activeUnits)
                    {
                        if (u != null && u.isAlive() && possessedAsset != null)
                        {
                            u.addStatusEffect(possessedAsset, 10f, false);
                        }
                    }
                }
            }

            if (assignedFollowerPositions.Count == 0)
            {
                return;
            }

            List<Actor> toRemove = new List<Actor>();

            foreach (var kvp in assignedFollowerPositions)
            {
                Actor actor = kvp.Key;
                WorldTile targetTile = kvp.Value;

                Vector2 tTargetPos = targetTile.posV3;

                Vector2 tUnitPos = actor.current_position;
                actor.next_step_position_possession = tTargetPos;

                float dist = Toolbox.DistVec2Float(tUnitPos, tTargetPos);
                if (dist > 0.5f)
                {
                    actor.setPossessedMovement(true);
                }
                else
                {
                    actor.setPossessedMovement(false);
                }

                float tSpeedDelta = actor.updatePossessedMovementTowards(World.world.elapsed, tTargetPos);
                if (!actor.isInAir() && !actor.under_forces && tSpeedDelta >= 0.03f)
                {
                    BaseEffect tEffect = EffectsLibrary.spawnAt("fx_walk", tUnitPos, 0.1f);
                    if (tEffect != null)
                    {
                        tEffect.GetComponent<SpriteRenderer>().flipX = !actor.flip;
                    }
                }

                CheckforAndAttackEnemy(actor);

                if (actor == null || !actor.isAlive() || targetTile == null)
                {
                    toRemove.Add(actor);
                }
            }

            foreach (Actor actor in toRemove)
            {
                assignedFollowerPositions.Remove(actor);
            }
        }

        private static bool CheckforAndAttackEnemy(Actor pActor)
        {
            {
                Actor actor = pActor;

                actor.addStatusEffect(AssetManager.status.get("possessed"), 10f, false);

                if (actor.is_immovable)
                {
                    return false;
                }

                // start enemy check
                if (actor._timeout_targets > 0f)
                {

                }
                else
                {
                    actor._timeout_targets = 0.1f + Randy.randomFloat(0f, 1f);
                    BaseSimObject tNewTarget = actor.findEnemyObjectTarget(actor.asset.can_attack_buildings);
                    if (tNewTarget == null && actor._aggression_targets.Count > 0)
                    {
                        List<Actor> tList = new List<Actor>(actor._aggression_targets.Count);

                        foreach (long tID in actor._aggression_targets)
                        {
                            Actor tActor = World.world.units.get(tID);
                            if (!tActor.isRekt())
                            {
                                tList.Add(tActor);
                            }
                        }

                        if (tList.Count > 0)
                        {
                            tNewTarget = actor.checkObjectList(
                                tList,
                                actor.asset.can_attack_buildings,
                                true,
                                true,
                                30
                            );
                        }
                        else
                        {
                            actor._aggression_targets.Clear();
                        }
                    }
                    if (tNewTarget == null)
                    {
                        //return false;
                    }
                    else
                    {//target found, attempt to fight
                        Actor tActorTarget = tNewTarget.a;
                        float tAttackRange = actor.getAttackRange();
                        Vector3 tAttackPos = tActorTarget.current_position;
                        if (tActorTarget != null && actor.hasMeleeAttack())
                        {
                            float tDistToActor = Vector2.Distance(actor.current_position, tActorTarget.current_position);
                            if (tDistToActor > tAttackRange)
                            {
                                //currently overrides and allows attacking anyone targeted?
                                tDistToActor = tAttackRange;
                            }
                            tAttackPos = Toolbox.getNewPointVec2(actor.current_position, tAttackPos, tDistToActor, true);
                        }
                        Kingdom tKingdomPossessed = actor.kingdom;//World.world.kingdoms_wild.get("possessed"); // fixes units hitting friendlies
                        actor.tryToAttack(null, false, null, tAttackPos, tKingdomPossessed, (tActorTarget != null) ? tActorTarget.current_tile : null, tAttackRange * 0.2f);
                        actor.setPossessionAttackHappened();
                    }
                }
            }

            return true;
        }

        private void checkPossessedMovement(Actor pActor)
        {
            if (pActor.is_immovable)
            {
                return;
            }

            if (assignedFollowerPositions.ContainsKey(pActor))
            {
                return;
            }

            Vector2 tMovementVector = ControllableUnit.getMovementVector();
            Vector2 tUnitPos = pActor.current_position;

            Actor leadActor = ControllableUnit.getControllableUnit();
            HashSet<Actor> unitsSet = ControllableUnit._units;
            if (unitsSet == null)
            {
                unitsSet = ControllableUnit.getCotrolledUnits();
            }

            Vector2 tTargetPos;
            if (unitsSet == null || unitsSet.Count <= 1 || leadActor == null)
            {
                tTargetPos = tUnitPos + tMovementVector * 2f;
            }
            else
            {
                List<Actor> activeFollowersList = new List<Actor>();
                foreach (Actor u in unitsSet)
                {
                    if (u != null && u.isAlive() && !assignedFollowerPositions.ContainsKey(u))
                    {
                        activeFollowersList.Add(u);
                    }
                }

                int index = activeFollowersList.IndexOf(pActor);
                if (index < 0)
                {
                    return;
                }
                Vector2 leadPos = leadActor != null ? (Vector2)leadActor.current_position : tUnitPos;
                Vector2 leadTargetPos = leadPos + tMovementVector * 2f;

                if (pActor == leadActor)
                {
                    tTargetPos = leadTargetPos;

                    if (!ControllableUnit.isMovementActionActive())
                    {
                        pActor.setPossessedMovement(false);
                        if (tMovementVector == Vector2.zero)
                        {
                            return;
                        }
                    }
                    else
                    {
                        pActor.setPossessedMovement(true);
                    }
                }
                else
                {
                    Vector2 offset = GetPossessionFormationOffset(index, activeFollowersList.Count, possessionFormationType, possessionFormationSpacing, tMovementVector);
                    tTargetPos = leadTargetPos + offset;
                    if (Toolbox.DistVec2Float(pActor.current_position, tTargetPos) > 0.5f)
                    {
                        pActor.setPossessedMovement(true);
                    }
                    else
                    {
                        if (tMovementVector == Vector2.zero)
                        {
                            pActor.setPossessedMovement(false);
                            return;
                        }
                    }
                }
            }

            pActor.next_step_position_possession = tTargetPos;
            float tSpeedDelta = pActor.updatePossessedMovementTowards(World.world.elapsed, tTargetPos);
            if (!pActor.isInAir() && !pActor.under_forces && tSpeedDelta >= 0.03f)
            {
                BaseEffect tEffect = EffectsLibrary.spawnAt("fx_walk", tUnitPos, 0.1f);
                if (tEffect != null)
                {
                    tEffect.GetComponent<SpriteRenderer>().flipX = !pActor.flip;
                }
            }
        }

        public void SwapHotkeysForRPG()
        {
            //disable extra keys for now, maybe unlock and then enable movement with traits/levels
            HotkeyLibrary.action_jump.default_key_1 = KeyCode.None;
            HotkeyLibrary.action_dash.default_key_1 = KeyCode.None;
            HotkeyLibrary.action_dash.default_key_2 = KeyCode.None;
            HotkeyLibrary.action_backstep.default_key_1 = KeyCode.None;
            HotkeyLibrary.action_backstep.default_key_2 = KeyCode.None;
            HotkeyLibrary.action_swear.default_key_1 = KeyCode.None;
            HotkeyLibrary.action_talk.default_key_1 = KeyCode.None;
            HotkeyLibrary.action_steal.default_key_1 = KeyCode.None;
        }

        public void ResetHotkeys()
        {
            //set keys to default, change this to cached copy of original
            HotkeyLibrary.action_jump.default_key_1 = KeyCode.Space;
            HotkeyLibrary.action_dash.default_key_1 = KeyCode.LeftShift;
            HotkeyLibrary.action_dash.default_key_2 = KeyCode.RightShift;
            HotkeyLibrary.action_backstep.default_key_1 = KeyCode.LeftControl;
            HotkeyLibrary.action_backstep.default_key_2 = KeyCode.RightControl;
            HotkeyLibrary.action_swear.default_key_1 = KeyCode.F;
            HotkeyLibrary.action_talk.default_key_1 = KeyCode.T;
            HotkeyLibrary.action_steal.default_key_1 = KeyCode.Q;
        }
    }
}
