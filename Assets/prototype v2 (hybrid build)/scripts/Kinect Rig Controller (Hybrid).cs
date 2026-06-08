using UnityEngine;
using System.Collections.Generic;

public class KinectRigController : MonoBehaviour
{
    public enum KinectVersion { KinectV1, KinectV2 }

    [Header("Hardware Selection")]
    public KinectVersion hardwareVersion = KinectVersion.KinectV2;
    
    [Header("Rig Core Roots")]
    public Transform animatorRoot;
    public Transform hipsBone;

    [Header("Core Joint Mapping (Shared)")]
    public Transform head;
    public Transform neck;
    public Transform spine;
    [Tooltip("Required for Kinect V2, optional/estimated for V1")]
    public Transform spineShoulder; 

    [Header("Left Arm")]
    public Transform shoulderLeft;
    public Transform elbowLeft;
    public Transform handLeft;

    [Header("Right Arm")]
    public Transform shoulderRight;
    public Transform elbowRight;
    public Transform handRight;

    [Header("Left Leg")]
    public Transform hipLeft;
    public Transform kneeLeft;
    public Transform footLeft;

    [Header("Right Leg")]
    public Transform hipRight;
    public Transform kneeRight;
    public Transform footRight;

    [Header("Smoothing Options")]
    [Range(0f, 1f)]
    public float movementSmoothing = 0.3f;

    // Internal data mimicking standard Kinect Joint Type structures safely
    private Dictionary<int, Transform> jointMap = new Dictionary<int, Transform>();

    void Start()
    {
        InitializeJointMapping();
        SetupPhysicsSafety();
    }

    void InitializeJointMapping()
    {
        jointMap.Clear();

        if (hardwareVersion == KinectVersion.KinectV2)
        {
            // Kinect V2 Joint Index Specifications (Standard SDK)
            jointMap[0]  = hipsBone;         // SpineBase
            jointMap[1]  = spine;            // SpineMid
            jointMap[2]  = neck;             // Neck
            jointMap[3]  = head;             // Head
            jointMap[4]  = shoulderLeft;     // ShoulderLeft
            jointMap[5]  = elbowLeft;        // ElbowLeft
            jointMap[6]  = handLeft;         // WristLeft (Mapped to hand)
            jointMap[8]  = shoulderRight;    // ShoulderRight
            jointMap[9]  = elbowRight;       // ElbowRight
            jointMap[10] = handRight;        // WristRight (Mapped to hand)
            jointMap[12] = hipLeft;          // HipLeft
            jointMap[13] = kneeLeft;         // KneeLeft
            jointMap[14] = footLeft;         // AnkleLeft
            jointMap[16] = hipRight;         // HipRight
            jointMap[17] = kneeRight;        // KneeRight
            jointMap[18] = footRight;        // AnkleRight
            jointMap[20] = spineShoulder;    // SpineShoulder
        }
        else
        {
            // Kinect V1 Joint Index Specifications (Standard SDK)
            jointMap[0]  = hipsBone;         // HipCenter
            jointMap[1]  = spine;            // Spine
            jointMap[2]  = neck;             // Neck
            jointMap[3]  = head;             // Head
            jointMap[4]  = shoulderLeft;     // ShoulderLeft
            jointMap[5]  = elbowLeft;        // ElbowLeft
            jointMap[6]  = handLeft;         // WristLeft
            jointMap[8]  = shoulderRight;    // ShoulderRight
            jointMap[9]  = elbowRight;       // ElbowRight
            jointMap[10] = handRight;        // WristRight
            jointMap[12] = hipLeft;          // HipLeft
            jointMap[13] = kneeLeft;         // KneeLeft
            jointMap[14] = footLeft;         // AnkleLeft
            jointMap[16] = hipRight;         // HipRight
            jointMap[17] = kneeRight;        // KneeRight
            jointMap[18] = footRight;        // AnkleRight
        }
    }

    // Automatically enforces the non-destructive kinematic rule we used earlier
    void SetupPhysicsSafety()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    // Call this function from your main Kinect SDK Data Loop Manager 
    // Pass the Joint ID and the tracked Vector3 position sent by the sensor
    public void UpdateJointPosition(int jointID, Vector3 rawKinectPosition)
    {
        if (jointMap.TryGetValue(jointID, out Transform boneTransform) && boneTransform != null)
        {
            // Smoothly interpolate positions to cut down on sensor jitter
            Vector3 targetPosition = transform.TransformPoint(rawKinectPosition);
            boneTransform.position = Vector3.Lerp(boneTransform.position, targetPosition, 1f - movementSmoothing);
        }
    }

    // Overload variant to update raw tracking rotations if using full skeletal tracking orientations
    public void UpdateJointRotation(int jointID, Quaternion rawKinectRotation)
    {
        if (jointMap.TryGetValue(jointID, out Transform boneTransform) && boneTransform != null)
        {
            boneTransform.localRotation = Quaternion.Slerp(boneTransform.localRotation, rawKinectRotation, 1f - movementSmoothing);
        }
    }
}