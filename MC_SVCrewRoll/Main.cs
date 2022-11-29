
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace MC_SVCrewRoll
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Main : BaseUnityPlugin
    {
        // BepInEx
        public const string pluginGuid = "mc.starvalor.crewroll";
        public const string pluginName = "SV Crew Roll";
        public const string pluginVersion = "1.0.0";

        // Star Valor
        internal const int crewItemType = 5;
        private const int lobbyPanelCode = 1;

        // Mod
        public static ConfigEntry<int> cfgSkillBasePrice;
        public static ConfigEntry<int> cfgBonusBasePrice;
        public static ConfigEntry<bool> cfgRetainLevel;
        internal static MethodInfo crewSkillGetQuantityShipBonuses = AccessTools.Method(typeof(CrewSkill), "GetQuantityShipBonuses");
        internal static MethodInfo crewSkillMaxQuantityShipBonuses = AccessTools.Method(typeof(CrewSkill), "MaxQuantityShipBonuses");
        internal static DockingUI dockingUIInstance = null;        

        // Debug
        internal static ManualLogSource log = BepInEx.Logging.Logger.CreateLogSource(pluginName);

        public void Awake()
        {
            LoadAssets();
            Configure();
            Harmony.CreateAndPatchAll(typeof(Main));
        }

        private void LoadAssets()
        {
            string pluginfolder = System.IO.Path.GetDirectoryName(GetType().Assembly.Location);
            string bundleName = "mc_svcrewroll";
            AssetBundle assets = AssetBundle.LoadFromFile($"{pluginfolder}\\{bundleName}");
            GameObject pack = assets.LoadAsset<GameObject>("Assets/mc_crewroll.prefab");
            UI.mainPanel = pack.transform.Find("Canvas").Find("CrewRR").gameObject;
            UI.crewBtnIcon = pack.transform.Find("BtnIcon").gameObject.GetComponent<SpriteRenderer>().sprite;
            UI.crewListItem = pack.transform.Find("CrewListItem").gameObject;
            UI.skillItem = pack.transform.Find("SkillItem").gameObject;
            UI.bonusItem = pack.transform.Find("BonusItem").gameObject;
            UI.addSkillItem = pack.transform.Find("AddSkillItem").gameObject;
            UI.confirmPanel = pack.transform.Find("ConfirmDlg").gameObject;
        }

        private void Configure()
        {
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
            cfgRetainLevel = Config.Bind<bool>(
                "Behaviour",
                "Retain levels on reroll?",
                true,
                "If enabled, when skills or bonuses are rerolled, they are replaced with skills or bonuses of equal level.");
        }
        
        [HarmonyPatch(typeof(DockingUI), nameof(DockingUI.OpenPanel))]
        [HarmonyPostfix]
        private static void DockingUIOpenPanel_Post(DockingUI __instance, GameObject ___lobbyPanel, int code)
        {
            dockingUIInstance = __instance;

            if (code != lobbyPanelCode)
            {
                UI.MainPanelSetActive(false);
                UI.CrewBtnSetActive(false);
                return;
            }

            UI.Initialise(___lobbyPanel);
            UI.CrewBtnSetActive(true);
            if (UI.rerollWasLastLobbyPanel)
                UI.MainPanelSetActive(true);
        }

        [HarmonyPatch(typeof(DockingUI), nameof(DockingUI.StartDockingStation))]
        [HarmonyPostfix]
        private static void DockingUIStartDocking_Post(DockingUI __instance, GameObject ___lobbyPanel)
        {
            dockingUIInstance = __instance;

            UI.Initialise(___lobbyPanel);
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

            UI.Initialise(___lobbyPanel);
            AccessTools.Method(typeof(DockingUI), "CloseLobbyPanels").Invoke(__instance, null);
            UI.MainPanelSetActive(true);
        }

        [HarmonyPatch(typeof(DockingUI), "CloseLobbyPanels")]
        [HarmonyPostfix]
        private static void DockingUICloseLobbyPanels_Post()
        {
            UI.MainPanelSetActive(false);
        }
    }
}
