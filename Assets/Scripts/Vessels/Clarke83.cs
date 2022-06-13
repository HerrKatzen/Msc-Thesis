using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clarke83 : BaseVessel
{
    public float blockCoef = 0.7f;
    /// <summary>
    /// rudder aspect ratio
    /// </summary>
    public float lambda = 0.7f;
    public float rudMax = 30f;
    public float rudRateMax = 5f;
    public float propMax = 90f;
    public float rudTimeDelta = 1f;
    public float speed = 0f;
    /// <summary>
    /// surge force (N) - pilot input
    /// </summary>
    public float tau_X = 100000f;
    /// <summary>
    /// approx radious of gyration in yaw (m)
    /// </summary>
    public float R66;

    //Heading Autopilot
    private float eIntegral = 0f;
    private float wn = 0.5f;
    private float zeta = 1f;

    //reference model
    private float yawRateMax = Mathf.PI / 180f;
    private float psi_d = 0f; //desired yaw angle
    private float r_d = 0f;
    private float a_d = 0f;
    private float wn_d = 0f;
    private float zeta_d = 1f;
    private float rudHeight = 0f;
    private float m_PID;
    private float d_PID;
    private float k_PID;
    private float AR = 0f;
    private float CN;
    private float t_R;
    private float a_H;
    private float x_R;
    private float x_H;
    private float Nd;
    private float REF;
    private int waypointIndex = 0;
    private List<Vector2> wayPoints;
    private Mat3x3.SystemMatrices systemMatrices;

    // Start is called before the first frame update
    void Start()
    {
        enviroment = GetComponent<Enviroment>();
        //ComputeSystemMatrices(0f, lenght, beam, draft, blockCoef, R66, 0f, lenght);
    }
    public override void Init(StartPoint startPoint, Enviroment _enviroment)
    {
        eta = new Eta(startPoint.eta);
        linSpeed = startPoint.linearSpeed;
        torSpeed = startPoint.torqueSpeed;
        wayPoints = startPoint.NEWayPoints;
        enviroment = _enviroment;

        REF = 0f;
        speed = 0f;
        betaVCurr = 0f;
        R66 = lenght > 100f ? 0.27f * lenght : 0.25f * lenght;
        //linSpeed = new Vector3(2f, 0f, 0f);
        //torSpeed = Vector3.zero;

        eIntegral = 0f;
        wn = 0.5f;
        zeta = 1f;

        wn_d = wn / 5f;
        zeta_d = 1f;
        yawRateMax = 1f * Mathf.PI / 180f;
        
        //controller parameters m, d, k
        float U0 = 3f; // cruise speed
        systemMatrices = ComputeSystemMatrices(U0, lenght, beam, draft, blockCoef, R66, 0f, lenght);

        m_PID = systemMatrices.M[2, 2];
        d_PID = systemMatrices.N[2, 2];
        k_PID = 0f;

        //rudder yaw moment coefficient
        rudHeight = 0.7f * draft;
        AR = (rudHeight * rudHeight) / lambda; //aspect ratio
        CN = 6.13f * lambda / (lambda + 2.25f); //normal coeff
        t_R = 1f - (0.28f * blockCoef) -0.55f;
        a_H = 0.4f;
        x_R = -0.45f * lenght;
        x_H = -1f * lenght;

        Nd = -0.25f * (x_R + a_H * x_H) * enviroment.rho * U0 * U0 * AR * CN;
    }

    public override void UpdateWayoints()
    {
        if (wayPoints == null) return;

        var NE = new Vector2(eta.north, eta.east);
        var command = LosPathFollower.GetPathCommand(NE,wayPoints, waypointIndex, lenght);
        waypointIndex = command.waypointIndex;
        REF = command.headingCommand;
    }

    /// <summary>
    /// dynamics
    /// </summary>
    public override void UpdateSimulation(float u_control, float _propSpeed, float sampleTime)
    {
        //current velocities
        float u_c = speed * Mathf.Cos(betaVCurr - eta.yaw);
        float v_c = speed * Mathf.Sin(betaVCurr - eta.yaw);
        Vector3 currLinSpeed = new Vector3(u_c, v_c, 0f);
        Vector3 currTorSpeed = Vector3.zero;
        Vector3 relLinSpeed = linSpeed - currLinSpeed;
        Vector3 relTorSpeed = torSpeed - currTorSpeed;
        float relSpeed = Mathf.Sqrt(relLinSpeed.x * relLinSpeed.x + relLinSpeed.y * relLinSpeed.y);

        //rudder command and actual rudder angle
        float delta_c = u_control;
        float delta = rudAngle;

        //rudder forces and moment
        float Xdd = -0.5f * (1f - t_R) * enviroment.rho * relSpeed * relSpeed * AR * CN;
        float Yd = -0.25f * (1f + a_H) * enviroment.rho * relSpeed * relSpeed * AR * CN;
        float Nd = -0.25f * (x_R + a_H * x_H) * enviroment.rho * relSpeed * relSpeed * AR * CN;

        //control forces and moment
        float delta_R = -delta;
        float T = tau_X;
        float t_deduction = 0.1f;
        float tau1 = (1f - t_deduction) * T - Xdd * Mathf.Sin(delta_R) * Mathf.Sin(delta_R);
        float tau2 = -Yd * Mathf.Sin(2f * delta_R);
        float tau6 = -Nd * Mathf.Sin(2f * delta_R);
        Vector3 tau = new Vector3(tau1, tau2, tau6);
        //linear maneuvering model
        float T_surge = lenght;
        float xg = 0f;
        //3-DOF ship model
        systemMatrices = ComputeSystemMatrices(relSpeed, lenght, beam, draft, blockCoef, R66, xg, T_surge);
        float[,] Minv = Mat3x3.MultInverse(systemMatrices.M);
        Vector3 nu3 = new Vector3(relLinSpeed.x, relLinSpeed.y, relTorSpeed.z);
        Vector3 nu3_dot = Vector3.zero;
        if(Minv != null)
        {
            nu3_dot = Mat3x3.Matmul(Minv, tau - Mat3x3.Matmul(systemMatrices.N, nu3));
        }

        //6-DOF ship model
        Vector3 calcLinSpeed = new Vector3(nu3_dot.x, nu3_dot.y, 0f);
        Vector3 calcTorSpeed = new Vector3(0f, 0f, nu3_dot.z);

        //rudder angle saturation
        delta = Mathf.Clamp(delta, -rudMax * Mathf.Deg2Rad, rudMax * Mathf.Deg2Rad);
        
        //rudder dynamics
        float delta_dot = (delta_c - delta) / rudTimeDelta;

        //forward euler integration [k+1]
        delta += sampleTime * delta_dot;
        linSpeed += sampleTime * calcLinSpeed;
        torSpeed += sampleTime * calcTorSpeed;

        rudAngle = delta;
        eta = AttitudeEuler(sampleTime);
    }


    /// <summary>
    /// dynamics
    /// </summary>
    public Mat3x3.SystemMatrices ComputeSystemMatrices(float U, float L, float B, float T, float Cb, float R66, float xg, float T_surge)
    {
        //Rigid body parameters
        float rho = enviroment.rho;
        float V = Cb * L * B * T; //volume displacement
        float m = rho * V; //mass
        float Iz = m * Mathf.Pow(R66, 2f) + m * Mathf.Pow(xg, 2f); //moment of inertia around CO

        //Rigid body inertia mtx
        float[,] MRB = new float[3, 3] { { m, 0f, 0f }, { 0f, m, m * xg }, { 0f, m * xg, Iz } };

        //Nondimenisonal hydrodynamic derivatives in surge
        float Xudot = -0.1f * m;
        U += 0.001f;
        float Xu = -((m - Xudot) / T_surge) / (0.5f * rho * Mathf.Pow(L ,2f) * U);
        Xudot = Xudot / (0.5f * rho * Mathf.Pow(L, 3f));

        //Nondimenisonal hydrodynamic derivatives in sway and yaw
        float S = Mathf.PI * Mathf.Pow((T / L), 2f);

        float Yvdot = -S * (1f + 0.16f * Cb * B / T - 5.1f * Mathf.Pow((B / L), 2f));
        float Yrdot = -S * (0.67f * B / L - 0.0033f * Mathf.Pow((B / T), 2f));
        float Nvdot = -S * (1.1f * B / L - 0.041f * (B / T));
        float Nrdot = -S * (1f / 12f + 0.017f * Cb * (B / T) - 0.33f * (B / L));
        float Yv = -S * (1f + 0.4f * Cb * (B / T));
        float Yr = -S * (-1f / 2f + 2.2f * (B / L) - 0.08f * (B / T));
        float Nv = -S * (1f / 2f + 2.4f * (T / L));
        float Nr = -S * (1f / 4f + 0.039f * (B / T) - 0.56f * (B / L));

        float[,] MA_prime = new float[3, 3] { { -Xudot, 0f, 0f }, { 0f, -Yvdot, -Yrdot }, { 0f, -Nvdot, -Nrdot } };
        float[,] N_prime = new float[3, 3] { { -Xu, 0f, 0f }, { 0f, -Yv, -Yr }, { 0f, -Nv, -Nr } };
        //Dimensional model (Fossen 2021, Appendix D)
        float[,] dimT = new float[3, 3] { { 1f, 0f, 0f }, { 0f, 1f, 0f }, { 0f, 0f, 1f / L } };
        float[,] dimTinv = new float[3, 3] { { 1f, 0f, 0f }, { 0f, 1f, 0f }, { 0f, 0f, L } };

        float[,] MA = Mat3x3.Matmul(
                        Mat3x3.Matmul(Mat3x3.Multiply3x3(dimTinv, (0.5f * rho * Mathf.Pow(L, 3f))), dimTinv),
                        Mat3x3.Matmul(dimT, Mat3x3.Matmul(MA_prime, dimTinv)));

        Mat3x3.SystemMatrices result = new Mat3x3.SystemMatrices();
        result.M = Mat3x3.Add3x3(MRB, MA);
        result.N = Mat3x3.Matmul(
                    Mat3x3.Matmul(Mat3x3.Multiply3x3(dimTinv, (0.5f * rho * Mathf.Pow(L, 2f) * U)), dimTinv),
                    Mat3x3.Matmul(dimT, Mat3x3.Matmul(N_prime, dimTinv)));

        return result;
    }

    protected override ControlData HeadingAutopilot(float sampleTime)
    {
        float psi = eta.yaw;
        float r = torSpeed.z;
        float e_psi = psi - psi_d;
        float e_r = r - r_d;
        float psi_ref = REF * Mathf.PI / 180f;

        var data = new PolePlacer.PIDData();
        data.e_int = eIntegral;
        data.e_x = e_psi;
        data.e_v = e_r;
        data.x_d = psi_d;
        data.v_d = r_d;
        data.a_d = a_d;
        data.m = m_PID;
        data.d = d_PID;
        data.k = k_PID;
        data.wn_d = wn_d;
        data.zeta_d = zeta_d;
        data.zeta = zeta;
        data.wn = wn;
        data.r = psi_ref;
        data.v_max = yawRateMax;
        var PIDfeddback = PolePlacer.PIDPolePlacement(data, sampleTime);
        eIntegral = PIDfeddback.e_int;
        psi_d = PIDfeddback.x_d;
        r_d = PIDfeddback.v_d;
        a_d = PIDfeddback.a_d;

        ControlData controlData = new ControlData();
        controlData.prop_speed = 0f;
        controlData.u_control = PIDfeddback.u / Nd;
        return controlData;

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
}
