using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using SimpleJSON;
using System;

public class VesselData : MonoBehaviour
{
    [SerializeField]
    private VesselDataUI vesselDataUI;
    private VesselMetaDataPackage dataPackage = null;
    private IDataHandler dataHandler;
    public VesselMetaDataPackage DataPackage { 
        get 
        {
            if (dataPackage == null)
            {
                dataPackage = new VesselMetaDataPackage();
            }
            return dataPackage;
        }
        set { dataPackage = value; } 
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
        vesselDataUI.vesselName.text = dataPackage.vesselName;
        vesselDataUI.nedN.text = dataPackage.eta.north.ToString();
        vesselDataUI.nedE.text = dataPackage.eta.east.ToString();
        vesselDataUI.nedD.text = dataPackage.eta.down.ToString();
        vesselDataUI.numWP.text = dataPackage.NEWayPoints.Count.ToString();
    }

    public void SetEditMode()
    {
        vesselDataUI.editModeOverlay.SetActive(true);
        vesselDataUI.normalModeOverlay.SetActive(false);
        dataHandler.OnEditClicked(this);
    }

    public void SetOverlayDone()
    {
        vesselDataUI.editModeOverlay.SetActive(false);
        vesselDataUI.normalModeOverlay.SetActive(true);
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
    public class VesselMetaDataPackage
    {
        public string vesselType;
        public string vesselName;
        public float length;
        public float beam;
        public float draft;
        public float rudMax;
        public float rudRateMax;
        public float tau_X;
        public BaseVessel.Eta eta = new BaseVessel.Eta();
        public Vector3 linearSpeed = new Vector3();
        public Vector3 angularSpeed = new Vector3();
        public BaseVessel.ControlSystem controlSystem;
        public List<Vector2> NEWayPoints = new List<Vector2>();

        public VesselMetaDataPackage() { }

        public VesselMetaDataPackage(JSONNode root) 
        {
            vesselName = root["vesselName"];
            vesselType = root["type"];
            length = root["length"];
            beam = root["beam"];
            draft = root["draft"];
            rudMax = root["rudMax"];
            rudRateMax = root["rudRateMax"];
            tau_X = root["surgeForce"];

            JSONNode startPos = root["startPoint"];
            eta.north = startPos["x"];
            eta.east = startPos["y"];
            eta.down = startPos["z"];
            eta.yaw = root["heading"];

            JSONNode linSpeed = root["startLinSpeed"];
            linearSpeed.x = linSpeed["x"];
            linearSpeed.y = linSpeed["y"];
            linearSpeed.z = linSpeed["z"];

            JSONNode torSpeed = root["startTorqSpeed"];
            angularSpeed.x = torSpeed["x"];
            angularSpeed.y = torSpeed["y"];
            angularSpeed.z = torSpeed["z"];

            controlSystem = (BaseVessel.ControlSystem) Enum.Parse(typeof(BaseVessel.ControlSystem), root["controller"], true);

            var waypoints = root["waypoints"];
            foreach (var p in waypoints.Children)
            {
                Vector2 v = new Vector2();
                v.x = p["x"];
                v.y = p["y"];
                NEWayPoints.Add(v);
            }
        }

        public JSONNode ToJsonNode()
        {
            JSONNode root = new JSONObject();
            root["vesselName"] = vesselName;
            root["type"] = vesselType;
            root["length"] = length;
            root["beam"] = beam;
            root["draft"] = draft;
            root["rudMax"] = rudMax;
            root["rudRateMax"] = rudRateMax;
            root["surgeForce"] = tau_X;

            JSONNode startPos = new JSONObject();
            startPos["x"] = eta.north;
            startPos["y"] = eta.east;
            startPos["z"] = eta.down;
            root["startPoint"] = startPos;
            root["heading"] = eta.yaw;

            JSONNode linSpeed = new JSONObject();
            linSpeed["x"] = linearSpeed.x;
            linSpeed["y"] = linearSpeed.y;
            linSpeed["z"] = linearSpeed.z;
            root["startLinSpeed"] = linSpeed;

            JSONNode torSpeed = new JSONObject();
            torSpeed["x"] = angularSpeed.x;
            torSpeed["y"] = angularSpeed.y;
            torSpeed["z"] = angularSpeed.z;
            root["startTorqSpeed"] = torSpeed;

            root["controller"] = controlSystem.ToString();

            var waypoints = new JSONArray();
            root["waypoints"] = waypoints;
            foreach (var p in NEWayPoints)
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
