using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class QuantityManager : MonoBehaviour
{
    public TMP_Text quantityText;  // Assign this to "Text (QuantityIncrement)"
    public Button incrementButton;
    public Button decrementButton;

    private int quantity = 0;  // Default quantity

    void Start()
    {
        // Ensure buttons are assigned
        if (incrementButton != null)
            incrementButton.onClick.AddListener(IncrementQuantity);

        if (decrementButton != null)
            decrementButton.onClick.AddListener(DecrementQuantity);

        UpdateQuantityUI(); // Initialize UI
    }

    void IncrementQuantity()
    {
        quantity++;
        UpdateQuantityUI();
    }

    void DecrementQuantity()
    {
        if (quantity > 0)
        {
            quantity--;
            UpdateQuantityUI();
        }
    }

    void UpdateQuantityUI()
    {
        if (quantityText != null)
        {
            quantityText.text = quantity.ToString(); // Update UI text
        }
    }
}
