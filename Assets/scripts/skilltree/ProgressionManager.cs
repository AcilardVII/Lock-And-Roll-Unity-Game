using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class ProgressionManager : MonoBehaviour
{
    public static ProgressionManager Instance;

    public event Action OnTicketsChanged;

    [Header("Durum")]
    public int currentTickets = 0;
    public List<string> unlockedSkillIDs = new List<string>();
    public List<string> equippedSkillIDs = new List<string>();

    
    public List<int> claimedBossIndexes = new List<int>();

    public int maxEquipSlots = 3;

    [Header("TÜM YETENEK VERİTABANI")]
    public List<SkillData> allSkillDatabase;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool IsBossRewardClaimed(int sectionIndex)
    {
        return claimedBossIndexes.Contains(sectionIndex);
    }

    public void MarkBossRewardClaimed(int sectionIndex)
    {
        if (!claimedBossIndexes.Contains(sectionIndex))
        {
            claimedBossIndexes.Add(sectionIndex);
            SaveData();
        }
    }
 

    public List<GameObject> GetStartingDicePrefabs()
    {
        List<GameObject> startingDice = new List<GameObject>();

        foreach (string id in equippedSkillIDs)
        {
            SkillData skill = allSkillDatabase.Find(s => s.skillID == id);

            if (skill == null) continue;

            if (skill.dicePrefabToGive != null)
            {
                startingDice.Add(skill.dicePrefabToGive);
            }
        }
        return startingDice;
    }

    public void AddTickets(int amount)
    {
        currentTickets += amount;
        SaveData();
        OnTicketsChanged?.Invoke();
    }

    public bool TrySpendTickets(int amount)
    {
        if (currentTickets >= amount)
        {
            currentTickets -= amount;
            SaveData();
            OnTicketsChanged?.Invoke();
            return true;
        }
        return false;
    }

    public bool IsSkillUnlocked(string id) => unlockedSkillIDs.Contains(id);
    public bool IsSkillEquipped(string id) => equippedSkillIDs.Contains(id);

    public void UnlockSkill(string id)
    {
        if (!unlockedSkillIDs.Contains(id))
        {
            unlockedSkillIDs.Add(id);
            SaveData();
        }
    }

    public bool ToggleEquipSkill(string id)
    {
        if (equippedSkillIDs.Contains(id))
        {
            equippedSkillIDs.Remove(id);
            SaveData();
            return false;
        }
        else
        {
            if (equippedSkillIDs.Count < maxEquipSlots)
            {
                equippedSkillIDs.Add(id);
                SaveData();
                return true;
            }
        }
        return false;
    }

   
    public void ResetAllProgress()
    {
        currentTickets = 0;
        unlockedSkillIDs.Clear();
        equippedSkillIDs.Clear();
        claimedBossIndexes.Clear();

        PlayerPrefs.DeleteAll();
        SaveData();
        OnTicketsChanged?.Invoke();

        Debug.Log("TÜM İLERLEME SIFIRLANDI!");
    }

    public void SaveData()
    {
        PlayerPrefs.SetInt("Tickets", currentTickets);
        PlayerPrefs.SetString("UnlockedSkills", string.Join(",", unlockedSkillIDs));
        PlayerPrefs.SetString("EquippedSkills", string.Join(",", equippedSkillIDs));
        PlayerPrefs.SetString("ClaimedBosses", string.Join(",", claimedBossIndexes));
        PlayerPrefs.Save();
    }

    public void LoadData()
    {
        currentTickets = PlayerPrefs.GetInt("Tickets", 0);

        string unlockedString = PlayerPrefs.GetString("UnlockedSkills", "");
        if (!string.IsNullOrEmpty(unlockedString))
            unlockedSkillIDs = unlockedString.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        string equippedString = PlayerPrefs.GetString("EquippedSkills", "");
        if (!string.IsNullOrEmpty(equippedString))
            equippedSkillIDs = equippedString.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        string claimedString = PlayerPrefs.GetString("ClaimedBosses", "");
        if (!string.IsNullOrEmpty(claimedString))
        {
            string[] split = claimedString.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var s in split)
            {
                if (int.TryParse(s, out int index))
                    claimedBossIndexes.Add(index);
            }
        }
    }

    public int GetTotalStartingMoney()
    {
        int totalMoney = 0;
        foreach (string id in unlockedSkillIDs)
        {
            SkillData skill = allSkillDatabase.Find(s => s.skillID == id);
            if (skill != null)
            {
                totalMoney += skill.startingMoneyBonus;
            }
        }
        return totalMoney;
    }

    
    public int GetTotalInterestCap()
    {
        int totalCap = 0;
        foreach (string id in unlockedSkillIDs)
        {
            SkillData skill = allSkillDatabase.Find(s => s.skillID == id);
            if (skill != null) totalCap += skill.interestCapBonus;
        }
        return totalCap;
    }

    
    public void GetInterestRates(out int bestThreshold, out int bestReward)
    {
        
        bestThreshold = 10;
        bestReward = 1;

        int lowestThresholdFound = 9999;

        foreach (string id in unlockedSkillIDs)
        {
            SkillData skill = allSkillDatabase.Find(s => s.skillID == id);
            if (skill != null)
            {
                
                if (skill.interestThreshold > 0)
                {
                   
                    if (skill.interestThreshold < lowestThresholdFound)
                    {
                        lowestThresholdFound = skill.interestThreshold;
                        
                        bestReward = (skill.interestRewardAmount > 0) ? skill.interestRewardAmount : 1;
                    }
                }
            }
        }

        
        if (lowestThresholdFound != 9999)
        {
            bestThreshold = lowestThresholdFound;
        }
    }

    [Header("JOKER VERİTABANI")]
    
    public List<GameObject> allJokerPrefabs;

    
    public GameObject GetRandomJokerByRarity(CoinDragSnap.JokerRarity targetRarity)
    {
        List<GameObject> candidates = new List<GameObject>();

        foreach (var prefab in allJokerPrefabs)
        {
            if (prefab == null) continue;

            
            var jokerScript = prefab.GetComponent<CoinDragSnap>();
            if (jokerScript != null && jokerScript.rarity == targetRarity)
            {
                candidates.Add(prefab);
            }
        }

        if (candidates.Count == 0) return null;

        
        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    
    [Header("Spawn Ayarları")]
    public Transform inventorySocketParent; 

    
    public bool GetUnlockedStartingJokerProfile(out CoinDragSnap.JokerRarity rarity, out int count)
    {
        rarity = CoinDragSnap.JokerRarity.Common;
        count = 0;
        bool foundAny = false;

        foreach (string id in equippedSkillIDs)
        {
            SkillData skill = allSkillDatabase.Find(s => s.skillID == id);

            if (skill != null && skill.givesStartingJoker)
            {
                foundAny = true;

                
                if (skill.jokerRarityToGive > rarity)
                {
                    rarity = skill.jokerRarityToGive;
                    count = skill.startingJokerCount;
                }
               
                else if (skill.jokerRarityToGive == rarity)
                {
                    if (skill.startingJokerCount > count)
                        count = skill.startingJokerCount;
                }
            }
        }

        return foundAny;
    }
}