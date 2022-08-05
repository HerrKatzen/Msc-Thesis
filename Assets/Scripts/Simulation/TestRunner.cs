using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VesselSimulator.Simulation.Collision;
using VesselSimulator.TFVesselSimulator.Vessels;
using VesselSimulator.UI;

namespace VesselSimulator.Simulation
{
    public class TestRunner : MonoBehaviour
    {
        [SerializeField]
        private WindowGraph windowGraph;
        [SerializeField]
        private PathPrediction pathPrediction;
        [SerializeField]
        private float noise = 0f;

        [ContextMenu("debug display vesseldatabase dumb vessel measurements")]
        public void DisplayPathPrediction()
        {
            windowGraph.ClearDots();
            if (VesselDatabase.Instance.vesselDataMap.TryGetValue("DumbVessel", out var value))
            {
                windowGraph.DisplayShipMesurementData(value.pathData);
            }
        }

        [ContextMenu("Display Path And Predicted Path Using Half Path")]
        public void DisplayPathAndPredictedPathUsingHalfPath()
        {
            windowGraph.ClearDots();
            var dataBundles = DataLogger.Instance.SimData;
            List<VesselMeasurementData> measurements = null;

            List<VesselMeasurementData> allData = null;
            foreach (var s in dataBundles)
            {
                measurements = ConvertDataLogToShipMeasurement(s.Value, 0.5f, noise);
                allData = ConvertDataLogToShipMeasurement(s.Value, 1f);
                break;
            }

            var prediction = pathPrediction.GeneratePathPrediction(measurements);

            windowGraph.SetDataBoundary(new Vector2(DataLogger.Instance.minEast, DataLogger.Instance.maxEast), new Vector2(DataLogger.Instance.minNorth, DataLogger.Instance.maxNorth));
            windowGraph.DisplayShipMesurementData(allData);
            windowGraph.DisplayShipMesurementData(pathPrediction.filteredDataDebug);
            //windowGraph.DisplayShipMesurementData(measurements);
            windowGraph.DisplayShipMesurementData(prediction);
            windowGraph.DisplayShipMesurementData(pathPrediction.filteredDataDebug2);
        }

        [ContextMenu("Collision Simulation")]
        public void CollisionSimulation()
        {

        }

        private List<VesselMeasurementData> ConvertDataLogToShipMeasurement(List<BaseVessel.DataBundle> dataList, float percent = 0.5f, float noise = 0f)
        {
            var measurements = new List<VesselMeasurementData>();
            for (int i = 0; i < dataList.Count * percent; i++)
            {
                var re = Random.Range(-1f * noise, 1f * noise);
                var rn = Random.Range(-1f * noise, 1f * noise);
                measurements.Add(new VesselMeasurementData(dataList[i].timeStamp, new Vector3(dataList[i].eta.east  + re, -dataList[i].eta.down, dataList[i].eta.north + rn)));
            }
            return measurements;
        }
    }
}