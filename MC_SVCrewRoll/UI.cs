
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MC_SVCrewRoll
{
    internal class UI
    {
        // Layout
        private const int crewListItemSpacing = 17;
        private const int skillBonusSpacing = 30;

        // UI objects
        internal static GameObject mainPanel;        
        internal static GameObject crewList;
        internal static GameObject crewListItem;
        internal static GameObject skillItem;
        internal static GameObject bonusItem;
        internal static GameObject addSkillItem;
        internal static Sprite crewBtnIcon;
        internal static GameObject confirmPanel;
        internal static GameObject possibleBonusesPopup;
        private static GameObject mainPanelI;
        private static Text crewMemberName;
        private static GameObject rerollSkillsBtn;
        private static GameObject topCreditsIcon;
        private static Text rerollSkillsPrice;
        private static GameObject skillBonusPanel;        
        private static GameObject crewBtn;
        private static GameObject confirmPanelI;
        private static GameObject possibleBonusesPopupI;

        // States and refs
        internal static Main mainRef;
        internal static bool rerollWasLastLobbyPanel = false;
        private static Dictionary<CrewPosition, string> crewPositionBonuses;

        internal static void Initialise(GameObject lobby)
        {
            // Lobby panel buttons
            Transform mainButtons = lobby.transform.Find("MainButtons");

            mainButtons.localScale = new Vector3(0.8f, 0.8f, 1);
            mainButtons.localPosition = new Vector3(
                mainButtons.localPosition.x - 100,
                mainButtons.localPosition.y + 50,
                mainButtons.localPosition.z);

            Transform srcBtn = mainButtons.GetChild(3);
            crewBtn = GameObject.Instantiate(srcBtn.gameObject);
            crewBtn.gameObject.name = "LobbyCrewButton";
            crewBtn.transform.Find("Image").GetComponentInChildren<Image>().sprite = crewBtnIcon;
            crewBtn.transform.SetParent(srcBtn.parent);
            crewBtn.transform.localPosition = new Vector3(
                srcBtn.transform.localPosition.x + 5,
                srcBtn.transform.localPosition.y,
                srcBtn.transform.localPosition.z);
            crewBtn.transform.localScale = srcBtn.localScale;
            crewBtn.layer = srcBtn.gameObject.layer;
            crewBtn.GetComponentInChildren<Text>().text = "Crew Repair";

            EventTrigger.Entry newTrig = new EventTrigger.Entry();
            newTrig.eventID = EventTriggerType.PointerDown;
            newTrig.callback.AddListener((data) => { ShowCrewRerollClick((PointerEventData)data); });
            EventTrigger component = crewBtn.GetComponentInChildren<EventTrigger>();
            component.triggers.RemoveAll(t => t.eventID == EventTriggerType.PointerDown);
            component.triggers.Add(newTrig);

            // Reroll panel
            mainPanelI = GameObject.Instantiate(mainPanel);
            mainPanelI.transform.SetParent(lobby.transform.parent);
            mainPanelI.layer = lobby.layer;
            mainPanelI.transform.position = lobby.transform.position;
            mainPanelI.transform.localScale = lobby.transform.localScale;
            mainPanelI.SetActive(false);
            crewMemberName = mainPanelI.transform.Find("mc_crewrollMainPanel").Find("mc_crewrollCrewMemberName").gameObject.GetComponent<Text>();
            rerollSkillsBtn = mainPanelI.transform.Find("mc_crewrollMainPanel").Find("mc_crewrollRollSkills").gameObject;
            topCreditsIcon = mainPanelI.transform.Find("mc_crewrollMainPanel").Find("mc_crewrollCreditsIcon").gameObject;
            rerollSkillsPrice = mainPanelI.transform.Find("mc_crewrollMainPanel").Find("mc_crewrollSkillPrice").gameObject.GetComponent<Text>();
            skillBonusPanel = mainPanelI.transform.Find("mc_crewrollMainPanel").Find("mc_crewrollSkillBonusList").GetChild(0).GetChild(0).gameObject;

            // Crew list
            crewList = mainPanelI.transform.Find("mc_crewrollCrewList").GetChild(0).GetChild(0).gameObject;

            // Confirm dialog
            confirmPanelI = GameObject.Instantiate(confirmPanel);
            confirmPanelI.transform.SetParent(mainPanelI.transform, false);
            confirmPanelI.layer = lobby.layer;
            Button.ButtonClickedEvent cancelButtonClickedEvent = new Button.ButtonClickedEvent();
            cancelButtonClickedEvent.AddListener(ConfirmPanelCancelClick);
            confirmPanelI.transform.Find("mc_crewrollPanel").Find("mc_crewrollCancel").GetComponent<Button>().onClick = cancelButtonClickedEvent;
            confirmPanelI.SetActive(false);

            // Possible bonuses popup
            possibleBonusesPopupI = GameObject.Instantiate(possibleBonusesPopup);
            possibleBonusesPopupI.transform.SetParent(mainPanelI.transform, false);
            possibleBonusesPopupI.SetActive(false);
        }

        internal static void CrewBtnSetActive(bool state)
        {
            if (crewBtn != null)
                crewBtn.SetActive(state);
        }

        internal static void MainPanelSetActive(bool state)
        {
            if (mainPanelI != null)
                mainPanelI.SetActive(state);
            if (!state && confirmPanelI != null)
                confirmPanelI.SetActive(state);

            if (state)
            {
                CrewReroll.Validate();
                PopulateCrewList();
                RefreshMainPanel();
            }
        }

        private static void PopulateCrewList()
        {
            for (int i = 0; i < crewList.transform.childCount; i++)
                GameObject.Destroy(crewList.transform.GetChild(i).gameObject);

            int cnt = 0;
            List<CargoItem> ci = GameManager.instance.Player.GetComponent<CargoSystem>().cargo;
            foreach (CargoItem item in ci)
            {
                if (item.itemType == Main.crewItemType)
                {
                    CrewMember crew = CrewDB.GetCrewMember(item.itemID);
                    if (!crew.everEvolving)
                    {
                        GameObject li = GameObject.Instantiate(crewListItem);
                        li.transform.SetParent(crewList.transform, false);
                        li.transform.localPosition = new Vector3(
                            li.transform.localPosition.x,
                            li.transform.localPosition.y - (crewListItemSpacing * cnt),
                            li.transform.localPosition.z);
                        li.layer = crewList.layer;
                        li.GetComponent<Text>().text = crew.GetNameModified(14, false);
                        CrewListItemData liData = li.AddComponent<CrewListItemData>();
                        liData.crewID = item.itemID;
                        EventTrigger.Entry newTrig = new EventTrigger.Entry();
                        newTrig.eventID = EventTriggerType.PointerDown;
                        newTrig.callback.AddListener((data) => { CrewListItemClick((PointerEventData)data); });
                        li.GetComponent<EventTrigger>().triggers.Add(newTrig);
                        li.SetActive(true);
                        cnt++;
                    }
                }
            }

            crewList.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (cnt + 1) * crewListItemSpacing);
        }

        private static void RefreshMainPanel()
        {
            // Clear old skill/bonus entries
            for (int i = 0; i < skillBonusPanel.transform.childCount; i++)
                GameObject.Destroy(skillBonusPanel.transform.GetChild(i).gameObject);

            if (CrewReroll.crew == null)
            {
                crewMemberName.gameObject.SetActive(false);
                rerollSkillsBtn.SetActive(false);
                topCreditsIcon.SetActive(false);
                rerollSkillsPrice.gameObject.SetActive(false);
                return;
            }

            crewMemberName.gameObject.SetActive(true);
            rerollSkillsBtn.SetActive(true);
            topCreditsIcon.SetActive(true);
            rerollSkillsPrice.gameObject.SetActive(true);

            // Update name
            crewMemberName.text = CrewReroll.crew.GetNameModified(16, false);

            // Update skill reroll price and event handler
            int price = CrewReroll.GetSkillRerollPrice();
            rerollSkillsPrice.text = price.ToString();
            Button.ButtonClickedEvent skillRerollButtonClickedEvent = new Button.ButtonClickedEvent();
            UnityAction skillRerollAction = null;
            skillRerollAction += () => RerollSkillsClick(price);
            skillRerollButtonClickedEvent.AddListener(skillRerollAction);
            rerollSkillsBtn.GetComponent<Button>().onClick = skillRerollButtonClickedEvent;

            // Create new skill/bonus items
            int itemCount = 0;
            for (int skillI = 0; skillI < CrewReroll.crew.skills.Count; skillI++)
            {
                CrewSkill skill = CrewReroll.crew.skills[skillI];
                CreateSkillItem(skill, itemCount, skillI);
                itemCount++;

                for (int bonusI = 0; bonusI < skill.skillBonus.Count; bonusI++)
                {
                    SkillShipBonus bonus = skill.skillBonus[bonusI];

                    CreateBonusItem(skill, bonus, itemCount, skillI, bonusI);
                    itemCount++;
                }
            }

            if (CrewReroll.crew.skills.Count < CrewReroll.crew.maxNumberOfSkills)
            {
                for (int i = 1; i < ((CrewReroll.crew.maxNumberOfSkills + 1) - CrewReroll.crew.skills.Count); i++)
                {
                    GameObject addSkillItemGO = GameObject.Instantiate(addSkillItem);
                    addSkillItemGO.transform.SetParent(skillBonusPanel.transform, false);
                    addSkillItemGO.transform.localPosition = new Vector3(
                        addSkillItemGO.transform.localPosition.x,
                        addSkillItemGO.transform.localPosition.y - (skillBonusSpacing * itemCount),
                        addSkillItemGO.transform.localPosition.z);
                    addSkillItemGO.layer = skillBonusPanel.layer;
                    int skillPrice = CrewReroll.GetAddSkillPrice() * i;
                    addSkillItemGO.transform.Find("mc_crewrollPrice").GetComponent<Text>().text = skillPrice.ToString();
                    Button.ButtonClickedEvent skillButtonClickedEvent = new Button.ButtonClickedEvent();
                    UnityAction skillButtonAction = null;
                    skillButtonAction += () => AddSkillClick(skillPrice);
                    skillButtonClickedEvent.AddListener(skillButtonAction);
                    addSkillItemGO.transform.Find("mc_crewrollAdd").GetComponent<Button>().onClick = skillButtonClickedEvent;
                    itemCount++;
                }
            }

            skillBonusPanel.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (itemCount + 1) * skillBonusSpacing);
        }

        private static void CreateSkillItem(CrewSkill skill, int itemCount, int skillIndex)
        {
            GameObject skillItemGO = GameObject.Instantiate(skillItem);
            skillItemGO.transform.SetParent(skillBonusPanel.transform, false);
            skillItemGO.transform.localPosition = new Vector3(
                skillItemGO.transform.localPosition.x,
                skillItemGO.transform.localPosition.y - (skillBonusSpacing * itemCount),
                skillItemGO.transform.localPosition.z);
            skillItemGO.layer = skillBonusPanel.layer;

            // Name
            skillItemGO.transform.Find("mc_crewrollName").gameObject.GetComponent<Text>().text =
                Lang.Get(23,
                skill.Rank(true),
                ItemDB.GetRarityColor(skill.Rank(true)),
                (skill.Rank(false) == 6 ? "+" : "") + "</color>",
                ColorSys.silver + Lang.Get(23, (int)(10 + (int)skill.ID * (int)CrewPosition.Navigator)) + "</color>");

            // Data reference
            SkillItemData skillItemData = skillItemGO.AddComponent<SkillItemData>();
            skillItemData.skillIndex = skillIndex;

            // Lock
            Transform lockTrans = skillItemGO.transform.Find("mc_crewrollLock");
            lockTrans.GetComponent<Toggle>().isOn = CrewReroll.data.Get(CrewReroll.crew.id).Contains(skill);
            AddLockTrigger(skillItemGO.transform.Find("mc_crewrollLock").GetComponent<EventTrigger>());

            // Price
            int price = CrewReroll.GeneratePrice(skill.Rank(false), Main.cfgSkillBasePrice.Value);
            skillItemGO.transform.Find("mc_crewrollPrice").GetComponent<Text>().text = price.ToString();

            // Reroll
            Button.ButtonClickedEvent rerollButtonClickedEvent = new Button.ButtonClickedEvent();
            UnityAction rerollButtonAction = null;
            rerollButtonAction += () => RerollBonusesClick(price, skillIndex);
            rerollButtonClickedEvent.AddListener(rerollButtonAction);
            skillItemGO.transform.Find("mc_crewrollRoll").GetComponent<Button>().onClick = rerollButtonClickedEvent;

            // Add
            bool show = (int)Main.crewSkillGetQuantityShipBonuses.Invoke(skill, null) < (int)Main.crewSkillMaxQuantityShipBonuses.Invoke(skill, new object[] { CrewReroll.crew });
            skillItemGO.transform.Find("mc_crewrollAdd").gameObject.SetActive(show);
            if (show)
            {
                Button.ButtonClickedEvent addButtonClickedEvent = new Button.ButtonClickedEvent();
                UnityAction addButtonAction = null;
                addButtonAction += () => AddBonusClick(price, skillIndex);
                addButtonClickedEvent.AddListener(addButtonAction);
                skillItemGO.transform.Find("mc_crewrollAdd").GetComponent<Button>().onClick = addButtonClickedEvent;
            }

            // Possible Bonuses
            EventTrigger eventTrigger = skillItemGO.transform.Find("mc_crewrollPossBonus").GetComponent<EventTrigger>();
            EventTrigger.Entry onEnter = new EventTrigger.Entry();
            onEnter.eventID = EventTriggerType.PointerEnter;
            onEnter.callback.AddListener((data) => { PossibleBonusesEnter((PointerEventData)data); });
            eventTrigger.triggers.Add(onEnter);
            EventTrigger.Entry onExit = new EventTrigger.Entry();
            onExit.eventID = EventTriggerType.PointerExit;
            onExit.callback.AddListener((data) => { PossibleBonusesExit((PointerEventData)data); });
            eventTrigger.triggers.Add(onExit);

        }

        private static void CreateBonusItem(CrewSkill skill, SkillShipBonus bonus, int itemCount, int skillIndex, int bonusIndex)
        {
            GameObject bonusItemGO = GameObject.Instantiate(bonusItem);
            bonusItemGO.transform.SetParent(skillBonusPanel.transform, false);
            bonusItemGO.transform.localPosition = new Vector3(
                bonusItemGO.transform.localPosition.x,
                bonusItemGO.transform.localPosition.y - (skillBonusSpacing * itemCount),
                bonusItemGO.transform.localPosition.z);
            bonusItemGO.layer = skillBonusPanel.layer;

            // Name
            string text = "";
            if (bonus.GetShipBonus() is SB_FleetShipBonuses)
                text = "Fleet: ";
            bonusItemGO.transform.Find("mc_crewrollName").gameObject.GetComponent<Text>().text = text +
                bonus.GetString(
                    ColorSys.infoText3,
                    null,
                    false,
                    false).Replace("\n", "").Replace("\t", " ").Replace(Lang.Get(23, 137) + ":", "");

            // Data reference
            BonusItemData bonusItemData = bonusItemGO.AddComponent<BonusItemData>();
            bonusItemData.skillIndex = skillIndex;
            bonusItemData.bonusIndex = bonusIndex;

            // Lock
            Transform lockTrans = bonusItemGO.transform.Find("mc_crewrollLock");
            lockTrans.GetComponent<Toggle>().isOn = CrewReroll.data.Get(CrewReroll.crew.id).Contains(bonus);
            AddLockTrigger(bonusItemGO.transform.Find("mc_crewrollLock").GetComponent<EventTrigger>());

            // Price
            bonusItemGO.transform.Find("mc_crewrollPrice").GetComponent<Text>().text = CrewReroll.GeneratePrice(bonus.level, Main.cfgBonusBasePrice.Value).ToString();
        }

        private static void UpdatePrices()
        {
            CrewReroll.GetSkillRerollPrice();

            // Re-roll all
            rerollSkillsPrice.text = CrewReroll.GetSkillRerollPrice().ToString();

            // List items
            for (int childI = 0; childI < skillBonusPanel.transform.childCount; childI++)
            {
                Transform item = skillBonusPanel.transform.GetChild(childI);
                SkillItemData sid = item.GetComponent<SkillItemData>();
                
                if (sid is BonusItemData)
                    item.Find("mc_crewrollPrice").GetComponent<Text>().text =
                        CrewReroll.GeneratePrice(CrewReroll.crew.skills[sid.skillIndex].skillBonus[(sid as BonusItemData).bonusIndex].level, Main.cfgBonusBasePrice.Value).ToString();
                else
                    item.Find("mc_crewrollPrice").GetComponent<Text>().text =
                        CrewReroll.GeneratePrice(CrewReroll.crew.skills[sid.skillIndex].Rank(false), Main.cfgSkillBasePrice.Value).ToString();
            }
        }

        private static void AddLockTrigger(EventTrigger eventTrigger)
        {
            EventTrigger.Entry newTrig = new EventTrigger.Entry();
            newTrig.eventID = EventTriggerType.PointerDown;
            newTrig.callback.AddListener((data) => { LockClick((PointerEventData)data); });
            eventTrigger.triggers.Add(newTrig);
        }

        private static void ShowCrewRerollClick(PointerEventData data)
        {
            if (mainPanelI != null)
            {
                AccessTools.Method(typeof(DockingUI), "CloseLobbyPanels").Invoke(Main.dockingUIInstance, null);
                MainPanelSetActive(true);
                rerollWasLastLobbyPanel = true;
            }
        }

        private static void CrewListItemClick(PointerEventData data)
        {
            int crewID = data.pointerCurrentRaycast.gameObject.GetComponent<CrewListItemData>().crewID;

            if (crewID == -1)
                return;

            CrewReroll.LoadCrewMember(crewID);
            RefreshMainPanel();
        }

        private static void LockClick(PointerEventData data)
        {
            CrewReroll.UpdateLockState(
                !data.pointerCurrentRaycast.gameObject.transform.parent.parent.GetComponent<Toggle>().isOn,
                data.pointerCurrentRaycast.gameObject.transform.parent.parent.parent.GetComponent<SkillItemData>());

            UpdatePrices();
        }

        private static void RerollSkillsClick(int cost)
        {
            if (!CrewReroll.LockedBonusOnUnlockedSkill())
            {
                ConfirmPanelContinueClick(cost);
            }
            else
            {
                Button.ButtonClickedEvent continueButtonClickedEvent = new Button.ButtonClickedEvent();
                UnityAction continueButtonAction = null;
                continueButtonAction += () => ConfirmPanelContinueClick(cost);
                continueButtonClickedEvent.AddListener(continueButtonAction);
                confirmPanelI.transform.Find("mc_crewrollPanel").Find("mc_crewrollContinue").GetComponent<Button>().onClick = continueButtonClickedEvent;                
                confirmPanelI.SetActive(true);
            }
        }

        private static void AddSkillClick(int cost)
        {
            CrewReroll.AddSkill(cost);
            RefreshMainPanel();
        }

        private static void RerollBonusesClick(int cost, int skillIndex)
        {
            CrewReroll.RerollBonuses(cost, skillIndex);
            RefreshMainPanel();
        }

        private static void AddBonusClick(int cost, int skillIndex)
        {
            CrewReroll.AddBonus(cost, skillIndex);
            RefreshMainPanel();
        }

        private static void ConfirmPanelContinueClick(int cost)
        {
            if (confirmPanelI.activeSelf)
                confirmPanelI.SetActive(false);

            if (Main.cfgRestrictSkillGen.Value)
                CrewReroll.RerollSkills(cost);
            else
                CrewReroll.RerollSkillsUnrestricted(cost);

            RefreshMainPanel();
        }

        private static void ConfirmPanelCancelClick()
        {
            confirmPanelI.SetActive(false);
        }

        private static void PossibleBonusesEnter(PointerEventData data)
        {
            if (!possibleBonusesPopupI.activeSelf)
                mainRef.StartCoroutine(PossibleBonusesPopup(data.position, data.pointerCurrentRaycast.gameObject));
        }

        private static void PossibleBonusesExit(PointerEventData data)
        {
            mainRef.StopAllCoroutines();
            if (possibleBonusesPopupI.activeSelf)
                possibleBonusesPopupI.SetActive(false);
        }

        private static IEnumerator PossibleBonusesPopup(Vector2 position, GameObject possBonusIcon)
        {
            yield return new WaitForSeconds(Main.cfgPopupDelay.Value);

            SkillItemData skillItemData = possBonusIcon.transform.parent.GetComponent<SkillItemData>();

            if (skillItemData == null)
                yield break;

            CrewPosition crewPosition = CrewReroll.crew.skills[skillItemData.skillIndex].ID;
            possibleBonusesPopupI.transform.GetChild(1).GetComponent<Text>().text = 
                GetPossibleSkillBonuses(crewPosition);
            float xOffset = position.x > Screen.width / 2 ? -possibleBonusesPopupI.GetComponent<RectTransform>().rect.width : possibleBonusesPopupI.GetComponent<RectTransform>().rect.width; ;
            float yOffset = position.y > Screen.height / 2 ? possibleBonusesPopupI.GetComponent<RectTransform>().rect.height : -possibleBonusesPopupI.GetComponent<RectTransform>().rect.height;            
            possibleBonusesPopupI.transform.position = new Vector3(
                position.x + (xOffset * 1.1f),
                position.y + (yOffset * 1.1f), 
                0);
            possibleBonusesPopupI.SetActive(true);
        }

        private static string GetPossibleSkillBonuses(CrewPosition position)
        {
            if (crewPositionBonuses == null)
                crewPositionBonuses = new Dictionary<CrewPosition, string>();

            if (!crewPositionBonuses.ContainsKey(position))
            {
                string result = "";

                foreach (ShipBonus bonus in AccessTools.StaticFieldRefAccess<List<ShipBonus>>(typeof(ShipBonusDB), "list"))
                    if (bonus.minSkillRank < 99 && bonus.field == position)
                    {
                        string outString = "";
                        if (bonus is SB_FleetShipBonuses)
                        {
                            SB_FleetShipBonuses.hideFleetShipGainText = true;
                            outString = "Fleet: ";
                        }
                        outString += bonus.GetStr(1, 0, ColorSys.infoText3);
                        outString = System.Text.RegularExpressions.Regex.Replace(outString,
                            "\\+\\d+",
                            "+n");
                        outString = System.Text.RegularExpressions.Regex.Replace(outString,
                            "<b>\\d+</b>",
                            "<b>n</b>");
                        outString = System.Text.RegularExpressions.Regex.Replace(outString,
                            "<b>\\d+%</b>",
                            "<b>n%</b>");
                        if (bonus.minSkillRank == 1)
                            outString += "  (Average)";
                        else
                            outString += "  (" + Lang.Get(23, bonus.minSkillRank, ItemDB.GetRarityColor(bonus.minSkillRank), "</color>", "").Trim() + ")";
                        result += outString + "\n";
                    }
                        

                crewPositionBonuses.Add(position, result);
            }

            return crewPositionBonuses[position];
        }

        internal class CrewListItemData : MonoBehaviour
        {
            public int crewID = -1;
        }

        internal class SkillItemData : MonoBehaviour
        {
            internal int skillIndex = -1;
        }

        internal class BonusItemData : SkillItemData
        {
            internal int bonusIndex = -1;
        }
    }
}
