using UnityEngine;

public class ToggleObject : MonoBehaviour
{
    public GameObject targetObject;

    public void OnButtonClick()
    {
        if (targetObject != null)
        {
            targetObject.SetActive(false);
        }
    }
}
