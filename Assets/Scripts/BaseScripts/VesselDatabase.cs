using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VesselDatabase : Singleton<VesselDatabase>
{
    [Tooltip("draws the path for the vessels.\nCaution! can be heavy on computation if there are lot of path points")]
    public bool drawPredictedPaths;
    public GameObject pathPrefab;
    public Dictionary<string, VesselData> vesselDataMap = new Dictionary<string, VesselData>();

    private List<GameObject> pathObectsPool = new List<GameObject>();
    private float pathTimeLenght = 120f;
    private float pathDataTimeLenght = 30f;
    private float turnRateAcceleration;

    public void SetupDatabasePathPredictionData(float _pathTimeLenght, float _pathDataTimeLenght, float _turnRateAcceleration)
    {
        pathTimeLenght = _pathTimeLenght;
        pathDataTimeLenght = _pathDataTimeLenght;
        turnRateAcceleration = _turnRateAcceleration;
    }

    //TODO: call this regularly
    public void UpdatePredictedPaths()
    {
        int i = 0;
        foreach (var vessel in vesselDataMap.Values)
        {
            vessel.predictedPath = vessel.pathPrediction.GeneratePathPrediction(vessel.pathData);
            if (vessel.predictedPath == null) continue;
            if(drawPredictedPaths)
            {
                StartCoroutine(UpdatePathRendering(vessel, i));
                i++;
            }
            else
            {
                if(pathObectsPool.Count > 0)
                {
                    for(int j = pathObectsPool.Count - 1; j >= 0; j--)
                    {
                        Destroy(pathObectsPool[j]);
                    }
                    pathObectsPool.Clear();
                }
            }
        }
    }

    private IEnumerator UpdatePathRendering(VesselData vessel, int i)
    {
        if (pathObectsPool.Count <= i)
        {
            pathObectsPool.Add(Instantiate(pathPrefab));
            yield return null;
            yield return null;
        }
        var lineRenderer = pathObectsPool[i].GetComponent<LineRenderer>();
        lineRenderer.positionCount = vessel.predictedPath.Count;
        var positions = new Vector3[vessel.predictedPath.Count];
        for (int j = 0; j < vessel.predictedPath.Count; j++)
        {
            positions[j] = vessel.predictedPath[j].EUN;
        }
        lineRenderer.SetPositions(positions);
        lineRenderer.Simplify(0.5f);
    }

    /// <summary>
    /// call whenewer a new dataoint is aqquired from any ship, should contain its name (ID)
    /// </summary>
    public void AddVesselPathDataPoint(string vessel, VesselMeasurementData dataPoint)
    {
        if(vesselDataMap.TryGetValue(vessel, out VesselData vesselData))
        {
            vesselData.pathData.Add(dataPoint);
        }
        else
        {
            vesselDataMap.Add(vessel, new VesselData(dataPoint, turnRateAcceleration, pathTimeLenght, pathDataTimeLenght));
        }
    }

    [System.Serializable]
    public class VesselData
    {
        //the current known path of the vessel
        public List<VesselMeasurementData> pathData;
        //the assigned path prediction profile
        public PathPrediction pathPrediction;
        //predicted path of the ship - its updated by the database handler
        public List<VesselMeasurementData> predictedPath;

        public VesselData() 
        {
            pathData = new List<VesselMeasurementData>();
            pathPrediction = new PathPrediction();
            pathPrediction.turnRateAcceleration = 0f;
            pathPrediction.timeTreshold = 20f;
            pathPrediction.predictionPathLenghtInTime = 180f;
        }
        public VesselData(VesselMeasurementData dataPoint)
        {
            pathData = new List<VesselMeasurementData>();
            pathData.Add(dataPoint);
            pathPrediction = new PathPrediction();
            pathPrediction.turnRateAcceleration = 0f;
            pathPrediction.timeTreshold = 20f;
            pathPrediction.predictionPathLenghtInTime = 180f;
        }
        public VesselData(VesselMeasurementData dataPoint, float turnRateAcceleration, float timeLength, float dataTimeLength)
        {
            pathData = new List<VesselMeasurementData>();
            pathData.Add(dataPoint);
            pathPrediction = new PathPrediction();
            pathPrediction.turnRateAcceleration = turnRateAcceleration;
            pathPrediction.timeTreshold = dataTimeLength;
            pathPrediction.predictionPathLenghtInTime = timeLength;
        }
    }
}
