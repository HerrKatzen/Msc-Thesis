using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VesselSimulator.TFVesselSimulator
{
    public class Enviroment : MonoBehaviour
    {
        public float beta_current = 0f;
        public float depth = 20f;
        /// <summary>
        /// density of water
        /// </summary>
        public float rho = 1025f;
        public GameObject groundEnvironment = null;
    }
}