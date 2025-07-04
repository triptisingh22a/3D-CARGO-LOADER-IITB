using UnityEngine;
using TMPro;

public class IncrementValue : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI valueText;

    private int currentValue = 0;

    private void Start()
    {
        if (valueText != null)
        {
            valueText.text = currentValue.ToString();
        }
    }

    public void Increment()
    {
        if (valueText != null)
        {
            currentValue++;
            valueText.text = currentValue.ToString();
        }
    }
}
