using Amazon.Runtime.CredentialManagement;
using BepInEx;
using HarmonyLib;
using NUnit.Framework.Interfaces;
using System.Reflection;
using UnityEngine;

namespace SimpleRPG
{
    public partial class SimpleRPG : BaseUnityPlugin
    {
        public void RunPatches()
        {
            SimpleRPG.harmony = new Harmony("simplerpg");
            MethodInfo original;
            MethodInfo patch;
            SimpleRPG.harmony.PatchAll();

            original = AccessTools.Method(typeof(MouseCursor), "Update");
            patch = AccessTools.Method(typeof(SimpleRPG), "CursorUpdatePrefix");
            SimpleRPG.harmony.Patch(original, new HarmonyMethod(patch));

            original = AccessTools.Method(typeof(MoveCamera), "followFocusUnit");
            patch = AccessTools.Method(typeof(SimpleRPG), "followFocusUnitPrefix");
            SimpleRPG.harmony.Patch(original, new HarmonyMethod(patch));

            original = AccessTools.Method(typeof(ControllableUnit), "clear");
            patch = AccessTools.Method(typeof(SimpleRPG), "clearPrefix");
            SimpleRPG.harmony.Patch(original, new HarmonyMethod(patch));

            original = AccessTools.Method(typeof(BaseAssetLibrary), "logAssetError");
            patch = AccessTools.Method(typeof(SimpleRPG), "logAssetError_Prefix");
            SimpleRPG.harmony.Patch(original, new HarmonyMethod(patch));
        }

        public static bool logAssetError_Prefix(string pMessage, string pRightPart = null)
        {
            return false;
            Debug.LogError(BaseAssetLibrary.formatLog(pMessage, pRightPart));
        }

        //adding lerp smoothness to movement
        public static bool followFocusUnitPrefix(MoveCamera __instance)
        {
            if (!MoveCamera.hasFocusUnit())
            {
                return false;
            }
            Actor tFocusActor = MoveCamera._focus_unit;
            if (!tFocusActor.isAlive())
            {
                BaseSimObject attackedBy = tFocusActor.attackedBy;
                Actor tAttackedBy = (attackedBy != null) ? attackedBy.a : null;
                if (tAttackedBy != null && tAttackedBy.isAlive())
                {
                    WorldTip.addWordReplacement("$name$", tFocusActor.coloredName);
                    WorldTip.addWordReplacement("$killer$", tAttackedBy.coloredName);
                    WorldTip.showNowTop("tip_followed_unit_killed", true);
                    Actor a = tAttackedBy.a;
                    tFocusActor.attackedBy = null;
                    MoveCamera.setFocusUnit(a);
                    __instance._focus_timer = 0f;
                    return false;
                }
                WorldTip.addWordReplacement("$name$", tFocusActor.coloredName);
                WorldTip.showNowTop("tip_followed_unit_died", true);
                __instance.clearFocusUnitAndUnselect();
                return false;
            }
            else
            {
                if (MoveCamera.camera_drag_run || InputHelpers.touchCount > 0)
                {
                    __instance._focus_timer = 0f;
                    return false;
                }
                Vector3 tPos = tFocusActor.current_position;
                tPos.z = __instance.transform.position.z;
                if (__instance._focus_timer <= 1f)
                {
                    __instance._focus_timer += Time.deltaTime;
                    __instance._focus_timer = Mathf.Clamp(__instance._focus_timer, 0f, 1f);
                    tPos.x = iTween.easeOutCubic(__instance.transform.position.x, tPos.x, __instance._focus_timer);
                    tPos.y = iTween.easeOutCubic(__instance.transform.position.y, tPos.y, __instance._focus_timer);
                }
                // only changed line, adding lerp to make camera movement smoother
                //__instance.transform.position = tPos;
                __instance.transform.position = Vector3.Lerp(__instance.transform.position, tPos, Time.deltaTime * 1f);
                return false;
            }
        }

