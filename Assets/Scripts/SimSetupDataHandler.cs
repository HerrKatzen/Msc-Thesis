using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using System;
using SimpleJSON;
using System.IO;

public class SimSetupDataHandler : MonoBehaviour, IDataHandler
{
    [SerializeField]
    private GameObject mainCanvas;
    [SerializeField]
    private SimulationEngine simEngine;
    [SerializeField]
    private GameObject editMain;
    [SerializeField]
    private VesselDataSerializer vesselDataSerializer;
    [SerializeField]
    private Transform vesselsParent;
    [SerializeField]
    private Transform waypointsParent;
    [SerializeField]
    private GameObject vesselDataPrefab;
    [SerializeField]
    private GameObject waypointPrefab;
    [SerializeField]
    private VesselDataUI ui;

    public UnityEvent OnEditClickedEvent;

    private List<VesselData> vessels = new List<VesselData>();
    private VesselData activeVesselData;
    private SetupValuesData setupValues = new SetupValuesData();

    void Start()
    {
        ResetUI();
    }

    public void AddNewShip()
    {
        var instance = Instantiate(vesselDataPrefab, vesselsParent);
        var VesselData = instance.GetComponent<VesselData>();
        vessels.Add(VesselData);
        VesselData.SetDataHandler(this);
        VesselData.SetEditMode();
        ResetUI();
    }

    public void AddNewWaypoint()
    {
        var instance = Instantiate(waypointPrefab, waypointsParent);
    }

