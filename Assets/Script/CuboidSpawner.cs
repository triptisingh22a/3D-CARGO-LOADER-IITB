using UnityEngine;
using TMPro;

public class CuboidSpawner : MonoBehaviour
{
    public GameObject cuboidPrefab;
    public Transform spawnPoint;

    public TMP_InputField lengthInput, breadthInput, heightInput;

    private GameObject spawnedCuboid;

    void Start()
    {

    }

    public void UpdateDimensions()
    {
        float length = GetInputValue(lengthInput);
        float breadth = GetInputValue(breadthInput);
        float height = GetInputValue(heightInput);

        if (spawnedCuboid == null)
        {
            spawnedCuboid = Instantiate(cuboidPrefab, spawnPoint.position, Quaternion.identity);
        }

        spawnedCuboid.transform.localScale = new Vector3(length, height, breadth);
    }

    float GetInputValue(TMP_InputField inputField)
    {
        return float.TryParse(inputField.text, out float value) && value > 0 ? value : 1f;
    }
}
