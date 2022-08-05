using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VesselSimulator.Simulation.Collision
{
    public class PathPrediction : MonoBehaviour
    {
        /// <summary>
        /// The time bound of the measured path to generate a path for a given ship.
        /// Should be large enough to get at least 12-20 measurements (9 is minimum).
        /// </summary>
        public float timeThreshold = 60f;
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
        /// the turning acceleration of a vessel that has a turnRateDelta of 0. Turns the vesssel onto a linear course.
        /// Must be between 0 and 1, where 0 will keep the current turn rate and 1 will put the vessel on a linear course immediately
        /// </summary>
        public float turnRateAcceleration = 0f;
        /// <summary>
        /// the linear acceleration of a vessel that has an accelerationDelta of 0. Accelerates the vesssel to a constant speed.
        /// Must be between 0 and 1, where 0 will keep the current acceleration and 1 will put the vessel on a constant speed immediately
        /// </summary>
        public float linearAcceleration = 0.1f;

        public List<VesselMeasurementData> filteredDataDebug = new List<VesselMeasurementData>();
        public List<VesselMeasurementData> filteredDataDebug2 = new List<VesselMeasurementData>();

        public List<VesselMeasurementData> GeneratePathPrediction(List<VesselMeasurementData> ShipData)
        {
            List<VesselMeasurementData> filteredData = FilterData(ShipData);
            //filteredDataDebug = filteredData;
            //we need at least 9 points for a prediction - the bezier will create 6 poinst from these, and the catmul rom 3 from the 6 - that we can further split
            //with 3 points we can mesure acceleration, turning rate and can predict a basic path.
            if (filteredData.Count < 9)
            {
                //marking ship as obstacle - predictiong its average position as its poistion in the future
                Vector3 averagePoint = Vector3.zero;
                foreach (var point in filteredData)
                {
                    averagePoint += point.EUN;
                }
                averagePoint /= (float)filteredData.Count;
                return GenerateStaticPointPrediction(averagePoint, filteredData[filteredData.Count - 1].timeStamp);
            }
            filteredDataDebug2 = filteredData;
            List<VesselMeasurementData> catmulRomPoints = GenerateCatmulRomShipData(filteredData);
            //Filtered and simplifed path generated, getting data
            filteredDataDebug = catmulRomPoints;
            float averageSpeed = 0f;
            Vector3 vectorizedDistance = catmulRomPoints[catmulRomPoints.Count - 1].EUN - catmulRomPoints[0].EUN;
            float distane = vectorizedDistance.magnitude;
            float timeDelta = catmulRomPoints[catmulRomPoints.Count - 1].timeStamp - catmulRomPoints[0].timeStamp;
            float averageTurnRate = 0f;
            float averageAcceleration = 0f;
            float averageTorque = 0f;
            List<float> turnRateDeltaList = new List<float>();


            if (anchoredShipTravelTreshold != 0f)
            {
                if (distane < anchoredShipTravelTreshold)
                {
                    //marking ship as obstacle - predictiong its average position as its position in the future
                    Vector3 averagePoint = Vector3.zero;
                    foreach (var point in catmulRomPoints)
                    {
                        averagePoint += point.EUN;
                    }
                    averagePoint /= (float)catmulRomPoints.Count;
                    return GenerateStaticPointPrediction(averagePoint, catmulRomPoints[catmulRomPoints.Count - 1].timeStamp);
                }
            }

            for (int i = 0; i < catmulRomPoints.Count; i++)
            {
                if (i + 1 < catmulRomPoints.Count)
                {
                    averageSpeed += (catmulRomPoints[i + 1].EUN - catmulRomPoints[i].EUN).magnitude / (catmulRomPoints[i + 1].timeStamp - catmulRomPoints[i].timeStamp);
                    if (i + 2 < catmulRomPoints.Count)
                    {
                        Vector3 currentHeadingVector = catmulRomPoints[i + 1].EUN - catmulRomPoints[i].EUN;
                        Vector3 nextHeadingVector = catmulRomPoints[i + 2].EUN - catmulRomPoints[i + 1].EUN;
                        averageTurnRate += Vector3.SignedAngle(currentHeadingVector, nextHeadingVector, Vector3.up) / (catmulRomPoints[i + 2].timeStamp - catmulRomPoints[i].timeStamp);
                        averageAcceleration += (nextHeadingVector.magnitude / (catmulRomPoints[i + 2].timeStamp - catmulRomPoints[i + 1].timeStamp)) -
                                               (currentHeadingVector.magnitude / (catmulRomPoints[i + 1].timeStamp - catmulRomPoints[i].timeStamp));
                        if (i + 4 < catmulRomPoints.Count)
                        {
                            Vector3 secondHeadingVector = catmulRomPoints[i + 3].EUN - catmulRomPoints[i + 2].EUN;
                            Vector3 thirdHeadingVector = catmulRomPoints[i + 4].EUN - catmulRomPoints[i + 3].EUN;
                            float firstAngle = Vector3.SignedAngle(currentHeadingVector, nextHeadingVector, Vector3.up) / (catmulRomPoints[i + 2].timeStamp - catmulRomPoints[i].timeStamp);
                            float secondAngle = Vector3.SignedAngle(secondHeadingVector, thirdHeadingVector, Vector3.up) / (catmulRomPoints[i + 4].timeStamp - catmulRomPoints[i + 2].timeStamp);
                            //turnRateDeltaList.Add(secondAngle - firstAngle);
                            averageTorque += secondAngle - firstAngle;
                        }
                    }
                }
            }

            averageSpeed /= ((float)catmulRomPoints.Count - 1f); //in seconds
            averageTurnRate /= ((float)catmulRomPoints.Count - 2f); //in seconds
            averageAcceleration /= ((float)catmulRomPoints.Count - 2f); //in seconds

            //the ship considering its average speed and travel time traveled less than what it would take to do a 180 degree turn back, we consider it stationary
            if (anchoredShipTravelTreshold == 0f && distane < timeDelta * averageSpeed / (Mathf.PI / 2f))
            {
                //marking ship as obstacle - predictiong its average position as its poistion in the future
                Vector3 averagePoint = Vector3.zero;
                foreach (var point in catmulRomPoints)
                {
                    averagePoint += point.EUN;
                }
                averagePoint /= (float)catmulRomPoints.Count;
                return GenerateStaticPointPrediction(averagePoint, catmulRomPoints[catmulRomPoints.Count - 1].timeStamp);
            }

            float medianTurnRateDelta = 0f;
            /*turnRateDeltaList.Sort();
            if (turnRateDeltaList.Count % 2 == 1)
            {
                medianTurnRateDelta = turnRateDeltaList[(turnRateDeltaList.Count + 1) / 2];
            }
            else
            {
                medianTurnRateDelta = (turnRateDeltaList[turnRateDeltaList.Count / 2] + turnRateDeltaList[(turnRateDeltaList.Count / 2) + 1]) / 2f;
            }*/
            VesselMeasurementData lastPathPoint = catmulRomPoints[catmulRomPoints.Count - 1];

            //generating path - we will gradually sink turn rate delta to 0.
            List<VesselMeasurementData> pathPrediction = new List<VesselMeasurementData>();

            VesselMeasurementData firstPredictionPoint = new VesselMeasurementData(
                (catmulRomPoints[catmulRomPoints.Count - 3].timeStamp + catmulRomPoints[catmulRomPoints.Count - 4].timeStamp) / 2f,
                (catmulRomPoints[catmulRomPoints.Count - 3].EUN + catmulRomPoints[catmulRomPoints.Count - 4].EUN) / 2f);
            VesselMeasurementData secondPredictionPoint = new VesselMeasurementData(
                (catmulRomPoints[catmulRomPoints.Count - 1].timeStamp + catmulRomPoints[catmulRomPoints.Count - 2].timeStamp) / 2f,
                (catmulRomPoints[catmulRomPoints.Count - 1].EUN + catmulRomPoints[catmulRomPoints.Count - 2].EUN) / 2f);

            //The last two datapoint from the path. Needed for prediction start
            pathPrediction.Add(firstPredictionPoint);
            pathPrediction.Add(secondPredictionPoint);
            int counter = 2;
            float normalizedTurnRateAcceleration = 1f - turnRateAcceleration;
            for (float f = timeBetweenPredictionValues; f < predictionPathLenghtInTime; f += timeBetweenPredictionValues)
            {
                //direction vectior in last frame, normalized
                Vector3 lastMovementDirectionNormalized = (pathPrediction[counter - 1].EUN - pathPrediction[counter - 2].EUN).normalized;
                //turning direction vector based on observed turn rate and turn rate delta
                float acceleratedTurnRate = (averageTurnRate * timeBetweenPredictionValues) + (averageTorque * timeBetweenPredictionValues);
                float scaledTurnRate = acceleratedTurnRate * normalizedTurnRateAcceleration;
                //if turn rate is smaller the 0.05 degrees a second we consider it 0
                Vector3 predictedMovement = Vector3.zero;
                if (Mathf.Abs(scaledTurnRate) / timeBetweenPredictionValues >= 0.05f)
                {
                    //turning the vector
                    lastMovementDirectionNormalized = Quaternion.AngleAxis(scaledTurnRate, Vector3.up) * lastMovementDirectionNormalized;
                    normalizedTurnRateAcceleration *= Mathf.Pow((1f - turnRateAcceleration), timeBetweenPredictionValues);
                }
                //if acceleration is smaller than 1cm/s^2 we consider it 0
                if (averageAcceleration >= 0.01f)
                {
                    averageSpeed += averageAcceleration;
                    averageAcceleration *= Mathf.Pow((1f - linearAcceleration),timeBetweenPredictionValues);
                }
                //scaling the vector
                predictedMovement = lastMovementDirectionNormalized.normalized * averageSpeed * timeBetweenPredictionValues;
                var newDataPoint = new VesselMeasurementData(lastPathPoint.timeStamp + f, pathPrediction[counter - 1].EUN + predictedMovement);
                pathPrediction.Add(newDataPoint);

                averageTorque *= Mathf.Pow(0.9f, timeBetweenPredictionValues);
                counter++;
            }
            //remove the first two points, as they overlap with the original path
            pathPrediction.RemoveAt(0);
            pathPrediction.RemoveAt(0);

            return pathPrediction;
        }

        private List<VesselMeasurementData> GenerateStaticPointPrediction(Vector3 point, float startingTime)
        {
            List<VesselMeasurementData> prediction = new List<VesselMeasurementData>();
            for (float f = timeBetweenPredictionValues; f < predictionPathLenghtInTime; f += timeBetweenPredictionValues)
            {
                prediction.Add(new VesselMeasurementData(startingTime + f, point));
            }
            return prediction;
        }

        private List<VesselMeasurementData> GenerateCatmulRomShipData(List<VesselMeasurementData> filteredData)
        {
            List<VesselMeasurementData> bezierPoints = new List<VesselMeasurementData>();
            for (int i = 0; i < filteredData.Count - 3; i++)
            {
                Vector3 bezier = CubicBezierPoint(filteredData[i].EUN, filteredData[i + 1].EUN, filteredData[i + 2].EUN, filteredData[i + 3].EUN, 0.5f);
                bezierPoints.Add(new VesselMeasurementData((filteredData[i].timeStamp + filteredData[i + 3].timeStamp) / 2f, bezier));
            }

            List<VesselMeasurementData> catmulRomPoints = new List<VesselMeasurementData>();
            for (int i = 0; i < bezierPoints.Count - 3; i++)
            {
                //adding a point between each pair
                Vector3 catmulRomPoint = CatmullRomPoint(bezierPoints[i].EUN, bezierPoints[i + 1].EUN, bezierPoints[i + 2].EUN, bezierPoints[i + 3].EUN, 0.5f);
                catmulRomPoints.Add(bezierPoints[i + 1]);
                catmulRomPoints.Add(new VesselMeasurementData((bezierPoints[i + 1].timeStamp + bezierPoints[i + 2].timeStamp) / 2f, catmulRomPoint));
                /*
                Vector3 catmulRomPoint1 = CatmullRomPoint(bezierPoints[i].EUN, bezierPoints[i + 1].EUN, bezierPoints[i + 2].EUN, bezierPoints[i + 3].EUN, 0.25f);
                catmulRomPoints.Add(new VesselMeasurementData((bezierPoints[i + 1].timeStamp * 3f + bezierPoints[i + 2].timeStamp) / 4f, catmulRomPoint1));

                Vector3 catmulRomPoint2 = CatmullRomPoint(bezierPoints[i].EUN, bezierPoints[i + 1].EUN, bezierPoints[i + 2].EUN, bezierPoints[i + 3].EUN, 0.75f);
                catmulRomPoints.Add(new VesselMeasurementData((bezierPoints[i + 1].timeStamp + bezierPoints[i + 2].timeStamp * 3f) / 4f, catmulRomPoint2));
                */
            }
            catmulRomPoints.Add(bezierPoints[bezierPoints.Count - 2]);
            return catmulRomPoints;
        }

        private List<VesselMeasurementData> FilterData(List<VesselMeasurementData> ShipData)
        {
            //if the time of measurments are too small, keeping all the points is unnecessary - 0 default value will ignore this
            //we will also ignore points that are older than the time Treshold - assuming that the data is ordered by time, oldest data first

            if (ShipData.Count < 9)
                return ShipData;

            //we are iterating the list from the end - when creating the filter we need to add objects to the start of it, thus we use LinkedList.
            //The returned value will be an in-order List of the last x mesurements that were in the range of the last [timeTreshold] seconds.
            LinkedList<VesselMeasurementData> filteredData = new LinkedList<VesselMeasurementData>();

            if (minTime != 0f)
            {
                filteredData.AddFirst(ShipData[ShipData.Count - 1]);
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
                        if (lastTime - timeThreshold > ShipData[j].timeStamp)
                        {
                            return new List<VesselMeasurementData>(filteredData);
                        }
                        posTemp += ShipData[j].EUN;
                        timeTemp += ShipData[j].timeStamp;
                        vecCounter += 1f;
                        if (ShipData[j].timeStamp + minTime > ShipData[i].timeStamp)
                        {
                            j--;
                        }
                        else
                        {
                            break;
                        }
                    }
                    VesselMeasurementData m = new VesselMeasurementData(timeTemp / vecCounter, posTemp / vecCounter);
                    filteredData.AddFirst(m);
                    i = j;
                }
            }
            else
            {
                float lastTime = ShipData[ShipData.Count - 1].timeStamp;
                int i = ShipData.Count - 1;
                while (i > 0 && ShipData[i].timeStamp > lastTime - timeThreshold)
                {
                    filteredData.AddFirst(ShipData[i]);
                    i--;
                }
            }
            return new List<VesselMeasurementData>(filteredData);
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
    }

    public class VesselMeasurementData
    {
        /// <summary>
        /// Time stamp in seconds
        /// </summary>
        public float timeStamp;
        /// <summary>
        /// East Up North: unity coordinates
        /// </summary>
        public Vector3 EUN;

        public VesselMeasurementData() { }
        public VesselMeasurementData(float _timeStamp, Vector3 _EUN)
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