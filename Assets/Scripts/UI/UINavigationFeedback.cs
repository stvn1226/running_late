using UnityEngine;
using UnityEngine.EventSystems;

public class UINavigationFeedback : MonoBehaviour
{
    [Header("Vibration Settings")]
    [SerializeField] private bool enableNavigationVibration = true;
    [SerializeField] private float navigationVibrationDuration = 0.05f;
    
    private GameObject lastSelectedObject;
    
    private void Update()
    {
        if (!enableNavigationVibration)
            return;
            
        if (EventSystem.current == null)
            return;
        
        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
        
        if (currentSelected != lastSelectedObject && currentSelected != null && lastSelectedObject != null)
        {
            GamepadVibrationManager.Instance?.TriggerLightVibration(navigationVibrationDuration);
        }
        
        lastSelectedObject = currentSelected;
    }
}