using UnityEngine;

public class Menu : MonoBehaviour
{
    public GameObject panel;
    private bool isActive = false;
    public void TogglePanel()
    {
        isActive = !isActive;
        panel.SetActive(isActive);
    }
}
