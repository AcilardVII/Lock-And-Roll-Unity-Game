using UnityEngine;

[CreateAssetMenu(fileName = "New Enchantment", menuName = "ScriptableObjects/DiceEnchantment")]
public class DiceEnchantment : ScriptableObject
{
    public enum EnchantType
    {
        
        Void,
        Hologram
    }

    [Header("Temel Bilgiler")]
    public string elementName;
    public EnchantType type;
    public float value;

    [Header("Görünüm")]
    public Color displayColor = Color.white;
    [TextArea] public string description;

    [Header("📝 Log & Popup Ayarları")]
    public string activationText = "ACTIVATED!"; 
    public Color activationTextColor = Color.magenta; 
}