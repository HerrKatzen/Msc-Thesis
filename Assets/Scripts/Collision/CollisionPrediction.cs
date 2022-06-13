using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BaseVessel))]
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
    float beam = 1f;
    float maxDistance = 1f;

    //TODO: probably not void, give back first collision
    public void PredictCollision(List<BaseVessel.DataBundle> shipData, List<PathPrediction.ShipMeasurementData> predictedPath)
    {
        lenght = GetComponent<BaseVessel>().lenght;
        beam = GetComponent<BaseVessel>().beam;
        maxDistance = lenght * Mathf.Max(beamClearanceOnSides, lenghtClearanceFrontShip, lenghtClearanceBackShip);
        int i = 1, j = 0;
        //Ship data time stamp i will be larger then predicted path j, but ship data i-1 will be smaller
        while(shipData[i].timeStamp > predictedPath[j + 1].timeStamp)
        {
            j++;
        }
        
        for(; j < predictedPath.Count; j++)
        {
            while(i < shipData.Count && shipData[i].timeStamp < predictedPath[j].timeStamp)
            {
                i++;
            }
            if (i >= shipData.Count) break;

            float lerp = (predictedPath[j].timeStamp - shipData[i].timeStamp) / (shipData[i].timeStamp - shipData[i - 1].timeStamp);
            Vector2 currentEN = new Vector2(shipData[i].eta.east, shipData[i].eta.north);
            Vector2 previousEN = new Vector2(shipData[i - 1].eta.east, shipData[i - 1].eta.north);
            Vector2 lerpedEN = Vector2.Lerp(previousEN, currentEN, lerp);
            Vector2 mesuredEN = new Vector2(predictedPath[j].EUN.x, predictedPath[j].EUN.z);

            //we only calculate the costly Lamé Curve Test if the other ship is inside a critical distance
            if (Vector2.Distance(lerpedEN, mesuredEN) < maxDistance * 1.2f)
            {
                if (LameCurveTest(lerpedEN, mesuredEN, shipData[i].eta.yaw))
                {
                    Debug.Log($"Ship will collide at time: {predictedPath[j].timeStamp}");
                    //TODO: UI
                }
            }
        }
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
        return Mathf.Pow(Mathf.Abs(eastRotated / (beam * (0.5f + beamClearanceOnSides))), lameCurveR) +
               Mathf.Pow(Mathf.Abs((northRotated - lenght * ((lenghtClearanceFrontShip - lenghtClearanceBackShip) / 2f)) / lenght * (0.5f + (lenghtClearanceBackShip + lenghtClearanceFrontShip) / 2f)), lameCurveR)
               < 1f;
    }
}
