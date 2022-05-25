using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataLogger : Singleton<DataLogger>
{
    public Dictionary<string, List<BaseVessel.DataBundle>> SimData { get; private set; } = new Dictionary<string, List<BaseVessel.DataBundle>>();
    public Dictionary<string, List<Vector3>> CheckPoints { get; private set; } = new Dictionary<string, List<Vector3>>();
    public float StepTime { get; private set; }

    public void LogVesselData(string vesselName, BaseVessel.DataBundle dataBundle)
    {
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

    internal void SetStepTime(float simulationTime)
    {
        StepTime = simulationTime;
    }
}
