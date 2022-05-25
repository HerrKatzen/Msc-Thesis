using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(StartPoint))]
public abstract class BaseVessel : MonoBehaviour
{
    public string vesselName = "";
    public float rpm;
    public float vCurr;
    public float betaVCurr;
    [HideInInspector]
    public float rudAngle = 0f;
    public ControlSystem controlSystem = ControlSystem.StepInput;
    protected Enviroment enviroment;
    [HideInInspector]
    public Eta eta;
    /// <summary>
    /// nu [1,2,3]
    /// </summary>
    [HideInInspector]
    public Vector3 linSpeed;
    /// <summary>
    /// nu [4,5,6]
    /// </summary>
    [HideInInspector]
    public Vector3 torSpeed;

    private void OnEnable()
    {
        if(vesselName == "") vesselName = gameObject.name;
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
        Vector3 p_dot = Mat3x3.Matmul(Rzyx(eta.roll, eta.pitch, eta.yaw), linSpeed);
        Vector3 v_dot = Mat3x3.Matmul(Tzyx(eta.roll, eta.pitch), torSpeed);

        //calculate vector for time frame
        p_dot = p_dot * deltaT;
        v_dot = v_dot * deltaT;

        Eta result = new Eta();
        // recalculate position and rotation
        result.north = eta.north + p_dot.x;
        result.east = eta.east + p_dot.y;
        result.down = eta.down + p_dot.z;
        result.roll = eta.roll + v_dot.x;
        result.pitch = eta.pitch + v_dot.y;
        result.yaw = eta.yaw + v_dot.z;

        return result;
    }

    public static float Ssa(float angle)
    {
        return (angle + Mathf.PI) % (2f * Mathf.PI) - Mathf.PI;
    }

    public static float[,] Smtrx(Vector3 a)
    {
        return new float[3, 3] { { 0f, -a.z, a.y }, 
                                 { a.z, 0f, -a.x }, 
                                 { -a.y, a.x, 0f } };
    }

    /// <summary>
    /// H = [eye(3)     S'
    ///     zeros(3,3) eye(3) ]
    /// </summary>
    public static float[,] Hmtrx(Vector3 r)
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
        public Vector3 linearSpeed;
        public Vector3 torqueSpeed;
        public float rudderAngle;
        public float rudderCommand;
        public float timeStamp;
        public DataBundle(Eta _eta, Vector3 linSpeed, Vector3 torSpeed, float rudA, float rudC, float time)
        {
            eta = new Eta(_eta);
            linearSpeed = new Vector3(linSpeed.x, linSpeed.y, linSpeed.z);
            torqueSpeed = new Vector3(torSpeed.x, torSpeed.y, torSpeed.z);
            rudderAngle = rudA;
            rudderCommand = rudC;
            timeStamp = time;
        }

        public new string ToString()
        {
            return $"Time: {timeStamp}\nNED: {eta.north}, {eta.east}, {eta.down}\nATT: {eta.roll}, {eta.pitch}, {eta.yaw}\nLinV: " +
                $"[{linearSpeed.x}, {linearSpeed.y}, {linearSpeed.z}]\nTorV: [{torqueSpeed.x}, {torqueSpeed.y}, {torqueSpeed.z}]" +
                $"\nRudC: {rudderCommand}\nRudA: {rudderAngle}";
        }
    }
}
