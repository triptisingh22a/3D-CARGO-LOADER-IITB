using UnityEngine;
using TMPro;

public class RealTimeCube : MonoBehaviour
{
    public TMP_InputField lengthInput;
    public TMP_InputField breadthInput;
    public TMP_InputField heightInput;
    public Transform spawnPoint;
    public GameObject cubePrefab;

    private GameObject currentCube;

    private void Start()
    {
        lengthInput.onValueChanged.AddListener(delegate { UpdateCube(); });
        breadthInput.onValueChanged.AddListener(delegate { UpdateCube(); });
        heightInput.onValueChanged.AddListener(delegate { UpdateCube(); });
    }

    private void CreateCube()
    {
        if (currentCube == null && cubePrefab != null)
        {
            currentCube = Instantiate(cubePrefab, spawnPoint.position, Quaternion.identity);
        }
    }

    public void UpdateCube()
    {
        CreateCube();

        if (float.TryParse(lengthInput.text, out float length) &&
            float.TryParse(breadthInput.text, out float breadth) &&
            float.TryParse(heightInput.text, out float height))
        {
            currentCube.transform.localScale = new Vector3(length, height, breadth);
        }
    }
}
