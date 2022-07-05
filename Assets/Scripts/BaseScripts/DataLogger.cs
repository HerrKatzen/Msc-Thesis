using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.UI;

public class DataLogger : Singleton<DataLogger>, IFileLoader
{
    public string simDataFolder;
    [SerializeField]
    private SimSetupDataHandler simSetupDataHandler;
    public GameObject simDataFilePrefab;
    public GameObject noFilesOverlay;
    public Transform listElementParent;
    public GameObject fileNameSetter;
    [SerializeField]
    private TMP_InputField fileNameInputField;
    [SerializeField]
    private Button startReplayButton;
    private FileLoadData lastLoadedFile;
    public Dictionary<string, List<BaseVessel.DataBundle>> SimData { get; private set; } = new Dictionary<string, List<BaseVessel.DataBundle>>();
    public Dictionary<string, List<Vector3>> CheckPoints { get; private set; } = new Dictionary<string, List<Vector3>>();
    public float StepTime { get; private set; }
    public SetupValuesData setupValuesData { get; private set; }
    public string ownVesselName { get; private set; }
    public List<VesselData.VesselMetaDataPackage> vesselData { get; private set; }
    [HideInInspector]
    public float minNorth = float.MaxValue;
    [HideInInspector]
    public float maxNorth = float.MinValue;
    [HideInInspector]
    public float minEast = float.MaxValue;
    [HideInInspector]
    public float maxEast = float.MinValue;
    private IFileLoader.ResetCallerDelegate resetCallerUI;

    private bool fileNameSet;

    public void LogVesselData(string vesselName, BaseVessel.DataBundle dataBundle)
    {
        minNorth = Mathf.Min(dataBundle.eta.north, minNorth);
        maxNorth = Mathf.Max(dataBundle.eta.north, maxNorth);
        minEast = Mathf.Min(dataBundle.eta.east, minEast);
        maxEast = Mathf.Max(dataBundle.eta.east, maxEast);
        List<BaseVessel.DataBundle> bundles;
        if(SimData.TryGetValue(vesselName, out bundles))
        {
            bundles.Add(dataBundle);
        }
        else
        {
            bundles = new List<BaseVessel.DataBundle>();
            bundles.Add(dataBundle);
            SimData.Add(vesselName, bundles);
        }
    }

    public void ClearVesselData(string vessel)
    {
        SimData.Remove(vessel);
    }

    public void AddVesselInitData(string vesselName, List<Vector2> _points)
    {
        if (CheckPoints.ContainsKey(vesselName))
        {
            CheckPoints.Remove(vesselName);
        }
        var p3 = new List<Vector3>();
        foreach (var p in _points)
        {
            p3.Add(new Vector3(p.y, 0f, p.x));
        }
        CheckPoints.Add(vesselName, p3);
    }

    [ContextMenu("Debug Log Data")]
    public void DebugLogData()
    {
        foreach (var vessel in SimData)
        {
            Debug.Log("Vessel " + vessel.Key);
            foreach (var data in vessel.Value)
            {
                Debug.Log(data.ToString());
            }
        }
    }

    [ContextMenu("Debug Log Json String")]
    public void DebugLogJsonString()
    {
        Debug.Log(GetDataAsJson());
    }

    public string GetDataAsJson()
    {
        JSONNode root = simSetupDataHandler.GetSetupDataAsJson();
        var simD = new JSONObject();
        root["SimData"] = simD;
        foreach (var data in SimData)
        {
            simD[data.Key] = new JSONArray();
            foreach (var bundleData in data.Value)
            {
                simD[data.Key].Add(bundleData.ToJsonNode());
            }
        }

        var checkPointNode = new JSONObject();
        root["CheckPoints"] = checkPointNode;
        foreach (var pointData in CheckPoints)
        {
            var values = new JSONArray();
            checkPointNode[pointData.Key] = values;
            foreach (var p in pointData.Value)
            {
                JSONNode vector3Node = new JSONObject();
                vector3Node["x"] = p.x;
                vector3Node["y"] = p.y;
                vector3Node["z"] = p.z;
                values.Add(vector3Node);
            }
        }

        return root.ToString();
    }

    [ContextMenu("Debug Read Log Data")]
    public void DebugReadLogData()
    {
        ReadLogDataFromJson(GetDataAsJson());
    }

