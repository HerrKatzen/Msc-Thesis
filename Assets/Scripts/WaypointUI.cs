using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WaypointUI : MonoBehaviour
{
    public TMP_InputField nedN;
    public TMP_InputField nedE;

    public void Delete()
    {
        DestroyImmediate(gameObject);
    }

    public void MoveWaypointUP(bool up)
    {
        if (up)
        {
            transform.SetSiblingIndex(Mathf.Max(0, transform.GetSiblingIndex() - 1));
        }
        else
        {
            transform.SetSiblingIndex(Mathf.Min(transform.parent.childCount - 1, transform.GetSiblingIndex() + 1));
        }
    }
}
