using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace SimpleRPG
{
    [BepInPlugin("simplerpg", "rpg", "0.0.0.1")]
    public partial class SimpleRPG : BaseUnityPlugin
    {
        public static Actor currentActor => getCurrentActor();
        public static NarrativeManager Narrative = new NarrativeManager();
        public static Harmony harmony;

        public void Start()
        {
            SettingSetup();
            RunPatches();
            setup();
        }

        public void Update()
        {
            if (global::Config.game_loaded)
            {
                HandleDirectUnitInput();
                if (ControllableUnit.isControllingUnit())
                {
                    ClickInteract();
                    RPGConstructionUpdate();
                }
                UpdateAssignedPositions();
                Narrative.Update();
                actorInventoryRect.height = 0;
                stockpileRect.height = 0;
            }
        }

        public void OnGUI()
        {
            if (GUI.Button(new Rect(Screen.width - 120, 160, 95, 20), "SimpleRPG"))
                showMenu = !showMenu;

            if (GUI.Button(new Rect(Screen.width - 25, 160, 25, 20), "x"))
            {
                showSubMod = false;
                showMenu = false;
            }

            ShowMenus();
        }

        public void OnDestroy()
        {
            if (SimpleRPG.harmony != null)
            {
                SimpleRPG.harmony.UnpatchSelf();
            }
            if (Axe != null) Destroy(Axe);
            if (Pickaxe != null) Destroy(Pickaxe);
            if (Shovel != null) Destroy(Shovel);
            if (Sickle != null) Destroy(Sickle);
            if (Sword != null) Destroy(Sword);
            if (Speak != null) Destroy(Speak);
            if (Hammer != null) Destroy(Hammer);
        }
    }
}
