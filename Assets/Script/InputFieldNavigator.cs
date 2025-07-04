using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class InputFieldNavigator : MonoBehaviour
{
    [Header("Input Fields & Icons")]
    [SerializeField] private TMP_InputField[] inputFields; // Array of input fields
    [SerializeField] private GameObject[] icons; // Array of GameObjects for icons

    [Header("Prefabs & Parents for Dynamic Additions")]
    [SerializeField] private TMP_InputField inputFieldPrefab; // Prefab for input fields
    [SerializeField] private GameObject iconPrefab; // Prefab for icons
    [SerializeField] private Transform inputFieldParent; // Parent transform for UI fields
    [SerializeField] private Transform iconParent; // Parent transform for icons

    private List<TMP_InputField> inputFieldList = new List<TMP_InputField>(); // List for fields
    private List<GameObject> iconList = new List<GameObject>(); // List for icons
    private int currentIndex = 0; // Track the current input field index

    private void Start()
    {
        // Initialize existing input fields and icons
        inputFieldList.AddRange(inputFields);
        iconList.AddRange(icons);

        // Set focus on the first field if available
        if (inputFieldList.Count > 0)
        {
            SetFocusOnField(currentIndex);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return)) // Enter key to move forward
        {
            MoveToNextField();
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow)) // Up Arrow to move backward
        {
            MoveToPreviousField();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow)) // Down Arrow to move forward
        {
            MoveToNextField();
        }
    }

    private void MoveToNextField()
    {
        if (inputFieldList.Count == 0) return;

        currentIndex = (currentIndex + 1) % inputFieldList.Count;
        SetFocusOnField(currentIndex);
    }

    private void MoveToPreviousField()
    {
        if (inputFieldList.Count == 0) return;

        currentIndex = (currentIndex - 1 + inputFieldList.Count) % inputFieldList.Count;
        SetFocusOnField(currentIndex);
    }

    private void SetFocusOnField(int index)
    {
        if (inputFieldList[index] != null)
        {
            inputFieldList[index].ActivateInputField(); // Activate the selected field
        }
    }

    // Function to dynamically add a new input field and icon
    public void AddNewShipment()
    {
        // Create a new input field
        TMP_InputField newField = Instantiate(inputFieldPrefab, inputFieldParent);
        inputFieldList.Add(newField);

        // Create a new icon
        GameObject newIcon = Instantiate(iconPrefab, iconParent);
        iconList.Add(newIcon);

        // Update arrays (to maintain compatibility)
        inputFields = inputFieldList.ToArray();
        icons = iconList.ToArray();

        // Update index to the new field and focus on it
        currentIndex = inputFieldList.Count - 1;
        SetFocusOnField(currentIndex);

        Debug.Log("Added new shipment field and icon.");
    }
}
