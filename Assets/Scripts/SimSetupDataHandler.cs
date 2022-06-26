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
    MenuHandler setupMenu;
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
    [SerializeField]
    private List<BaseVessel> vesselTypes;

    public UnityEvent OnEditClickedEvent;

    private List<VesselData> vessels = new List<VesselData>();
    private VesselData activeVesselData;

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
    }

    public void AddNewWaypoint()
    {
        var instance = Instantiate(waypointPrefab, waypointsParent);
    }

    public void SaveDataChanges()
    {
        if (activeVesselData == null) return;

        var vessel = activeVesselData.DataPackage.vessel;
        if (vessel == null)
        {
            foreach (var bv in vesselTypes)
            {
                if(bv.name.Equals(ui.vesselType.text))
                {
                    vessel = (BaseVessel)activeVesselData.gameObject.AddComponent(bv.GetType());
                }
            }
            activeVesselData.DataPackage.vessel = vessel;
        }
        vessel.vesselName = ui.vesselName.text.Length > 0 ? ui.vesselName.text : Guid.NewGuid().ToString();
        vessel.length = ui.lenght.text.Length > 0 ? float.Parse(ui.lenght.text) : float.Parse(ui.lenght.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text);
        vessel.beam = ui.beam.text.Length > 0 ? float.Parse(ui.beam.text) : float.Parse(ui.beam.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text);
        vessel.draft = ui.draft.text.Length > 0 ? float.Parse(ui.draft.text) : float.Parse(ui.draft.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text);
        vessel.rudMax = ui.rudAngMax.text.Length > 0 ? float.Parse(ui.rudAngMax.text) : float.Parse(ui.rudAngMax.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text);
        vessel.rudRateMax = ui.rudAngRateMax.text.Length > 0 ? float.Parse(ui.rudAngRateMax.text) : float.Parse(ui.rudAngRateMax.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text);
        vessel.tau_X = ui.surgeForce.text.Length > 0 ? float.Parse(ui.surgeForce.text) : float.Parse(ui.surgeForce.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text);

        var startPoint = activeVesselData.DataPackage.startPoint;
        if (startPoint.eta == null) startPoint.eta = new BaseVessel.Eta();

        startPoint.eta.north = ui.etaN.text.Length > 0 ? float.Parse(ui.etaN.text) : float.Parse(ui.etaN.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text);
        startPoint.eta.east = ui.etaE.text.Length > 0 ? float.Parse(ui.etaE.text) : float.Parse(ui.etaE.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text);
        startPoint.eta.down = ui.etaD.text.Length > 0 ? float.Parse(ui.etaD.text) : float.Parse(ui.etaD.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text);
        startPoint.eta.yaw = ui.heading.text.Length > 0 ? float.Parse(ui.heading.text) * Mathf.Deg2Rad : float.Parse(ui.heading.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text) * Mathf.Deg2Rad; //heading in radian
        startPoint.linearSpeed = new Vector3(
            ui.speedN.text.Length > 0 ? float.Parse(ui.speedN.text) : float.Parse(ui.speedN.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text),
            ui.speedE.text.Length > 0 ? float.Parse(ui.speedE.text) : float.Parse(ui.speedE.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text),
            ui.speedD.text.Length > 0 ? float.Parse(ui.speedD.text) : float.Parse(ui.speedD.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text));
        startPoint.torqueSpeed = new Vector3(
            ui.speedR.text.Length > 0 ? float.Parse(ui.speedR.text) : float.Parse(ui.speedR.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text),
            ui.speedP.text.Length > 0 ? float.Parse(ui.speedP.text) : float.Parse(ui.speedP.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text),
            ui.speedY.text.Length > 0 ? float.Parse(ui.speedY.text) : float.Parse(ui.speedY.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text));

        var NEwaypoints = activeVesselData.DataPackage.startPoint.NEWayPoints;
        if (NEwaypoints == null)
        {
            NEwaypoints = new List<Vector2>();
            activeVesselData.DataPackage.startPoint.NEWayPoints = NEwaypoints;
        }
        NEwaypoints.Clear();
        for (int i = 0; i < waypointsParent.childCount; i++)
        {
            var waypoint = waypointsParent.GetChild(i).GetComponent<WaypointUI>();
            float nedN = float.Parse(waypoint.nedN.text.Length > 0 ? waypoint.nedN.text : waypoint.nedN.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text);
            float nedE = float.Parse(waypoint.nedE.text.Length > 0 ? waypoint.nedE.text : waypoint.nedE.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text);
            NEwaypoints.Add(new Vector2(nedN, nedE));
        }

        ui.ownVesselNameSelector.options.Add(new TMP_Dropdown.OptionData() { text = vessel.vesselName });
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
        setupMenu.SetMenuNONE();

        vesselDataSerializer.SerializeAndSaveVesselData(vessels);
    }

    private void ResetUI()
    {
        ui.vesselName.text = "";
        ui.beam.text = "";
        ui.lenght.text = "";
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
            if(ui.ownVesselNameSelector.options[i].text.Equals(vesselData.DataPackage.vessel.vesselName))
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
        setupMenu.SetMenuNONE();
    }

    public void OnEditClicked(VesselData vesselData)
    {
        if(activeVesselData != null) activeVesselData.EditDone();
        activeVesselData = vesselData;
        var waypoints = activeVesselData.DataPackage.startPoint.NEWayPoints;
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
        OnEditClickedEvent.Invoke();
    }

    public void OnDoneClicked()
    {
        SaveDataChanges();
        activeVesselData = null;
        setupMenu.SetMenuNONE();
    }

    public void StartSimulation()
    {
        if (vessels.Count == 0) return;
        List<VesselData.VesselDataPackage> dataPackageList = new List<VesselData.VesselDataPackage>();
        foreach (var vd in vessels)
        {
            dataPackageList.Add(vd.DataPackage);
        }

        float stepTime = float.Parse(ui.stepTime.text.Length > 0 ? ui.stepTime.text : ui.stepTime.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text);
        float simTime = float.Parse(ui.simTime.text.Length > 0 ? ui.simTime.text : ui.simTime.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text);

        mainCanvas.SetActive(false);
        
        simEngine.StartSimulationFromSetup(dataPackageList, stepTime, simTime, ui.ownVesselNameSelector.options[ui.ownVesselNameSelector.value].text);
    }

    [System.Serializable]
    public class VesselDataUI
    {
        public TMP_InputField vesselName;
        public TMP_InputField lenght;
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
        public TextMeshProUGUI vesselType;
        public TMP_Dropdown ownVesselNameSelector;
        public TMP_InputField simTime;
        public TMP_InputField stepTime;
    }
}

public interface IDataHandler
{
    public void OnVesselRemoved(VesselData vesselData);
    public void OnEditClicked(VesselData vesselData);
    public void OnDoneClicked();
}
