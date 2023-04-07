
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using static MC_SVCrewRoll.UI;

namespace MC_SVCrewRoll
{
    internal class CrewReroll
    {
        internal static CrewMember crew = null;
        internal static PersistentData data;

        internal static void LoadCrewMember(int crewID)
        {
            crew = CrewDB.GetCrewMember(crewID);

            if(data == null)
                data = new PersistentData();

            if (!data.Contains(crewID))
                data.Add(crewID);
        }

        internal static void Validate()
        {
            if (crew == null)
                return;

            List<CargoItem> ci = Main.player.GetComponent<CargoSystem>().cargo;
            bool found = false;
            foreach (CargoItem item in ci)
            {
                if (item.itemType == Main.crewItemType && item.itemID == crew.id)
                {
                    found = true;
                    break;
                }
            }

            if(!found)
                crew = null;
        }

        internal static void UpdateLockState(bool val, SkillItemData sid)
        {
            List<object> lockedItems = data.Get(crew.id);
            object skillOrBonus;

            if (sid is BonusItemData)
            {
                BonusItemData bid = sid as BonusItemData;
                skillOrBonus = crew.skills[bid.skillIndex].skillBonus[bid.bonusIndex];
            }
            else
            {
                skillOrBonus = crew.skills[sid.skillIndex];
            }

            if (skillOrBonus == null)
                return;

            if (val)
            {
                if (!lockedItems.Contains(skillOrBonus))
                    lockedItems.Add(skillOrBonus);
            }
            else
            {
                if (lockedItems.Contains(skillOrBonus))
                    lockedItems.Remove(skillOrBonus);
            }
        }

        internal static int GetSkillRerollPrice()
        {
            int rerollSkillsCost = 0;
            for (int i = 0; i < crew.skills.Count; i++)
            {
                int cost = GeneratePrice(crew.skills[i].Rank(false), Main.cfgSkillBasePrice.Value);
                if (data.Get(crew.id).Contains(crew.skills[i]))
                    cost *= 2;
                rerollSkillsCost += cost;
            }
            return rerollSkillsCost;
        }

        internal static int GeneratePrice(int level, int basePrice)
        {
            return Mathf.RoundToInt(Mathf.Exp(0.3f * level) * basePrice);
        }

        internal static int GetAddSkillPrice()
        {
            int levelFactor = crew.aiChar.level / 10;
            return Mathf.RoundToInt(Main.cfgSkillBasePrice.Value * crew.skills.Count * (levelFactor < 1 ? 1:levelFactor));
        }

        internal static void RerollSkills(int cost)
        {
            if (crew.maxNumberOfSkills == 0 || !CanPay(cost))
                return;

            int[] removedSkillVals = new int[crew.skills.Count];
            List<CrewSkill> newSkills = new List<CrewSkill>(crew.skills);
            foreach (CrewSkill skill in crew.skills)
            {
                if(!data.Get(crew.id).Contains(skill))
                {
                    // Remove locked bonuses, if any
                    List<object> lockedBonuses = GetLockedBonusesOnSkill(skill);
                    if (lockedBonuses != null)
                        foreach (object bonus in lockedBonuses)
                            data.Get(crew.id).Remove(bonus);

                    // Remove skill
                    removedSkillVals[crew.skills.IndexOf(skill)] = skill.value;
                    newSkills.Remove(skill);
                }
            }

            // Update, if any changes made (at least 1 unlocked skill)
            if (newSkills.Count < crew.skills.Count && 
                CanPay(cost))
            {
                PayCost(cost);

                // Create dupes list and get current dupes
                List<int> dupeSkills = new List<int>();
                List<int> newSkillIDs = new List<int>();
                for (int i = 0; i < newSkills.Count; i++)
                {
                    newSkillIDs.Add((int)newSkills[i].ID);
                    for (int j = 0; j < newSkills.Count; j++)
                        if (i != j && newSkills[i].ID == newSkills[j].ID)
                            dupeSkills.Add((int)newSkills[i].ID);
                }

                // Get new skills
                for (int i = crew.skills.Count - 1; newSkills.Count < crew.skills.Count; i--)
                {
                    int newSkillID;
                    do
                    {
                        newSkillID = CrewDB.Rand.Next(0, 7);                        
                        
                        // Duplicate skill check
                        if (newSkillIDs.Contains(newSkillID) &&
                            !dupeSkills.Contains(newSkillID) &&
                            CrewDB.Rand.Next(0, 100001) <= (Main.cfgDualSkillChance.Value * 1000))
                        {
                            dupeSkills.Add(newSkillID);
                            break;
                        }
                    } while (newSkillIDs.Contains(newSkillID));

                    newSkillIDs.Add(newSkillID);
                    CrewSkill newSkill = new CrewSkill(newSkillID, 0, crew.aiChar.level, crew.rarity, crew, true, CrewDB.Rand);
                    if (Main.cfgRetainLevel.Value)
                        newSkill.value = removedSkillVals[i];
                    newSkills.Add(newSkill); 
                }

                crew.skills = newSkills;
                crew.SortSkills();
            }
        }

