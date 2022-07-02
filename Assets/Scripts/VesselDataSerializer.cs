using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class VesselDataSerializer : MonoBehaviour, IFileLoader
{
    [SerializeField]
    private string vesselDataFolder = "VesselData";
    [SerializeField]
    private GameObject fileNameSetter;
    [SerializeField]
    private TMP_InputField fileNameInputField;
    [SerializeField]
    private GameObject FileLoaderElementPrefab;
    [SerializeField]
    private Transform listElementsParent;
    [SerializeField]
    private TMP_Dropdown fileSelector;
    [SerializeField]
    private Button editSetupDataButton;
    [SerializeField]
    private GameObject noFilesOverlay;
    [SerializeField]
    private SimSetupDataHandler simSetupDataHandler;

    public UnityEvent OnMenuExit;

    private Dictionary<string, List<VesselData.VesselMetaDataPackage>> loadedFilesMap = new Dictionary<string, List<VesselData.VesselMetaDataPackage>>();
    private Dictionary<string, JSONNode> loadedFilesJsonNodes = new Dictionary<string, JSONNode>();
    private bool fileNameSet = false;

    public void SerializeAndSaveVesselData(List<VesselData> vessels, SetupValuesData setupValuesData, string ownVessel)
    {
        StartCoroutine(SerializeAndSaveVesselDataCO(vessels, setupValuesData, ownVessel));
    }

    private IEnumerator SerializeAndSaveVesselDataCO(List<VesselData> vessels, SetupValuesData setupValuesData, string ownVessel)
    {
        JSONNode root = new JSONObject();
        JSONNode dataArray = new JSONArray();
        root["allShipData"] = dataArray;
        foreach (var data in vessels)
        {
            dataArray.Add(data.DataPackage.ToJsonNode());
        }
        root["stepTime"] = setupValuesData.stepTime;
        root["simTime"] = setupValuesData.simTime;
        root["enviromentRho"] = setupValuesData.enviromentRho;
        root["enviromentDepth"] = setupValuesData.enviromentDepth;
        root["radarScanDistance"] = setupValuesData.radarScanDistance;
        root["radarScanTime"] = setupValuesData.radarScanTime;
        root["radarScanNoisePercent"] = setupValuesData.radarScanNoisePercent;
        root["pathTimeLength"] = setupValuesData.pathTimeLength;
        root["pathDataTimeLength"] = setupValuesData.pathDataTimeLength;
        root["pathUpdateTime"] = setupValuesData.pathUpdateTime;
        root["pathTurnRateAcceleration"] = setupValuesData.pathTurnRateAcceleration;
        root["exclusionZoneFront"] = setupValuesData.exclusionZoneFront;
        root["exclusionZoneSides"] = setupValuesData.exclusionZoneSides;
        root["exclusionZoneBack"] = setupValuesData.exclusionZoneBack;
        root["ownVessel"] = ownVessel;
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

    public async void ReadFileSystem()
    {
        var path = Path.Combine(Application.persistentDataPath, vesselDataFolder);
        if (!Directory.Exists(path)) return;

        var info = new DirectoryInfo(path);
        var fileInfo = info.GetFiles();

        foreach (var file in fileInfo)
        {
            if(file.Extension.Equals(".json") || file.Extension.Equals(".JSON"))
            {
                var instance = Instantiate(FileLoaderElementPrefab, listElementsParent);
                await Task.Yield();
                await Task.Yield();
                var fileLoadData = instance.GetComponent<FileLoadData>();
                if (fileLoadData == null) continue;
                fileLoadData.SetText(file.Name);
                fileLoadData.SetFileLoader(this);
                noFilesOverlay.SetActive(false);
            }
        }
    }

    public void EditFile()
    {
        if (fileSelector.options.Count == 0) return;
        string fileName = fileSelector.options[fileSelector.value].text;

        if (loadedFilesMap.TryGetValue(fileName, out List<VesselData.VesselMetaDataPackage> file))
        {
            loadedFilesJsonNodes.TryGetValue(fileName, out JSONNode node);
            SetupValuesData setupValuesData = new SetupValuesData();
            setupValuesData.stepTime = node["stepTime"];
            setupValuesData.simTime = node["simTime"];
            setupValuesData.enviromentRho = node["enviromentRho"];
            setupValuesData.enviromentDepth = node["enviromentDepth"];
            setupValuesData.radarScanDistance = node["radarScanDistance"];
            setupValuesData.radarScanTime = node["radarScanTime"];
            setupValuesData.radarScanNoisePercent = node["radarScanNoisePercent"];
            setupValuesData.pathTimeLength = node["pathTimeLength"];
            setupValuesData.pathDataTimeLength = node["pathDataTimeLength"];
            setupValuesData.pathUpdateTime = node["pathUpdateTime"];
            setupValuesData.pathTurnRateAcceleration = node["pathTurnRateAcceleration"];
            setupValuesData.exclusionZoneFront = node["exclusionZoneFront"];
            setupValuesData.exclusionZoneSides = node["exclusionZoneSides"];
            setupValuesData.exclusionZoneBack = node["exclusionZoneBack"];
            simSetupDataHandler.LoadFileData(file, setupValuesData, node["ownVessel"]);
            OnMenuExit.Invoke();
        }
    }

    public bool LoadFileFromFileName(string fileName)
    {
        var path = Path.Combine(Application.persistentDataPath, vesselDataFolder);
        if (!Directory.Exists(path)) return false;
        if (!File.Exists(Path.Combine(path, fileName))) return false;

        string jsonData = File.ReadAllText(Path.Combine(path, fileName));

        JSONNode root = JSON.Parse(jsonData);
        JSONNode dataArray = root["allShipData"];
        var vessels = new List<VesselData.VesselMetaDataPackage>();
        try
        {
            foreach (var data in dataArray)
            {
                var vessel = new VesselData.VesselMetaDataPackage(data);
                if (vessel.vesselName == null) return false;
                vessels.Add(vessel);
            }
        }
        catch
        {
            return false;
        }
        fileSelector.options.Add(new TMP_Dropdown.OptionData(fileName));
        fileSelector.RefreshShownValue();
        loadedFilesMap.Add(fileName, vessels);
        loadedFilesJsonNodes.Add(fileName, root);
        editSetupDataButton.interactable = true;
        return true;
    }

    public void DeleteFile(string fileName)
    {
        var path = Path.Combine(Application.persistentDataPath, vesselDataFolder);
        if (!Directory.Exists(path)) return;
        if (!File.Exists(Path.Combine(path, fileName))) return;

        File.Delete(Path.Combine(path, fileName));
        if(listElementsParent.childCount == 0)
        {
            noFilesOverlay.SetActive(true);
        }
        if (loadedFilesMap.Remove(fileName))
        {
            for (int i = fileSelector.options.Count - 1; i >= 0; i--)
            {
                if (fileSelector.options[i].text.Equals(fileName))
                {
                    fileSelector.options.RemoveAt(i);
                }
            }
            fileSelector.RefreshShownValue();
            if (fileSelector.options.Count == 0)
            {
                editSetupDataButton.interactable = false;
            }
        }
    }
}
public interface IFileLoader
{
    public bool LoadFileFromFileName(string fileName);
    public void DeleteFile(string fileName);
}
