using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipResistance : MonoBehaviour
{
    [SerializeField]
    private Transform referenceTransform;

    public float flatArea = 1f;
    public bool calculateFlatArea = false;
    public ShapeType shapeType = ShapeType.Rectangle;

    private void Start()
    {
        if(calculateFlatArea)
        {
            switch (shapeType)
            {
                case ShapeType.Triangle:
                    flatArea = (transform.localScale.x * transform.localScale.y) / 2f; //triangle has a 90 degree angle
                    break;
                case ShapeType.Rectangle:
                    flatArea = transform.localScale.x * transform.localScale.y;
                    break;
            }
        }
    }

    public float GetResistanceValue(Vector3 waterFlowDirection)
    {
        var inverseNormal = -1f * transform.forward;
        if (Vector3.Angle(inverseNormal, waterFlowDirection) >= 90f) return 0f;

        var attackVector = Vector3.Project(inverseNormal, waterFlowDirection);
        return attackVector.magnitude * flatArea;
    }

    public enum ShapeType
    {
        Triangle,
        Rectangle
    }
}