    public void SaveDataChanges()
    {
        if (activeVesselData == null) return;

        for(int i = ui.ownVesselNameSelector.options.Count - 1; i >= 0; i--)
        {
            if (ui.ownVesselNameSelector.options[i].text.Equals(activeVesselData.name))
            {
                ui.ownVesselNameSelector.options.RemoveAt(i);
            }
        }
        ui.ownVesselNameSelector.RefreshShownValue();

        var dp = activeVesselData.DataPackage;
        dp.vesselType = ui.vesselType.options[ui.vesselType.value].text;
        dp.vesselName = ui.vesselName.text.Length > 0 ? ui.vesselName.text : Guid.NewGuid().ToString();
        dp.length = ui.length.text.Length > 0 ? float.Parse(ui.length.text) : float.Parse(ui.length.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text);
        dp.beam = ui.beam.text.Length > 0 ? float.Parse(ui.beam.text) : float.Parse(ui.beam.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text);
        dp.draft = ui.draft.text.Length > 0 ? float.Parse(ui.draft.text) : float.Parse(ui.draft.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text);
        dp.rudMax = ui.rudAngMax.text.Length > 0 ? float.Parse(ui.rudAngMax.text) : float.Parse(ui.rudAngMax.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text);
        dp.rudRateMax = ui.rudAngRateMax.text.Length > 0 ? float.Parse(ui.rudAngRateMax.text) : float.Parse(ui.rudAngRateMax.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text);
        dp.tau_X = ui.surgeForce.text.Length > 0 ? float.Parse(ui.surgeForce.text) : float.Parse(ui.surgeForce.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text);

        dp.eta.north = ui.etaN.text.Length > 0 ? float.Parse(ui.etaN.text) : float.Parse(ui.etaN.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text);
        dp.eta.east = ui.etaE.text.Length > 0 ? float.Parse(ui.etaE.text) : float.Parse(ui.etaE.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text);
        dp.eta.down = ui.etaD.text.Length > 0 ? float.Parse(ui.etaD.text) : float.Parse(ui.etaD.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text);
        dp.eta.yaw = ui.heading.text.Length > 0 ? float.Parse(ui.heading.text) * Mathf.Deg2Rad : float.Parse(ui.heading.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text) * Mathf.Deg2Rad; //heading in radian
        dp.linearSpeed = new Vector3(
            ui.speedN.text.Length > 0 ? float.Parse(ui.speedN.text) : float.Parse(ui.speedN.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text),
            ui.speedE.text.Length > 0 ? float.Parse(ui.speedE.text) : float.Parse(ui.speedE.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text),
            ui.speedD.text.Length > 0 ? float.Parse(ui.speedD.text) : float.Parse(ui.speedD.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text));
        dp.torqueSpeed = new Vector3(
            ui.speedR.text.Length > 0 ? float.Parse(ui.speedR.text) : float.Parse(ui.speedR.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text),
            ui.speedP.text.Length > 0 ? float.Parse(ui.speedP.text) : float.Parse(ui.speedP.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text),
            ui.speedY.text.Length > 0 ? float.Parse(ui.speedY.text) : float.Parse(ui.speedY.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text));
        dp.controlSystem = (BaseVessel.ControlSystem)Enum.Parse(typeof(BaseVessel.ControlSystem), ui.controller.options[ui.controller.value].text, true);

        var NEwaypoints = activeVesselData.DataPackage.NEWayPoints;
        if (NEwaypoints == null)
        {
            NEwaypoints = new List<Vector2>();
            activeVesselData.DataPackage.NEWayPoints = NEwaypoints;
        }
        NEwaypoints.Clear();
        for (int i = 0; i < waypointsParent.childCount; i++)
        {
            var waypoint = waypointsParent.GetChild(i).GetComponent<WaypointUI>();
            float nedN = float.Parse(waypoint.nedN.text.Length > 0 ? waypoint.nedN.text : waypoint.nedN.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text);
            float nedE = float.Parse(waypoint.nedE.text.Length > 0 ? waypoint.nedE.text : waypoint.nedE.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text);
            NEwaypoints.Add(new Vector2(nedN, nedE));
        }

        setupValues = new SetupValuesData();
        if (ui.simTime.text.Length > 0) setupValues.simTime = float.Parse(ui.simTime.text);
        if (ui.stepTime.text.Length > 0) setupValues.stepTime = float.Parse(ui.stepTime.text);
        if (ui.enviromentRho.text.Length > 0) setupValues.enviromentRho = float.Parse(ui.enviromentRho.text);
        if (ui.enviromentDepth.text.Length > 0) setupValues.enviromentDepth = float.Parse(ui.enviromentDepth.text);
        if (ui.radarScanDistance.text.Length > 0) setupValues.radarScanDistance = float.Parse(ui.radarScanDistance.text);
        if (ui.radarScanTime.text.Length > 0) setupValues.radarScanTime = float.Parse(ui.radarScanTime.text);
        if (ui.radarScanNoisePercent.text.Length > 0) setupValues.radarScanNoisePercent = float.Parse(ui.radarScanNoisePercent.text);
        if (ui.pathTimeLength.text.Length > 0) setupValues.pathTimeLength = float.Parse(ui.pathTimeLength.text);
        if (ui.pathDataTimeLength.text.Length > 0) setupValues.pathDataTimeLength = float.Parse(ui.pathDataTimeLength.text);
        if (ui.pathTurnRateAcceleration.text.Length > 0) setupValues.pathTurnRateAcceleration = float.Parse(ui.pathTurnRateAcceleration.text);
        if (ui.pathUpdateTime.text.Length > 0) setupValues.pathUpdateTime = float.Parse(ui.pathUpdateTime.text);
        if (ui.exclusionZoneFront.text.Length > 0) setupValues.exclusionZoneFront = float.Parse(ui.exclusionZoneFront.text);
        if (ui.exclusionZoneSides.text.Length > 0) setupValues.exclusionZoneSides = float.Parse(ui.exclusionZoneSides.text);
        if (ui.exclusionZoneBack.text.Length > 0) setupValues.exclusionZoneBack = float.Parse(ui.exclusionZoneBack.text);

        ui.ownVesselNameSelector.options.Add(new TMP_Dropdown.OptionData() { text = dp.vesselName });
        if(ui.ownVesselNameSelector.options.Count == 1)
        {
            ui.ownVesselNameSelector.value = 0;
        }
        ui.ownVesselNameSelector.RefreshShownValue();

        activeVesselData.SetVesselDataUI();
        for (int i = waypointsParent.childCount - 1; i >= 0; i--)
        {
            waypointsParent.GetChild(i).GetComponent<WaypointUI>().Delete();
        }
        ResetUI();
    }

    public void SerializeDataAndSave()
    {
        SaveDataChanges();
        activeVesselData = null;
        editMain.SetActive(false);
        string ownVesselName = ui.ownVesselNameSelector.options[ui.ownVesselNameSelector.value].text;

        vesselDataSerializer.SerializeAndSaveVesselData(vessels, setupValues, ownVesselName);
    }

