
using UnityEngine;

public class AddItem : MonoBehaviour
{
    public GameObject DimentionPanel;


    private void Start()
    {
        DimentionPanel.SetActive(false);
    }

    public void AddPrefab()
    {
        DimentionPanel.SetActive(true);
    }

    public void DeletePrefab()
    {

        DimentionPanel.SetActive(false);
    }
}
