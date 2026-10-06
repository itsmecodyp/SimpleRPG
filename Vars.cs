using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SimpleRPG
{
    //file for storing mostly once/rarely used fields out of sight
    public partial class SimpleRPG : BaseUnityPlugin
    {
        public static bool canAttackAnyone;
        public bool showSubMod = true;
        public bool showMenu;
        public Rect windowRect;
        public bool showActorInventory = false;
        public bool showStockpile = false;
        public Rect actorInventoryRect = new Rect(200f, 200f, 250f, 300f);
        public Rect stockpileRect = new Rect(500f, 200f, 250f, 300f);
        public static int cursorID = -1;
        public static Texture2D Axe;
        public static Texture2D Pickaxe;
        public static Texture2D Shovel;
        public static Texture2D Sickle;
        public static Texture2D Sword;
        public static Texture2D Speak;
        public static Texture2D Hammer;
        public static bool hasMessagesMod = false;
        public static Texture2D DefaultCursor => MouseCursor._cursors[0];

        public bool showRPGConstruction;
        public Rect rpgConstructionRect = new Rect(200f, 200f, 500f, 350f);
        public Vector2 rpgConstructionScroll;
        public BuildingAsset selectedRPGBuilding;
        public bool placingRPGBuildingToggle;
        public bool filterEnabledRPG;
        public string filterStringRPG = "human";
        public bool placedOnceRPG;

        public Dictionary<string, Sprite> buildingSpriteCache = new Dictionary<string, Sprite>();

        public static Dictionary<Actor, WorldTile> assignedFollowerPositions = new Dictionary<Actor, WorldTile>();
        public static float lastCtrlTapTime = 0f;
        public static float lastClickTime = 0f;
        public static float lastTileClickTime = 0f;
        public static bool isCtrlDoubleClicked = false;
        public static WorldTile lastDragTile = null;
        public static bool isTileDoubleClicked = false;
        public static WorldTile lastTileDragTile = null;
        public static int ctrlTapCount = 0;
        public static bool pendingDoubleTap = false;
        public static float doubleTapTime = 0f;

        public static Actor getCurrentActor()
        {
            if (ControllableUnit.isControllingUnit())
            {
                return ControllableUnit.getControllableUnit();
            }
            else return null;
        }
    }
}
