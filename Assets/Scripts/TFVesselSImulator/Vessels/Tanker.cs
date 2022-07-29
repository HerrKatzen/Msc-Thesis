using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VesselSimulator.TFVesselSimulator.Vessels
{
    public class Tanker : BaseVessel
    {
        public float propMax = 90f;
        public float speed;

        //Heading Autopilot
        private float eIntegral = 0f;
        private float wn = 0.2f;
        private float zeta = 0.8f;

        //reference model
        private float yawRateMax = Mathf.PI / 180f;
        private float psi_d; //desired yaw angle
        private float r_d;
        private float a_d;
        private float wn_d;
        private float zeta_d = 1.0f;


        private void Start()
        {
            wn_d = wn / 5f;
        }

        public override void Init(StartPoint startPoint, Enviroment _enviroment)
        {
            eta = new Eta(startPoint.eta);
            linSpeed = startPoint.linearSpeed;
            torSpeed = startPoint.torqueSpeed;
            enviroment = _enviroment;
            speed = 0f;
        }

        public override void UpdateSimulation(float u_control, float _propSpeed, float sampleTime)
        {
            //controls
            float n = rpm / 60f;
            float u = linSpeed[0];
            float v = linSpeed[1];
            float r = torSpeed[2];

            //current velocities
            float u_c = speed * Mathf.Cos(enviroment.beta_current - eta.yaw);
            float v_c = speed * Mathf.Sin(enviroment.beta_current - eta.yaw);
            Vector3 nu_c = new Vector3(u_c, v_c, 0f);
            Vector3 nu_r = linSpeed - nu_c;

            float beta = 0f;
            float u_r = nu_r[0];
            float v_r = nu_r[1];
            if (u_r != 0f) beta = v_r / u_r;

            //shallow water effects
            float z = draft / (enviroment.depth - draft);
            float Yuvz_mod = Yuvz;
            if (z >= 0.8f)
            {
                Yuvz_mod = -0.85f * (1f - (0.8f / z));
            }

            //forces and moment
            float gT = ((1f / length) * Tuu * Mathf.Pow(u_r, 2f)) + (Tun * u_r * n) + (length * Tnn * Mathf.Abs(n) * n);
            float c = Mathf.Sqrt(cun * u_r * n + cnn * Mathf.Pow(n, 2f));
            float gX = (1f / length) * (Xuu * Mathf.Pow(u_r, 2f) + length * d11 * v_r * r + Xvv * Mathf.Pow(v_r, 2f)
                        + Xccdd * Mathf.Abs(c) * c * Mathf.Pow(rudAngle, 2f)
                        + Xccbd * Mathf.Abs(c) * c * beta * rudAngle + length * gT * (1f - t)
                        + Xuuz * Mathf.Pow(u_r, 2f) * z
                        + length * Xvrz * v_r * r * z + Xvvzz * Mathf.Pow(v_r, 2f) * Mathf.Pow(z, 2f));
            float gY = (1f / length) * (Yuv * u_r * v_r + Yvv * Mathf.Abs(v_r) * v_r
                        + Yccd * Mathf.Abs(c) * c * rudAngle + length * d22 * u_r * r
                        + Yccbbd * Mathf.Abs(c) * c * Mathf.Abs(beta) * beta * Mathf.Abs(rudAngle)
                        + YT * gT * length + length * Yurz * u_r * r * z + Yuvz_mod * u_r * v_r * z
                        + Yvvz * Mathf.Abs(v_r) * v_r * z
                        + Yccbbdz * Mathf.Abs(c) * c * Mathf.Abs(beta) * beta * Mathf.Abs(rudAngle) * z);
            float gLN = Nuv * u_r * v_r + length * Nvr * Mathf.Abs(v_r) * r
                        + Nccd * Mathf.Abs(c) * c * rudAngle + length * d33 * u_r * r
                        + Nccbbd * Mathf.Abs(c) * c * Mathf.Abs(beta) * beta * Mathf.Abs(rudAngle)
                        + length * NT * gT + length * Nurz * u_r * r * z
                        + Nuvz * u_r * v_r * z + length * Nvrz * Mathf.Abs(v_r) * r * z
                        + Nccbbdz * Mathf.Abs(c) * c * Mathf.Abs(beta) * beta * Mathf.Abs(rudAngle) * z;

            //shallow water effects
            float m11_mod = m11 - Xudotz * z;
            float m22_mod = m22 - Yvdotz * z;
            float m33_mod = m33 - Nrdotz * z;

            //Dimensional state derivatives

            Vector3 linSpeedDot = new Vector3(gX / m11_mod, gY / m22_mod, 0f);
            Vector3 torSpeedDot = new Vector3(0f, 0f, gLN / (Mathf.Pow(length, 2f) * m33_mod));

            //rudder angle saturation
            if (Mathf.Abs(rudAngle) >= rudMax * Mathf.PI / 180f)
            {
                rudAngle = Mathf.Sign(rudAngle) * rudMax * Mathf.PI / 180f;
            }

            float rudDot = u_control - rudAngle;
            if (Mathf.Abs(rudDot) >= rudRateMax * Mathf.PI / 180f)
            {
                rudDot = Mathf.Sign(rudDot) * rudRateMax * Mathf.PI / 180f;
            }

            //Forward Euler integration [k+1]
            linSpeed += sampleTime * linSpeedDot;
            torSpeed += sampleTime * torSpeedDot;
            rudAngle += sampleTime * rudDot;
        }

        protected override ControlData HeadingAutopilot(float time)
        {
            throw new System.NotImplementedException();
        }

        protected override ControlData LOSPathFollowing(float time)
        {
            throw new System.NotImplementedException();
        }

        protected override ControlData DynamicPositioning(float time)
        {
            throw new System.NotImplementedException();
        }

        protected override ControlData StepInput(float time)
        {
            throw new System.NotImplementedException();
        }

        public override void UpdateWayoints()
        {
            throw new System.NotImplementedException();
        }

        #region StaticParameters
        static float t = 0.22f;
        static float cun = 0.605f;
        static float cnn = 38.2f;
        static float Tuu = -0.00695f;
        static float Tun = -0.00063f;
        static float Tnn = 0.0000354f;
        static float m11 = 1.05f;
        static float m22 = 2.02f;
        static float m33 = 0.1232f;
        static float d11 = 2.020f;
        static float d22 = -0.752f;
        static float d33 = -0.231f;
        static float Xuuz = -0.0061f;
        static float Xuu = -0.0377f;
        static float Xvv = 0.3f;
        static float Xudotz = -0.05f;
        static float Xvrz = 0.387f;
        static float Xccdd = -0.093f;
        static float Xccbd = 0.152f;
        static float Xvvzz = 0.0125f;
        static float Yccbbdz = -0.191f;
        static float YT = 0.04f;
        static float Yvv = -2.4f;
        static float Yuv = -1.205f;
        static float Yvdotz = -0.387f;
        static float Yurz = 0.182f;
        static float Yvvz = -1.5f;
        static float Yuvz = 0f;
        static float Yccd = 0.208f;
        static float Yccbbd = -2.16f;
        static float Nccbbdz = 0.344f;
        static float NT = -0.02f;
        static float Nvr = -0.3f;
        static float Nuv = -0.451f;
        static float Nrdotz = -0.0045f;
        static float Nurz = -0.047f;
        static float Nvrz = -0.12f;
        static float Nuvz = -0.241f;
        static float Nccd = -0.098f;
        static float Nccbbd = 0.688f;
        #endregion
    }
}