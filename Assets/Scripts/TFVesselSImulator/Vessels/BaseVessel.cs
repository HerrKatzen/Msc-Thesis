using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using System;

namespace VesselSimulator.TFVesselSimulator.Vessels
{
    public abstract class BaseVessel : MonoBehaviour
    {
        public string vesselName = Guid.NewGuid().ToString();
        public float length = 50f;
        public float draft = 5f;
        public float beam = 7f;
        public float rudMax = 30f;
        public float rudRateMax = 5f;
        public float rudTimeDelta = 1f;
        /// <summary>
        /// surge force (N) - pilot input
        /// </summary>
        public float tau_X = 100000f;
        public float rpm;
        public float vCurr;
        public float betaVCurr;
        [HideInInspector]
        public float rudAngle = 0f;
        public ControlSystem controlSystem = ControlSystem.HeadingAutopilot;
        protected Enviroment enviroment;
        [HideInInspector]
        public Eta eta;
        /// <summary>
        /// nu [1,2,3]
        /// </summary>
        [HideInInspector]
        public float[] linSpeed = new float[3];
        /// <summary>
        /// nu [4,5,6]
        /// </summary>
        [HideInInspector]
        public float[] torSpeed = new float[3];

        private void OnEnable()
        {
            if (vesselName == "") vesselName = gameObject.name;
        }

        public abstract void Init(StartPoint startPoint, Enviroment _enviroment);
        public abstract void UpdateSimulation(float u_control, float _propSpeed, float sampleTime);

        public abstract void UpdateWayoints();

        public ControlData AutoPilotStep(float time)
        {
            switch (controlSystem)
            {
                case ControlSystem.HeadingAutopilot:
                    return HeadingAutopilot(time);
                case ControlSystem.LOSPathFollowing:
                    return LOSPathFollowing(time);
                case ControlSystem.DynamicPositioning:
                    return DynamicPositioning(time);
                case ControlSystem.StepInput:
                    return StepInput(time);
                default:
                    Debug.LogError("Unknown Autopilot input");
                    return null;
            }
        }
        protected abstract ControlData HeadingAutopilot(float time);
        protected abstract ControlData LOSPathFollowing(float time);
        protected abstract ControlData DynamicPositioning(float time);
        protected abstract ControlData StepInput(float time);

        public Eta AttitudeEuler(float deltaT)
        {
            float[] p_dot = Mat3x3.Matmul(Rzyx(eta.roll, eta.pitch, eta.yaw), linSpeed);
            float[] v_dot = Mat3x3.Matmul(Tzyx(eta.roll, eta.pitch), torSpeed);

            //calculate vector for time frame
            p_dot = Mat3x3.Mult3(p_dot, deltaT);
            v_dot = Mat3x3.Mult3(v_dot, deltaT);

            Eta result = new Eta();
            // recalculate position and rotation
            result.north = eta.north + p_dot[0];
            result.east = eta.east + p_dot[1];
            result.down = eta.down + p_dot[2];
            result.roll = eta.roll + v_dot[0];
            result.pitch = eta.pitch + v_dot[1];
            result.yaw = eta.yaw + v_dot[2];

            return result;
        }

        public static float Ssa(float angle)
        {
            return (angle + Mathf.PI) % (2f * Mathf.PI) - Mathf.PI;
        }

        public static float[,] Smtrx(float[] a)
        {
            return new float[3, 3] { { 0f, -a[2], a[1] },
                                 { a[2], 0f, -a[0] },
                                 { -a[1], a[0], 0f } };
        }

        /// <summary>
        /// H = [eye(3)     S'
        ///     zeros(3,3) eye(3) ]
        /// </summary>
        public static float[,] Hmtrx(float[] r)
        {
            float[,] S = Smtrx(r);
            return new float[6, 6] { { 1f, 0f, 0f, S[0,0], S[1,0], S[2,0] },
                                 { 0f, 1f, 0f, S[0,1], S[1,1], S[2,1] },
                                 { 0f, 0f, 1f, S[0,2], S[1,2], S[2,2] },
                                 { 0f, 0f, 0f, 1f, 0f, 0f },
                                 { 0f, 0f, 0f, 0f, 1f, 0f },
                                 { 0f, 0f, 0f, 0f, 0f, 1f } };
        }

        [ContextMenu("Test Mtrx")]
        public void TestMtrx()
        {
            Mat3x3.Print3x3(Rzyx(0.5f, 1f, 1.5f));
        }

        public static float[,] Rzyx(float phi, float theta, float psi)
        {
            float cphi = Mathf.Cos(phi);
            float sphi = Mathf.Sin(phi);
            float cth = Mathf.Cos(theta);
            float sth = Mathf.Sin(theta);
            float cpsi = Mathf.Cos(psi);
            float spsi = Mathf.Sin(psi);

            return new float[3, 3] { { cpsi*cth, -spsi*cphi+cpsi*sth*sphi, spsi*sphi+cpsi*cphi*sth },
                                 { spsi*cth,  cpsi*cphi+sphi*sth*spsi, -cpsi*sphi+sth*spsi*cphi },
                                 {-sth, cth*sphi, cth*cphi } };
        }

