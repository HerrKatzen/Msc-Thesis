using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using VesselSimulator.Simulation.Collision;
using VesselSimulator.TFVesselSimulator;
using VesselSimulator.TFVesselSimulator.Vessels;
using VesselSimulator.UI;

namespace VesselSimulator.Simulation
{
    //TODO: scailng issue
    [RequireComponent(typeof(ThorFossenSimulationHandler))]
    [RequireComponent(typeof(Enviroment))]
    [RequireComponent(typeof(DataPlayer))]
    public class SimulationEngine : MonoBehaviour, IColissionHandler
    {
        [SerializeField]
        [Tooltip("Should the algorithm check for grounding")]
        private bool checkForGrounding = false;
        [SerializeField]
        [Tooltip("time between every radar scan in seconds")]
        private float radarScanTime = 1f;

        [SerializeField]
        [Tooltip("the range of the radar in meters")]
        private float radarScanDistance = 2000f;

        [SerializeField]
        [Tooltip("time between each path prediction run in seconds")]
        private float generatedPathUpdateTime = 5f;

        [SerializeField]
        private string ownVesselName = "";

        [SerializeField]
        [Tooltip("the list of vessel types. Should use a prefab with the types as children")]
        private List<BaseVessel> vesselTypes;
        [SerializeField]
        private HUDController HUD;
        [SerializeField]
        private Material whiteAlphaMat;
        [SerializeField]
        private Material redAlphaMat;

        private ThorFossenSimulationHandler simHandler;
        private Enviroment environment;
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
        private GameObject sceneElements;

        private void Awake()
        {
            simHandler = GetComponent<ThorFossenSimulationHandler>();
            environment = GetComponent<Enviroment>();
            dataPlayer = GetComponent<DataPlayer>();
            sceneElements = environment.groundEnvironment;
        }

        public void StartSimulationFromLog()
        {
            StartSimFromLogAsync();
        }
        private async void StartSimFromLogAsync()
        {
            SetupValuesData setupValuesData = DataLogger.Instance.setupValuesData;
            radarScanDistance = setupValuesData.radarScanDistance;
            radarScanTime = setupValuesData.radarScanTime;
            generatedPathUpdateTime = setupValuesData.pathUpdateTime;
            environment.rho = setupValuesData.enviromentRho;
            environment.depth = setupValuesData.enviromentDepth;
            ownVesselName = DataLogger.Instance.ownVesselName;

            var metaDataDict = new Dictionary<string, VesselData.VesselMetaDataPackage>();
            foreach (var data in DataLogger.Instance.vesselData)
            {
                metaDataDict.Add(data.vesselName, data);
            }

            allVesselGameobjects = await dataPlayer.SetupDataReplayAsync(metaDataDict);

            if (allVesselGameobjects.TryGetValue(ownVesselName, out GameObject go))
            {
                radar = go.AddComponent<Radar>();
                collisionPreditor = go.AddComponent<CollisionPrediction>();
                await Task.Yield();
                await Task.Yield();

                radar.InitRadar(ownVesselName, radarScanDistance, setupValuesData.radarScanNoisePercent, allVesselGameobjects);

                VesselDatabase.Instance.SetupDatabasePathPredictionData(setupValuesData.pathTimeLength, setupValuesData.pathDataTimeLength, setupValuesData.pathTurnRateAcceleration, setupValuesData.pathDataMinTime);
                collisionPreditor.SetCollisionHandler(this);

                for (int i = 0; i < DataLogger.Instance.vesselData.Count; i++)
                {
                    if (DataLogger.Instance.vesselData[i].vesselName.Equals(ownVesselName))
                    {
                        var dataPackage = DataLogger.Instance.vesselData[i];
                        collisionPreditor.InitCollisionPredictionData(ownVesselName, dataPackage.length, setupValuesData.exclusionZoneFront, setupValuesData.exclusionZoneSides, setupValuesData.exclusionZoneBack);
                        break;
                    }
                }

                var exclusionZone = collisionPreditor.GenerateExclusionZone(go);
                HUD.SetExclusionZone(exclusionZone);
                HUD.SetOverlookingCam();
            }

            if (DataLogger.Instance.SimData.TryGetValue(ownVesselName, out List<BaseVessel.DataBundle> ownShipData))
            {
                ownData = new List<BaseVessel.DataBundle>(ownShipData.Count);
                for (int i = 0; i < ownShipData.Count; i++)
                {
                    //transforming global data to local for the collision predictor to use
                    var eta = new BaseVessel.Eta(ownShipData[i].eta.north - dataPlayer.AnimationDelta.z, ownShipData[i].eta.east - dataPlayer.AnimationDelta.x,
                                                        ownShipData[i].eta.down, ownShipData[i].eta.roll, ownShipData[i].eta.pitch, ownShipData[i].eta.yaw);
                    var timeStamp = ownShipData[i].timeStamp;
                    var linearSpeed = ownShipData[i].linearSpeed;
                    var torqueSpeed = ownShipData[i].angularSpeed;
                    var rudderAngle = ownShipData[i].rudderAngle;
                    var rudderCommand = ownShipData[i].rudderCommand;
                    ownData.Add(new BaseVessel.DataBundle(eta, linearSpeed, torqueSpeed, rudderAngle, rudderCommand, timeStamp));
                }
                if (sceneElements != null)
                {
                    sceneElements.transform.position -= dataPlayer.AnimationDelta;
                }
                if (checkForGrounding) await collisionPreditor.CheckGrounding(ownData);
                dataPlayer.StartAnimation();
            }
        }

