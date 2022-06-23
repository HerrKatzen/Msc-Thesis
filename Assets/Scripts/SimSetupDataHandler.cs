using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using System;

public class SimSetupDataHandler : MonoBehaviour, IDataHandler
{
    [SerializeField]
    MenuHandler setupMenu;
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
        SaveDataChanges();
        //generateShipData
        var instance = Instantiate(vesselDataPrefab, vesselsParent);
        activeVesselData = instance.GetComponent<VesselData>();
        vessels.Add(activeVesselData);
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
        }
        vessel.vesselName = ui.vesselName.text.Length > 0 ? ui.vesselName.text : Guid.NewGuid().ToString();
        vessel.lenght = ui.lenght.text.Length > 0 ? float.Parse(ui.lenght.text) : float.Parse(ui.lenght.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text);
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
        for (int i = 1; i < waypointsParent.childCount + 1; i++)
        {
            var waypoint = waypointsParent.GetChild(i).GetComponent<WaypointUI>();
            NEwaypoints.Add(new Vector2(float.Parse(waypoint.nedN.text), float.Parse(waypoint.nedE.text)));
        }
        ResetUI();
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
        setupMenu.SetMenuNONE();
    }

    public void OnEditClicked(VesselData vesselData)
    {
        SaveDataChanges();
        activeVesselData = vesselData;
        OnEditClickedEvent.Invoke();
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
    }
}

interface IDataHandler
{
    public void OnVesselRemoved(VesselData vesselData);
    public void OnEditClicked(VesselData vesselData);
}