    public void ReadLogDataFromJson(string json)
    {
        minNorth = float.MaxValue;
        maxNorth = float.MinValue;
        minEast = float.MaxValue;
        maxEast = float.MinValue;
        SimData = new Dictionary<string, List<BaseVessel.DataBundle>>();
        CheckPoints = new Dictionary<string, List<Vector3>>();

        var root = JSON.Parse(json);
        foreach (var pair in root["SimData"])
        {
            var itemList = new List<BaseVessel.DataBundle>();
            foreach (var o in pair.Value)
            {
                var dataBundle = new BaseVessel.DataBundle(o);
                minNorth = Mathf.Min(dataBundle.eta.north, minNorth);
                maxNorth = Mathf.Max(dataBundle.eta.north, maxNorth);
                minEast = Mathf.Min(dataBundle.eta.east, minEast);
                maxEast = Mathf.Max(dataBundle.eta.east, maxEast);
                itemList.Add(dataBundle);
            }
            SimData.Add(pair.Key, itemList);
        }

        foreach (var pair in root["CheckPoints"])
        {
            var itemList = new List<Vector3>();
            foreach (var o in pair.Value.Children)
            {
                itemList.Add(new Vector3(o["x"], o["y"], o["z"]));
            }
            CheckPoints.Add(pair.Key, itemList);
        }

        StepTime = root["stepTime"];
        setupValuesData = new SetupValuesData(root);
        ownVesselName = root["ownVessel"];

        JSONNode dataArray = root["allShipData"];
        vesselData = new List<VesselData.VesselMetaDataPackage>();
        foreach (var data in dataArray)
        {
            var vessel = new VesselData.VesselMetaDataPackage(data);
            vesselData.Add(vessel);
        }
    }

    internal void SetStepTime(float simulationTime)
    {
        StepTime = simulationTime;
    }

    public void SaveDataToFile()
    {
        StartCoroutine(SaveDataToFileCO());
    }
    private IEnumerator SaveDataToFileCO()
    {
        var json = GetDataAsJson();
        fileNameSet = false;
        fileNameSetter.SetActive(true);
        yield return new WaitUntil(() => fileNameSet);
        var fileName = fileNameInputField.text.Length > 0 ? fileNameInputField.text : Guid.NewGuid().ToString();
        if (!Directory.Exists(Path.Combine(Application.persistentDataPath, simDataFolder)))
        {
            Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, simDataFolder));
        }
        var path = Path.Combine(Application.persistentDataPath, simDataFolder, fileName) + ".json";
        File.WriteAllText(path, json);
        fileNameSet = false;
        fileNameSetter.SetActive(false);
        PopUpWithButton.Instance.PopupText("File saved at:\n" + path);
    }

    public async void ReadFileSystem()
    {
        DeleteFileList();
        var path = Path.Combine(Application.persistentDataPath, simDataFolder);
        if (!Directory.Exists(path)) return;

        var info = new DirectoryInfo(path);
        var fileInfo = info.GetFiles();

        foreach (var file in fileInfo)
        {
            if (file.Extension.Equals(".json") || file.Extension.Equals(".JSON"))
            {
                var instance = Instantiate(simDataFilePrefab, listElementParent);
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
    public void DeleteFileList()
    {
        for (int i = listElementParent.childCount - 1; i >= 0; i--)
        {
            Destroy(listElementParent.GetChild(i).gameObject);
        }
        startReplayButton.interactable = false;
    }

    public void FileNameSetDone()
    {
        fileNameSet = true;
    }

    public bool LoadFileFromFileName(string fileName, IFileLoader.ResetCallerDelegate resetCaller)
    {
        var path = Path.Combine(Application.persistentDataPath, simDataFolder);
        if (!Directory.Exists(path)) return false;
        if (!File.Exists(Path.Combine(path, fileName))) return false;

        if (resetCallerUI != null) resetCallerUI.Invoke();
        resetCallerUI = resetCaller;

        string jsonData = File.ReadAllText(Path.Combine(path, fileName));

        try
        {
            ReadLogDataFromJson(jsonData);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message + "\n" + e.StackTrace);
            startReplayButton.interactable = false;
            return false;
        }
        startReplayButton.interactable = true;
        return true;
    }

    public void DeleteFile(string fileName)
    {
        var path = Path.Combine(Application.persistentDataPath, simDataFolder);
        if (!Directory.Exists(path)) return;
        if (!File.Exists(Path.Combine(path, fileName))) return;

        File.Delete(Path.Combine(path, fileName));
        if (listElementParent.childCount == 0)
        {
            noFilesOverlay.SetActive(true);
        }
    }
}
