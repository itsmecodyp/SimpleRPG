using BepInEx;
using BepInEx.Configuration;

namespace SimpleRPG
{
    public static class ModSettings
    {
        public static ConfigEntry<string> formationType => SimpleRPG.formationType;
        public static ConfigEntry<float> formationSpacing => SimpleRPG.formationSpacing;
    }

    public partial class SimpleRPG : BaseUnityPlugin
    {
        public static ConfigEntry<bool> showCursorIcons;
        public static ConfigEntry<bool> buildNatureBuildings;
        public static ConfigEntry<bool> buildEverything;
        public static ConfigEntry<bool> buildForFree;
        public static ConfigEntry<string> formationType;
        public static ConfigEntry<float> formationSpacing;

        public void SettingSetup()
        {
            showCursorIcons = Config.Bind("General.Toggles",
                                               "ShowCursorIcons",
                                               true,
                                               "Whether or not to show the cursor interaction icons");
            buildNatureBuildings = Config.Bind("General.Toggles",
                                               "BuildNatureBuildings",
                                               true,
                                               "Whether or not units can build nature buildings (trees/bushes)");
            buildEverything = Config.Bind("General.Toggles",
                                               "BuildEverything",
                                               false,
                                               "Whether or not units can build all buildings regardless of kingdom");
            buildForFree = Config.Bind("General.Toggles",
                                               "BuildForFree",
                                               false,
                                               "Whether or not units can build buildings without required resources");
            formationType = Config.Bind("Possession.Formations",
                                        "FormationType",
                                        "circle",
                                        "Formation shape for multi-unit possession control (circle, wedge, line)");
            formationSpacing = Config.Bind("Possession.Formations",
                                           "FormationSpacing",
                                           2f,
                                           "Spacing distance between units in possession formation");
        }
    }
}
