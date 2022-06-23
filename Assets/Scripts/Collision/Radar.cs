using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Radar : MonoBehaviour
{
    private string ownVessel = "";
    private float scanDistance = 2000f;
    private float scanAngle = 10f;
    private float turnAroundTime = 1f;
    private Dictionary<string, GameObject> VesselGameObjects;

    public void InitRadar(string _ownVessel, float _scanDistance, Dictionary<string, GameObject> vesselGO)
    {
        ownVessel = _ownVessel;
        scanDistance = _scanDistance;
        VesselGameObjects = vesselGO;
    }
    public void PrimitiveScan(float currentTime)
    {
        foreach (var vessel in VesselGameObjects)
        {
            if (ownVessel.Equals(vessel.Key)) continue;
            var position = new Vector3(vessel.Value.transform.position.x, vessel.Value.transform.position.y, vessel.Value.transform.position.z);
            VesselDatabase.Instance.AddVesselPathDataPoint(vessel.Key, new VesselMeasurementData(currentTime, position));
        }
    }
}
