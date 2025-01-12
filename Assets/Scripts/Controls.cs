﻿using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

public class Controls : MonoBehaviour {

    public GameObject fakeGo;
    public Transform cg;
    public bool AIControl;
    private float delta;
    public PIDSet pidSet;
    public PIDSet AIControlPID;
    public int rcMode;
    public enum FlightModes { Angle, Horizon, Accro };
    // public Dropdown flightModeDropdown;
    public FlightModes flightMode;
    public int pitchAngleMax = 45;
    public int rollAngleMax = 45;
    public float stabspeed = 0.25f;
    public bool joystick;
    public AxisMap axisMap;
    public Mapping mapping;
    public float throttle;
    public float minimalSpeed;
    public float minimalRotationSpeed;
    public float throttleRate;
    public float ratioThrottleCmd;
    public float rollRate;
    public float rollExpo;
    public float pitchRate;
    public float pitchExpo;
    public float yawSpeed;
    public float yawRate;
    public float yawExpo;
    public float accroRate;
    public MotorSet motorSet;
    public Vector3 consignVector;
    public Vector3 AngularConsignVector;
    public Vector3 ActualRotationVector;
    public float cmdRoll;
    public float cmdPitch;
    public float cmdYaw;
    public float convertedThrottle;
    public float desiredSpeed;
    public Rigidbody rgChassi;
    public Rigidbody motorFrontLeft;
    private float motorCmdFrontLeft;
    public Rigidbody motorFrontRight;
    private float motorCmdFrontRight;
    public Rigidbody motorRearLeft;
    private float motorCmdRearLeft;
    public Rigidbody motorRearRight;
    private float motorCmdRearRight;
    public Transform spawnTransform;
//     public Quaternion spawnRotation;
    public Button mapbutton;
    public WaypointSystem waypointSystem;
    public int waypointnb;
    public Transform targetWaypoint;
    public Vector3 RelativeWaypointPosition;
    public Vector3 RelativeWaypointRotation;
    public Quaternion QRelativeWaypointRotation;
    public Quaternion rotationDelta;
    public float rotationDeltaY;
    public Vector3 rotationDeltaEuler;

    // Use this for initialization
    void  Awake () {
        fakeGo = new GameObject();
        waypointnb = 0;
        targetWaypoint= waypointSystem.waypoints[waypointnb];
    }

    void Start()
    {   
        if (axisMap.joystickDetected==true)
        {
            mapping = axisMap.mapping;
            joystick = true;
        }
        else
        {
            joystick = false;
        }       
        rgChassi.transform.gameObject.SetActive(false);
        rgChassi.transform.gameObject.SetActive(true);
        rgChassi.transform.position = spawnTransform.position;
        rgChassi.transform.rotation = spawnTransform.rotation;
        rgChassi.isKinematic = true; // clear forces
        rgChassi.isKinematic = false; 
        rgChassi.velocity = Vector3.zero;
        rgChassi.maxAngularVelocity = 10; // Mathf.Infinity;
        rgChassi.angularVelocity = Vector3.zero;
        rgChassi.ResetInertiaTensor();
        rgChassi.centerOfMass = cg.localPosition; // descendre le centre de gravité rend plus mou

    }

    // Update is called once per frame
    void Update () {
        delta = Time.deltaTime;       
        GetControls();
        convertedThrottle = (1 + throttle) / 2 ;
        // desiredSpeed = (minimalSpeed + throttle) * throttleRate;
        desiredSpeed = minimalSpeed+ (throttleRate*convertedThrottle); // + (motorSet.motorSet[0].motActualVmax*0.5f)   * convertedThrottle;
        //FlightMode();
        SendCmdToMotors();
    }
    
    void FixedUpdate()
    {
        RotationCorrection();
        FlightMode();
        ClampVelocity(30f);
        rgChassi.AddRelativeForce(new Vector3(0, desiredSpeed* ratioThrottleCmd, 0), ForceMode.Force);
    }

    void RotationCorrection()
    {
        ActualRotationVector = rgChassi.rotation.eulerAngles;
        if (ActualRotationVector.x >= 180)
            ActualRotationVector.x = -(360 - ActualRotationVector.x);
        if (ActualRotationVector.y >= 180)
            ActualRotationVector.y = -(360 - ActualRotationVector.y);
        if (ActualRotationVector.z >= 180)
            ActualRotationVector.z = -(360 - ActualRotationVector.z);
    }

