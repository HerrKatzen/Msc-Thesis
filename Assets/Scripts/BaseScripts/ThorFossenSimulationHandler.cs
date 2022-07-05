using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ThorFossenSimulationHandler : MonoBehaviour
{
    [SerializeField]
    private List<BaseVessel> vessels;
    /// <summary>
    /// The step time of the simulation: the lower the more accurate but at the cost of more calcualtions. Given in seconds
    /// </summary>
    [SerializeField]
    private float simulationTimeStep = 0.1f;
    /// <summary>
    /// The time of the whole simulation. Given in seconds.
    /// </summary>
    [SerializeField]
    private float timeToSimulate = 120f;

    private Enviroment enviroment;

    public void SetupSimulation(List<BaseVessel> allVessels, float _stepTime, float _timeToSimulate, Enviroment _enviroment)
    {
        vessels = allVessels;
        simulationTimeStep = _stepTime;
        timeToSimulate = _timeToSimulate;
        enviroment = _enviroment;
        DataLogger.Instance.SetStepTime(simulationTimeStep);
        foreach (var vessel in vessels)
        {
            vessel.Init(vessel.GetComponent<StartPoint>(), enviroment);
            DataLogger.Instance.AddVesselInitData(vessel.vesselName, vessel.GetComponent<StartPoint>().NEWayPoints);
        }
    }

    public async Task RunSimulation()
    {
        foreach (var vessel in vessels)
        {
            DataLogger.Instance.ClearVesselData(vessel.vesselName);
        }
        int count = 0;
        Debug.Log("Starting simulation: " + Time.time);
        for (float step = simulationTimeStep; step < timeToSimulate; step += simulationTimeStep)
        {
            foreach (var vessel in vessels)
            {
                vessel.UpdateWayoints();
                var controlData = vessel.AutoPilotStep(simulationTimeStep);
                vessel.UpdateSimulation(controlData.u_control, controlData.prop_speed, simulationTimeStep);
                DataLogger.Instance.LogVesselData(vessel.vesselName, new BaseVessel.DataBundle(vessel.eta, vessel.linSpeed, vessel.torSpeed, vessel.rudAngle, controlData.u_control, step));
            }
            count++;
            if(count % 100 == 0)
            {
                await Task.Yield();
            }
        }
        //DataLogger.Instance.DebugLogData();
        Debug.Log("Simulation Done: " + Time.time);
    }

    public void SimulateSingleVessel(BaseVessel vessel)
    {
        DataLogger.Instance.ClearVesselData(vessel.vesselName);

        for (float step = simulationTimeStep; step < timeToSimulate; step += simulationTimeStep)
        {
            vessel.UpdateWayoints();
            var controlData = vessel.AutoPilotStep(simulationTimeStep);
            vessel.UpdateSimulation(controlData.u_control, controlData.prop_speed, simulationTimeStep);
            DataLogger.Instance.LogVesselData(vessel.vesselName, new BaseVessel.DataBundle(vessel.eta, vessel.linSpeed, vessel.torSpeed, vessel.rudAngle, controlData.u_control, step));
        }
        //DataLogger.Instance.DebugLogData();
    }
}