        public async void StartSimulationFromSetup(List<VesselData> setupVesselData, SetupValuesData setupValuesData, string _ownVesselName)
        {
            radarScanDistance = setupValuesData.radarScanDistance;
            radarScanTime = setupValuesData.radarScanTime;
            generatedPathUpdateTime = setupValuesData.pathUpdateTime;
            environment.rho = setupValuesData.enviromentRho;
            environment.depth = setupValuesData.enviromentDepth;

            var baseVessels = new List<BaseVessel>();
            var metaDataList = new List<VesselData.VesselMetaDataPackage>();
            ownVesselName = _ownVesselName;
            foreach (var sd in setupVesselData)
            {
                metaDataList.Add(sd.DataPackage);
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
                        startPoint.torqueSpeed = sd.DataPackage.angularSpeed;
                        startPoint.NEWayPoints = sd.DataPackage.NEWayPoints;

                        baseVessels.Add(baseVessel);
                    }
                }
            }
            //generate all paths
            simHandler.SetupSimulation(baseVessels, setupValuesData.stepTime, setupValuesData.simTime, environment);
            await simHandler.RunSimulation();
            await Task.Yield();



            var metaDataDict = new Dictionary<string, VesselData.VesselMetaDataPackage>();
            foreach (var data in setupVesselData)
            {
                metaDataDict.Add(data.DataPackage.vesselName, data.DataPackage);
            }

            allVesselGameobjects = await dataPlayer.SetupDataReplayAsync(metaDataDict);
            if (allVesselGameobjects.TryGetValue(ownVesselName, out GameObject go))
            {
                radar = go.AddComponent<Radar>();
                collisionPreditor = go.AddComponent<CollisionPrediction>();
                await Task.Yield();
                await Task.Yield();

                radar.InitRadar(ownVesselName, radarScanDistance, setupValuesData.radarScanNoisePercent, allVesselGameobjects);

                VesselDatabase.Instance.SetupDatabasePathPredictionData(setupValuesData.pathTimeLength, setupValuesData.pathDataTimeLength, setupValuesData.pathTurnRateAcceleration, setupValuesData.pathDataMinTime);
                collisionPreditor.SetCollisionHandler(this);
                collisionPreditor.InitCollisionPredictionData(ownVesselName, ownVesselBase.length, setupValuesData.exclusionZoneFront, setupValuesData.exclusionZoneSides, setupValuesData.exclusionZoneBack);
                var exclusionZone = collisionPreditor.GenerateExclusionZone(go);
                HUD.SetExclusionZone(exclusionZone);
                HUD.SetOverlookingCam();
            }

