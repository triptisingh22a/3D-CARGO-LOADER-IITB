using TMPro;
using UnityEngine;

public class ProductUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI lengthText;
    public TextMeshProUGUI breadthText;
    public TextMeshProUGUI heightText;
    public TextMeshProUGUI weightText;
    public TextMeshProUGUI volumeText;
     public TextMeshProUGUI quantityText;

    public void SetProductDetails(string name, float length, float breadth, float height, float weight, float volume, float quantity)
    {
        nameText.text = name;
        lengthText.text = length.ToString();
        breadthText.text = breadth.ToString();
        heightText.text = height.ToString();
        weightText.text = weight.ToString();
        volumeText.text = volume.ToString();
        quantityText.text = quantity.ToString();
    }
}
