using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

public class DataLogger : Singleton<DataLogger>
{
    public Dictionary<string, List<BaseVessel.DataBundle>> SimData { get; private set; } = new Dictionary<string, List<BaseVessel.DataBundle>>();
    public Dictionary<string, List<Vector3>> CheckPoints { get; private set; } = new Dictionary<string, List<Vector3>>();
    public float StepTime { get; private set; }
    public float minNorth = 0f;
    public float maxNorth = 100f;
    public float minEast = 0f;
    public float maxEast = 100f;

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
        JSONNode root = new JSONObject();
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
        root["stepTime"] = StepTime;
        return root.ToString();
    }

    [ContextMenu("Debug Read Log Data")]
    public void DebugReadLogData()
    {
        ReadLogDataFromJson(GetDataAsJson());
    }

    public void ReadLogDataFromJson(string json)
    {
        SimData = new Dictionary<string, List<BaseVessel.DataBundle>>();
        CheckPoints = new Dictionary<string, List<Vector3>>();

        var root = JSON.Parse(json);
        foreach (var pair in root["SimData"])
        {
            var itemList = new List<BaseVessel.DataBundle>();
            foreach (var o in pair.Value)
            {
                itemList.Add(new BaseVessel.DataBundle(o));
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
    }

    internal void SetStepTime(float simulationTime)
    {
        StepTime = simulationTime;
    }
}
