using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

//TODO: scailng issue
[RequireComponent(typeof(ThorFossenSimulationHandler))]
[RequireComponent(typeof(Enviroment))]
[RequireComponent(typeof(DataPlayer))]
public class SimulationEngine : MonoBehaviour, IColissionHandler
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
    [SerializeField]
    private HUDController HUD;

    private ThorFossenSimulationHandler simHandler;
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
    private GameObject collisionPoint;
    private bool pauseOnCollision = true;

    private void Awake()
    {
        simHandler = GetComponent<ThorFossenSimulationHandler>();
        enviroment = GetComponent<Enviroment>();
        dataPlayer = GetComponent<DataPlayer>();
    }

    public void StartSimFromLog()
    {
        StartSimFromLogAsync();
    }
    private async void StartSimFromLogAsync()
    {
        SetupValuesData setupValuesData = DataLogger.Instance.setupValuesData;
        radarScanDistance = setupValuesData.radarScanDistance;
        radarScanTime = setupValuesData.radarScanTime;
        generatedPathUpdateTime = setupValuesData.pathUpdateTime;
        enviroment.rho = setupValuesData.enviromentRho;
        enviroment.depth = setupValuesData.enviromentDepth;
        ownVesselName = DataLogger.Instance.ownVesselName;
        var vesselsData = new Dictionary<string, VesselData.VesselMetaDataPackage>();
        foreach (var vessel in DataLogger.Instance.vesselData)
        {
            vesselsData.Add(vessel.vesselName, vessel);
        }

        allVesselGameobjects = await dataPlayer.SetupDataReplayAsync();


        if (allVesselGameobjects.TryGetValue(ownVesselName, out GameObject go))
        {
            radar = go.AddComponent<Radar>();
            collisionPreditor = go.AddComponent<CollisionPrediction>();
            await Task.Yield();
            await Task.Yield();

            radar.InitRadar(ownVesselName, radarScanDistance, setupValuesData.radarScanNoisePercent, allVesselGameobjects);

            VesselDatabase.Instance.SetupDatabasePathPredictionData(setupValuesData.pathTimeLength, setupValuesData.pathDataTimeLength, setupValuesData.pathTurnRateAcceleration);
            collisionPreditor.SetCollisionHandler(this);
            if(vesselsData.TryGetValue(ownVesselName, out VesselData.VesselMetaDataPackage dataPackage))
            {
                collisionPreditor.InitCollisionPredictionData(ownVesselName, dataPackage.length, setupValuesData.exclusionZoneFront, setupValuesData.exclusionZoneSides, setupValuesData.exclusionZoneBack);
            }
            var exclusionZone = collisionPreditor.GenerateExclusionZone(go);
            HUD.SetExclusionZone(exclusionZone);
            HUD.SetOverlookingCam();
        }

        dataPlayer.StartAnimation();
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
            collisionPreditor.SetCollisionHandler(this);
            collisionPreditor.InitCollisionPredictionData(ownVesselName, ownVesselBase.length, setupValuesData.exclusionZoneFront, setupValuesData.exclusionZoneSides, setupValuesData.exclusionZoneBack);
            var exclusionZone = collisionPreditor.GenerateExclusionZone(go);
            HUD.SetExclusionZone(exclusionZone);
            HUD.SetOverlookingCam();
        }

        if (DataLogger.Instance.SimData.TryGetValue(ownVesselName, out List<BaseVessel.DataBundle> ownShipData))
        {
            ownData = new List<BaseVessel.DataBundle>(ownShipData.Count);
            for(int i = 0; i < ownShipData.Count; i++)
            {
                //transforming global data to local for the collision predictor to use
                var eta = new BaseVessel.Eta(ownShipData[i].eta.north - dataPlayer.AnimationDelta.z, ownShipData[i].eta.east - dataPlayer.AnimationDelta.x,
                                                    ownShipData[i].eta.down, ownShipData[i].eta.roll, ownShipData[i].eta.pitch, ownShipData[i].eta.yaw);
                var timeStamp = ownShipData[i].timeStamp;
                var linearSpeed = ownShipData[i].linearSpeed;
                var torqueSpeed = ownShipData[i].torqueSpeed;
                var rudderAngle = ownShipData[i].rudderAngle;
                var rudderCommand = ownShipData[i].rudderCommand;
                ownData.Add(new BaseVessel.DataBundle(eta, linearSpeed, torqueSpeed, rudderAngle, rudderCommand, timeStamp));
            }
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

    public void SetPauseOnCollision(bool pause)
    {
        pauseOnCollision = pause;
    }

    public async void RaiseCollision(string vesselName, Vector3 position, Vector3 heading, float collisionTime)
    {
        if(pauseOnCollision)
        {
            GameObject otherVessel, ownVessel;
            if (!allVesselGameobjects.TryGetValue(vesselName, out otherVessel)) return;
            if (!allVesselGameobjects.TryGetValue(ownVesselName, out ownVessel)) return;

            dataPlayer.PauseReplay();
            collisionPoint = new GameObject("collisionPoint");
            HUD.SetOverlookingCam();
            var other = Instantiate(otherVessel, collisionPoint.transform);
            var own = Instantiate(ownVessel, collisionPoint.transform);
            await Task.Yield();
            await Task.Yield();
            other.transform.position = position;
            other.transform.rotation = Quaternion.LookRotation(heading, Vector3.up);

            for (int i = 1; i < ownData.Count; i++)
            {
                if (ownData[i].timeStamp >= collisionTime)
                {
                    float lerp = (ownData[i].timeStamp - collisionTime) / (ownData[i].timeStamp - ownData[i - 1].timeStamp);
                    Vector2 currentEN = new Vector2(ownData[i].eta.east, ownData[i].eta.north);
                    Vector2 previousEN = new Vector2(ownData[i - 1].eta.east, ownData[i - 1].eta.north);
                    Vector2 lerpedEN = Vector2.Lerp(previousEN, currentEN, lerp);
                    own.transform.position = new Vector3(lerpedEN.x, 0f, lerpedEN.y);
                    own.transform.rotation = Quaternion.LookRotation(new Vector3(currentEN.x - previousEN.x, 0f, currentEN.y - previousEN.y), Vector3.up);
                    break;
                }
            }
            HUD.SetupCollisionCam(own, other);
            PopUpWithButton.Instance.PopupText($"Collision Detected with vessel: {vesselName}.\nCollision in {collisionTime - dataPlayer.Time} seconds.");
        }
        else
        {
            DelayedPopUp.Instance.PopupTextWithDelay($"Collision Detected with vessel: {vesselName}.\nCollision in {collisionTime - dataPlayer.Time} seconds.");
        }
    }

    public void RemoveCollisionObjectsIfPresent()
    {
        HUD.SetOverlookingCam();
        HUD.DisableCollisionCam();
        Destroy(collisionPoint);
    }
}

public interface IColissionHandler
{
    public void RaiseCollision(string vesselName, Vector3 position, Vector3 heading, float time);
}