        internal static void RerollSkillsUnrestricted(int cost)
        {
            if (crew.maxNumberOfSkills == 0 || !CanPay(cost))
                return;

            bool modsMade = false;
            List<CrewSkill> newSkills = new List<CrewSkill>(crew.skills);
            List<int> rolledThisCycle = new List<int>();
            foreach (CrewSkill skill in crew.skills)
            {
                if (!data.Get(crew.id).Contains(skill))
                {
                    // Remove locked bonuses, if any
                    List<object> lockedBonuses = GetLockedBonusesOnSkill(skill);
                    if (lockedBonuses != null)
                        foreach (object bonus in lockedBonuses)
                            data.Get(crew.id).Remove(bonus);

                    // Now get a new skill
                    int value = skill.value;                    
                    newSkills.Remove(skill);
                    int nextSkill = CrewDB.Rand.Next(0, 7);
                    int rolls = 0;
                    while (rolledThisCycle.Contains(nextSkill) && rolls < 5)
                    {
                        nextSkill = CrewDB.Rand.Next(0, 7);
                        rolls++;
                    }
                    rolledThisCycle.Add(nextSkill);
                    CrewSkill newSkill = new CrewSkill(nextSkill, 0, crew.aiChar.level, crew.rarity, crew, true, CrewDB.Rand);
                    if (Main.cfgRetainLevel.Value)
                        newSkill.value = value;
                    newSkills.Add(newSkill);
                    modsMade = true;
                }
            }

            // Update, if any changes made (at least 1 unlocked skill)
            if (modsMade && CanPay(cost))
            {
                PayCost(cost);
                crew.skills = newSkills;
                crew.SortSkills();
            }
        }

        internal static bool LockedBonusOnUnlockedSkill()
        {
            foreach (CrewSkill skill in crew.skills)
                if (!data.Get(crew.id).Contains(skill) &&
                    GetLockedBonusesOnSkill(skill) != null)
                    return true;

            return false;
        }

        internal static List<object> GetLockedBonusesOnSkill(CrewSkill skill)
        {
            List<object> locked = new List<object>();

            foreach (SkillShipBonus ssb in skill.skillBonus)
            {
                if (data.Get(crew.id).Contains(ssb))
                        locked.Add(ssb);
            }

            if (locked.Count > 0)
                return locked;
            else
                return null;
        }

        internal static void AddSkill(int cost)
        {
            if (crew.skills.Count >= crew.maxNumberOfSkills || !CanPay(cost))
                return;

            PayCost(cost);
            AccessTools.Method(typeof(CrewMember), "AddSkill", new Type[] { typeof(bool), typeof(System.Random) }).Invoke(crew, new object[] { true, null });
            AccessTools.FieldRefAccess<CrewMember, int>("nextSkillCount")(crew) = 0;
            crew.SortSkills();
        }

        internal static void RerollBonuses(int cost, int skillIndex)
        {
            if (skillIndex > crew.skills.Count - 1 ||
                !CanPay(cost))
                return;

            bool modsMade = false;
            int bonusCount = (int)Main.crewSkillGetQuantityShipBonuses.Invoke(crew.skills[skillIndex], null);

            // Remove unlocked bonuses
            List<SkillShipBonus> newBonuses = new List<SkillShipBonus>(crew.skills[skillIndex].skillBonus);
            int indexShift = 0;
            foreach (SkillShipBonus ssb in crew.skills[skillIndex].skillBonus)
            {
                if (!data.Get(crew.id).Contains(ssb))
                {
                    newBonuses.Remove(ssb);
                    indexShift++;
                    modsMade = true;
                }
            }

            if (modsMade && CanPay(cost))
            {
                PayCost(cost);

                // Replace existing bonuses with list where unlocked bonuses have been removed
                crew.skills[skillIndex].skillBonus = newBonuses;

                // Add new bonuses
                while ((int)Main.crewSkillGetQuantityShipBonuses.Invoke(crew.skills[skillIndex], null) < bonusCount)
                {                    
                    crew.skills[skillIndex].nextSkillShipBonus = -1;
                    crew.skills[skillIndex].AddSkillShipBonus(crew, false, null, CrewDB.Rand);
                }

                // Sort
                crew.skills[skillIndex].SortBonuses();
            }
        }

