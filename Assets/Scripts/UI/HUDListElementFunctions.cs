using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HUDListElementFunctions : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField vesselName;
    private GameObject vessel;
    private Transform camTop = null;
    private Transform camTrailing = null;

    public void Init(string _vessselName, GameObject _vessel)
    {
        vessel = _vessel;
        vesselName.text = _vessselName;
    }
    public void SetCameraTop()
    {
        if(camTop == null)
        {
            camTop = vessel.transform.Find("CamTop");
        }
        if (camTop == null) return;

        Camera.main.transform.parent = camTop;
        Camera.main.transform.localPosition = Vector3.zero;
        Camera.main.transform.localRotation = Quaternion.identity;
    }
    public void SetCameraFollower()
    {
        if (camTrailing == null)
        {
            camTrailing = vessel.transform.Find("CamTrailing");
        }
        if (camTrailing == null) return;

        Camera.main.transform.parent = camTrailing;
        Camera.main.transform.localPosition = Vector3.zero;
        Camera.main.transform.localRotation = Quaternion.identity;
    }
}
