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
    public Vector3 AnimationDelta { get; private set; }

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
        foreach (var vessel in localDataDictionary)
        {
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
            var back = v.transform.Find("BACK");
            back.localScale = new Vector3(1f / v.transform.localScale.x, 1f / v.transform.localScale.y,1f /  v.transform.localScale.z);
            camTrailing.transform.parent = back;
            camTrailing.transform.localScale = Vector3.one;
            camTrailing.transform.localPosition = new Vector3(0f, 3f + v.transform.localScale.z / 2f, -10f - v.transform.localScale.z);
            camTrailing.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
            camTrailing.transform.parent = v.transform;
            GameObject camTop = new GameObject("CamTop");
            var top = v.transform.Find("TOP");
            top.localScale = new Vector3(1f / v.transform.localScale.x, 1f / v.transform.localScale.y, 1f / v.transform.localScale.z);
            camTop.transform.parent = top;
            camTop.transform.localPosition = new Vector3(0f, v.transform.localScale.z * 3f, 0f);
            camTop.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            camTop.transform.parent = v.transform;
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
        AnimationDelta = new Vector3((DataLogger.Instance.minEast + DataLogger.Instance.maxEast) / 2f, 
                                    0f, 
                                    (DataLogger.Instance.minNorth + DataLogger.Instance.maxNorth) / 2f);

        //for the animation we transform everything as close to 0;0 as we can
        foreach (var g in vessels.Values)
        {
            g.transform.position -= AnimationDelta;
        }
        foreach (var l in points.Values)
        {
            foreach (var g in l)
            {
                g.transform.position -= AnimationDelta;
            }
        }
        await Task.Yield();
        await Task.Yield();

        HUD.InitHudController(vessels, AnimationDelta);
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
        float lerp = (Time - ((float)currentTimeIndex * stepTime)) / stepTime;
        foreach (var vessel in localDataDictionary)
        {
            Vector3 currentPos = new Vector3(vessel.Value[currentTimeIndex].eta.east, -vessel.Value[currentTimeIndex].eta.down, vessel.Value[currentTimeIndex].eta.north);
            Vector3 nextPos = new Vector3(vessel.Value[currentTimeIndex + 1].eta.east, -vessel.Value[currentTimeIndex + 1].eta.down, vessel.Value[currentTimeIndex + 1].eta.north);
            Quaternion currRot = Quaternion.Euler(vessel.Value[currentTimeIndex].eta.pitch * Mathf.Rad2Deg,
                                                  vessel.Value[currentTimeIndex].eta.yaw * Mathf.Rad2Deg,
                                                  vessel.Value[currentTimeIndex].eta.roll * Mathf.Rad2Deg);
            Quaternion nextRot = Quaternion.Euler(vessel.Value[currentTimeIndex + 1].eta.pitch * Mathf.Rad2Deg,
                                                  vessel.Value[currentTimeIndex + 1].eta.yaw * Mathf.Rad2Deg,
                                                  vessel.Value[currentTimeIndex + 1].eta.roll * Mathf.Rad2Deg);

            if (vessels.TryGetValue(vessel.Key, out GameObject go))
            {
                go.transform.position = Vector3.Lerp(currentPos, nextPos, lerp) - AnimationDelta;
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
        ReplaySpeed = 0f;
        Time = 0f;
        VesselDatabase.Instance.ResetDatabase();
    }

    public void AbortReplay()
    {
        replaying = false;
        Camera.main.transform.parent = null;
        foreach (var vessel in vessels)
        {
            Destroy(vessel.Value);
        }
        foreach (var point in points)
        {
            for(int i = point.Value.Count - 1; i >= 0; i--)
            {
                Destroy(point.Value[i]);
            }
        }
        vessels = null;
        points = null;
        localDataDictionary = null;
        checkPoints = null;

        ResetDataReplay();
    }     
}
