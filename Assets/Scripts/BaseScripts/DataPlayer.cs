using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class DataPlayer : MonoBehaviour
{
    public HUDController HUD;
    public GameObject vesselPrefab;
    public GameObject pinPoint;
    public float ReplaySpeed
    {
        get { return replaySpeed; }
        set 
        { 
            replaySpeed = value;
            HUD.SetReplaySpeedText(replaySpeed);
        }
    }
    private float replaySpeed;
    public float Time { get; private set; }
    public bool replaying { get; private set; } = false;
    private Dictionary<string, List<BaseVessel.DataBundle>> localDataDictionary;
    private Dictionary<string, List<Vector3>> checkPoints;
    private float stepTime;
    private int numberOfSteps;
    private Dictionary<string, GameObject> vessels;
    private Dictionary<string, List<GameObject>> points;
    private Vector3 animationDelta;

    [ContextMenu("Replay All Data")]
    public void ReplayAllData()
    {
        ReplayAllDataAsync();
    }

    private async void ReplayAllDataAsync()
    {
        await SetupDataReplayAsync();
        StartAnimation();
    }

    public async Task<Dictionary<string, GameObject>> SetupDataReplayAsync()
    {
        numberOfSteps = int.MaxValue;
        Time = 0f;
        stepTime = DataLogger.Instance.StepTime;
        localDataDictionary = DataLogger.Instance.SimData;
        checkPoints = DataLogger.Instance.CheckPoints;
        vessels = new Dictionary<string, GameObject>();
        points = new Dictionary<string, List<GameObject>>();
        float maxX = 0f;
        float maxZ = 0f;
        float minX = 0f;
        float minZ = 0f;
        foreach (var vessel in localDataDictionary)
        {
            minX = Mathf.Min(minX, vessel.Value[0].eta.east);
            maxX = Mathf.Max(maxX, vessel.Value[0].eta.east);
            minZ = Mathf.Min(minZ, vessel.Value[0].eta.north);
            maxZ = Mathf.Max(maxZ, vessel.Value[0].eta.north);
            GameObject v = Instantiate(vesselPrefab,
                                       new Vector3(vessel.Value[0].eta.east, -vessel.Value[0].eta.down, vessel.Value[0].eta.north),
                                       Quaternion.AngleAxis(vessel.Value[0].eta.yaw * Mathf.Rad2Deg, Vector3.up));
            v.name = vessel.Key;
            v.transform.localScale = new Vector3(7f, 7f, 50f); //TODO: get this data somehow
            var trailRenderer = v.GetComponentInChildren<TrailRenderer>();
            if (trailRenderer != null)
            {
                trailRenderer.startWidth = v.transform.localScale.x / 2f;
                trailRenderer.endWidth = v.transform.localScale.x * 1.2f;
            }
            GameObject camTrailing = new GameObject("CamTrailing");
            camTrailing.transform.parent = v.transform;
            camTrailing.transform.localPosition = new Vector3(0f, v.transform.localScale.y / 2f + v.transform.localScale.y / 10f, v.transform.localScale.z / 2f + v.transform.localScale.z / 10f);
            camTrailing.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
            GameObject camTop = new GameObject("CamTop");
            camTop.transform.parent = v.transform;
            camTop.transform.localPosition = new Vector3(0f, v.transform.localScale.z * 2f, 0f);
            camTop.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            vessels.Add(vessel.Key, v);

            if (checkPoints.TryGetValue(vessel.Key, out List<Vector3> _points))
            {
                var pins = new List<GameObject>();
                foreach (var p in _points)
                {
                    var pin = Instantiate(pinPoint, p, Quaternion.identity);
                    pins.Add(pin);
                }
                points.Add(vessel.Key, pins);
            }

            numberOfSteps = (int)Mathf.Min(numberOfSteps, vessel.Value.Count);
        }
        animationDelta = new Vector3((minX + maxX) / 2f, 0f, (minZ + maxZ) / 2f);

        //for the animation we transform everything as close to 0;0 as we can
        foreach (var g in vessels.Values)
        {
            g.transform.position -= animationDelta;
        }
        foreach (var l in points.Values)
        {
            foreach (var g in l)
            {
                g.transform.position -= animationDelta;
            }
        }
        await Task.Yield();
        await Task.Yield();

        HUD.InitHudController(vessels, animationDelta);
        HUD.SetReplaySpeedText(ReplaySpeed);
        return vessels;
    }

    public void StartAnimation()
    {
        replaying = true;
    }

    private void Update()
    {
        StepAnimation();
    }

    public void StepAnimation()
    {
        if (!replaying) return;
        int currentTimeIndex = (int)Mathf.Clamp(Mathf.Floor(Time / stepTime), 0f, numberOfSteps - 2); //we dont want to index out of bounds, and we are lerping to the next
        float lerp = stepTime / (Time / currentTimeIndex);
        if (float.IsNaN(lerp)) lerp = 0.5f;
        foreach (var vessel in localDataDictionary)
        {
            Vector3 currentPos = new Vector3(vessel.Value[currentTimeIndex].eta.east, -vessel.Value[currentTimeIndex].eta.down, vessel.Value[currentTimeIndex].eta.north);
            Vector3 nextPos = new Vector3(vessel.Value[currentTimeIndex + 1].eta.east, -vessel.Value[currentTimeIndex + 1].eta.down, vessel.Value[currentTimeIndex + 1].eta.north);
            Quaternion currRot = Quaternion.Euler(vessel.Value[currentTimeIndex].eta.pitch * Mathf.Rad2Deg,
                                                  vessel.Value[currentTimeIndex].eta.yaw * Mathf.Rad2Deg,
                                                  vessel.Value[currentTimeIndex].eta.roll * Mathf.Rad2Deg);
            //Quaternion.AngleAxis(vessel.Value[currentTimeIndex].eta.yaw * Mathf.Rad2Deg, Vector3.up);
            Quaternion nextRot = Quaternion.Euler(vessel.Value[currentTimeIndex + 1].eta.pitch * Mathf.Rad2Deg,
                                                  vessel.Value[currentTimeIndex + 1].eta.yaw * Mathf.Rad2Deg,
                                                  vessel.Value[currentTimeIndex + 1].eta.roll * Mathf.Rad2Deg);
            //Quaternion.AngleAxis(vessel.Value[currentTimeIndex + 1].eta.yaw * Mathf.Rad2Deg, Vector3.up);

            if (vessels.TryGetValue(vessel.Key, out GameObject go))
            {
                go.transform.position = Vector3.Lerp(currentPos, nextPos, lerp) - animationDelta;
                go.transform.localRotation = Quaternion.Lerp(currRot, nextRot, lerp);
            }

        }
        if (Time / stepTime > numberOfSteps)
        {
            replaying = false;
        }
        Time += UnityEngine.Time.deltaTime * ReplaySpeed;
    }

    public void PauseReplay()
    {
        ReplaySpeed = 0f;
    }

    public void PlayReplay()
    {
        ReplaySpeed = 1f;
    }

    public void IncreaseReplaySpeed()
    {
        ReplaySpeed += 0.5f;
    }

    public void ResetDataReplay()
    {
        Time = 0f;
        VesselDatabase.Instance.ResetDatabase();
    }
}
