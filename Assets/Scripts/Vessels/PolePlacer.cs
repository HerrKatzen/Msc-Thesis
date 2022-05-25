using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PolePlacer
{
    public static PIDData PIDPolePlacement(PIDData data, float sampleTime)
    {
        data.wn = 0.1f; //TODO: why is it 0.1 when its set to 0.5?
        PIDData result = new PIDData();
        //PID gains based on pole placement
        float Kp = data.m * data.wn * data.wn - data.k;
        float Kd = data.m * 2f * data.zeta * data.wn - data.d;
        float Ki = (data.wn / 10f) * Kp;
        //PID control law
        result.u = -Kp * data.e_x - Kd * data.e_v - Ki * data.e_int;
        
        //Integral error, Euler's method
        result.e_int += sampleTime * data.e_x;
        Vector3 refModel = RefModel13(data.x_d, data.v_d, data.a_d, data.r, data.wn_d, data.zeta_d, data.v_max, sampleTime);
        
        result.x_d = refModel.x;
        result.v_d = refModel.y;
        result.a_d = refModel.z;

        return result;
    }

    public struct PIDData
    {
        public float u;
        public float e_int;
        public float x_d;
        public float v_d;
        public float a_d;
        public float e_v;
        public float e_x;
        public float m;
        public float d;
        public float k;
        public float wn_d;
        public float zeta_d;
        public float wn;
        public float zeta;
        public float r;
        public float v_max;
    }

    #region Guidance

    public static Vector3 RefModel13(float x_d, float v_d, float a_d, float r, float wn_d, float zeta_d, float v_max, float sampleTime)
    {
        v_d = Mathf.Clamp(v_d, -v_max, v_max);
        float j_d = wn_d * wn_d * wn_d * (r - x_d) - (2f * zeta_d + 1f) * wn_d * wn_d * v_d - (2f * zeta_d + 1f) * wn_d * a_d;
        x_d += sampleTime * v_d;
        v_d += sampleTime * a_d;
        a_d += sampleTime * j_d;
        return new Vector3(x_d, v_d, a_d);
    }

    #endregion
}
