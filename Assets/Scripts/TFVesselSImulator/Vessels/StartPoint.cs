using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VesselSimulator.TFVesselSimulator.Vessels
{
    public class StartPoint : MonoBehaviour
    {
        public BaseVessel.Eta eta;
        public Vector3 linearSpeed = Vector3.zero;
        public Vector3 torqueSpeed = Vector3.zero;
        public List<Vector2> NEWayPoints;
    }
}