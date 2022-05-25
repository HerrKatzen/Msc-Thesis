using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuoyancyHandler : MonoBehaviour
{
    [SerializeField]
    private float draft = 1f;
    [SerializeField]
    private float dragMultiplier = 1f;
    [SerializeField]
    private float angularDragMultiplier = 1f;
    [SerializeField]
    private Transform centerOfMass;
    [SerializeField]
    private List<Transform> buoyancyPoints = new List<Transform>();
    private Rigidbody rb;
    private float mass;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        mass = rb.mass;
        if (centerOfMass == null) centerOfMass = transform;
    }


    void FixedUpdate()
    {
        var drag = 0.1f;
        foreach (var bp in buoyancyPoints)
        {
            if (bp.position.y < 0f)
            {
                var force = Vector3.up * (mass / (float)buoyancyPoints.Count) * (bp.position.y * (Physics.gravity.y / draft));
                rb.AddForceAtPosition(force, bp.position);
                drag += (bp.position.y * -1f) / (float)buoyancyPoints.Count * (1f / draft);
            }
        }
        rb.drag = drag * dragMultiplier;
        rb.angularDrag = drag * angularDragMultiplier;
        rb.centerOfMass = centerOfMass.localPosition;
    }
}
