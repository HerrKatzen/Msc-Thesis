using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Radar : MonoBehaviour
{
    private string ownVessel = "";
    private float scanDistance = 10000f;
    private float noisePercent = 0.01f;
    private Dictionary<string, GameObject> VesselGameObjects;

    public void InitRadar(string _ownVessel, float _scanDistance, float _noisePercent, Dictionary<string, GameObject> vesselGO)
    {
        ownVessel = _ownVessel;
        scanDistance = _scanDistance;
        noisePercent = _noisePercent;
        VesselGameObjects = vesselGO;
    }
    public void PrimitiveScan(float currentTime)
    {
        foreach (var vessel in VesselGameObjects)
        {
            if (ownVessel.Equals(vessel.Key)) continue;
            var position = new Vector3(vessel.Value.transform.position.x, vessel.Value.transform.position.y, vessel.Value.transform.position.z);
            float distance = Vector3.Distance(position, transform.position);
            if (distance < scanDistance)
            {
                position = position + GenerateNoise(distance);
                VesselDatabase.Instance.AddVesselPathDataPoint(vessel.Key, new VesselMeasurementData(currentTime, position));
            }
        }
    }

    private Vector3 GenerateNoise(float distance)
    {
        Vector3 rotation3 = Vector3.zero;
        while(Mathf.Abs(rotation3.x) <= 0.01f && Mathf.Abs(rotation3.z) <= 0.01f)
        {
            rotation3 = Random.rotationUniform.eulerAngles;
        }
        rotation3.y = 0f;
        rotation3.Normalize();
        return rotation3 * distance * noisePercent * 0.1f;
    }
}
