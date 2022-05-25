using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class SimulationHandler : MonoBehaviour
{
    [SerializeField]
    private Enviroment enviroment;
    [SerializeField]
    private List<BaseVessel> vessels;
    /// <summary>
    /// The step time of the simulation: the lower the more accurate but at the cost of more calcualtions. Given in seconds
    /// </summary>
    [SerializeField]
    private float simulationTime = 0.1f;
    /// <summary>
    /// The time of the whole simulation. Given in seconds.
    /// </summary>
    [SerializeField]
    private float timeToSimulate = 120f;

    private void Start()
    {
        StartSimulation();
    }

    private void StartSimulation()
    {
        DataLogger.Instance.SetStepTime(simulationTime);
        foreach (var vessel in vessels)
        {
            vessel.Init(vessel.GetComponent<StartPoint>(), enviroment);
            DataLogger.Instance.AddVesselInitData(vessel.vesselName, vessel.GetComponent<StartPoint>().NEWayPoints);
        }

        for (float step = simulationTime; step < timeToSimulate; step += simulationTime)
        {
            foreach (var vessel in vessels)
            {
                vessel.UpdateWayoints();
                var controlData = vessel.AutoPilotStep(simulationTime);
                vessel.UpdateSimulation(controlData.u_control, controlData.prop_speed, simulationTime);
                DataLogger.Instance.LogVesselData(vessel.vesselName, new BaseVessel.DataBundle(vessel.eta, vessel.linSpeed, vessel.torSpeed, vessel.rudAngle, controlData.u_control, step));
            }
            //yield return delay;
        }
        DataLogger.Instance.DebugLogData();
    }
}