    void GetControls()
    {
        Transform rgChassiTransform = rgChassi.transform;
        RelativeWaypointPosition = rgChassiTransform.InverseTransformPoint(targetWaypoint.position);
        
        fakeGo.transform.position = rgChassi.transform.position;
        fakeGo.transform.rotation = rgChassi.transform.rotation;
        Vector3 targetDir = targetWaypoint.position - fakeGo.transform.position;
        targetDir = targetDir.normalized;
        targetDir.y = fakeGo.transform.position.y;
        fakeGo.transform.rotation = Quaternion.LookRotation(targetDir);
        rotationDeltaY = Vector3.Cross(rgChassiTransform.transform.forward, targetDir).y; 

        if (AIControl == true)
        {
            consignVector.y = Mathf.Clamp(AIControlPID.yawPID.Update(rotationDeltaY,0, stabspeed * delta), -1f, +1f);
            consignVector.z = Mathf.Clamp(AIControlPID.rollPID.Update(rgChassiTransform.position.x + RelativeWaypointPosition.x, rgChassiTransform.position.x, stabspeed * delta), -1f, +1f); // consignVector.y*0.5f;            
            consignVector.x = Mathf.Clamp(AIControlPID.pitchPID.Update(rgChassiTransform.position.z + RelativeWaypointPosition.z, rgChassiTransform.position.z, stabspeed * delta), -1f, +1f);
            throttle = Mathf.Clamp(AIControlPID.throttlePID.Update(targetWaypoint.position.y, rgChassi.position.y, stabspeed * delta), -1f, +1f);
        }
        else // c'est un joueur
        {
            if (axisMap.joystickDetected == true)
            {
                mapping = axisMap.mapping;
                joystick = true;
                mapbutton.interactable = true;
            }
            else 
            {
                joystick = false;
                mapbutton.interactable = false;
            }
            if (joystick)
            {
                if (mapping.rollAxisInversor == true)
                    consignVector.z = -Expo(Input.GetAxis("axis" + mapping.rollAxisNb.ToString()), rollExpo, 0.0002f);    // Roll
                else
                    consignVector.z = Expo(Input.GetAxis("axis" + mapping.rollAxisNb.ToString()), rollExpo, 0.0002f);    // Roll
                if (mapping.yawAxisInversor == true)
                    consignVector.y = -Expo(Input.GetAxis("axis" + mapping.yawAxisNb.ToString()), yawExpo, 0.0002f);     // Yaw
                else
                    consignVector.y = Expo(Input.GetAxis("axis" + mapping.yawAxisNb.ToString()), yawExpo, 0.0002f);     // Yaw
                if (mapping.pitchAxisInversor == true)
                    consignVector.x = Expo(Input.GetAxis("axis" + mapping.pitchAxisNb.ToString()), pitchExpo, 0.0002f); // Pitch 
                else
                    consignVector.x = -Expo(Input.GetAxis("axis" + mapping.pitchAxisNb.ToString()), pitchExpo, 0.0002f); // Pitch 
                if (mapping.throttleAxisInversor == true)
                    throttle = Input.GetAxis("axis" + mapping.throttleAxisNb.ToString());
                else
                    throttle = -Input.GetAxis("axis" + mapping.throttleAxisNb.ToString());
            }
            else
            {
                consignVector.z = Expo(Input.GetAxis("KeyRoll"), rollExpo, 0.0002f);
                consignVector.y = Expo(Input.GetAxis("KeyYaw"), yawExpo, 0.0002f);
                consignVector.x = Expo(Input.GetAxis("KeyPitch(Mode1)-Trottle(Mode2)"), pitchExpo, 0.0002f);
                throttle = Input.GetAxis("KeyTrottle(Mode1)-Pitch(Mode2)");
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }
        }
    }
    void FlightMode()
    {
        float fixeddelta = Time.fixedDeltaTime;
        switch (flightMode)
        // switch (flightMode.value)
        {
            case FlightModes.Angle: //  0:  // angle
                AngularConsignVector.x = consignVector.x * pitchAngleMax;
                AngularConsignVector.z = consignVector.z * rollAngleMax;
                AngularConsignVector.y = consignVector.y * yawSpeed;
                cmdPitch = pidSet.pitchPID.Update(AngularConsignVector.x, ActualRotationVector.x, stabspeed * fixeddelta);
                cmdRoll = pidSet.rollPID.Update(AngularConsignVector.z, -ActualRotationVector.z, stabspeed * fixeddelta);
                cmdYaw = pidSet.yawPID.Update(AngularConsignVector.y + ActualRotationVector.y, ActualRotationVector.y, stabspeed * fixeddelta);
                break;
            case FlightModes.Horizon: //2: // horizon
                cmdRoll = consignVector.z * rollRate * accroRate;
                cmdPitch = consignVector.x * pitchRate * accroRate;
                cmdYaw = consignVector.y * yawRate * accroRate;
                break;
            case FlightModes.Accro: // 1: // acro
                AngularConsignVector.x = consignVector.x * pitchAngleMax;
                AngularConsignVector.z = consignVector.z * rollAngleMax;
                AngularConsignVector.y = consignVector.y * yawRate;
                cmdPitch = pidSet.pitchPID.Update(AngularConsignVector.x, 0, stabspeed * fixeddelta);
                cmdRoll = pidSet.rollPID.Update(AngularConsignVector.z, 0, stabspeed * fixeddelta);
                cmdYaw = pidSet.yawPID.Update(AngularConsignVector.y, 0, stabspeed * fixeddelta);
                break;
            default:
                AngularConsignVector.x = consignVector.x * pitchAngleMax;
                AngularConsignVector.z = consignVector.z * rollAngleMax;
                AngularConsignVector.y = consignVector.y * yawRate;
                cmdPitch = pidSet.pitchPID.Update(AngularConsignVector.x, ActualRotationVector.x, stabspeed * fixeddelta);
                cmdRoll = pidSet.rollPID.Update(AngularConsignVector.z, -ActualRotationVector.z, stabspeed * fixeddelta);
                cmdYaw = pidSet.yawPID.Update(AngularConsignVector.y + ActualRotationVector.y, ActualRotationVector.y, stabspeed * fixeddelta);
                break;
        }
    }
    void SendCmdToMotors()
    {
        motorCmdFrontLeft = (-cmdRoll + cmdPitch + cmdYaw) * throttleRate;
        motorCmdFrontRight = (cmdRoll + cmdPitch - cmdYaw) * throttleRate;
        motorCmdRearLeft = (-cmdRoll - cmdPitch - cmdYaw) * throttleRate;
        motorCmdRearRight = (cmdRoll - cmdPitch + cmdYaw) * throttleRate;
        motorSet.motorSet[0].hoverSpeed = desiredSpeed; //* (1/ratioThrottleCmd);
        motorSet.motorSet[1].hoverSpeed = desiredSpeed; //* (1/ratioThrottleCmd);
        motorSet.motorSet[2].hoverSpeed = desiredSpeed; //* (1/ratioThrottleCmd);
        motorSet.motorSet[3].hoverSpeed = desiredSpeed; //* (1/ratioThrottleCmd); 
        motorSet.motorSet[0].motCmdSpeed = motorCmdFrontLeft;// + motorCmdFrontLeft* motorSet.motorSet[0].motActualAcc*0.5f;// *  throttleRate;
        motorSet.motorSet[1].motCmdSpeed = motorCmdFrontRight;// + motorCmdFrontRight* motorSet.motorSet[1].motActualAcc*0.5f; // * throttleRate;
        motorSet.motorSet[2].motCmdSpeed = motorCmdRearLeft;// + motorCmdRearLeft* motorSet.motorSet[2].motActualAcc*0.5f; // * throttleRate;
        motorSet.motorSet[3].motCmdSpeed = motorCmdRearRight;// + motorCmdRearRight* motorSet.motorSet[3].motActualAcc*0.5f; // * throttleRate;     
    }
    void ClampVelocity(float speed)
    {
        rgChassi.velocity = Vector3.ClampMagnitude(rgChassi.velocity, speed);
    }
    
    public float Expo(float value, float expo, float deadband)
    {  // expo allant de 0 à 1 
        if (Mathf.Abs(value) > deadband)
            return value*Mathf.Pow(Mathf.Abs(value), expo);
        else
            return 0f;
    }
    public static float FindDegree(float x, float y)
    {
        float value = (float)((Mathf.Atan2(x, y) / Math.PI) * 180f);
        if (value < 0) value += 360f;
        return value;
    }


}