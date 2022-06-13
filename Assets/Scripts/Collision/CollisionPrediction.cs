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


    

    private bool LameCurveTest(Vector2 ownPosition, Vector2 otherPosition, float heading, float lenght, float beam)
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
