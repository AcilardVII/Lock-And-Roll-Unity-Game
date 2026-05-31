using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class UIBlockerFinder : MonoBehaviour
{
    void Update()
    {
        
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                Debug.Log("⛔ Tıklama UI tarafından engellendi!");

                
                PointerEventData pointerData = new PointerEventData(EventSystem.current)
                {
                    position = Input.mousePosition
                };

                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);

                foreach (RaycastResult result in results)
                {
                    Debug.Log("👉 Engelleyen Obje: " + result.gameObject.name, result.gameObject);
                }
            }
            else
            {
                Debug.Log("✅ UI Engeli Yok. Zara ulaşılabilir.");
            }
        }
    }
}