            if (DataLogger.Instance.SimData.TryGetValue(ownVesselName, out List<BaseVessel.DataBundle> ownShipData))
            {
                ownData = new List<BaseVessel.DataBundle>(ownShipData.Count);
                for (int i = 0; i < ownShipData.Count; i++)
                {
                    //transforming global data to local for the collision predictor to use
                    var eta = new BaseVessel.Eta(ownShipData[i].eta.north - dataPlayer.AnimationDelta.z, ownShipData[i].eta.east - dataPlayer.AnimationDelta.x,
                                                        ownShipData[i].eta.down, ownShipData[i].eta.roll, ownShipData[i].eta.pitch, ownShipData[i].eta.yaw);
                    var timeStamp = ownShipData[i].timeStamp;
                    var linearSpeed = ownShipData[i].linearSpeed;
                    var torqueSpeed = ownShipData[i].angularSpeed;
                    var rudderAngle = ownShipData[i].rudderAngle;
                    var rudderCommand = ownShipData[i].rudderCommand;
                    ownData.Add(new BaseVessel.DataBundle(eta, linearSpeed, torqueSpeed, rudderAngle, rudderCommand, timeStamp));
                }
                if(sceneElements != null)
                {
                    sceneElements.transform.position -= dataPlayer.AnimationDelta;
                }
                if (checkForGrounding) await collisionPreditor.CheckGrounding(ownData);
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

            if (pathPredictionTimer == 0f)
            {
                collisionPreditor.UpdateCollisionData(dataPlayer.Time, ownData);
            }

            if (pathPredictionTimer >= generatedPathUpdateTime)
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
            if (pauseOnCollision)
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
                var exZone = own.transform.Find("ExclusionZone");
                if (exZone != null) exZone.gameObject.SetActive(true);
                var pathPrediction = other.GetComponentInChildren<LineRenderer>();
                if (pathPrediction != null) pathPrediction.enabled = false;

                var meshRendererOwn = own.GetComponentInChildren<MeshRenderer>();
                if (meshRendererOwn != null)
                {
                    Destroy(meshRendererOwn.material);
                    meshRendererOwn.material = new Material(whiteAlphaMat);
                }
                var meshRendererOther = other.GetComponentInChildren<MeshRenderer>();
                if (meshRendererOther != null)
                {
                    Destroy(meshRendererOther.material);
                    meshRendererOther.material = new Material(whiteAlphaMat);
                }
                var lineRendererOwn = own.GetComponentInChildren<LineRenderer>();
                if (lineRendererOwn != null)
                {
                    Destroy(lineRendererOwn.material);
                    lineRendererOwn.material = new Material(redAlphaMat);
                }

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

        public void RaiseGrounding(string vesselName, Vector3 position, Quaternion rotation)
        {
            PopUpWithButton.Instance.PopupText($"Grounding Detected with vessel: {vesselName}.");
            GameObject ownVessel;
            if (!allVesselGameobjects.TryGetValue(ownVesselName, out ownVessel)) return;

            HUD.SetOverlookingCam();
            collisionPoint = new GameObject("collisionPoint");
            var own = Instantiate(ownVessel, position, rotation, collisionPoint.transform);
            var top = own.transform.Find("TOP");
            if(top != null)
            {
                var camtop = top.transform.Find("CamTop");
                if(camtop != null)
                {
                    Camera.main.transform.position = camtop.position;
                    Camera.main.transform.rotation = camtop.rotation;
                }
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
        public void RaiseGrounding(string vesselName, Vector3 position, Quaternion rotation);
    }
}