using System;
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

    [Tooltip("the list of vessel types. Should use a prefab with the types as children")]
    [SerializeField]
    private List<BaseVessel> vesselTypes;

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
            radar.InitRadar(ownVesselName, radarScanDistance, 0.01f, allVesselGameobjects);
            collisionPreditor.InitCollisionPredictionData(ownVesselName, ownVesselBase.length);
            collisionPreditor.GenerateExclusionZone(go);
        }

        if(DataLogger.Instance.SimData.TryGetValue(ownVesselName, out ownData))
        {
            dataPlayer.StartAnimation();
        }
    }

    public async void StartSimulationFromSetup(List<VesselData> setupVesselData, SetupValuesData setupValuesData, string _ownVesselName)
    {
        radarScanDistance = setupValuesData.radarScanDistance;
        radarScanTime = setupValuesData.radarScanTime;
        generatedPathUpdateTime = setupValuesData.pathUpdateTime;
        enviroment.rho = setupValuesData.enviromentRho;
        enviroment.depth = setupValuesData.enviromentDepth;

        var baseVessels = new List<BaseVessel>();
        ownVesselName = _ownVesselName;
        foreach (var sd in setupVesselData)
        {
            foreach (var bv in vesselTypes)
            {
                if (bv.name.Equals(sd.DataPackage.vesselType))
                {
                    BaseVessel baseVessel = (BaseVessel)sd.gameObject.AddComponent(bv.GetType());
                    baseVessel.length = sd.DataPackage.length;
                    baseVessel.beam = sd.DataPackage.beam;
                    baseVessel.draft = sd.DataPackage.draft;
                    baseVessel.eta = new BaseVessel.Eta(sd.DataPackage.eta);
                    baseVessel.vesselName = sd.DataPackage.vesselName;
                    baseVessel.rudMax = sd.DataPackage.rudMax;
                    baseVessel.rudRateMax = sd.DataPackage.rudRateMax;
                    baseVessel.tau_X = sd.DataPackage.tau_X;

                    if (sd.DataPackage.vesselName.Equals(ownVesselName))
                    {
                        ownVesselBase = baseVessel;
                    }
                    StartPoint startPoint = sd.gameObject.AddComponent<StartPoint>();
                    startPoint.eta = new BaseVessel.Eta(sd.DataPackage.eta);
                    startPoint.linearSpeed = sd.DataPackage.linearSpeed;
                    startPoint.torqueSpeed = sd.DataPackage.torqueSpeed;
                    startPoint.NEWayPoints = sd.DataPackage.NEWayPoints;

                    baseVessels.Add(baseVessel);
                }
            }
        }
        //generate all paths
        simHandler.SetupSimulation(baseVessels, setupValuesData.stepTime, setupValuesData.simTime, enviroment);
        await simHandler.RunSimulation();
        await Task.Yield();

        allVesselGameobjects = await dataPlayer.SetupDataReplayAsync();
        if (allVesselGameobjects.TryGetValue(ownVesselName, out GameObject go))
        {
            radar = go.AddComponent<Radar>();
            collisionPreditor = go.AddComponent<CollisionPrediction>();
            await Task.Yield();
            await Task.Yield();

            radar.InitRadar(ownVesselName, radarScanDistance, setupValuesData.radarScanNoisePercent, allVesselGameobjects);

            VesselDatabase.Instance.SetupDatabasePathPredictionData(setupValuesData.pathTimeLength, setupValuesData.pathDataTimeLength, setupValuesData.pathTurnRateAcceleration);
            
            collisionPreditor.InitCollisionPredictionData(ownVesselName, ownVesselBase.length, setupValuesData.exclusionZoneFront, setupValuesData.exclusionZoneSides, setupValuesData.exclusionZoneBack);
            collisionPreditor.GenerateExclusionZone(go);

            Camera.main.transform.parent = go.transform;
            Camera.main.transform.localScale = Vector3.one;
            Camera.main.transform.localPosition = new Vector3(0f, 8f, -3f);
            Camera.main.transform.localRotation = Quaternion.identity;
            Camera.main.transform.RotateAround(Camera.main.transform.position, Camera.main.transform.right, 30f);
        }

        if (DataLogger.Instance.SimData.TryGetValue(ownVesselName, out ownData))
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
