using UnityEngine;
using Michsky.MUIP;

public class ToggleCamera : MonoBehaviour
{
    public ButtonManager modernUIButton;
    public MonoBehaviour cameraMovementScript;
    
    void Start()
    {
        if (modernUIButton != null && cameraMovementScript != null)
        {
            modernUIButton.onClick.AddListener(ToggleCameraMovement);
        }
    }
    
    public void ToggleCameraMovement()
{
    if (cameraMovementScript != null)
    {
        cameraMovementScript.enabled = !cameraMovementScript.enabled;
        Debug.Log("Camera movement toggled. New state: " + cameraMovementScript.enabled);
    }
    else
    {
        Debug.LogWarning("Camera script reference is missing.");
    }
}
}