using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI replaySpeedText;
    [SerializeField]
    private Transform scrollViewParent;
    [SerializeField]
    private GameObject scrollElementPrefab;
    [SerializeField]
    private Button collisionCamButton;

    private GameObject exclusionZone;
    private GameObject expectedPathLineHolder;
    private GameObject actualPathLineHolder;
    private Transform overLookingCam;
    private Transform collisionCam;

    public void InitHudController(Dictionary<string, GameObject> vessels, Vector3 animationDelta)
    {
        foreach (var vessel in vessels)
        {
            var scrollElement = Instantiate(scrollElementPrefab, scrollViewParent);
            scrollElement.GetComponent<HUDListElementFunctions>().Init(vessel.Key, vessel.Value);
        }

        expectedPathLineHolder = new GameObject("ExpectedPathLinesHolder");
        expectedPathLineHolder.transform.parent = transform;
        foreach (var points in DataLogger.Instance.CheckPoints)
       {
            if (points.Value == null || points.Value.Count == 0) continue;

            var path = new GameObject("path");
            path.transform.parent = expectedPathLineHolder.transform;
            var lineRenderer = path.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = 1f;
            lineRenderer.endWidth = 1f;
            lineRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            lineRenderer.material.color = Color.yellow;
            lineRenderer.positionCount = points.Value.Count + 1;
            if(DataLogger.Instance.SimData.TryGetValue(points.Key, out List<BaseVessel.DataBundle> dataBundleList))
            {
                lineRenderer.SetPosition(0, new Vector3(dataBundleList[0].eta.east, 1f, dataBundleList[0].eta.north) - animationDelta);
            }
            else
            {
                lineRenderer.SetPosition(0, points.Value[0] + new Vector3(0f, 1f, 0f) - animationDelta);
            }
            for (int i = 1; i < points.Value.Count + 1; i++)
            {
                lineRenderer.SetPosition(i, points.Value[i - 1] + new Vector3(0f, 1f, 0f) - animationDelta);
            }
        }

        actualPathLineHolder = new GameObject("actualPathLineHolder");
        actualPathLineHolder.transform.parent = transform;
        foreach (var vesselPositions in DataLogger.Instance.SimData.Values)
        {
            var path = new GameObject("path");
            path.transform.parent = actualPathLineHolder.transform;
            var lineRenderer = path.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = 1f;
            lineRenderer.endWidth = 1f;
            lineRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            lineRenderer.material.color = Color.white;
            lineRenderer.positionCount = vesselPositions.Count;
            for (int i = 0; i < vesselPositions.Count; i++)
            {
                lineRenderer.SetPosition(i, new Vector3(vesselPositions[i].eta.east, 1f, vesselPositions[i].eta.north) - animationDelta);
            }
            lineRenderer.Simplify(0.3f);
        }

        float y = Mathf.Max(((DataLogger.Instance.maxNorth - DataLogger.Instance.minNorth) / 2f) / Mathf.Tan(Camera.main.fieldOfView / 2f * Mathf.Deg2Rad),
                             ((DataLogger.Instance.maxEast - DataLogger.Instance.minEast) / 2f) / Mathf.Tan((Camera.main.fieldOfView + 10f) / 2f * Mathf.Deg2Rad));
        var overLookingCamObject = new GameObject("OverLookingCamera");
        overLookingCam = overLookingCamObject.transform;
        overLookingCam.position = new Vector3(0f, y * 1.1f, 0f);
        overLookingCam.rotation = Quaternion.Euler(90f, 0f, 0f);

        var collisionCamObject = new GameObject("CollisionCamera");
        collisionCam = collisionCamObject.transform;
    }

    public void SetExclusionZone(GameObject exZone)
    {
        exclusionZone = exZone;
    }

    public void SetShowExpectedPaths(bool show)
    {
        expectedPathLineHolder.SetActive(show);
    }
    public void SetShowActualPaths(bool show)
    {
        actualPathLineHolder.SetActive(show);
    }
    public void SetShowPredictedPaths(bool show)
    {
        VesselDatabase.Instance.drawPredictedPaths = show;
    }
    public void SetShowExclusionZone(bool show)
    {
        exclusionZone.SetActive(show);
    }

    public void SetOverlookingCam()
    {
        Camera.main.transform.parent = overLookingCam;
        Camera.main.transform.localPosition = Vector3.zero;
        Camera.main.transform.localRotation = Quaternion.identity;
    }

    public void SetReplaySpeedText(float value)
    {
        replaySpeedText.text = "Playback Speed: " + value;
    }

    public void AbortPlaybackAndDelete()
    {
        Camera.main.transform.parent = null;
        Destroy(actualPathLineHolder);
        Destroy(expectedPathLineHolder);
        for(int i = scrollViewParent.childCount - 1; i >= 0; i--)
        {
            Destroy(scrollViewParent.GetChild(i).gameObject);
        }
        Destroy(overLookingCam.gameObject);
        collisionCamButton.interactable = false;
    }

    public void SetCollisionCam()
    {
        Camera.main.transform.parent = collisionCam;
        Camera.main.transform.localPosition = Vector3.zero;
        Camera.main.transform.localRotation = Quaternion.identity;
    }

    internal void SetupCollisionCam(GameObject own, GameObject other)
    {
        var ownTop = own.transform.Find("CamTop");
        var otherTop = other.transform.Find("CamTop");
        float distance = Vector3.Distance(ownTop.position, otherTop.position);
        float y = Mathf.Sqrt(distance * distance - (distance / 2f) * (distance / 2f));
        Vector3 midPoint = Vector3.Lerp(ownTop.position, otherTop.position, 0.5f);

        collisionCam.transform.position = new Vector3(midPoint.x, midPoint.y + y, midPoint.z);
        collisionCam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        collisionCamButton.interactable = true;
    }

    internal void DisableCollisionCam()
    {
        collisionCamButton.interactable = false;
    }
}
