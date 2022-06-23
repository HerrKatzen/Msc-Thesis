using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionPrediction : MonoBehaviour
{
    [SerializeField]
    float beamClearanceOnSides = 2f;
    [SerializeField]
    float lenghtClearanceBackShip = 2f;
    [SerializeField]
    float lenghtClearanceFrontShip = 5f;
    [SerializeField]
    float lameCurveR = 5f;

    float lenght = 1f;
    float maxDistance = 1f;
    string vesselName;

    public void InitCollisionPredictionData(string _vesselName, float _lenght)
    {
        vesselName = _vesselName;
        lenght = _lenght;
    }

    public void UpdateCollisionData(float currentTime, List<BaseVessel.DataBundle> ownPathData)
    {
        foreach (var vessel in VesselDatabase.Instance.vesselDataMap)
        {
            if (vessel.Key.Equals(vesselName)) continue;

            VesselMeasurementData collision = PredictCollisionTime(ownPathData, vessel.Value.predictedPath);
            if(collision != null)
            {
                Debug.Log($"Collision Detected {collision.timeStamp - currentTime} seconds ahead of collision!" +
                    $"\ncurrent time: {currentTime}, time of collision: {collision.timeStamp}");
            }
        }
    }

    public VesselMeasurementData PredictCollisionTime(List<BaseVessel.DataBundle> ownPathData, List<VesselMeasurementData> predictedPath)
    {
        if (ownPathData == null || predictedPath == null) return null;

        maxDistance = Mathf.Max(lenght * beamClearanceOnSides, lenght* lenghtClearanceFrontShip,lenght * lenghtClearanceBackShip) + lenght / 2f;
        int i = 1, j = 0;
        //Ship data time stamp i will be larger then predicted path j, but ship data i-1 will be smaller
        while(ownPathData[i].timeStamp > predictedPath[j + 1].timeStamp)
        {
            j++;
        }
        
        for(; j < predictedPath.Count; j++)
        {
            while(i < ownPathData.Count && ownPathData[i].timeStamp < predictedPath[j].timeStamp)
            {
                i++;
            }
            if (i >= ownPathData.Count) break;

            float lerp = (predictedPath[j].timeStamp - ownPathData[i].timeStamp) / (ownPathData[i].timeStamp - ownPathData[i - 1].timeStamp);
            Vector2 currentEN = new Vector2(ownPathData[i].eta.east, ownPathData[i].eta.north);
            Vector2 previousEN = new Vector2(ownPathData[i - 1].eta.east, ownPathData[i - 1].eta.north);
            Vector2 lerpedEN = Vector2.Lerp(previousEN, currentEN, lerp);
            Vector2 mesuredEN = new Vector2(predictedPath[j].EUN.x, predictedPath[j].EUN.z);

            //we only calculate the costly Lamé Curve Test if the other ship is inside a critical distance - 2 here will always be larger than
            //the actual lamé curve test. Proof in the thesis for lim = TODO
            if (Vector2.Distance(lerpedEN, mesuredEN) < maxDistance * 2f)
            {
                if (LameCurveTest(lerpedEN, mesuredEN, ownPathData[i].eta.yaw))
                {
                    return predictedPath[j];
                }
            }
        }
        return null;
    }

    private bool LameCurveTest(Vector2 ownPosition, Vector2 otherPosition, float heading)
    {
        float x0 = otherPosition.x - ownPosition.x;
        float y0 = otherPosition.y - ownPosition.y;

        //rotating lamé curve values around 0;0 but with negative heading,
        //as rotating on the plane is anticlockwise while heading is clockwise
        float eastRotated = Mathf.Cos(-heading) * x0 + Mathf.Sin(-heading) * y0; 
        float northRotated = Mathf.Cos(-heading) * y0 - Mathf.Sin(-heading) * x0;
        //we are inside of the Lamé curve if the point's value < 1
        return Mathf.Pow(Mathf.Abs(eastRotated / (lenght * (0.5f + beamClearanceOnSides))), lameCurveR) +
               Mathf.Pow(Mathf.Abs((northRotated - lenght * ((lenghtClearanceFrontShip - lenghtClearanceBackShip) / 2f)) / (lenght * (0.5f + (lenghtClearanceBackShip + lenghtClearanceFrontShip) / 2f))), lameCurveR)
               < 1f;
    }

    public void GenerateExclusionZone(GameObject go, float lenght)
    {
        var lineHolder = new GameObject();
        lineHolder.transform.parent = go.transform;
        lineHolder.transform.localPosition = Vector3.zero;
        lineHolder.transform.localRotation = Quaternion.identity;
        lineHolder.transform.localScale = new Vector3(1f / go.transform.localScale.x, 1f / go.transform.localScale.y, 1f / go.transform.localScale.z);
        
        var lineRenderer = lineHolder.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        lineRenderer.startWidth = 1f;
        lineRenderer.endWidth = 1f;
        lineRenderer.material = new Material(Shader.Find("Standard"));
        lineRenderer.material.color = Color.red;
        lineRenderer.loop = true;

        float a = lenght * (0.5f + beamClearanceOnSides);
        float b = lenght * (0.5f + (lenghtClearanceBackShip + lenghtClearanceFrontShip) / 2f);
        float r = lameCurveR;
        float delta = lenght * ((lenghtClearanceFrontShip - lenghtClearanceBackShip) / 2f);

        Vector3[] linePositions = new Vector3[Mathf.RoundToInt(4f * (a/0.01f))];
        for(float f = -a; f < a; f += 0.01f)
        {
            float y = b * Mathf.Pow(1f - Mathf.Pow(Mathf.Abs(f / a), r), 1f / r);

            linePositions[Mathf.RoundToInt((f + a) / 0.01f)] = new Vector3(f, 0f, y + delta);
            linePositions[linePositions.Length - 1 - Mathf.RoundToInt((f + a) / 0.01f)] = new Vector3(f, 0f, -y + delta);
        }

        for (int i = 1; i < linePositions.Length; i++)
        {
            if (linePositions[i].x == 0f && linePositions[i].z == 0f)
            {
                linePositions[i] = new Vector3(linePositions[i - 1].x, linePositions[i - 1].y, linePositions[i - 1].z);
            }
        }
        lineRenderer.positionCount = linePositions.Length;
        lineRenderer.SetPositions(linePositions);
        lineRenderer.Simplify(0.3f);
    }
}
