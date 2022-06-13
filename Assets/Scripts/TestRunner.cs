using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestRunner : MonoBehaviour
{
    [SerializeField]
    private WindowGraph windowGraph;
    [SerializeField]
    private PathPrediction pathPrediction;

    [ContextMenu("Display Path And Predicted Path Using Half Path")]
    public void DisplayPathAndPredictedPathUsingHalfPath()
    {
        var dataBundles = DataLogger.Instance.SimData;
        List<PathPrediction.ShipMeasurementData> measurements = null;

        List<PathPrediction.ShipMeasurementData> allData = null;
        foreach (var s in dataBundles)
        {
            measurements = ConvertDataLogToShipMeasurement(s.Value);
            allData = ConvertDataLogToShipMeasurement(s.Value, 1f);
            break;
        }

        var prediction = pathPrediction.GeneratePathPrediction(measurements);

        windowGraph.SetDataBoundary(new Vector2(DataLogger.Instance.minEast, DataLogger.Instance.maxEast), new Vector2(DataLogger.Instance.minNorth, DataLogger.Instance.maxNorth));
        windowGraph.DisplayShipMesurementData(allData);
        windowGraph.DisplayShipMesurementData(pathPrediction.filteredDataDebug);
        //windowGraph.DisplayShipMesurementData(measurements);
        windowGraph.DisplayShipMesurementData(prediction);
    }

    private List<PathPrediction.ShipMeasurementData> ConvertDataLogToShipMeasurement(List<BaseVessel.DataBundle> dataList, float percent = 0.5f)
    {
        var measurements = new List<PathPrediction.ShipMeasurementData>();
        for(int i = 0; i < dataList.Count * percent; i++)
        {
            measurements.Add(new PathPrediction.ShipMeasurementData(dataList[i].timeStamp, new Vector3(dataList[i].eta.east, -dataList[i].eta.down, dataList[i].eta.north)));
        }
        return measurements;
    }
}