        internal static void AddBonus(int cost, int skillIndex)
        {
            if (skillIndex > crew.skills.Count - 1 ||
                (int)Main.crewSkillGetQuantityShipBonuses.Invoke(crew.skills[skillIndex], null) >=
                (int)Main.crewSkillMaxQuantityShipBonuses.Invoke(crew.skills[skillIndex], new object[] { crew }) || 
                !CanPay(cost))
                return;

            PayCost(cost);
            crew.skills[skillIndex].AddSkillShipBonus(crew, false, null, CrewDB.Rand);
            AccessTools.FieldRefAccess<CrewSkill, int>("nextShipBonusCount")(crew.skills[skillIndex]) = 0;
            crew.SortSkills();
        }

        internal static bool CanPay(int cost)
        {
            return Main.player.GetComponent<CargoSystem>().credits >= cost;
        }

        internal static void PayCost(int cost)
        {
            Main.player.GetComponent<CargoSystem>().PayCreditCost(cost);
        }

        [Serializable]
        internal class PersistentData
        {
            
            private List<int> crewIDs;
            private List<List<int>> lockedSkills;
            private List<List<List<int>>> lockedBonuses;

            // Due to natural learning and sorts reshuffling skill/bonus lists
            [NonSerialized]
            private List<List<object>> runtimeLocks;

            internal PersistentData()
            {
                crewIDs = new List<int>();
                runtimeLocks = new List<List<object>>();
            }

            internal int Count()
            {
                return crewIDs.Count;
            }

            internal void Add(int id)
            {
                if (crewIDs.Contains(id))
                    return;

                crewIDs.Add(id);
                runtimeLocks.Add(new List<object>());
            }

            internal void Remove(int id)
            {
                runtimeLocks.RemoveAt(crewIDs.IndexOf(id));
                crewIDs.Remove(id);
            }

            internal bool Contains(int id)
            {
                if (crewIDs.Contains(id))
                    return true;
                else
                    return false;
            }

            internal List<object> Get(int id)
            {
                if (crewIDs.Contains(id))
                    return runtimeLocks[crewIDs.IndexOf(id)];

                return null;
            }

            internal void Update(int id, List<object> locks)
            {
                if (!crewIDs.Contains(id))
                    return;

                locks[crewIDs.IndexOf(id)] = locks;
            }

            internal void PrepareSaveData()
            {
                lockedSkills = new List<List<int>>();
                lockedBonuses = new List<List<List<int>>>();

                foreach (int id in crewIDs)
                {
                    lockedSkills.Add(new List<int>());
                    lockedBonuses.Add(new List<List<int>>());
                    int dataIndex = crewIDs.IndexOf(id);

                    CrewMember crewMember = CrewDB.GetCrewMember(id);
                    if (crewMember != null)
                    {
                        foreach (CrewSkill skill in crewMember.skills)
                        {
                            int skillIndex = crewMember.skills.IndexOf(skill);
                            
                            if (runtimeLocks[dataIndex].Contains(skill))
                                lockedSkills[dataIndex].Add(skillIndex);

                            List<int> skillLockedBonuses = new List<int>();
                            foreach (SkillShipBonus ssb in skill.skillBonus)
                                if (runtimeLocks[dataIndex].Contains(ssb))
                                    skillLockedBonuses.Add(skill.skillBonus.IndexOf(ssb));
                            lockedBonuses[dataIndex].Add(skillLockedBonuses);
                        }
                    }
                }
            }

            internal void LoadRuntimeLocks()
            {
                runtimeLocks = new List<List<object>>();
                foreach (int id in crewIDs)
                {
                    runtimeLocks.Add(new List<object>());
                    int dataIndex = crewIDs.IndexOf(id);
                    CrewMember crewMember = CrewDB.GetCrewMember(id);

                    if (crewMember != null)
                    {
                        foreach (int skillIndex in lockedSkills[dataIndex])
                            if (skillIndex >= 0 && skillIndex < crewMember.skills.Count)
                                runtimeLocks[dataIndex].Add(crewMember.skills[skillIndex]);

                        foreach (List<int> skillWithLockedBonuses in lockedBonuses[dataIndex])
                        {
                            CrewSkill skill = crewMember.skills[lockedBonuses[dataIndex].IndexOf(skillWithLockedBonuses)];
                            foreach (int bonusIndex in skillWithLockedBonuses)
                                if (bonusIndex >= 0 && bonusIndex < skill.skillBonus.Count)
                                    runtimeLocks[dataIndex].Add(skill.skillBonus[bonusIndex]);
                        }
                    }
                }
            }
        }
    }
}
