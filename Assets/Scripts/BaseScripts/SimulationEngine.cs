using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

//TODO: scailng issue
[RequireComponent(typeof(SimulationHandler))]
[RequireComponent(typeof(Enviroment))]
[RequireComponent(typeof(DataPlayer))]
public class SimulationEngine : MonoBehaviour
{
    public string ownVesselName = "";

    [Tooltip("time between every radar scan in seconds")]
    public float radarScanTime = 1f;

    [Tooltip("the range of the radar in meters")]
    public float radarScanDistance = 2000f;

    [Tooltip("time between each path prediction run in seconds")]
    public float generatedPathUpdateTime = 5f;

    private SimulationHandler simHandler;
    private Enviroment enviroment;
    private DataPlayer dataPlayer;

    private float radarTimer = 0f;
    private float pathPredictionTimer = 0f;
    private Radar radar;
    private CollisionPrediction collisionPreditor;
    Dictionary<string, GameObject> allVesselGameobjects;
    private float previousTime = 0f;
    private List<BaseVessel.DataBundle> ownData;
    private BaseVessel ownVesselBase;

    private void Awake()
    {
        simHandler = GetComponent<SimulationHandler>();
        enviroment = GetComponent<Enviroment>();
        dataPlayer = GetComponent<DataPlayer>();
    }

    [ContextMenu("Run Test Sim")]
    public void RunTestSimulation()
    {
        RunTestSimAsync();
    }

    private async void RunTestSimAsync()
    {
        var list = new List<BaseVessel>();
        var c1 = new GameObject();
        var c2 = new GameObject();
        await Task.Yield();
        var startPoint1 = c1.AddComponent<StartPoint>();
        ownVesselBase = c1.AddComponent<Clarke83>();

        var startPoint2 = c2.AddComponent<StartPoint>();
        var clarke2 = c2.AddComponent<Clarke83>();
        await Task.Yield();
        startPoint1.linearSpeed = new Vector3(2f, 0f, 0f);
        startPoint1.NEWayPoints = new List<Vector2>();
        startPoint1.NEWayPoints.Add(new Vector3(150f, 100f));
        startPoint1.NEWayPoints.Add(new Vector3(300f, 200f));
        startPoint1.NEWayPoints.Add(new Vector3(450f, 100f));
        startPoint1.eta = new BaseVessel.Eta(0f, 0f, 0f, 0f, 0f, 0f);

        startPoint2.linearSpeed = new Vector3(2f, 0f, 0f);
        startPoint2.NEWayPoints = new List<Vector2>();
        startPoint2.NEWayPoints.Add(new Vector3(150f, 100f));
        startPoint2.NEWayPoints.Add(new Vector3(300f, 0f));
        startPoint2.NEWayPoints.Add(new Vector3(450f, -100f));
        startPoint2.eta = new BaseVessel.Eta(0f, 200f, 0f, 0f, 0f, 0f);

        c1.name = ownVesselBase.vesselName = ownVesselName;
        c2.name = clarke2.vesselName = "Dangerous Vessel";
        
        list.Add(ownVesselBase);
        list.Add(clarke2);

        //generate all paths
        simHandler.SetupSimulation(list, 0.02f, 180f, enviroment);
        await simHandler.RunSimulation();

        await Task.Yield();
        //run collision simulation

        allVesselGameobjects = await dataPlayer.SetupDataReplayAsync();
        if(allVesselGameobjects.TryGetValue(ownVesselName, out GameObject go))
        {
            radar = go.AddComponent<Radar>();
            collisionPreditor = go.AddComponent<CollisionPrediction>();
            await Task.Yield();
            collisionPreditor.InitCollisionPredictionData(ownVesselName, ownVesselBase.lenght);
            radar.InitRadar(ownVesselName, radarScanDistance, allVesselGameobjects);
            collisionPreditor.GenerateExclusionZone(go, ownVesselBase.lenght);
        }

        if(DataLogger.Instance.SimData.TryGetValue(ownVesselName, out ownData))
        {
            dataPlayer.StartAnimation();
        }
    }

    private void Update()
    {
        if (!dataPlayer.replaying) return;

        if (radarTimer >= radarScanTime)
        {
            radar.PrimitiveScan(dataPlayer.Time);
            radarTimer = 0f;
        }
        else
        {
            radarTimer += dataPlayer.Time - previousTime;
        }

        if(pathPredictionTimer == 0f)
        {
            collisionPreditor.UpdateCollisionData(dataPlayer.Time, ownData);
        }

        if(pathPredictionTimer >= generatedPathUpdateTime)
        {
            VesselDatabase.Instance.UpdatePredictedPaths();
            pathPredictionTimer = 0f;
        }
        else
        {
            pathPredictionTimer += dataPlayer.Time - previousTime;
        }
        previousTime = dataPlayer.Time;
    }

}