    public void LoadFileData(List<VesselData.VesselMetaDataPackage> dataPackages, SetupValuesData _setupValuesData, string _ownVesselName)
    {
        ResetUI();
        editMain.SetActive(false);
        ui.ownVesselNameSelector.options.Clear();
        activeVesselData = null;
        vessels = new List<VesselData>();

        for(int i = vesselsParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(vesselsParent.GetChild(i));
        }
        for (int i = waypointsParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(waypointsParent.GetChild(i));
        }

        foreach (var dp in dataPackages)
        {
            var instance = Instantiate(vesselDataPrefab, vesselsParent);
            var VesselData = instance.GetComponent<VesselData>();
            vessels.Add(VesselData);
            VesselData.SetDataHandler(this);
            VesselData.DataPackage = dp;
            ui.ownVesselNameSelector.options.Add(new TMP_Dropdown.OptionData() { text = dp.vesselName });
            if (dp.vesselName.Equals(_ownVesselName)) ui.ownVesselNameSelector.value = ui.ownVesselNameSelector.options.Count - 1;

            ui.stepTime.text = _setupValuesData.stepTime.ToString();
            ui.simTime.text = _setupValuesData.simTime.ToString();
            ui.enviromentRho.text = _setupValuesData.enviromentRho.ToString();
            ui.enviromentDepth.text = _setupValuesData.enviromentDepth.ToString();
            ui.radarScanDistance.text = _setupValuesData.radarScanDistance.ToString();
            ui.radarScanNoisePercent.text = _setupValuesData.radarScanNoisePercent.ToString();
            ui.radarScanTime.text = _setupValuesData.radarScanTime.ToString();
            ui.pathUpdateTime.text = _setupValuesData.pathUpdateTime.ToString();
            ui.pathTimeLength.text = _setupValuesData.pathTimeLength.ToString();
            ui.pathDataTimeLength.text = _setupValuesData.pathDataTimeLength.ToString();
            ui.pathTurnRateAcceleration.text = _setupValuesData.pathTurnRateAcceleration.ToString();
            ui.exclusionZoneFront.text = _setupValuesData.exclusionZoneFront.ToString();
            ui.exclusionZoneSides.text = _setupValuesData.exclusionZoneSides.ToString();
            ui.exclusionZoneBack.text = _setupValuesData.exclusionZoneBack.ToString();
        }
        ui.ownVesselNameSelector.RefreshShownValue();
    }

    private void ResetUI()
    {
        ui.vesselName.text = "";
        ui.beam.text = "";
        ui.length.text = "";
        ui.heading.text = "";
        ui.draft.text = "";
        ui.etaD.text = "";
        ui.etaE.text = "";
        ui.etaN.text = "";
        ui.rudAngMax.text = "";
        ui.rudAngRateMax.text = "";
        ui.speedD.text = "";
        ui.speedE.text = "";
        ui.speedN.text = "";
        ui.speedP.text = "";
        ui.speedR.text = "";
        ui.speedY.text = "";
        ui.surgeForce.text = "";
    }

    public void OnVesselRemoved(VesselData vesselData)
    {
        vessels.Remove(vesselData);
        for (int i = 0; i < ui.ownVesselNameSelector.options.Count; i++)
        {
            if(ui.ownVesselNameSelector.options[i].text.Equals(vesselData.DataPackage.vesselName))
            {
                ui.ownVesselNameSelector.options.RemoveAt(i);
                if(ui.ownVesselNameSelector.options.Count > 0)
                {
                    if(ui.ownVesselNameSelector.value == i)
                    {
                        ui.ownVesselNameSelector.value = 0;
                    }
                    else if(ui.ownVesselNameSelector.value > i)
                    {
                        ui.ownVesselNameSelector.value--;
                    }
                }
                break;
            }
        }
        ui.ownVesselNameSelector.RefreshShownValue();
        editMain.SetActive(false);
    }

