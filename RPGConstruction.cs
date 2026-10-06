using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleRPG
{
    public partial class SimpleRPG
    {
        public void RPGConstructionWindow(int windowID)
        {
            if (!global::Config.game_loaded || SmoothLoader.isLoading()) return;
            
            Color defaultColor = GUI.backgroundColor;
            if (currentActor == null)
            {
                GUILayout.Button("No actor is controlled");
                return;
            }
            GUILayout.BeginHorizontal();
            GUILayout.Button("Selected:");
            GUILayout.Button(selectedRPGBuilding != null ? selectedRPGBuilding.id : "none");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = filterEnabledRPG ? Color.green : defaultColor;
            if (GUILayout.Button("FilterToggle")) filterEnabledRPG = !filterEnabledRPG;
            GUI.backgroundColor = defaultColor;
            filterStringRPG = GUILayout.TextField(filterStringRPG);
            GUILayout.EndHorizontal();

            GUI.backgroundColor = placingRPGBuildingToggle ? Color.green : defaultColor;
            if (GUILayout.Button("Toggle placing"))
            {
                placingRPGBuildingToggle = !placingRPGBuildingToggle;
                global::Config.lockGameControls = placingRPGBuildingToggle;
            }
            GUI.backgroundColor = defaultColor;

            rpgConstructionScroll = GUILayout.BeginScrollView(rpgConstructionScroll, GUILayout.Width(450f), GUILayout.Height(300f));
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();

            if (GUILayout.Button("none", GUILayout.Height(30f)))
            {
                selectedRPGBuilding = null;
            }

            int count = 1;
            string actorKingdom = currentActor.kingdom.species_id;

            foreach (var building in AssetManager.buildings.list)
            {
                if (building.id.Contains("!")) continue;
                if (filterEnabledRPG && !building.id.Contains(filterStringRPG)) continue;

                bool isNature = building.kingdom == "nature" || building.civ_kingdom == "nature";

                bool canBuild = buildEverything.Value ||
                               (buildNatureBuildings.Value && isNature) ||
                               (!isNature && (building.kingdom == actorKingdom || building.civ_kingdom == actorKingdom));

                if (!canBuild) continue;

                GUILayout.BeginHorizontal();

                // Draw button with Sprite and Name
                if (GUILayout.Button(building.id, GUILayout.Height(40f)))
                {
                    selectedRPGBuilding = building;
                }

                if (building.building_sprites != null && building.building_sprites.animation_data != null && building.building_sprites.animation_data.Count > 0)
                {
                    Sprite sprite = null;
                    if (buildingSpriteCache.ContainsKey(building.id))
                    {
                        sprite = buildingSpriteCache[building.id];
                    }
                    else
                    {
                        var animData = building.building_sprites.animation_data.GetRandom();
                        if (animData != null && animData.main != null && animData.main.Length > 0)
                        {
                            sprite = animData.main.GetRandom();
                            if (sprite != null)
                            {
                                buildingSpriteCache[building.id] = sprite;
                            }
                        }
                    }

                    if (sprite != null && sprite.texture != null)
                    {
                        Rect tRect = sprite.textureRect;
                        Rect drawRect = GUILayoutUtility.GetRect(40f, 40f, GUILayout.Width(40f), GUILayout.Height(40f));
                        GUI.DrawTextureWithTexCoords(drawRect, sprite.texture,
                            new Rect(tRect.x / sprite.texture.width,
                                     tRect.y / sprite.texture.height,
                                     tRect.width / sprite.texture.width,
                                     tRect.height / sprite.texture.height));
                    }
                }

                GUILayout.EndHorizontal();

                count++;
                if (count % 10 == 0)
                {
                    GUILayout.EndVertical();
                    GUILayout.BeginVertical();
                }
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        public void RPGConstructionUpdate()
        {
            if (placingRPGBuildingToggle && selectedRPGBuilding != null && currentActor != null)
            {
                var tile = MapBox.instance.getMouseTilePos();
                if (tile == null) return;

                PixelFlashEffects flashEffects = MapBox.instance.flash_effects;

                if (selectedRPGBuilding.fundament != null)
                {
                    int baseX = tile.x - selectedRPGBuilding.fundament.left;
                    int baseY = tile.y - selectedRPGBuilding.fundament.bottom;
                    int width = selectedRPGBuilding.fundament.right + selectedRPGBuilding.fundament.left + 1;
                    int height = selectedRPGBuilding.fundament.top + selectedRPGBuilding.fundament.bottom + 1;

                    for (int j = 0; j < width; j++)
                    {
                        for (int k = 0; k < height; k++)
                        {
                            var subTile = MapBox.instance.GetTile(baseX + j, baseY + k);
                            if (subTile != null)
                            {
                                flashEffects.flashPixel(subTile, 10);
                            }
                        }
                    }
                }

                // Place building if mouse clicked
                if (Input.GetMouseButton(0) && !placedOnceRPG)
                {
                    if (Vector2.Distance(currentActor.current_position, tile.posV3) < 30f)
                    {
                        TryPlaceRPGBuilding(tile);
                    }
                    else
                    {
                        string msg = "Too far away to build!";
                        if (hasMessagesMod)
                        {
                            //Messages.ActorSay(currentActor, msg, 2f);
                        }
                        else
                        {
                            WorldTip.showNowTop(msg, false);
                        }
                        placedOnceRPG = true;
                    }
                }

                if (Input.GetMouseButtonUp(0))
                {
                    placedOnceRPG = false;
                }
            }
        }

        private void TryPlaceRPGBuilding(WorldTile tile)
        {
            placedOnceRPG = true;

            if (selectedRPGBuilding == null || tile == null || currentActor == null) return;

            // Check costs
            if (!buildForFree.Value)
            {
                var cost = selectedRPGBuilding.cost;
                int requiredWood = cost != null ? cost.wood : 0;
                int requiredStone = cost != null ? cost.stone : 0;
                int requiredGold = cost != null ? cost.gold : 0;
                int requiredMetals = cost != null ? cost.common_metals : 0;

                int woodHas = GetResourceAmount("wood");
                int stoneHas = GetResourceAmount("stone");
                int goldHas = GetResourceAmount("gold");
                int metalsHas = GetResourceAmount("common_metals");

                if (woodHas < requiredWood || stoneHas < requiredStone || goldHas < requiredGold || metalsHas < requiredMetals)
                {
                    string msg = $"Not enough resources! Need";
                    if (requiredWood > 0) msg += $" {requiredWood} wood";
                    if (requiredStone > 0) msg += $" {requiredStone} stone";
                    if (requiredGold > 0) msg += $" {requiredGold} gold";
                    if (requiredMetals > 0) msg += $" {requiredMetals} metals";

                    if (hasMessagesMod)
                    {
                        //Messages.ActorSay(currentActor, msg, 3f);
                    }
                    else
                    {
                        WorldTip.showNowTop(msg, false);
                    }
                    return;
                }

                // Deduct costs
                if (requiredWood > 0) currentActor.inventory.remove("wood", requiredWood);
                if (requiredStone > 0) currentActor.inventory.remove("stone", requiredStone);
                if (requiredGold > 0) currentActor.inventory.remove("gold", requiredGold);
                if (requiredMetals > 0) currentActor.inventory.remove("common_metals", requiredMetals);
            }

            // Place Building
            var building = MapBox.instance.buildings.addBuilding(selectedRPGBuilding.id, tile);
            if (building != null)
            {
                building.updateBuild(100);
            }

            if (!Input.GetKey(KeyCode.LeftShift))
            {
                selectedRPGBuilding = null;
            }
        }

        private int GetResourceAmount(string id)
        {
            if (currentActor.inventory == null || currentActor.inventory.dict == null) return 0;
            if (currentActor.inventory.dict.ContainsKey(id))
            {
                return currentActor.inventory.dict[id].amount;
            }
            return 0;
        }
    }
}
