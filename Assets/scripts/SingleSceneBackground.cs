using UnityEngine;
using System.Collections;

public class SingleSceneBackground : MonoBehaviour
{
    [Header("Shader Ayarları")]
    public Material backgroundMaterial; 
    public string colorPropertyName = "_OverlayColor"; 

    [Header("Renk Paleti")]
    public Color defaultGameColor = new Color(0.1f, 0.1f, 0.1f, 1f); 
    public Color skillTreeColor = new Color(0.5f, 0.0f, 1.0f, 1f);   
    public Color shopColor = new Color(1.0f, 0.6f, 0.0f, 1f);       

    private Coroutine colorRoutine;
    private int propertyID;

    void Start()
    {
        
        propertyID = Shader.PropertyToID(colorPropertyName);
        ResetColor();
    }

    
    public void SetSkillTreeMode(bool isOpen, float duration)
    {
        Color targetColor = isOpen ? skillTreeColor : defaultGameColor;
        StartColorLerp(targetColor, duration);
    }

    
    public void SwitchToShop()
    {
        StartColorLerp(shopColor, 0.5f);
    }

    public void SwitchToGame()
    {
        StartColorLerp(defaultGameColor, 0.5f);
    }

    
    public void StartColorChange()
    {
        
        ResetColor();
    }

    
    private void StartColorLerp(Color target, float duration)
    {
        if (colorRoutine != null) StopCoroutine(colorRoutine);
        colorRoutine = StartCoroutine(LerpMaterialColor(target, duration));
    }

    private IEnumerator LerpMaterialColor(Color targetColor, float duration)
    {
        if (backgroundMaterial == null) yield break;

        Color startColor = backgroundMaterial.GetColor(propertyID);
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / duration);

           
            Color newColor = Color.Lerp(startColor, targetColor, Mathf.SmoothStep(0f, 1f, t));

            
            backgroundMaterial.SetColor(propertyID, newColor);

            yield return null;
        }

        
        backgroundMaterial.SetColor(propertyID, targetColor);
    }

    public void ResetColor()
    {
        if (backgroundMaterial != null)
            backgroundMaterial.SetColor(propertyID, defaultGameColor);
    }

    
    void OnApplicationQuit()
    {
        ResetColor();
    }
}