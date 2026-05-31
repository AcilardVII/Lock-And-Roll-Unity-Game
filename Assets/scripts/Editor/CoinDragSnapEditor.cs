using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CoinDragSnap))]
[CanEditMultipleObjects]
public class CoinDragSnapEditor : Editor
{
    SerializedProperty jokerTitle;
    SerializedProperty rarity;
    SerializedProperty shopPrice;
    SerializedProperty shopDescription;

    SerializedProperty triggerType;
    SerializedProperty targetFaceValue;
    SerializedProperty requiredCount;
    SerializedProperty targetSum;
    SerializedProperty chancePercent;

    SerializedProperty usePercentageForTotalSum;
    SerializedProperty targetSumPercentage;
    SerializedProperty requireAllDice;

    SerializedProperty bonusChips;
    SerializedProperty bonusMultAdd;
    SerializedProperty multiplier;
    SerializedProperty moneyReward;
    SerializedProperty rerollReward;
    SerializedProperty roundReward;

    SerializedProperty passiveRerolls;
    SerializedProperty passiveRounds;
    SerializedProperty lockRerollAbility;

    SerializedProperty rewardPerItem;
    SerializedProperty useDiceValueAsXMult;
    SerializedProperty createsClone;
    SerializedProperty consumeOnTrigger;

    
    SerializedProperty thinSprite;
    SerializedProperty normalSprite;
    SerializedProperty snapRadius;
    SerializedProperty snapDuration;
    SerializedProperty hoverOffsetY;
    SerializedProperty hoverDuration;
    SerializedProperty dragTiltAngle;
    SerializedProperty dragAnimSpeed;
    SerializedProperty lockScale;
    SerializedProperty lockDuration;
    SerializedProperty lockColor;
    SerializedProperty jokerPulseScale;
    SerializedProperty jokerPulseDuration;
    SerializedProperty jokerWobbleAngle;
    SerializedProperty jokerWobbleSpeed;

    
    SerializedProperty failureColor;
    SerializedProperty failureShakeAmount;
    SerializedProperty failureDuration;

    private bool showVisualSettings = false;

