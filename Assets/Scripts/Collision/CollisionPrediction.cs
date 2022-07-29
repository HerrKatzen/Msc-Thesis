using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionPrediction : MonoBehaviour
{
    [SerializeField]
    float clearanceOnSides = 2f;
    [SerializeField]
    float clearanceBackShip = 2f;
    [SerializeField]
    float clearanceFrontShip = 5f;
    [SerializeField]
    float lameCurveR = 5f;

    float length = 1f;
    float maxDistance = 1f;
    string vesselName;
    IColissionHandler colissionHandler;

    public void InitCollisionPredictionData(string _vesselName, float _length, float _frontClearance = 5f, float _sideClearance = 2f, float _backClearance = 2f)
    {
        vesselName = _vesselName;
        length = _length;
        clearanceFrontShip = _frontClearance;
        clearanceOnSides = _sideClearance;
        clearanceBackShip = _backClearance;
    }

    public void UpdateCollisionData(float currentTime, List<BaseVessel.DataBundle> ownPathData)
    {
        foreach (var vessel in VesselDatabase.Instance.vesselDataMap)
        {
            if (vessel.Key.Equals(vesselName)) continue;
            Vector3 heading;
            VesselMeasurementData collision = PredictCollision(ownPathData, vessel.Value.predictedPath, out heading);
            if(collision != null)
            {
                Debug.Log($"Collision Detected {collision.timeStamp - currentTime} seconds ahead of collision!" +
                    $"\ncurrent time: {currentTime}, time of collision: {collision.timeStamp}");

                colissionHandler.RaiseCollision(vessel.Key, collision.EUN, heading, collision.timeStamp);
            }
        }
    }

    public VesselMeasurementData PredictCollision(List<BaseVessel.DataBundle> ownPathData, List<VesselMeasurementData> predictedPath, out Vector3 heading)
    {
        heading = Vector3.forward;
        if (ownPathData == null || predictedPath == null) return null;

        maxDistance = (Mathf.Max(length * clearanceOnSides, length * clearanceFrontShip, length * clearanceBackShip) + length / 2f) * Mathf.Sqrt(2f);
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
            if (i >= ownPathData.Count) return null;

            float lerp = (predictedPath[j].timeStamp - ownPathData[i].timeStamp) / (ownPathData[i].timeStamp - ownPathData[i - 1].timeStamp);
            Vector2 currentEN = new Vector2(ownPathData[i].eta.east, ownPathData[i].eta.north);
            Vector2 previousEN = new Vector2(ownPathData[i - 1].eta.east, ownPathData[i - 1].eta.north);
            Vector2 lerpedEN = Vector2.Lerp(previousEN, currentEN, lerp);
            Vector2 mesuredEN = new Vector2(predictedPath[j].EUN.x, predictedPath[j].EUN.z);

            //we only calculate the costly Lamé Curve Test if the other ship is inside a critical distance that will always be larger than
            //the actual lamé curve test. Proof in the thesis
            if (Vector2.Distance(lerpedEN, mesuredEN) <= maxDistance)
            {
                if (LameCurveTest(lerpedEN, mesuredEN, ownPathData[i].eta.yaw))
                {
                    if(j < predictedPath.Count - 1)
                    {
                        heading = predictedPath[j + 1].EUN - predictedPath[j].EUN;
                    }
                    else if(j > 0)
                    {
                        heading = predictedPath[j].EUN - predictedPath[j - 1].EUN;
                    }
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
        return Mathf.Pow(Mathf.Abs(eastRotated / (length * (0.5f + clearanceOnSides))), lameCurveR) +
               Mathf.Pow(Mathf.Abs((northRotated - length * ((clearanceFrontShip - clearanceBackShip) / 2f)) / (length * (0.5f + (clearanceBackShip + clearanceFrontShip) / 2f))), lameCurveR)
               < 1f;
    }

    /// <summary>
    /// Generates a visual Exclusion zone around the gameobject go
    /// </summary>
    public GameObject GenerateExclusionZone(GameObject go)
    {
        var lineHolder = new GameObject("ExclusionZone");
        lineHolder.transform.parent = go.transform;
        lineHolder.transform.localPosition = Vector3.zero;
        lineHolder.transform.localRotation = Quaternion.identity;
        lineHolder.transform.localScale = new Vector3(1f / go.transform.localScale.x, 1f / go.transform.localScale.y, 1f / go.transform.localScale.z);
        
        var lineRenderer = lineHolder.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        lineRenderer.startWidth = 1f;
        lineRenderer.endWidth = 1f;
        lineRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        lineRenderer.material.color = Color.red;
        lineRenderer.loop = true;

        float a = length * (0.5f + clearanceOnSides);
        float b = length * (0.5f + (clearanceBackShip + clearanceFrontShip) / 2f);
        float r = lameCurveR;
        float delta = length * ((clearanceFrontShip - clearanceBackShip) / 2f);

        Vector3[] linePositions = new Vector3[Mathf.RoundToInt(4f * (a/0.01f))];
        for(float f = -a; f < a; f += 0.01f)
        {
            float y = b * Mathf.Pow(1f - Mathf.Pow(Mathf.Abs(f / a), r), 1f / r);

            linePositions[Mathf.RoundToInt((f + a) / 0.01f)] = new Vector3(f, 1f, y + delta);
            linePositions[linePositions.Length - 1 - Mathf.RoundToInt((f + a) / 0.01f)] = new Vector3(f, 1f, -y + delta);
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

        return lineHolder;
    }

    public void SetCollisionHandler(IColissionHandler _colissionHandler)
    {
        colissionHandler = _colissionHandler;
    }
}
