using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine.Networking;

public class JsonApiSender : MonoBehaviour
{
    [SerializeField] private string apiUrl = "http://10.119.11.41:8001/start";
    [SerializeField] private bool useSavedFile = true;
    
    
    // Reference to your CargoSubmit script if you want to get data directly
    [SerializeField] private CargoSubmit cargoSubmitReference;
    
    public void SendJsonToApi()
{
    Debug.Log("SendJsonToApi method called");
    if (useSavedFile)
    {
        Debug.Log("Starting coroutine to send saved JSON file");
        StartCoroutine(SendSavedJsonFile());
    }
    else
    {
        Debug.Log("Direct JSON sending option selected, but not implemented");
    }
}
    private IEnumerator SendSavedJsonFile()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "cargo_payload.json");
        
        if (!File.Exists(filePath))
        {
            Debug.LogError("JSON file not found at: " + filePath);
            yield break;
        }
        
        // Read the JSON file content
        string jsonContent = File.ReadAllText(filePath);
        Debug.Log("Loaded JSON content: " + jsonContent);

      
        
        // Create the web request
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonContent);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        
        // Set headers
        request.SetRequestHeader("Content-Type", "application/json");

        // Send the request
        string jsonBody = JsonUtility.ToJson(jsonContent, true);
        Debug.Log("Sending JSON data to API: " + jsonContent);
        yield return request.SendWebRequest();
       
        
        // Handle the response
        if (request.result == UnityWebRequest.Result.ConnectionError || 
            request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("API Request Error: " + request.error);
            Debug.LogError("Response Code: " + request.responseCode);
            Debug.LogError("Response: " + request.downloadHandler.text);
        }
        else
        {
            Debug.Log("API Request Successful!");
            Debug.Log("Response Code: " + request.responseCode);
            Debug.Log("Response: " + request.downloadHandler.text);
            
            // You can process the response here if needed
            ProcessApiResponse(request.downloadHandler.text);
        }
    }
    
    private void ProcessApiResponse(string responseText)
    {
        // Parse and handle the API response here
        // For example, you might want to deserialize the JSON response
        try
        {
            // Example: JsonUtility.FromJson<YourResponseType>(responseText);
             Debug.Log(responseText);
            Debug.Log("Successfully processed API response");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error processing API response: " + e.Message);
        }
    }
    
    // You can add a button in the Unity Inspector that calls this method
    public void SendDataButton()
    {
        SendJsonToApi();
    }
}