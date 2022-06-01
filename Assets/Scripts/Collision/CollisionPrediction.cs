using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionPrediction : MonoBehaviour
{
    /// <summary>
    /// The time bound to generate a path for a given ship.
    /// Should be large enough to get at least 12-20 measurements.
    /// </summary>
    public float timeTreshold = 60f;
    /// <summary>
    /// the minimum time between two measurements. 0 value ignores this.
    /// If too large, ships will be marked as stationary with invalid amount of data.
    /// </summary>
    public float minTime = 0f;
    /// <summary>
    /// The minimum distance a ship has to travel in [timeThershold] time to be considered traveling and non stationary.
    /// Setting to 0 will default to 180 degree turn around path distance.
    /// </summary>
    public float anchoredShipTravelTreshold = 0f;
    /// <summary>
    /// the stepTime of the prediction algorithm, the smaller the more number of steps calculated at the cost of increased time.
    /// Must be larger than 0.
    /// </summary>
    public float timeBetweenPredictionValues = 1f;
    /// <summary>
    /// the time we want the algorithm to predict the observed ship's path.
    /// </summary>
    public float predictionPathLenghtInTime = 120f;
    /// <summary>
    /// the reate with the turn rate acceleration is going to 0 if a vessel is going INTO a turn.
    /// </summary>
    public float turnRateDeltaSinkRate = 0.1f;
    /// <summary>
    /// the turning acceleration of a vessel that has a turnRateDelta of 0.
    /// Turns the vesssel onto a linear course.
    /// </summary>
    public float turnRateAcceleration = 0.1f;
    public List<ShipMeasurementData> GeneratePathPrediction(List<ShipMeasurementData> ShipData)
    {
        List<ShipMeasurementData> filteredData = FilterData(ShipData);
        //we need at least 9 points for a prediction - the bezier will create 6 poinst from these, and the catmul rom 3 from the 6 - that we can further split
        //with 3 points we can mesure acceleration, turning rate and can predict a basic path.
        if (filteredData.Count < 9)
            return null; // maybe check if its an obstacle in range?

        List<ShipMeasurementData> catmulRomPoints = GenerateCatmulRomShipData(filteredData);
        //Filtered and simplifed path generated, getting data

        float headingChange = Vector3.Angle(catmulRomPoints[0].EUN, catmulRomPoints[catmulRomPoints.Count - 1].EUN) *
                                            Mathf.Sign(catmulRomPoints[0].EUN.x * catmulRomPoints[catmulRomPoints.Count - 1].EUN.z -
                                                       catmulRomPoints[0].EUN.z * catmulRomPoints[catmulRomPoints.Count - 1].EUN.x);
        float currentHeading = Vector3.Angle(Vector3.forward, catmulRomPoints[catmulRomPoints.Count - 1].EUN) * 
                                             Mathf.Sign(Vector3.forward.x * catmulRomPoints[catmulRomPoints.Count - 1].EUN.z - 
                                                        Vector3.forward.z * catmulRomPoints[catmulRomPoints.Count - 1].EUN.x);
        float averageSpeed = 0f;
        Vector3 vectorizedDistance = catmulRomPoints[catmulRomPoints.Count - 1].EUN - catmulRomPoints[0].EUN;
        float distane = vectorizedDistance.magnitude;
        float timeDelta = catmulRomPoints[catmulRomPoints.Count - 1].timeStamp - catmulRomPoints[0].timeStamp;
        float averageTurnRate = 0f;
        List<float> turnRateDeltaList = new List<float>();
        for (int i = 0; i < catmulRomPoints.Count; i++)
        {
            if (i + 1 < catmulRomPoints.Count)
            {
                averageSpeed += (catmulRomPoints[i + 1].EUN - catmulRomPoints[i].EUN).magnitude / (catmulRomPoints[i + 1].timeStamp - catmulRomPoints[i].timeStamp);
                if(i + 2 < catmulRomPoints.Count)
                {
                    Vector3 currentHeadingVector = catmulRomPoints[i + 1].EUN - catmulRomPoints[i].EUN;
                    Vector3 nextHeadingVector = catmulRomPoints[i + 2].EUN - catmulRomPoints[i + 1].EUN;
                    averageTurnRate += Vector3.Angle(currentHeadingVector, nextHeadingVector) / (catmulRomPoints[i + 2].timeStamp - catmulRomPoints[i].timeStamp) * 
                                                     Mathf.Sign(currentHeadingVector.x * nextHeadingVector.z - currentHeadingVector.z * nextHeadingVector.x);
                    if(i + 4 < catmulRomPoints.Count)
                    {
                        Vector3 secondHeadingVector = catmulRomPoints[i + 3].EUN - catmulRomPoints[i + 2].EUN;
                        Vector3 thirdHeadingVector = catmulRomPoints[i + 4].EUN - catmulRomPoints[i + 3].EUN;
                        float firstAngle = Vector3.Angle(currentHeadingVector, nextHeadingVector) / (catmulRomPoints[i + 2].timeStamp - catmulRomPoints[i].timeStamp) * 
                                                         Mathf.Sign(currentHeadingVector.x * nextHeadingVector.z - currentHeadingVector.z * nextHeadingVector.x);
                        float secondAngle = Vector3.Angle(secondHeadingVector, thirdHeadingVector) / (catmulRomPoints[i + 4].timeStamp - catmulRomPoints[i + 2].timeStamp) *
                                                         Mathf.Sign(secondHeadingVector.x * thirdHeadingVector.z - secondHeadingVector.z * thirdHeadingVector.x);
                        turnRateDeltaList.Add(secondAngle - firstAngle);
                    }
                }
            }
        }

        averageSpeed /= ((float)catmulRomPoints.Count - 1f); //in seconds
        averageTurnRate /= ((float)catmulRomPoints.Count - 2f); //in seconds

        if (anchoredShipTravelTreshold != 0f)
        {
            if(distane < anchoredShipTravelTreshold)
            {
                //TODO: ship didn't move far enough to be considered traveling. mark as stationary obstacle
            }
        }
        //the ship considering its average speed and travel time traveled less than what it would take to do a 180 degree turn back, we consider it stationary
        else if (distane < timeDelta * averageSpeed / (Mathf.PI / 2f))
        {
            //TODO: ship didn't move far enough to be considered traveling. mark as stationary obstacle
        }

        float medianTurnRateDelta = 0f;
        turnRateDeltaList.Sort();
        if (turnRateDeltaList.Count % 2 == 1)
        {
            medianTurnRateDelta = turnRateDeltaList[(turnRateDeltaList.Count + 1) / 2];
        }
        else
        {
            medianTurnRateDelta = (turnRateDeltaList[turnRateDeltaList.Count / 2] + turnRateDeltaList[(turnRateDeltaList.Count / 2) + 1]) / 2f;
        }

        ShipMeasurementData lastPathPoint = catmulRomPoints[catmulRomPoints.Count - 1];
        //TODO: Generate paths that are using a turn rate delta deviation - in a given range.

        //generating path - we will gradually sink turn rate delta to 0.
        List<ShipMeasurementData> pathPrediction = new List<ShipMeasurementData>();
        for(float f = timeBetweenPredictionValues; f < predictionPathLenghtInTime; f += timeBetweenPredictionValues)
        {
            ShipMeasurementData data = new ShipMeasurementData();
            data.timeStamp = lastPathPoint.timeStamp + f;
            
            //TODO: fix heading problems, create vector based on speed and heading, scale it to time step, and generate point.
        }

        return null;
    }

    private List<ShipMeasurementData> GenerateCatmulRomShipData(List<ShipMeasurementData> filteredData)
    {
        List<ShipMeasurementData> bezierPoints = new List<ShipMeasurementData>();
        for (int i = 0; i < filteredData.Count - 3; i++)
        {
            Vector3 bezier = CubicBezierPoint(filteredData[i].EUN, filteredData[i + 1].EUN, filteredData[i + 2].EUN, filteredData[i + 3].EUN, 0.5f);
            bezierPoints.Add(new ShipMeasurementData((filteredData[i].timeStamp + filteredData[i + 3].timeStamp) / 2f, bezier));
        }

        List<ShipMeasurementData> catmulRomPoints = new List<ShipMeasurementData>();
        for (int i = 0; i < bezierPoints.Count - 3; i++)
        {
            //adding 2 points from each pair
            Vector3 catmulRomPoint1 = CatmullRomPoint(bezierPoints[i].EUN, bezierPoints[i + 1].EUN, bezierPoints[i + 2].EUN, bezierPoints[i + 3].EUN, 0.25f);
            catmulRomPoints.Add(new ShipMeasurementData((bezierPoints[i + 1].timeStamp * 3f + bezierPoints[i + 2].timeStamp) / 4f, catmulRomPoint1));

            Vector3 catmulRomPoint2 = CatmullRomPoint(bezierPoints[i].EUN, bezierPoints[i + 1].EUN, bezierPoints[i + 2].EUN, bezierPoints[i + 3].EUN, 0.75f);
            catmulRomPoints.Add(new ShipMeasurementData((bezierPoints[i + 1].timeStamp + bezierPoints[i + 2].timeStamp * 3f) / 4f, catmulRomPoint2));
        }
        return catmulRomPoints;
    }

    private List<ShipMeasurementData> FilterData(List<ShipMeasurementData> ShipData) //TODO: reverse order of output list...
    {
        //if the time of measurments are too small, keeping all the points is unnecessary - 0 default value will ignore this
        //we will also ignore points that are older than the time Treshold - assuming that the data is ordered by time, oldest data first
        
        if (ShipData.Count < 9)
            return ShipData;

        List<ShipMeasurementData> filteredData = new List<ShipMeasurementData>();

        if (minTime != 0f)
        {
            filteredData.Add(ShipData[ShipData.Count - 1]);
            float lastTime = ShipData[ShipData.Count - 1].timeStamp;
            int i = ShipData.Count - 1;
            while (i > 0)
            {
                int j = i - 1;
                Vector3 posTemp = Vector3.zero;
                float timeTemp = 0f;
                float vecCounter = 0f;
                while (j >= 0)
                {
                    if(lastTime - timeTreshold > ShipData[j].timeStamp)
                    {
                        return filteredData;
                    }
                    if (ShipData[i].timeStamp + minTime < ShipData[j].timeStamp)
                    {
                        posTemp += ShipData[j].EUN;
                        timeTemp += ShipData[j].timeStamp;
                        vecCounter += 1f;
                        j--;
                    }
                    else
                    {
                        posTemp += ShipData[j].EUN;
                        timeTemp += ShipData[j].timeStamp;
                        vecCounter += 1f;
                        break;
                    }
                }
                ShipMeasurementData m = new ShipMeasurementData(timeTemp / vecCounter, posTemp / vecCounter);
                filteredData.Add(m);
                i = j;
            }
        }
        else
        {
            float lastTime = ShipData[ShipData.Count - 1].timeStamp;
            int i = ShipData.Count - 1;
            while (ShipData[i].timeStamp > lastTime - timeTreshold)
            {
                filteredData.Add(ShipData[i]);
                i--;
            }
        }
        return filteredData;
    }

    /// <summary>
    /// Returns a catmull-Rom spline point given 4 points (in order) and a value t between 0 and 1.
    /// Can be used to connect points p1 and p2 with a single non-breaking curve.
    /// When creating a continuous curve on points, itertations should be called like 
    /// (p0,p1,p2,p3)->line between p1 and p2, (p1,p2,p3,p4)->line between p2 and p3, etc.
    /// </summary>
    private Vector3 CatmullRomPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        return p1 + (0.5f * (p2 - p0) * t) + 0.5f * (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t +
                0.5f * (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t;
    }

    /// <summary>
    /// Returns a cubic bezier point based on the input 4 points, and a t between 0 and 1.
    /// can be used to mitigate noise in 3D point data, by using it with the value t = 0.5,
    /// and as the basis of some other line drawing technique.
    /// </summary>
    private Vector3 CubicBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 p = uuu * p0;
        p += 3f * uu * t * p1;
        p += 3f * u * tt * p2;
        p += ttt * p3;

        return p;
    }

    public class ShipMeasurementData
    {
        /// <summary>
        /// Time stamp in seconds
        /// </summary>
        public float timeStamp;
        /// <summary>
        /// East North Up: unity coordinates
        /// </summary>
        public Vector3 EUN;

        public ShipMeasurementData() { }
        public ShipMeasurementData(float _timeStamp, Vector3 _EUN)
        {
            timeStamp = _timeStamp;
            EUN = _EUN;
        }

        /// <summary>
        /// Converts NED coordinates to Unity coordinates
        /// </summary>
        public static Vector3 ToUnityCoordinates(Vector3 NED)
        {
            return new Vector3(NED.y, -NED.z, NED.x);
        }
    }
}
