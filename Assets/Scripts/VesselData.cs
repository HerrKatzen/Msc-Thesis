using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using SimpleJSON;

public class VesselData : MonoBehaviour
{
    [SerializeField]
    private VesselDataUI vesselDataUI;
    [SerializeField]
    private StartPoint startPoint;
    private VesselDataPackage dataPackage = null;
    private IDataHandler dataHandler;
    public VesselDataPackage DataPackage { 
        get 
        {
            if (dataPackage == null)
            {
                dataPackage = new VesselDataPackage(startPoint);
            }
            return dataPackage;
        }
        private set { dataPackage = value; } 
    }

    public void SetDataHandler(IDataHandler _dataHandler)
    {
        dataHandler = _dataHandler;
    }

    public void MoveVesselUP(bool up)
    {
        if (up)
        {
            transform.SetSiblingIndex(Mathf.Max(0, transform.GetSiblingIndex() - 1));
        }
        else
        {
            transform.SetSiblingIndex(Mathf.Min(transform.parent.childCount - 1, transform.GetSiblingIndex() + 1));
        }
    }

    public void SetVesselDataUI()
    {
        vesselDataUI.vesselName.text = dataPackage.vessel.vesselName;
        vesselDataUI.nedN.text = dataPackage.startPoint.eta.north.ToString();
        vesselDataUI.nedE.text = dataPackage.startPoint.eta.east.ToString();
        vesselDataUI.nedD.text = dataPackage.startPoint.eta.down.ToString();
        vesselDataUI.numWP.text = dataPackage.startPoint.NEWayPoints.Count.ToString();
    }

    public void SetEditMode()
    {
        vesselDataUI.editModeOverlay.SetActive(true);
        vesselDataUI.normalModeOverlay.SetActive(false);
        dataHandler.OnEditClicked(this);
    }

    public void EditDone()
    {
        vesselDataUI.editModeOverlay.SetActive(false);
        vesselDataUI.normalModeOverlay.SetActive(true);
        dataHandler.OnDoneClicked();
    }

    public void DestroyVesselData()
    {
        dataHandler.OnVesselRemoved(this);
        Destroy(gameObject);
    }

    [System.Serializable]
    public class VesselDataPackage
    {
        public BaseVessel vessel;
        public StartPoint startPoint;

        public VesselDataPackage() { }
        public VesselDataPackage(StartPoint sp)
        {
            startPoint = sp;
        }

        public JSONNode ToJsonNode()
        {
            JSONNode root = new JSONObject();
            root["vesselName"] = vessel.vesselName;
            root["length"] = vessel.length;
            root["beam"] = vessel.beam;
            root["rudMax"] = vessel.rudMax;
            root["rudRateMax"] = vessel.rudRateMax;
            root["surgeForce"] = vessel.tau_X;

            JSONNode startPos = new JSONObject();
            startPos["x"] = startPoint.eta.north;
            startPos["y"] = startPoint.eta.east;
            startPos["z"] = startPoint.eta.down;
            root["startPoint"] = startPos;
            root["heading"] = startPoint.eta.yaw;

            JSONNode linSpeed = new JSONObject();
            linSpeed["x"] = startPoint.linearSpeed.x;
            linSpeed["y"] = startPoint.linearSpeed.y;
            linSpeed["z"] = startPoint.linearSpeed.z;
            root["startLinSpeed"] = linSpeed;

            JSONNode torSpeed = new JSONObject();
            torSpeed["x"] = startPoint.torqueSpeed.x;
            torSpeed["y"] = startPoint.torqueSpeed.y;
            torSpeed["z"] = startPoint.torqueSpeed.z;
            root["startTorqSpeed"] = torSpeed;

            var waypoints = new JSONArray();
            root["waypoints"] = waypoints;
            foreach (var p in startPoint.NEWayPoints)
            {
                JSONNode vector2Node = new JSONObject();
                vector2Node["x"] = p.x;
                vector2Node["y"] = p.y;
                waypoints.Add(vector2Node);
            }

            return root;
        }
    }
    [System.Serializable]
    public class VesselDataUI
    {
        public TMP_InputField vesselName;
        public TMP_InputField nedN;
        public TMP_InputField nedE;
        public TMP_InputField nedD;
        public TMP_InputField numWP;
        public GameObject editModeOverlay;
        public GameObject normalModeOverlay;
    }
}
