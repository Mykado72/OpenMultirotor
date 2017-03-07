﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stab : MonoBehaviour {


    public float stability = 0.1f;
    public float speed = 2.0f;
    public Rigidbody rgChassi;
    public Vector3 vpredictedUp;
    public float anglePitch;
    public float angleRoll;
    // Update is called once per frame
    void FixedUpdate()
    {
        Quaternion qpredictedUp = Quaternion.AngleAxis(rgChassi.angularVelocity.magnitude * Mathf.Rad2Deg, rgChassi.angularVelocity); 
        vpredictedUp = qpredictedUp.eulerAngles;
        // Debug.Log(vpredictedUp);
        // Vector3 torqueVector = Vector3.Cross(predictedUp, Vector3.up);
        // rgChassi.AddTorque(torqueVector * speed * speed);
        anglePitch = PosNegAngleY(Vector3.up, transform.forward);  //     Vector3.Angle(transform.forward, Vector3.up);
        angleRoll = PosNegAngleX(Vector3.up, transform.right);  // Vector3.Angle(transform.right, Vector3.up);
        //transform.up //+y axis
        //transform.forward //+z axis
        // transform.right //+x axis
    }

    public float PosNegAngleY(Vector3 a1, Vector3 a2)
    {
        float angle = Vector3.Angle(a1, a2);
        Vector3 cross = Vector3.Cross(a1, a2);
        if (cross.y < 0) angle = -angle;
        return angle;
    }

    public float PosNegAngleX(Vector3 a1, Vector3 a2)
    {
        float angle = Vector3.Angle(a1, a2);
        Vector3 cross = Vector3.Cross(a1, a2);
        if (cross.x < 0) angle = -angle;
        return angle;
    }

}
