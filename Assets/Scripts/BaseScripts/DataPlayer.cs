using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class DataPlayer : MonoBehaviour
{
    public GameObject vesselPrefab;
    public GameObject pinPoint;
    public float replaySpeed = 1f;
    public float itemScaling = 0.1f;
    public float Time { get; private set; }
    public bool replaying { get; private set; } = false;
    private Dictionary<string, List<BaseVessel.DataBundle>> localDataDictionary;
    private Dictionary<string, List<Vector3>> checkPoints;
    private float stepTime;
    private int numberOfSteps;
    private Dictionary<string, GameObject> vessels;
    private Dictionary<string, List<GameObject>> points;

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
                                       new Vector3(vessel.Value[0].eta.east, vessel.Value[0].eta.down, vessel.Value[0].eta.north) * itemScaling,
                                       Quaternion.AngleAxis(vessel.Value[0].eta.yaw * Mathf.Rad2Deg, Vector3.up));
            v.name = vessel.Key;
            v.transform.localScale = new Vector3(7f, 7f, 50f); //TODO: get this data somehow
            v.transform.localScale *= itemScaling;
            vessels.Add(vessel.Key, v);
            if (checkPoints.TryGetValue(vessel.Key, out List<Vector3> _points))
            {
                var pins = new List<GameObject>();
                foreach (var p in _points)
                {
                    var pin = Instantiate(pinPoint, p * itemScaling, Quaternion.identity);
                    pin.transform.localScale *= itemScaling;
                    pins.Add(pin);
                }
                points.Add(vessel.Key, pins);
            }

            numberOfSteps = (int)Mathf.Min(numberOfSteps, vessel.Value.Count);
        }

        await Task.Yield();
        await Task.Yield();
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
            Vector3 currentPos = new Vector3(vessel.Value[currentTimeIndex].eta.east, vessel.Value[currentTimeIndex].eta.down, vessel.Value[currentTimeIndex].eta.north);
            Vector3 nextPos = new Vector3(vessel.Value[currentTimeIndex + 1].eta.east, vessel.Value[currentTimeIndex + 1].eta.down, vessel.Value[currentTimeIndex + 1].eta.north);
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
                go.transform.position = Vector3.Lerp(currentPos, nextPos, lerp) * itemScaling;
                go.transform.localRotation = Quaternion.Lerp(currRot, nextRot, lerp);
            }

        }
        if (Time / stepTime > numberOfSteps)
        {
            replaying = false;
        }
        Time += UnityEngine.Time.deltaTime * replaySpeed;
    }
}