        //overriding textures for cursor interactions
        public static bool CursorUpdatePrefix(MouseCursor __instance)
        {
            if (global::Config.game_loaded && ControllableUnit.isControllingUnit())
            {
                cursorID = -1;
                __instance.setCursor(0);

                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                {
                    selectedActor = null;
                    selectedBuilding = null;
                    if (showCursorIcons.Value)
                    {
                        Cursor.SetCursor(DefaultCursor, Vector2.zero, CursorMode.Auto);
                        __instance.renderCursor();
                    }
                    return false;
                }

                WorldTile hoveredTile = MapBox.instance.getMouseTilePos();
                if (hoveredTile == null || currentActor == null || currentActor.current_tile == null || hoveredTile.distanceTo(currentActor.current_tile) > 3f)
                {
                    selectedActor = null;
                    selectedBuilding = null;
                    if (showCursorIcons.Value)
                    {
                        Cursor.SetCursor(DefaultCursor, Vector2.zero, CursorMode.Auto);
                        __instance.renderCursor();
                    }
                    return false;
                }

                Building buildingOnTile = hoveredTile.building;
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    if (hoveredTile.Type.decrease_to_id != null)
                    {
                        bool shouldShowIcon = showCursorIcons.Value;
                        switch (hoveredTile.Type.id)
                        {
                            case "hills":
                                cursorID = 0;
                                if (shouldShowIcon)
                                {
                                    Cursor.SetCursor(Pickaxe, Vector2.zero, CursorMode.Auto);
                                }
                                break;
                            case "mountains":
                                cursorID = 0;
                                if (shouldShowIcon)
                                {
                                    Cursor.SetCursor(Pickaxe, Vector2.zero, CursorMode.Auto);
                                }
                                break;
                            case "soil_high":
                                cursorID = 0;
                                if (shouldShowIcon)
                                {
                                    Cursor.SetCursor(Shovel, Vector2.zero, CursorMode.Auto);
                                }
                                break;
                            case "soil_low":
                                cursorID = 0;
                                if (shouldShowIcon)
                                {
                                    Cursor.SetCursor(Shovel, Vector2.zero, CursorMode.Auto);
                                }
                                break;
                            case "sand":
                                cursorID = 0;
                                if (shouldShowIcon)
                                {
                                    Cursor.SetCursor(Shovel, Vector2.zero, CursorMode.Auto);
                                }
                                break;
                            default:
                                cursorID = 0;
                                if (shouldShowIcon)
                                {
                                    Cursor.SetCursor(Shovel, Vector2.zero, CursorMode.Auto);
                                }
                                break;
                        }

                    }
                    return false;
                }
                if (hoveredTile.hasUnits())
                {
                    bool shouldShowIcon = showCursorIcons.Value;
                    selectedActor = hoveredTile._units.First();
                    if (selectedActor != currentActor)
                    {
                        if (selectedActor.areFoes(currentActor) || canAttackAnyone)
                        {
                            //sword, for attacking
                            cursorID = 4;
                            if (shouldShowIcon)
                            {
                                Cursor.SetCursor(Sword, Vector2.zero, CursorMode.Auto);
                            }
                            return false;
                        }
                        else
                        {
                            //speak, for speaking
                            cursorID = 5;
                            if (shouldShowIcon)
                            {
                                Cursor.SetCursor(Speak, Vector2.zero, CursorMode.Auto);
                            }
                        }
                    }
                   
                }
                else
                {
                    if (buildingOnTile != null)
                    {
                        bool shouldShowIcon = showCursorIcons.Value;
                        if (buildingOnTile.isUnderConstruction())
                        {
                            //hammer for finishing constructions
                            cursorID = 6;
                            selectedBuilding = buildingOnTile;
                            if (shouldShowIcon)
                            {
                                Cursor.SetCursor(Hammer, Vector2.zero, CursorMode.Auto);
                            }
                        }
                        else
                        {
                            switch (buildingOnTile.asset.building_type)
                            {
                                case BuildingType.Building_Tree:
                                    cursorID = 1;
                                    selectedBuilding = buildingOnTile;
                                    if (shouldShowIcon)
                                    {
                                        Cursor.SetCursor(Axe, Vector2.zero, CursorMode.Auto);
                                    }
                                    break;
                                case BuildingType.Building_Plant:
                                    cursorID = 2;
                                    selectedBuilding = buildingOnTile;
                                    if (shouldShowIcon)
                                    {
                                        Cursor.SetCursor(Sickle, Vector2.zero, CursorMode.Auto);
                                    }
                                    break;
                                case BuildingType.Building_Fruits:
                                    cursorID = 2;
                                    selectedBuilding = buildingOnTile;
                                    if (shouldShowIcon)
                                    {
                                        Cursor.SetCursor(Sickle, Vector2.zero, CursorMode.Auto);
                                    }
                                    break;
                                case BuildingType.Building_Mineral:
                                    cursorID = 3;
                                    selectedBuilding = buildingOnTile;
                                    if (shouldShowIcon)
                                    {
                                        Cursor.SetCursor(Pickaxe, Vector2.zero, CursorMode.Auto);
                                    }
                                    break;
                                default:
                                    cursorID = -1;
                                    if (shouldShowIcon)
                                    {
                                        Cursor.SetCursor(DefaultCursor, Vector2.zero, CursorMode.Auto);
                                    }
                                    break;
                            }
                        }
                    }
                    else
                    {
                        cursorID = -1;
                        selectedActor = null;
                        selectedBuilding = null;
                        if (showCursorIcons.Value)
                        {
                            Cursor.SetCursor(DefaultCursor, Vector2.zero, CursorMode.Auto);
                            __instance.renderCursor();
                        }
                    }

                }
                return false;
            }
            return true;
        }

        public static bool clearPrefix(bool pCallKill)
        {
            PossessionUI.toggle(false);
            if (global::Config.joyControls)
            {
                World.world.joys.SetActive(false);
                UltimateJoystick.ResetJoysticks();
            }
            if (!ControllableUnit.isControllingUnit())
            {
                return false;
            }
            foreach (Actor ptr in (ControllableUnit.getCotrolledUnits()))
            {
                Actor tActor = ptr;
                tActor.finishStatusEffect("possessed");
                //tActor.cancelAllBeh();
                //tActor.applyRandomForce(1.5f, 2f);
                //tActor.makeStunned(1f);
                //tActor.makeConfused(6f, false);
                //tActor.setPossessedMovement(false);
                if (pCallKill && tActor.asset.id == "crabzilla")
                {
                   // tActor.getHitFullHealth(AttackType.Divine);
                }
            }
            ControllableUnit._unit_main = null;
            ControllableUnit._units.Clear();
            World.world.selected_buttons.unselectAll();
            return false;
        }


    }
}
