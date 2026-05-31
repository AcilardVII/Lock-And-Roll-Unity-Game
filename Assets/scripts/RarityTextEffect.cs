using UnityEngine;
using TMPro;

public class RarityTextEffect : MonoBehaviour
{
    private TMP_Text textComponent;

    [Header("🎨 Rarity Renkleri")]
    public Color commonColor = Color.white;
    public Color uncommonColor = new Color(0.2f, 0.8f, 0.4f); 
    public Color rareColor = new Color(1f, 0.8f, 0.2f);       
    public Color legendaryColor = new Color(0.7f, 0.3f, 1f);  

    [Header("🌊 Animasyon Ayarları")]
    public float angleMultiplier = 2.0f; 
    public float speedMultiplier = 4.0f; 
    public float curveScale = 5.0f;      

    private bool isAnimating = false;
    private float currentSpeed = 0f;
    private float currentCurve = 0f;

    void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
    }

    public void SetRarity(CoinDragSnap.JokerRarity rarity)
    {
        if (textComponent == null) textComponent = GetComponent<TMP_Text>();

        
        textComponent.text = rarity.ToString().ToUpper();

        
        switch (rarity)
        {
            case CoinDragSnap.JokerRarity.Common:
                textComponent.color = commonColor;
                isAnimating = false; 
              
                textComponent.transform.rotation = Quaternion.identity;
                textComponent.ForceMeshUpdate();
                break;

            case CoinDragSnap.JokerRarity.Uncommon:
                textComponent.color = uncommonColor;
                isAnimating = true;
                currentSpeed = speedMultiplier * 0.8f; 
                currentCurve = curveScale * 0.6f;
                break;

            case CoinDragSnap.JokerRarity.Rare:
                textComponent.color = rareColor;
                isAnimating = true;
                currentSpeed = speedMultiplier * 1.2f; 
                currentCurve = curveScale * 1.0f;
                break;

            case CoinDragSnap.JokerRarity.Legendary:
                textComponent.color = legendaryColor;
                isAnimating = true;
                currentSpeed = speedMultiplier * 2.0f; 
                currentCurve = curveScale * 1.5f;
                break;
        }
    }

    void Update()
    {
        
        if (!isAnimating || textComponent == null) return;

        textComponent.ForceMeshUpdate();
        var textInfo = textComponent.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            var charInfo = textInfo.characterInfo[i];

            
            if (!charInfo.isVisible) continue;

            int vertexIndex = charInfo.vertexIndex;
            int materialIndex = charInfo.materialReferenceIndex;
            var vertices = textInfo.meshInfo[materialIndex].vertices;

            
            float offset = Mathf.Sin(Time.time * currentSpeed + i * angleMultiplier) * currentCurve;

            
            Vector3 offsetVector = new Vector3(0, offset, 0);

            vertices[vertexIndex + 0] += offsetVector;
            vertices[vertexIndex + 1] += offsetVector;
            vertices[vertexIndex + 2] += offsetVector;
            vertices[vertexIndex + 3] += offsetVector;
        }

        
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            var meshInfo = textInfo.meshInfo[i];
            meshInfo.mesh.vertices = meshInfo.vertices;
            textComponent.UpdateGeometry(meshInfo.mesh, i);
        }
    }
}