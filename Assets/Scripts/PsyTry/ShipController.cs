using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipController : MonoBehaviour
{
    /// <summary>
    /// Drag coefficient of a plane. We are calculating several planes andd adding them together, and scaling them by their angle.
    /// This is not perfect, but currently the best guess. The more we angle the plane, the smaller this value gets linearly.
    /// </summary>

    public Transform rudder;
    public Rigidbody rb;
    public Collider collider;

    public float dragCoeficient = 1.17f;
    public float massDensity = 1f;

    private Vector3 waterFlowDirection = Vector3.forward;
    private float flowSpeed  = 0f;

    public float PowerSetting { get; private set; } = 0f;
    [SerializeField]
    private float maxPower = 50000f;
    public float Power { get; private set; } = 0f;
    [SerializeField]
    private float maxPowerSetting = 5f;
    [SerializeField]
    private float minPowerSetting = -2f;
    [SerializeField]
    private float powerChangeSpeed = 1f;

    public float RudderSetting { get; private set; } = 0f;
    [SerializeField]
    private float rudderMaxAngle = 45f;
    [SerializeField]
    private float ruderChangeSpeed = 3f;

    private Vector3 FinalForce = Vector3.forward;
    private Vector3 rotorForce = Vector3.forward;


    public List<ShipResistance> areas;

    private void FixedUpdate()
    {
        //setting water speed and direction
        waterFlowDirection = rb.velocity.normalized;
        flowSpeed = rb.velocity.magnitude;

        //calculating drag
        float dragCoefTimesArea = 0f;
        foreach (var area in areas)
        {
            dragCoefTimesArea += area.GetResistanceValue(waterFlowDirection);
        }
        dragCoefTimesArea *= dragCoeficient;
        var dragForceMagnitude = (dragCoefTimesArea * massDensity * flowSpeed) / 2f;
        Vector3 dragForce = -1f * waterFlowDirection * dragForceMagnitude;
        
        //calculating rotor speed and direction
        Power = (maxPower / maxPowerSetting) * PowerSetting;
        rotorForce = transform.forward * Power;

        rudder.localRotation = Quaternion.Euler(0f, RudderSetting, 0f);
        rotorForce = Vector3.RotateTowards(rotorForce, rudder.forward, 180f /Mathf.PI * 0.01f * (Mathf.Clamp(rb.velocity.magnitude, 0f, 10f) / 10f), 0f);

        //applying force
        FinalForce = dragForce + rotorForce;
        rb.AddForceAtPosition(FinalForce, rudder.position);
        //rb.AddForce(finalForce, ForceMode.Impulse);
        //rb.AddTorque((new Vector3(0f, -RudderSetting, 0f)) * Mathf.Clamp(rb.velocity.magnitude, -1f, 1f), ForceMode.Impulse);

        Debug.Log("Power: " + Power + " ; Power Setting: " + PowerSetting + " ; Drag force: " + dragForce + " ; Rotor Force: " + rotorForce + " ; Final Force: " + FinalForce);
    }

    private void OnDrawGizmos()
    {
        DrawArrow.ForGizmo(rudder.position, rotorForce, Color.red);
        DrawArrow.ForGizmo(rudder.position, FinalForce, Color.green);
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.UpArrow))
        {
            PowerSetting += Time.deltaTime * powerChangeSpeed;
            PowerSetting = Mathf.Clamp(PowerSetting, minPowerSetting, maxPowerSetting);
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            PowerSetting -= Time.deltaTime * powerChangeSpeed;
            PowerSetting = Mathf.Clamp(PowerSetting, minPowerSetting, maxPowerSetting);
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            RudderSetting -= Time.deltaTime * ruderChangeSpeed;
            RudderSetting = Mathf.Clamp(RudderSetting, -rudderMaxAngle, rudderMaxAngle);
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            RudderSetting += Time.deltaTime * ruderChangeSpeed;
            RudderSetting = Mathf.Clamp(RudderSetting, -rudderMaxAngle, rudderMaxAngle);
        }
    }
}
