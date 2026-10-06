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
        public static Building selectedBuilding;
        public static Actor selectedActor;
        public void ClickInteract()
        {
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                return;
            }
            if (Input.GetMouseButton(0))
            {
                WorldTile hoveredTile = MapBox.instance.getMouseTilePos();
                /*
                Building buildingOnTile = hoveredTile.building;
                if(buildingOnTile == null)
                {
                    //check neighbor tiles for a building
                    foreach (WorldTile neighbor in hoveredTile.neighboursAll)
                    {
                        if (neighbor.building != null)
                        {
                            buildingOnTile = neighbor.building;
                            break;
                        }
                    }
                }
                */
                //range is too high with ranged-weapon users like monkey
                if (currentActor.isAttackPossible() && hoveredTile.distanceTo(currentActor.current_tile) < (currentActor.getAttackRange() + 3))
                {
                    switch (cursorID)
                    {
                        case -1:
                            //default cursor/no hovered object
                            if (hoveredTile.building != null)
                            {
                                if (hoveredTile.building.asset.is_stockpile)
                                {
                                    //Select building for depositing/withdrawal in a floating window
                                    selectedBuilding = hoveredTile.building;
                                    showStockpile = true;
                                    showActorInventory = true;
                                }
                                else
                                {
                                    selectedBuilding = null;
                                    showStockpile = false;
                                }
                            }
                            else
                            {
                                selectedBuilding = null;
                                showStockpile = false;
                            }
                            break;
                        case 0:
                            //dig tile action, soil, sand, misc
                            MapAction.decreaseTile(hoveredTile, false);
                            break;
                        case 1:
                            //trees
                            if (selectedBuilding.hasResourcesToCollect())
                            {
                                if (selectedBuilding.asset.resources_given != null)
                                {
                                    foreach (ResourceContainer tContainer in selectedBuilding.asset.resources_given)
                                    {
                                        int tAmount = tContainer.amount;
                                        if (selectedBuilding.asset.building_type == BuildingType.Building_Mineral)
                                        {
                                            tAmount++;
                                        }
                                        currentActor.addToInventory(tContainer.id, tAmount);
                                    }
                                }
                                selectedBuilding.extractResources(currentActor);
                            }
                            break;
                        case 2:
                            //farm/gather, sickle
                            if (selectedBuilding.hasResourcesToCollect())
                            {
                                if (selectedBuilding.asset.resources_given != null)
                                {
                                    foreach (ResourceContainer tContainer in selectedBuilding.asset.resources_given)
                                    {
                                        int tAmount = tContainer.amount;
                                        if (selectedBuilding.asset.building_type == BuildingType.Building_Mineral)
                                        {
                                            tAmount++;
                                        }
                                        currentActor.addToInventory(tContainer.id, tAmount);
                                    }
                                }
                                selectedBuilding.extractResources(currentActor);
                            }
                            break;
                        case 3:
                            //mineral
                            if (selectedBuilding.hasResourcesToCollect())
                            {
                                if (selectedBuilding.asset.resources_given != null)
                                {
                                    foreach (ResourceContainer tContainer in selectedBuilding.asset.resources_given)
                                    {
                                        int tAmount = tContainer.amount;
                                        if (selectedBuilding.asset.building_type == BuildingType.Building_Mineral)
                                        {
                                            tAmount++;
                                        }
                                        currentActor.addToInventory(tContainer.id, tAmount);
                                    }
                                }
                                selectedBuilding.extractResources(currentActor);
                            }
                            break;
                        case 4:
                            //sword, hovered "enemy"
                            if (selectedActor.isAlive() && currentActor.isAttackReady() && currentActor.canAttackTarget(selectedActor, false, false))
                            {
                                currentActor.tryToAttack(selectedActor);
                            }
                            break;
                        case 5:
                            //speak, hovered friendly unit
                            if (selectedActor.isAlive())
                            {
                                string id = selectedActor.asset.id;
                                if (SimpleRPG.HasActorStartPhrase?.Invoke(id) == true)
                                {
                                    string phraseToSay = SimpleRPG.GetRandomActorStartPhrase(id);

                                    if (phraseToSay != null)
                                    {
                                        SimpleRPG.ActorSay(selectedActor, phraseToSay, 3);
                                    }
                                }
                            }
                            break;
                        case 6:
                            //hammer, help under construction buildings
                            if (selectedBuilding != null && selectedBuilding.isUnderConstruction())
                            {
                                selectedBuilding.updateBuild(1);
                            }
                            break;
                        default:
                            break;
                    }
                    currentActor.startAttackCooldown();
                    currentActor.punchTargetAnimation(hoveredTile.posV3, true, currentActor.hasRangeAttack(), 40f);
                }
            }

        }
    }
}
