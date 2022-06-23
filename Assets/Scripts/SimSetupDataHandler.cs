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

    public void AddNewShip()
    {
        //generateShipData
        var instance = Instantiate(vesselDataPrefab);
        VesselData vd = instance.GetComponent<VesselData>();
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
        vessel.vesselName = ui.vesselName.text;
        vessel.lenght = float.Parse(ui.lenght.text);
        vessel.beam = float.Parse(ui.beam.text);
        vessel.draft = float.Parse(ui.draft.text);
        vessel.rudMax = float.Parse(ui.rudAngMax.text);
        vessel.rudRateMax = float.Parse(ui.rudAngRateMax.text);
        vessel.tau_X = float.Parse(ui.surgeForce.text);

        var startPoint = activeVesselData.DataPackage.startPoint;
        if (startPoint.eta == null) startPoint.eta = new BaseVessel.Eta();

        startPoint.eta.north = float.Parse(ui.etaN.text);
        startPoint.eta.east = float.Parse(ui.etaE.text);
        startPoint.eta.down = float.Parse(ui.etaD.text);
        startPoint.eta.yaw = float.Parse(ui.heading.text) * Mathf.Deg2Rad; //heading in radian
    }

    public void OnVesselRemoved(VesselData vesselData)
    {
        vessels.Remove(vesselData);
        setupMenu.SetMenuNONE();
    }

    public void OnEditClicked(VesselData vesselData)
    {
        activeVesselData = vesselData;
        OnEditClickedEvent.Invoke();
    }

    [System.Serializable]
    public class VesselDataUI
    {
        public TextMeshProUGUI vesselName;
        public TextMeshProUGUI lenght;
        public TextMeshProUGUI beam;
        public TextMeshProUGUI draft;
        public TextMeshProUGUI rudAngMax;
        public TextMeshProUGUI rudAngRateMax;
        public TextMeshProUGUI surgeForce;
        public TextMeshProUGUI etaN;
        public TextMeshProUGUI etaE;
        public TextMeshProUGUI etaD;
        public TextMeshProUGUI heading;
        public TextMeshProUGUI speedN;
        public TextMeshProUGUI speedE;
        public TextMeshProUGUI speedD;
        public TextMeshProUGUI speedR;
        public TextMeshProUGUI speedP;
        public TextMeshProUGUI speedY;
        public TextMeshProUGUI vesselType;
    }
}

interface IDataHandler
{
    public void OnVesselRemoved(VesselData vesselData);
    public void OnEditClicked(VesselData vesselData);
}
