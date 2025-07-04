using UnityEngine;
using TMPro;

public class ProductItem : MonoBehaviour
{
    public TMP_Text productNameText, dimensionsText, weightText, volumeText;
    
    public void SetProductDetails(string name, float length, float breadth, float height, float weight, float volume, int quantityText)
    {
        productNameText.text = name;
        dimensionsText.text = $"L: {length} B: {breadth} H: {height}";
        weightText.text = $"Weight: {weight}";
        volumeText.text = $"Volume: {volume}";

    }
}
