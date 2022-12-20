
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace MC_SVCrewRoll
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Main : BaseUnityPlugin
    {
        // BepInEx
        public const string pluginGuid = "mc.starvalor.crewroll";
        public const string pluginName = "SV Crew Roll";
        public const string pluginVersion = "1.0.7";

        // Star Valor
        internal const int crewItemType = 5;
        private const int lobbyPanelCode = 1;

        // Mod
        private const string modSaveFolder = "/MCSVSaveData/";  // /SaveData/ sub folder
        private const string modSaveFilePrefix = "CrewRoll_"; // modSaveFilePrefixNN.dat
        public static ConfigEntry<int> cfgSkillBasePrice;
        public static ConfigEntry<int> cfgBonusBasePrice;
        public static ConfigEntry<bool> cfgRetainLevel;
        public static ConfigEntry<bool> cfgRestrictSkillGen;
        public static ConfigEntry<float> cfgDualSkillChance;
        public static ConfigEntry<int> cfgPopupDelay;
        internal static MethodInfo crewSkillGetQuantityShipBonuses = AccessTools.Method(typeof(CrewSkill), "GetQuantityShipBonuses");
        internal static MethodInfo crewSkillMaxQuantityShipBonuses = AccessTools.Method(typeof(CrewSkill), "MaxQuantityShipBonuses");
        internal static DockingUI dockingUIInstance = null;
        internal static GameObject player = null;

        // Debug
        internal static ManualLogSource log = BepInEx.Logging.Logger.CreateLogSource(pluginName);

        public void Awake()
        {
            LoadAssets();
            Configure();
            UI.mainRef = this;
            Harmony.CreateAndPatchAll(typeof(Main));
        }

        private void LoadAssets()
        {
            string pluginfolder = System.IO.Path.GetDirectoryName(GetType().Assembly.Location);
            string bundleName = "mc_svcrewroll";
            AssetBundle assets = AssetBundle.LoadFromFile($"{pluginfolder}\\{bundleName}");
            GameObject pack = assets.LoadAsset<GameObject>("Assets/mc_crewroll.prefab");
            UI.mainPanel = pack.transform.Find("mc_crewrollCanvas").Find("mc_crewrollCrewRR").gameObject;
            UI.crewBtnIcon = pack.transform.Find("mc_crewrollBtnIcon").gameObject.GetComponent<SpriteRenderer>().sprite;
            UI.crewListItem = pack.transform.Find("mc_crewrollCrewListItem").gameObject;
            UI.skillItem = pack.transform.Find("mc_crewrollSkillItem").gameObject;
            UI.bonusItem = pack.transform.Find("mc_crewrollBonusItem").gameObject;
            UI.addSkillItem = pack.transform.Find("mc_crewrollAddSkillItem").gameObject;
            UI.confirmPanel = pack.transform.Find("mc_crewrollConfirmDlg").gameObject;
            UI.possibleBonusesPopup = pack.transform.Find("mc_crewrollPossBonusPanel").gameObject;
        }

        private void Configure()
        {
            // Costs
            cfgSkillBasePrice = Config.Bind<int>(
                "Costs",
                "Skill re-roll / lock",
                50000,
                "Base price to re-roll or lock a skill");
            cfgBonusBasePrice = Config.Bind<int>(
                "Costs",
                "Bonus re-roll / lock",
                75000,
                "Base price to lock a bonus");

            // Behaviour
            cfgRetainLevel = Config.Bind<bool>(
                "Behaviour",
                "Retain skill levels?",
                true,
                "If enabled, when skills are rerolled, they are replaced with skills of equal level.");
            cfgDualSkillChance = Config.Bind<float>(
                "Behaviour",
                "% chance of duplicate skill",
                0.1f,
                "% chance of rolling a duplicate skill if restricted skill generation is enabled.");
            cfgRestrictSkillGen = Config.Bind<bool>(
                "Behaviour",
                "Restricted skill generation",
                true,
                "If disabled, duplicate and triplicate skills are not restricted.  Otherwise % chance to accept duplicates is used and triplicates are impossible.");

            // UI
            cfgPopupDelay = Config.Bind<int>(
                "UI",
                "Popup delay (s)",
                1,
                "Delay before possible bonuses popup appears.");
        }
        
        [HarmonyPatch(typeof(DockingUI), nameof(DockingUI.OpenPanel))]
        [HarmonyPostfix]
        private static void DockingUIOpenPanel_Post(DockingUI __instance, GameObject ___lobbyPanel, int code)
        {
            if (__instance.station.factionIndex == (int)TFaction.Tecnomancers)
                return;

            if (code != lobbyPanelCode)
            {
                UI.MainPanelSetActive(false);
                UI.CrewBtnSetActive(false);
                return;
            }

            if (dockingUIInstance != __instance)
            {
                dockingUIInstance = __instance;
                UI.Initialise(___lobbyPanel);
            }

            if (player == null)
                player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                return;

            UI.CrewBtnSetActive(true);
            if (UI.rerollWasLastLobbyPanel)
                UI.MainPanelSetActive(true);
        }

        [HarmonyPatch(typeof(DockingUI), nameof(DockingUI.StartDockingStation))]
        [HarmonyPostfix]
        private static void DockingUIStartDocking_Post(DockingUI __instance, GameObject ___lobbyPanel)
        {
            if (__instance.station.factionIndex == (int)TFaction.Tecnomancers)
                return;

            if (dockingUIInstance != __instance)
            {
                dockingUIInstance = __instance;
                UI.Initialise(___lobbyPanel);
            }

            if (player == null)
                player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                return;

            if (AccessTools.FieldRefAccess<DockingUI, GameObject>("lobbyPanel")(__instance).activeSelf)
                UI.CrewBtnSetActive(true);
        }

        [HarmonyPatch(typeof(DockingUI), nameof(DockingUI.CloseDockingStation))]
        [HarmonyPrefix]
        private static void DockingUICloseDockingStation_Pre()
        {
            UI.MainPanelSetActive(false);
            UI.CrewBtnSetActive(false);
        }

        [HarmonyPatch(typeof(DockingUI), nameof(DockingUI.ShowQuests))]
        [HarmonyPrefix]
        private static void DockingUIShowQuests_Pre()
        {
            UI.MainPanelSetActive(false);
            UI.rerollWasLastLobbyPanel = false;
        }

        [HarmonyPatch(typeof(DockingUI), "ShowContacts")]
        [HarmonyPrefix]
        private static void DockingUIShowContacts_Pre()
        {
            UI.MainPanelSetActive(false);
            UI.rerollWasLastLobbyPanel = false;
        }

        [HarmonyPatch(typeof(DockingUI), "ShowCrewForHire")]
        [HarmonyPrefix]
        private static void DockingUIShowCrewForHire_Pre()
        {
            UI.MainPanelSetActive(false);
            UI.rerollWasLastLobbyPanel = false;
        }

        [HarmonyPatch(typeof(DockingUI), "OpenLastLobbyPanel")]
        [HarmonyPostfix]
        private static void DockingUIOpenLastLobbyPanel_Post(DockingUI __instance, GameObject ___lobbyPanel)
        {
            if (!UI.rerollWasLastLobbyPanel)
                return;

            if (dockingUIInstance != __instance)
            {
                dockingUIInstance = __instance;
                UI.Initialise(___lobbyPanel);
            }

            if (player == null)
                player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                return;

            AccessTools.Method(typeof(DockingUI), "CloseLobbyPanels").Invoke(__instance, null);
            UI.MainPanelSetActive(true);
        }

        [HarmonyPatch(typeof(DockingUI), "CloseLobbyPanels")]
        [HarmonyPostfix]
        private static void DockingUICloseLobbyPanels_Post()
        {
            UI.MainPanelSetActive(false);
        }

        [HarmonyPatch(typeof(GameData), nameof(GameData.SaveGame))]
        [HarmonyPrefix]
        private static void GameDataSaveGame_Pre()
        {
            SaveGame();
        }

        private static void SaveGame()
        {
            if (CrewReroll.data == null || CrewReroll.data.Count() == 0)
                return;

            CrewReroll.data.PrepareSaveData();

            string tempPath = Application.dataPath + GameData.saveFolderName + modSaveFolder + "LOTemp.dat";

            if (!Directory.Exists(Path.GetDirectoryName(tempPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(tempPath));

            if (File.Exists(tempPath))
                File.Delete(tempPath);

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            FileStream fileStream = File.Create(tempPath);
            binaryFormatter.Serialize(fileStream, CrewReroll.data);
            fileStream.Close();

            File.Copy(tempPath, Application.dataPath + GameData.saveFolderName + modSaveFolder + modSaveFilePrefix + GameData.gameFileIndex.ToString("00") + ".dat", true);
            File.Delete(tempPath);
        }

        [HarmonyPatch(typeof(MenuControl), nameof(MenuControl.LoadGame))]
        [HarmonyPostfix]
        private static void MenuControlLoadGame_Post()
        {
            Main.player = GameObject.FindGameObjectWithTag("Player");
            LoadData(GameData.gameFileIndex.ToString("00"));
        }

        private static void LoadData(string saveIndex)
        {
            string modData = Application.dataPath + GameData.saveFolderName + modSaveFolder + modSaveFilePrefix + saveIndex + ".dat";
            try
            {
                if (!saveIndex.IsNullOrWhiteSpace() && File.Exists(modData))
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    FileStream fileStream = File.Open(modData, FileMode.Open);
                    CrewReroll.PersistentData loadData = (CrewReroll.PersistentData)binaryFormatter.Deserialize(fileStream);
                    fileStream.Close();

                    if (loadData == null)
                        CrewReroll.data = new CrewReroll.PersistentData();
                    else
                    {
                        CrewReroll.data = loadData;
                        CrewReroll.data.LoadRuntimeLocks();
                    }

                    if(loadData == null)
                    {
                        log.LogInfo("null");
                    }
                }
                else
                    CrewReroll.data = new CrewReroll.PersistentData();
            }
            catch
            {
                SideInfo.AddMsg("<color=red>CrewRoll mod load failed.</color>");
            }
        }
    }
}
