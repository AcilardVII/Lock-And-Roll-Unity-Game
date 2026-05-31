using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Skill", menuName = "Skill Tree/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("Temel Bilgiler")]
    public string skillID;
    public string skillName;
    [TextArea] public string description;
    public Sprite icon;
    public int ticketCost;

    [Header("Gereksinimler (Multi-Parent)")]
    
    public List<SkillData> requiredParentSkills = new List<SkillData>();

    [Header("Ödüller: Eşya & Para")]
    public GameObject dicePrefabToGive;
    public int startingMoneyBonus = 0;

    [Header("Pasif: Faiz Ayarları")]
    public int interestCapBonus = 0;
    public int interestThreshold = 0;
    public int interestRewardAmount = 0;

    [Header("🎁 Başlangıç Hediyesi: Joker")]
    
    public bool givesStartingJoker = false;

    
    public CoinDragSnap.JokerRarity jokerRarityToGive;
    [Range(1, 5)] public int startingJokerCount = 1;
}