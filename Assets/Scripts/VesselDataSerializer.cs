using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VesselDataSerializer : MonoBehaviour
{
    [SerializeField]
    private string vesselDataFolder = "VesselData";
    [SerializeField]
    private GameObject fileNameSetter;
    [SerializeField]
    private TMP_InputField fileNameInputField;

    private bool fileNameSet = false;

    public void SerializeAndSaveVesselData(List<VesselData> vessels)
    {
        StartCoroutine(SerializeAndSaveVesselDataCO(vessels));
    }

    private IEnumerator SerializeAndSaveVesselDataCO(List<VesselData> vessels)
    {
        JSONNode root = new JSONObject();
        JSONNode dataArray = new JSONArray();
        root["allShipData"] = dataArray;
        foreach (var data in vessels)
        {
            dataArray.Add(data.DataPackage.ToJsonNode());
        }
        string json = root.ToString();

        fileNameSet = false;
        fileNameSetter.SetActive(true);
        yield return new WaitUntil(() => fileNameSet);
        var fileName = fileNameInputField.text.Length > 0 ? fileNameInputField.text : Guid.NewGuid().ToString();
        if(!Directory.Exists(Path.Combine(Application.persistentDataPath, vesselDataFolder)))
        {
            Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, vesselDataFolder));
        }
        var path = Path.Combine(Application.persistentDataPath, vesselDataFolder, fileName) + ".json";
        File.WriteAllText(path, json);
        fileNameSet = false;
        fileNameSetter.SetActive(false);
        PopUpWithButton.Instance.PopupText("File saved at:\n" + path);
    }

    public void SetButtonClicked()
    {
        fileNameSet = true;
    }
}
