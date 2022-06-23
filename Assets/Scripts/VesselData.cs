using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class VesselData : MonoBehaviour
{
    [SerializeField]
    private StartPoint startPoint;
    private VesselDataPackage dataPackage = null;
    public VesselDataPackage DataPackage { 
        get 
        {
            if (dataPackage == null)
            {
                dataPackage = new VesselDataPackage(startPoint);
            }
            return dataPackage;
        }
        private set { dataPackage = value; } 
    }



    [System.Serializable]
    public class VesselDataPackage
    {
        public BaseVessel vessel;
        public StartPoint startPoint;

        public VesselDataPackage() { }
        public VesselDataPackage(StartPoint sp)
        {
            startPoint = sp;
        }
    }

    public void MoveVessel(bool up)
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

    public void DestroyVesselData()
    {
        Destroy(gameObject);
    }
}
