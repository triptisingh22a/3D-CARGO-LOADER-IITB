using UnityEngine;
using TMPro;

public class Shipment : MonoBehaviour
{
    [Header("Shipment Box")]
    [Header("TMP Input Fields for Dimensions")]
    public TMP_InputField inputX;
    public TMP_InputField inputY;
    public TMP_InputField inputZ;

    [Header("Cube to Modify")]
    public GameObject cube;

    void Start()
    {
        inputX.onEndEdit.AddListener((value) => UpdateCubeDimension());
        inputY.onEndEdit.AddListener((value) => UpdateCubeDimension());
        inputZ.onEndEdit.AddListener((value) => UpdateCubeDimension());
    }

    void UpdateCubeDimension()
    {
        if (float.TryParse(inputX.text, out float x) &&
            float.TryParse(inputY.text, out float y) &&
            float.TryParse(inputZ.text, out float z))
        {
            cube.transform.localScale = new Vector3(x, y, z);
        }
    }

    void OnDestroy()
    {
        if (inputX != null) inputX.onEndEdit.RemoveListener((value) => UpdateCubeDimension());
        if (inputY != null) inputY.onEndEdit.RemoveListener((value) => UpdateCubeDimension());
        if (inputZ != null) inputZ.onEndEdit.RemoveListener((value) => UpdateCubeDimension());
    }
}
