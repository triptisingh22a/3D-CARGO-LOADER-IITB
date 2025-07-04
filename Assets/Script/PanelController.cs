using UnityEngine;

public class PanelController : MonoBehaviour
{
    public GameObject panel;
    public GameObject Menu;
    public GameObject disableObject;
    public GameObject enableObject;

    public void ShowPanel()
    {
        Menu.SetActive(false);
        disableObject.SetActive(false);
        enableObject.SetActive(true);
        panel.SetActive(true);
    }
}