    public void OnEditClicked(VesselData vesselData)
    {
        if(activeVesselData != null) activeVesselData.EditDone();
        activeVesselData = vesselData;
        var waypoints = activeVesselData.DataPackage.NEWayPoints;
        if(waypoints != null)
        {
            foreach (var wp in waypoints)
            {
                var instance = Instantiate(waypointPrefab, waypointsParent);
                var wpUI = instance.GetComponent<WaypointUI>();
                wpUI.nedN.text = wp.x.ToString();
                wpUI.nedE.text = wp.y.ToString();
            }
        }

        ui.vesselName.text = vesselData.DataPackage.vesselName;
        ui.length.text = vesselData.DataPackage.length.ToString();
        ui.beam.text = vesselData.DataPackage.beam.ToString();
        ui.draft.text = vesselData.DataPackage.draft.ToString();
        ui.rudAngRateMax.text = vesselData.DataPackage.rudRateMax.ToString();
        ui.rudAngMax.text = vesselData.DataPackage.rudMax.ToString();
        ui.surgeForce.text = vesselData.DataPackage.tau_X.ToString();
        ui.etaN.text = vesselData.DataPackage.eta.north.ToString();
        ui.etaE.text = vesselData.DataPackage.eta.east.ToString();
        ui.etaD.text = vesselData.DataPackage.eta.down.ToString();
        ui.heading.text = (vesselData.DataPackage.eta.yaw * Mathf.Rad2Deg).ToString();
        ui.speedN.text = vesselData.DataPackage.linearSpeed.x.ToString();
        ui.speedE.text = vesselData.DataPackage.linearSpeed.y.ToString();
        ui.speedD.text = vesselData.DataPackage.linearSpeed.z.ToString();
        ui.speedR.text = vesselData.DataPackage.torqueSpeed.x.ToString();
        ui.speedP.text = vesselData.DataPackage.torqueSpeed.y.ToString();
        ui.speedY.text = vesselData.DataPackage.torqueSpeed.z.ToString();

        for(int i = 0; i < ui.controller.options.Count; i++)
        {
            if(ui.controller.options[i].text.Equals(vesselData.DataPackage.controlSystem.ToString()))
            {
                ui.controller.value = i;
            }
        }

        for (int i = 0; i < ui.vesselType.options.Count; i++)
        {
            if (ui.vesselType.options[i].text.Equals(vesselData.DataPackage.vesselType))
            {
                ui.vesselType.value = i;
            }
        }
        OnEditClickedEvent.Invoke();
    }

    public void OnDoneClicked()
    {
        SaveDataChanges();
        activeVesselData = null;
        editMain.SetActive(false);
    }

    public void StartSimulation()
    {
        if (vessels.Count == 0) return;

        float stepTime = float.Parse(ui.stepTime.text.Length > 0 ? ui.stepTime.text : ui.stepTime.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text);
        float simTime = float.Parse(ui.simTime.text.Length > 0 ? ui.simTime.text : ui.simTime.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text);

        mainCanvas.SetActive(false);
        
        simEngine.StartSimulationFromSetup(vessels, setupValues, ui.ownVesselNameSelector.options[ui.ownVesselNameSelector.value].text);
    }

    [System.Serializable]
    public class VesselDataUI
    {
        public TMP_InputField vesselName;
        public TMP_InputField length;
        public TMP_InputField beam;
        public TMP_InputField draft;
        public TMP_InputField rudAngMax;
        public TMP_InputField rudAngRateMax;
        public TMP_InputField surgeForce;
        public TMP_InputField etaN;
        public TMP_InputField etaE;
        public TMP_InputField etaD;
        public TMP_InputField heading;
        public TMP_InputField speedN;
        public TMP_InputField speedE;
        public TMP_InputField speedD;
        public TMP_InputField speedR;
        public TMP_InputField speedP;
        public TMP_InputField speedY;
        public TMP_Dropdown vesselType;
        public TMP_Dropdown ownVesselNameSelector;
        public TMP_Dropdown controller;
        public TMP_InputField simTime;
        public TMP_InputField stepTime;
        public TMP_InputField enviromentRho;
        public TMP_InputField enviromentDepth;
        public TMP_InputField radarScanDistance;
        public TMP_InputField radarScanTime;
        public TMP_InputField radarScanNoisePercent;
        public TMP_InputField pathUpdateTime;
        public TMP_InputField pathTimeLength;
        public TMP_InputField pathDataTimeLength;
        public TMP_InputField pathTurnRateAcceleration;
        public TMP_InputField exclusionZoneFront;
        public TMP_InputField exclusionZoneBack;
        public TMP_InputField exclusionZoneSides;
    }
}

public class SetupValuesData
{
    public float stepTime = 0.02f;
    public float simTime = 120f;
    public float enviromentRho = 1025f;
    public float enviromentDepth = 20f;
    public float radarScanDistance = 10000f;
    public float radarScanTime = 1f;
    public float radarScanNoisePercent = 0.01f;
    public float pathUpdateTime = 3f;
    public float pathTimeLength = 120f;
    public float pathDataTimeLength = 30f;
    public float pathTurnRateAcceleration = 0f;
    public float exclusionZoneFront = 5f;
    public float exclusionZoneBack = 2f;
    public float exclusionZoneSides = 2f;
}

public interface IDataHandler
{
    public void OnVesselRemoved(VesselData vesselData);
    public void OnEditClicked(VesselData vesselData);
    public void OnDoneClicked();
}