        public static float[,] Tzyx(float phi, float theta)
        {
            float cphi = Mathf.Cos(phi);
            float sphi = Mathf.Sin(phi);
            float cth = Mathf.Cos(theta);
            float sth = Mathf.Sin(theta);

            if (cth == 0) return null;

            return new float[3, 3] { { 1f, sphi*sth/cth, cphi*sth/cth },
                                 { 0f, cphi, -sphi },
                                 { 0f, sphi/cth, cphi/cth} };
        }

        public enum ControlSystem
        {
            HeadingAutopilot,
            LOSPathFollowing,
            DynamicPositioning,
            StepInput

        }

        [System.Serializable]
        public class Eta
        {
            public float north = 0f;
            public float east = 0f;
            public float down = 0f;
            public float roll = 0f;
            public float pitch = 0f;
            public float yaw = 0f;

            public Eta() { }
            public Eta(float _north, float _east, float _down, float _roll, float _pitch, float _yaw)
            {
                north = _north;
                east = _east;
                down = _down;
                roll = _roll;
                pitch = _pitch;
                yaw = _yaw;
            }
            public Eta(Eta _eta)
            {
                north = _eta.north;
                east = _eta.east;
                down = _eta.down;
                roll = _eta.roll;
                pitch = _eta.pitch;
                yaw = _eta.yaw;
            }
        }

        public class ControlData
        {
            public float u_control;
            public float prop_speed;
        }

        [System.Serializable]
        public class DataBundle
        {
            public Eta eta;
            public float[] linearSpeed;
            public float[] angularSpeed;
            public float rudderAngle;
            public float rudderCommand;
            public float timeStamp;
            public DataBundle(Eta _eta, float[] linSpeed, float[] torSpeed, float rudA, float rudC, float time)
            {
                eta = new Eta(_eta);
                linearSpeed = new float[3] { linSpeed[0], linSpeed[1], linSpeed[2] };
                angularSpeed = new float[3] { torSpeed[0], torSpeed[1], torSpeed[2] };
                rudderAngle = rudA;
                rudderCommand = rudC;
                timeStamp = time;
            }

            public DataBundle(JSONNode bundleDataNode)
            {
                rudderAngle = bundleDataNode["rudderAngle"].AsFloat;
                rudderCommand = bundleDataNode["rudderCommand"].AsFloat;
                timeStamp = bundleDataNode["timeStamp"].AsFloat;

                JSONNode etaNode = bundleDataNode["eta"];
                eta = new Eta();
                eta.north = etaNode["north"].AsFloat;
                eta.east = etaNode["east"].AsFloat;
                eta.down = etaNode["down"].AsFloat;
                eta.roll = etaNode["roll"].AsFloat;
                eta.pitch = etaNode["pitch"].AsFloat;
                eta.yaw = etaNode["yaw"].AsFloat;

                JSONNode linearSpeedNode = bundleDataNode["linearSpeed"];
                linearSpeed = new float[3] { linearSpeedNode["x"].AsFloat, linearSpeedNode["y"].AsFloat, linearSpeedNode["z"].AsFloat };
                JSONNode torqueSpeedNode = bundleDataNode["torqueSpeed"];
                angularSpeed = new float[3] { torqueSpeedNode["x"].AsFloat, torqueSpeedNode["y"].AsFloat, torqueSpeedNode["z"].AsFloat };
            }

            public new string ToString()
            {
                return $"Time: {timeStamp}\nNED: {eta.north}, {eta.east}, {eta.down}\nATT: {eta.roll}, {eta.pitch}, {eta.yaw}\nLinV: " +
                    $"[{linearSpeed[0]}, {linearSpeed[1]}, {linearSpeed[2]}]\nTorV: [{angularSpeed[0]}, {angularSpeed[1]}, {angularSpeed[2]}]" +
                    $"\nRudC: {rudderCommand}\nRudA: {rudderAngle}";
            }

            public JSONNode ToJsonNode()
            {
                JSONNode etaNode = new JSONObject();
                etaNode["north"] = eta.north;
                etaNode["east"] = eta.east;
                etaNode["down"] = eta.down;
                etaNode["roll"] = eta.roll;
                etaNode["pitch"] = eta.pitch;
                etaNode["yaw"] = eta.yaw;

                JSONNode linearSpeedNode = new JSONObject();
                linearSpeedNode["x"] = linearSpeed[0];
                linearSpeedNode["y"] = linearSpeed[1];
                linearSpeedNode["z"] = linearSpeed[2];
                JSONNode torqueSpeedNode = new JSONObject();
                torqueSpeedNode["x"] = angularSpeed[0];
                torqueSpeedNode["y"] = angularSpeed[1];
                torqueSpeedNode["z"] = angularSpeed[2];

                JSONNode bundleDataNode = new JSONObject();
                bundleDataNode["eta"] = etaNode;
                bundleDataNode["linearSpeed"] = linearSpeedNode;
                bundleDataNode["torqueSpeed"] = torqueSpeedNode;
                bundleDataNode["rudderAngle"] = rudderAngle;
                bundleDataNode["rudderCommand"] = rudderCommand;
                bundleDataNode["timeStamp"] = timeStamp;

                return bundleDataNode;
            }
        }
    }
}