    void OnEnable()
    {
        jokerTitle = serializedObject.FindProperty("jokerTitle");
        rarity = serializedObject.FindProperty("rarity");
        shopPrice = serializedObject.FindProperty("shopPrice");
        shopDescription = serializedObject.FindProperty("shopDescription");

        triggerType = serializedObject.FindProperty("triggerType");
        targetFaceValue = serializedObject.FindProperty("targetFaceValue");
        requiredCount = serializedObject.FindProperty("requiredCount");
        targetSum = serializedObject.FindProperty("targetSum");
        chancePercent = serializedObject.FindProperty("chancePercent");

        usePercentageForTotalSum = serializedObject.FindProperty("usePercentageForTotalSum");
        targetSumPercentage = serializedObject.FindProperty("targetSumPercentage");
        requireAllDice = serializedObject.FindProperty("requireAllDice");

        bonusChips = serializedObject.FindProperty("bonusChips");
        bonusMultAdd = serializedObject.FindProperty("bonusMultAdd");
        multiplier = serializedObject.FindProperty("multiplier");
        moneyReward = serializedObject.FindProperty("moneyReward");
        rerollReward = serializedObject.FindProperty("rerollReward");
        roundReward = serializedObject.FindProperty("roundReward");

        passiveRerolls = serializedObject.FindProperty("passiveRerolls");
        passiveRounds = serializedObject.FindProperty("passiveRounds");
        lockRerollAbility = serializedObject.FindProperty("lockRerollAbility");

        rewardPerItem = serializedObject.FindProperty("rewardPerItem");
        useDiceValueAsXMult = serializedObject.FindProperty("useDiceValueAsXMult");
        createsClone = serializedObject.FindProperty("createsClone");
        consumeOnTrigger = serializedObject.FindProperty("consumeOnTrigger");

        // Görseller
        thinSprite = serializedObject.FindProperty("thinSprite");
        normalSprite = serializedObject.FindProperty("normalSprite");
        snapRadius = serializedObject.FindProperty("snapRadius");
        snapDuration = serializedObject.FindProperty("snapDuration");
        hoverOffsetY = serializedObject.FindProperty("hoverOffsetY");
        hoverDuration = serializedObject.FindProperty("hoverDuration");
        dragTiltAngle = serializedObject.FindProperty("dragTiltAngle");
        dragAnimSpeed = serializedObject.FindProperty("dragAnimSpeed");
        lockScale = serializedObject.FindProperty("lockScale");
        lockDuration = serializedObject.FindProperty("lockDuration");
        lockColor = serializedObject.FindProperty("lockColor");
        jokerPulseScale = serializedObject.FindProperty("jokerPulseScale");
        jokerPulseDuration = serializedObject.FindProperty("jokerPulseDuration");
        jokerWobbleAngle = serializedObject.FindProperty("jokerWobbleAngle");
        jokerWobbleSpeed = serializedObject.FindProperty("jokerWobbleSpeed");

        failureColor = serializedObject.FindProperty("failureColor");
        failureShakeAmount = serializedObject.FindProperty("failureShakeAmount");
        failureDuration = serializedObject.FindProperty("failureDuration");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // ---------------------------------------------------------
        // BAŞLIK VE TANIM
        // ---------------------------------------------------------
        EditorGUILayout.LabelField("📝 Joker Kimliği", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(jokerTitle);
        EditorGUILayout.PropertyField(rarity, new GUIContent("💎 Nadirlik"));
        EditorGUILayout.PropertyField(shopPrice, new GUIContent("🏷️ Fiyat ($)"));
        EditorGUILayout.PropertyField(shopDescription);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("---------------------------------------------------------------", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space();

        // ---------------------------------------------------------
        // TETİKLEYİCİ SEÇİMİ
        // ---------------------------------------------------------
        EditorGUILayout.LabelField("1️⃣ Tetikleyici (Trigger)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(triggerType);

        CoinDragSnap.TriggerType type = (CoinDragSnap.TriggerType)triggerType.enumValueIndex;

        EditorGUILayout.Space();

        
        switch (type)
        {
            case CoinDragSnap.TriggerType.AlwaysActive:
            case CoinDragSnap.TriggerType.LastRound:
            case CoinDragSnap.TriggerType.FirstRound:
                EditorGUILayout.HelpBox("Bu mod herhangi bir zar koşulu gerektirmez.", MessageType.Info);
                break;

            case CoinDragSnap.TriggerType.N_Of_A_Kind:
                EditorGUILayout.PropertyField(requiredCount, new GUIContent("En Az Kaç Tane?"));
                break;

            case CoinDragSnap.TriggerType.SpecificFaceCount:
                EditorGUILayout.PropertyField(targetFaceValue, new GUIContent("Hangi Zar?"));
                EditorGUILayout.PropertyField(requiredCount, new GUIContent("Kaç Tane?"));
                break;

            case CoinDragSnap.TriggerType.Straight:
                EditorGUILayout.HelpBox("Sıralı dizi (Örn: 1-2-3-4-5) kontrol edilir.", MessageType.Info);
                break;

            case CoinDragSnap.TriggerType.AllDifferent:
                EditorGUILayout.HelpBox("Zarların hepsi birbirinden farklıysa tetiklenir (Örn: 1-2-4-5-6).", MessageType.Info);
                break;

            case CoinDragSnap.TriggerType.TotalSumGreater:
            case CoinDragSnap.TriggerType.TotalSumLess:
                EditorGUILayout.PropertyField(usePercentageForTotalSum, new GUIContent("Yüzdelik Kullan?"));
                if (usePercentageForTotalSum.boolValue)
                    EditorGUILayout.PropertyField(targetSumPercentage, new GUIContent("Hedef Yüzde (%)"));
                else
                    EditorGUILayout.PropertyField(targetSum, new GUIContent("Hedef Toplam"));
                break;

            case CoinDragSnap.TriggerType.ContainsFace:
                EditorGUILayout.PropertyField(targetFaceValue, new GUIContent("İçerilen Sayı"));
                break;

            case CoinDragSnap.TriggerType.Odd:
            case CoinDragSnap.TriggerType.Even:
                EditorGUILayout.PropertyField(requireAllDice, new GUIContent("Hepsi mi Uymalı?"));
                break;

            case CoinDragSnap.TriggerType.Chance:
                EditorGUILayout.PropertyField(chancePercent, new GUIContent("Şans (%)"));
                break;

            case CoinDragSnap.TriggerType.OnlyOneDie:
                EditorGUILayout.HelpBox("Masada sadece 1 zar varsa çalışır.", MessageType.Info);
                break;

            case CoinDragSnap.TriggerType.HighestDieIs:
            case CoinDragSnap.TriggerType.LowestDieIs:
            case CoinDragSnap.TriggerType.AllDiceAreX:
                EditorGUILayout.PropertyField(targetFaceValue, new GUIContent("Hangi Sayı?"));
                break;

            case CoinDragSnap.TriggerType.HighestDieDynamic:
            case CoinDragSnap.TriggerType.LowestDieDynamic:
                EditorGUILayout.HelpBox("Mevcut zarların en yükseği/düşüğü dinamik alınır.", MessageType.Info);
                break;
            case CoinDragSnap.TriggerType.PerRemainingReroll:
                EditorGUILayout.HelpBox("Kalan Reroll hakkı kadar ödülü çarpar.", MessageType.Info);
                break;

            case CoinDragSnap.TriggerType.PerDiceValue:
                EditorGUILayout.HelpBox("Zarların üzerindeki sayıların TOPLAMI kadar ödülü çarpar.", MessageType.Info);
                break;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("---------------------------------------------------------------", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space();

        // ---------------------------------------------------------
        // ÖDÜLLER
        // ---------------------------------------------------------
        EditorGUILayout.LabelField("2️⃣ Ödüller (Rewards)", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Chips (+)");
        bonusChips.intValue = EditorGUILayout.IntField(bonusChips.intValue);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Mult (+)");
        bonusMultAdd.floatValue = EditorGUILayout.FloatField(bonusMultAdd.floatValue);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Mult (x)");
        multiplier.floatValue = EditorGUILayout.FloatField(multiplier.floatValue);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Para ($)");
        moneyReward.intValue = EditorGUILayout.IntField(moneyReward.intValue);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Reroll (+)");
        rerollReward.intValue = EditorGUILayout.IntField(rerollReward.intValue);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Round (+)");
        roundReward.intValue = EditorGUILayout.IntField(roundReward.intValue);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("---------------------------------------------------------------", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space();

        // ---------------------------------------------------------
        // HESAPLAMA AYARLARI
        // ---------------------------------------------------------
        EditorGUILayout.LabelField("🧮 Hesaplama Modları", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(consumeOnTrigger, new GUIContent("Kullanınca Yok Ol?", "Tetiklendikten sonra joker silinir."));

        if (type == CoinDragSnap.TriggerType.ContainsFace ||
            type == CoinDragSnap.TriggerType.SpecificFaceCount ||
            type == CoinDragSnap.TriggerType.Odd ||
            type == CoinDragSnap.TriggerType.Even ||
            type == CoinDragSnap.TriggerType.AllDifferent ||
            type == CoinDragSnap.TriggerType.HighestDieDynamic ||
            type == CoinDragSnap.TriggerType.LowestDieDynamic ||
            type == CoinDragSnap.TriggerType.OnlyOneDie ||
            type == CoinDragSnap.TriggerType.AllDiceAreX ||
            type == CoinDragSnap.TriggerType.PerDiceValue)
        {
            EditorGUILayout.PropertyField(rewardPerItem, new GUIContent("Adet/Değer Başına Ödül?", "Tetikleyen sayı kadar ödülü çarpar."));
        }

        if (type == CoinDragSnap.TriggerType.HighestDieDynamic || type == CoinDragSnap.TriggerType.LowestDieDynamic || type == CoinDragSnap.TriggerType.OnlyOneDie)
        {
            EditorGUILayout.PropertyField(useDiceValueAsXMult, new GUIContent("Zar Değerini X-Mult Yap?", "Zarın üzerindeki sayıyı çarpan olarak kullanır."));
        }

        if (type == CoinDragSnap.TriggerType.OnlyOneDie)
        {
            EditorGUILayout.PropertyField(createsClone, new GUIContent("Kopya Oluşturur mu?"));
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("---------------------------------------------------------------", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space();

        // ---------------------------------------------------------
        // PASİF BONUSLAR
        // ---------------------------------------------------------
        EditorGUILayout.LabelField("🌟 Pasif Bonuslar (Tur Başı)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(passiveRerolls, new GUIContent("Reroll (+)"));
        EditorGUILayout.PropertyField(passiveRounds, new GUIContent("Eller (+)"));
        EditorGUILayout.PropertyField(lockRerollAbility, new GUIContent("Reroll'u Kilitle"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("---------------------------------------------------------------", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space();

        // ---------------------------------------------------------
        // GÖRSEL AYARLAR 
        // ---------------------------------------------------------
        showVisualSettings = EditorGUILayout.Foldout(showVisualSettings, "🎨 Görsel Efekt Ayarları");
        if (showVisualSettings)
        {
            EditorGUILayout.HelpBox("Popup (Yazı) ayarları artık DiceManager'dan yönetiliyor.", MessageType.None);
            EditorGUILayout.PropertyField(thinSprite);
            EditorGUILayout.PropertyField(normalSprite);
            EditorGUILayout.PropertyField(snapRadius);
            EditorGUILayout.PropertyField(snapDuration);
            EditorGUILayout.PropertyField(hoverOffsetY);
            EditorGUILayout.PropertyField(hoverDuration);
            EditorGUILayout.PropertyField(dragTiltAngle);
            EditorGUILayout.PropertyField(dragAnimSpeed);
            EditorGUILayout.PropertyField(lockScale);
            EditorGUILayout.PropertyField(lockDuration);
            EditorGUILayout.PropertyField(lockColor);
            EditorGUILayout.PropertyField(jokerPulseScale);
            EditorGUILayout.PropertyField(jokerPulseDuration);
            EditorGUILayout.PropertyField(jokerWobbleAngle);
            EditorGUILayout.PropertyField(jokerWobbleSpeed);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Hata / Başarısızlık", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(failureColor);
            EditorGUILayout.PropertyField(failureShakeAmount);
            EditorGUILayout.PropertyField(failureDuration);
        }

        serializedObject.ApplyModifiedProperties();
    }
}