using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public TMP_InputField nameInput;
    public TMP_InputField lengthInput;
    public TMP_InputField breadthInput;
    public TMP_InputField heightInput;
    public TMP_InputField weightInput;
    public TMP_InputField volumeInput;


    public void ClearInputFields()
    {
        nameInput.text = "";
        lengthInput.text = "";
        breadthInput.text = "";
        heightInput.text = "";
        weightInput.text = "";
        volumeInput.text="";
    }
}
