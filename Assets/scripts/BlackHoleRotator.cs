using UnityEngine;

public class BlackHoleRotator : MonoBehaviour
{
    public float rotateSpeed = 360f; 
    public float pulseSpeed = 2f;    
    public float pulseAmount = 0.1f; 
    private Vector3 baseScale;

    void Start()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
       
        transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);

       
        float scale = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        transform.localScale = baseScale * scale;
    }
}