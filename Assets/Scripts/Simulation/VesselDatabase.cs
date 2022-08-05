using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VesselSimulator.Simulation.Collision;
using VesselSimulator.Util;

namespace VesselSimulator.Simulation
{
    public class VesselDatabase : Singleton<VesselDatabase>
    {
        [Tooltip("draws the path for the vessels.\nCaution! can be heavy on computation if there are lot of path points")]
        public bool drawPredictedPaths;
        public GameObject pathPrefab;
        public Dictionary<string, VesselDataLog> vesselDataMap = new Dictionary<string, VesselDataLog>();

        private List<GameObject> pathObectsPool = new List<GameObject>();
        private float pathTimeLenght = 120f;
        private float pathDataTimeLenght = 30f;
        private float pathDataMinTime = 2f;
        private float turnRateAcceleration;

        public void SetupDatabasePathPredictionData(float _pathTimeLenght, float _pathDataTimeLenght, float _turnRateAcceleration, float _minTime)
        {
            pathTimeLenght = _pathTimeLenght;
            pathDataTimeLenght = _pathDataTimeLenght;
            turnRateAcceleration = _turnRateAcceleration;
            pathDataMinTime = _minTime;
        }

        //call this regularly
        public void UpdatePredictedPaths()
        {
            int i = 0;
            foreach (var vessel in vesselDataMap.Values)
            {
                vessel.predictedPath = vessel.pathPrediction.GeneratePathPrediction(vessel.pathData);
                if (vessel.predictedPath == null) continue;
                if (drawPredictedPaths)
                {
                    StartCoroutine(UpdatePathRendering(vessel, i));
                    i++;
                }
                else
                {
                    if (pathObectsPool.Count > 0)
                    {
                        for (int j = pathObectsPool.Count - 1; j >= 0; j--)
                        {
                            Destroy(pathObectsPool[j]);
                        }
                        pathObectsPool.Clear();
                    }
                }
            }
        }

        private IEnumerator UpdatePathRendering(VesselDataLog vessel, int i)
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
                positions[j] = vessel.predictedPath[j].EUN + new Vector3(0f, 1f, 0f);
            }
            lineRenderer.SetPositions(positions);
            lineRenderer.Simplify(0.5f);
        }

        /// <summary>
        /// call whenewer a new dataoint is aqquired from any ship, should contain its name (ID)
        /// </summary>
        public void AddVesselPathDataPoint(string vessel, VesselMeasurementData dataPoint)
        {
            if (vesselDataMap.TryGetValue(vessel, out VesselDataLog vesselData))
            {
                vesselData.pathData.Add(dataPoint);
            }
            else
            {
                vesselDataMap.Add(vessel, new VesselDataLog(dataPoint, turnRateAcceleration, pathTimeLenght, pathDataTimeLenght, pathDataMinTime));
            }
        }

        public void ResetDatabase()
        {
            vesselDataMap = new Dictionary<string, VesselDataLog>();
            var temp = drawPredictedPaths;
            drawPredictedPaths = false;
            UpdatePredictedPaths();
            drawPredictedPaths = temp;
        }

        [System.Serializable]
        public class VesselDataLog
        {
            //the current known path of the vessel
            public List<VesselMeasurementData> pathData;
            //the assigned path prediction profile
            public PathPrediction pathPrediction;
            //predicted path of the ship - its updated by the database handler
            public List<VesselMeasurementData> predictedPath;

            public VesselDataLog()
            {
                pathData = new List<VesselMeasurementData>();
                pathPrediction = new PathPrediction();
                pathPrediction.turnRateAcceleration = 0f;
                pathPrediction.timeThreshold = 20f;
                pathPrediction.predictionPathLenghtInTime = 180f;
            }
            public VesselDataLog(VesselMeasurementData dataPoint)
            {
                pathData = new List<VesselMeasurementData>();
                pathData.Add(dataPoint);
                pathPrediction = new PathPrediction();
                pathPrediction.turnRateAcceleration = 0f;
                pathPrediction.timeThreshold = 20f;
                pathPrediction.predictionPathLenghtInTime = 180f;
            }
            public VesselDataLog(VesselMeasurementData dataPoint, float turnRateAcceleration, float timeLength, float dataTimeLength, float pathMinTime)
            {
                pathData = new List<VesselMeasurementData>();
                pathData.Add(dataPoint);
                pathPrediction = new PathPrediction();
                pathPrediction.turnRateAcceleration = turnRateAcceleration;
                pathPrediction.timeThreshold = dataTimeLength;
                pathPrediction.predictionPathLenghtInTime = timeLength;
                pathPrediction.minTime = pathMinTime;
            }
        }
    }
}