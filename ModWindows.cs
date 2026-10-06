using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SimpleRPG
{
    public partial class SimpleRPG : BaseUnityPlugin
    {
        public void ShowMenus()
        {
            if (showMenu)
            {
                windowRect = GUILayout.Window(9675309, windowRect, WindowM, " ", GUILayout.MaxWidth(200f), GUILayout.MinWidth(200f));
            }
            if (showActorInventory && currentActor != null)
            {

                actorInventoryRect = GUILayout.Window(9675314, actorInventoryRect, ActorInventoryWindow, "Actor Inventory", GUILayout.MinWidth(200f));
            }
            if (showRPGConstruction && currentActor != null)
            {
                rpgConstructionRect = GUILayout.Window(9675316, rpgConstructionRect, RPGConstructionWindow, "Construction", GUILayout.MaxWidth(500f), GUILayout.MinWidth(200f));
            }
            if (showStockpile && selectedBuilding != null)
            {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(selectedBuilding.current_position);
                // Float above the building; Unity's GUI coordinate system starts top-left, while ScreenPoint starts bottom-left
                stockpileRect.x = screenPos.x - (stockpileRect.width / 2);
                stockpileRect.y = Screen.height - screenPos.y - 100f; // offset upwards

                Matrix4x4 oldMatrix = GUI.matrix;
                float zoom = Mathf.Max(World.world.move_camera._target_zoom, 0.1f);
                float scale = 10f / zoom;
                Vector2 pivot = new Vector2(screenPos.x, Screen.height - screenPos.y);
                GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), pivot);

                stockpileRect = GUILayout.Window(9675315, stockpileRect, StockpileWindow, "Stockpile", GUILayout.MinWidth(200f));

                GUI.matrix = oldMatrix;
            }
        }

        public void WindowM(int windowID)
        {
            if (GUILayout.Button("disablehotkeys"))
            {
                PatchPossessionLogic();
                SwapHotkeysForRPG();
            }
            if (GUILayout.Button("resethotkeys"))
            {
                ResetHotkeys();
            }
            if (GUILayout.Button("narrative+messages"))
            {
                Actor actor = MapBox.instance.units.getSimpleList().GetRandom();

                Narrative.QueueEvent(
                    new NarrativeEvent(
                      targetActor: actor,
                      textToShow: "This is " + actor.data.name + ".",
                      shouldPause: true,
                      actorSaysText: true,
                      duration: 6f,
                      targetZoom: 15f
                      )
                );
            }
            if (GUILayout.Button("series test"))
            {
                //maybe add NE param to NE itself to chain them first
            }
            if (GUILayout.Button("narrative test2"))
            {
                Actor actor = MapBox.instance.units.getSimpleList().GetRandom();

                Narrative.QueueEvents(
                     new NarrativeEvent(
                       targetActor: actor,
                       textToShow: "This is " + actor.data.name + ".",
                       duration: 6f,
                       targetZoom: 15f
                       ),
                     new NarrativeEvent(
                       targetTile: actor.current_tile.tile_up.tile_up.tile_up.tile_up.tile_up.tile_up.tile_up,
                       textToShow: "This is a tile north of " + actor.data.name + ".",
                       duration: 6f,
                       targetZoom: 15f
                       )
                 );
            }
            if(GUILayout.Button("canAttackAnyone: " + canAttackAnyone))
            {
                canAttackAnyone = !canAttackAnyone;
            }

            GUILayout.BeginVertical("box");
            GUILayout.Label("Mod Settings:");
            showCursorIcons.Value = GUILayout.Toggle(showCursorIcons.Value, "Show Cursor Icons");
            buildNatureBuildings.Value = GUILayout.Toggle(buildNatureBuildings.Value, "Build Nature Buildings");
            buildEverything.Value = GUILayout.Toggle(buildEverything.Value, "Build Everything");
            buildForFree.Value = GUILayout.Toggle(buildForFree.Value, "Build For Free");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Space");
            formationSpacing.Value = GUILayout.HorizontalScrollbar(formationSpacing.Value, 1, 1, 15);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Type");
            if (GUILayout.Button("circ"))
            {
                formationType.Value = "circle";
            }
            if (GUILayout.Button("wedge"))
            {
                formationType.Value = "wedge";
            }
            if (GUILayout.Button("line"))
            {
                formationType.Value = "line";
            }
            if (GUILayout.Button("rect"))
            {
                formationType.Value = "rect";
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            if (currentActor != null)
            {
                GUILayout.Button("Controlling " + currentActor.name);
                if (GUILayout.Button("Toggle Inventory"))
                {
                    showActorInventory = !showActorInventory;
                }
                if (GUILayout.Button("Toggle Construction"))
                {
                    showRPGConstruction = !showRPGConstruction;
                }
                GUILayout.Button("assetID:" + currentActor.asset.id);
            }
            GUI.DragWindow();
        }
        public Vector2 InventoryRect;
        public Vector2 StockpileRect;
        public void ActorInventoryWindow(int windowID)
        {
            if (currentActor != null && currentActor.inventory != null)
            {
                bool hasResources = currentActor.inventory.hasResources();
                if (hasResources && currentActor.inventory.dict != null)
                {
                    // To avoid dictionary modification during enumeration
                    string transferResID = null;
                    foreach (string resID in currentActor.inventory.dict.Keys)
                    {
                        ResourceContainer resContainer = currentActor.inventory.dict[resID];
                        int resAmount = resContainer.amount;
                        if (resAmount > 0)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(resID + ": " + resAmount);
                            if (selectedBuilding != null && GUILayout.Button("To Stockpile"))
                            {

                                transferResID = resID;
                            }
                            GUILayout.EndHorizontal();
                        }
                    }

                    if (transferResID != null && selectedBuilding != null)
                    {
                        ThrowToStockpile(currentActor, selectedBuilding, transferResID, 1);
                    }
                }
                else
                {
                    GUILayout.Label("Inventory is empty.");
                }
            }
            GUI.DragWindow();
        }

        public static void ThrowToStockpile(Actor actor, Building building, string resourceID, int amount)
        {
            float duration = Toolbox.DistTile(actor.current_tile, building.current_tile);
            duration = Mathf.Clamp(duration, 1f, 1.5f);

            Vector2 start = actor.getThrowStartPosition();

            Vector2 end = building.current_position + building.asset.stockpile_center_offset;
            end.x += Randy.randomFloat(-0.1f, 0.1f);
            end.y += Randy.randomFloat(-0.1f, 0.1f);

            BehaviourActionBase<Actor>.world.resource_throw_manager.addNew(
                start,
                end,
                duration,
                resourceID,
                amount,
                4f,
                building
            );

            actor.inventory.remove(resourceID, amount);
            building.resources.change(resourceID, amount);
        }

        public void StockpileWindow(int windowID)
        {
            if (selectedBuilding != null && selectedBuilding.resources != null)
            {
                Dictionary<string, CityStorageSlot> resources = selectedBuilding.resources._resources;
                if (resources != null)
                {
                    string transferResID = null;
                    foreach (string resID in resources.Keys)
                    {
                        CityStorageSlot resContainer = resources[resID];
                        int amount = resContainer.amount;
                        string id = resContainer.id;
                        if (amount > 0)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(id + ": " + amount);
                            if (GUILayout.Button("To Bag"))
                            {
                                transferResID = id;
                            }
                            GUILayout.EndHorizontal();
                        }
                    }
                    if (transferResID != null)
                    {
                        if (currentActor != null && currentActor.inventory != null)
                        {
                            selectedBuilding.resources.change(transferResID, -1);
                            currentActor.inventory.add(transferResID, 1);
                        }
                    }
                }
                else
                {
                    GUILayout.Label("Stockpile is empty.");
                }
            }
        }

    }
}
