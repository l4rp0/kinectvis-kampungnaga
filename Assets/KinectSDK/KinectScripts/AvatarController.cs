using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Text; 

[RequireComponent(typeof(Animator))]
public class AvatarController : MonoBehaviour
{   
    // --- NEW ADDITION ---
    [Tooltip("If checked, the avatar will stay in its last tracked position when the user is lost instead of resetting.")]
    public bool keepPositionOnLoss = true; 
    // --------------------

    public bool mirroredMovement = false;
    public bool verticalMovement = false;
    protected int moveRate = 1;
    public float smoothFactor = 5f;
    public bool offsetRelativeToSensor = false;

    protected Transform bodyRoot;
    protected GameObject offsetNode;
    protected Transform[] bones;
    protected Quaternion[] initialRotations;
    protected Quaternion[] initialLocalRotations;
    protected Vector3 initialPosition;
    protected Quaternion initialRotation;
    protected bool offsetCalibrated = false;
    protected float xOffset, yOffset, zOffset;
    protected KinectManager kinectManager;

    private Transform _transformCache;
    public new Transform transform
    {
        get
        {
            if (!_transformCache) 
                _transformCache = base.transform;
            return _transformCache;
        }
    }
    
    public void Awake()
    {   
        if(bones != null) return;
        
        bones = new Transform[22];
        initialRotations = new Quaternion[bones.Length];
        initialLocalRotations = new Quaternion[bones.Length];

        MapBones();
        GetInitialRotations();
    }
    
    public void UpdateAvatar(uint UserID)
    {   
        if(!transform.gameObject.activeInHierarchy) return;
        
        if(kinectManager == null)
        {
            kinectManager = KinectManager.Instance;
        }
        
        MoveAvatar(UserID);

        for (var boneIndex = 0; boneIndex < bones.Length; boneIndex++)
        {
            if (!bones[boneIndex]) continue;
            
            if(boneIndex2JointMap.ContainsKey(boneIndex))
            {
                KinectWrapper.NuiSkeletonPositionIndex joint = !mirroredMovement ? boneIndex2JointMap[boneIndex] : boneIndex2MirrorJointMap[boneIndex];
                TransformBone(UserID, joint, boneIndex, !mirroredMovement);
            }
            else if(specIndex2JointMap.ContainsKey(boneIndex))
            {
                List<KinectWrapper.NuiSkeletonPositionIndex> alJoints = !mirroredMovement ? specIndex2JointMap[boneIndex] : specIndex2MirrorJointMap[boneIndex];
                // if(alJoints.Count >= 2) { ... }
            }
        }
    }
    
    // UPDATED METHOD
    public void ResetToInitialPosition()
    {   
        if(bones == null) return;

        // CHECKLIST OPTION LOGIC:
        // If 'keepPositionOnLoss' is true, we exit this function immediately.
        // This prevents the code below from resetting the character to (0,0,0).
        if (keepPositionOnLoss)
        {
            return; 
        }
        
        if(offsetNode != null)
        {
            offsetNode.transform.rotation = Quaternion.identity;
        }
        else
        {
            transform.rotation = Quaternion.identity;
        }
        
        for (int i = 0; i < bones.Length; i++)
        {
            if (bones[i] != null)
            {
                bones[i].rotation = initialRotations[i];
            }
        }
        
        if(bodyRoot != null)
        {
            bodyRoot.localPosition = Vector3.zero;
            bodyRoot.localRotation = Quaternion.identity;
        }
        
        if(offsetNode != null)
        {
            offsetNode.transform.position = initialPosition;
            offsetNode.transform.rotation = initialRotation;
        }
        else
        {
            transform.position = initialPosition;
            transform.rotation = initialRotation;
        }
    }
    
    public void SuccessfulCalibration(uint userId)
    {
        if(offsetNode != null)
        {
            offsetNode.transform.rotation = initialRotation;
        }
        offsetCalibrated = false;
    }
    
    protected void TransformBone(uint userId, KinectWrapper.NuiSkeletonPositionIndex joint, int boneIndex, bool flip)
    {
        Transform boneTransform = bones[boneIndex];
        if(boneTransform == null || kinectManager == null) return;
        
        int iJoint = (int)joint;
        if(iJoint < 0) return;
        
        Quaternion jointRotation = kinectManager.GetJointOrientation(userId, iJoint, flip);
        if(jointRotation == Quaternion.identity) return;
        
        Quaternion newRotation = Kinect2AvatarRot(jointRotation, boneIndex);
        
        if(smoothFactor != 0f)
            boneTransform.rotation = Quaternion.Slerp(boneTransform.rotation, newRotation, smoothFactor * Time.deltaTime);
        else
            boneTransform.rotation = newRotation;
    }
    
    protected void MoveAvatar(uint UserID)
    {
        if(bodyRoot == null || kinectManager == null) return;
        if(!kinectManager.IsJointTracked(UserID, (int)KinectWrapper.NuiSkeletonPositionIndex.HipCenter)) return;
        
        Vector3 trans = kinectManager.GetUserPosition(UserID);
        
        if (!offsetCalibrated)
        {
            offsetCalibrated = true;
            xOffset = !mirroredMovement ? trans.x * moveRate : -trans.x * moveRate;
            yOffset = trans.y * moveRate;
            zOffset = -trans.z * moveRate;
            
            if(offsetRelativeToSensor)
            {
                Vector3 cameraPos = Camera.main.transform.position;
                float yRelToAvatar = (offsetNode != null ? offsetNode.transform.position.y : transform.position.y) - cameraPos.y;
                Vector3 relativePos = new Vector3(trans.x * moveRate, yRelToAvatar, trans.z * moveRate);
                Vector3 offsetPos = cameraPos + relativePos;
                
                if(offsetNode != null) offsetNode.transform.position = offsetPos;
                else transform.position = offsetPos;
            }
        }
    
        Vector3 targetPos = Kinect2AvatarPos(trans, verticalMovement);

        if(smoothFactor != 0f)
            bodyRoot.localPosition = Vector3.Lerp(bodyRoot.localPosition, targetPos, smoothFactor * Time.deltaTime);
        else
            bodyRoot.localPosition = targetPos;
    }
    
    protected virtual void MapBones()
    {
        offsetNode = new GameObject(name + "Ctrl") { layer = transform.gameObject.layer, tag = transform.gameObject.tag };
        offsetNode.transform.position = transform.position;
        offsetNode.transform.rotation = transform.rotation;
        offsetNode.transform.parent = transform.parent;
        
        transform.parent = offsetNode.transform;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        
        bodyRoot = transform;
        var animatorComponent = GetComponent<Animator>();
        
        for (int boneIndex = 0; boneIndex < bones.Length; boneIndex++)
        {
            if (!boneIndex2MecanimMap.ContainsKey(boneIndex)) continue;
            bones[boneIndex] = animatorComponent.GetBoneTransform(boneIndex2MecanimMap[boneIndex]);
        }
    }
    
    protected void GetInitialRotations()
    {
        if(offsetNode != null)
        {
            initialPosition = offsetNode.transform.position;
            initialRotation = offsetNode.transform.rotation;
            offsetNode.transform.rotation = Quaternion.identity;
        }
        else
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            transform.rotation = Quaternion.identity;
        }
        
        for (int i = 0; i < bones.Length; i++)
        {
            if (bones[i] != null)
            {
                initialRotations[i] = bones[i].rotation;
                initialLocalRotations[i] = bones[i].localRotation;
            }
        }
        
        if(offsetNode != null) offsetNode.transform.rotation = initialRotation;
        else transform.rotation = initialRotation;
    }
    
    protected Quaternion Kinect2AvatarRot(Quaternion jointRotation, int boneIndex)
    {
        Quaternion newRotation = jointRotation * initialRotations[boneIndex];
        if (offsetNode != null)
        {
            Vector3 totalRotation = newRotation.eulerAngles + offsetNode.transform.rotation.eulerAngles;
            newRotation = Quaternion.Euler(totalRotation);
        }
        return newRotation;
    }
    
    protected Vector3 Kinect2AvatarPos(Vector3 jointPosition, bool bMoveVertically)
    {
        float xPos = !mirroredMovement ? jointPosition.x * moveRate - xOffset : -jointPosition.x * moveRate - xOffset;
        float yPos = jointPosition.y * moveRate - yOffset;
        float zPos = -jointPosition.z * moveRate - zOffset;
        
        return new Vector3(xPos, bMoveVertically ? yPos : 0f, zPos);
    }

    // Mapping Dictionaries
    private readonly Dictionary<int, HumanBodyBones> boneIndex2MecanimMap = new Dictionary<int, HumanBodyBones>
    {
        {0, HumanBodyBones.Hips}, {1, HumanBodyBones.Spine}, {2, HumanBodyBones.Neck}, {3, HumanBodyBones.Head},
        {4, HumanBodyBones.LeftShoulder}, {5, HumanBodyBones.LeftUpperArm}, {6, HumanBodyBones.LeftLowerArm}, {7, HumanBodyBones.LeftHand}, {8, HumanBodyBones.LeftIndexProximal},
        {9, HumanBodyBones.RightShoulder}, {10, HumanBodyBones.RightUpperArm}, {11, HumanBodyBones.RightLowerArm}, {12, HumanBodyBones.RightHand}, {13, HumanBodyBones.RightIndexProximal},
        {14, HumanBodyBones.LeftUpperLeg}, {15, HumanBodyBones.LeftLowerLeg}, {16, HumanBodyBones.LeftFoot}, {17, HumanBodyBones.LeftToes},
        {18, HumanBodyBones.RightUpperLeg}, {19, HumanBodyBones.RightLowerLeg}, {20, HumanBodyBones.RightFoot}, {21, HumanBodyBones.RightToes},
    };

    protected readonly Dictionary<int, KinectWrapper.NuiSkeletonPositionIndex> boneIndex2JointMap = new Dictionary<int, KinectWrapper.NuiSkeletonPositionIndex>
    {
        {0, KinectWrapper.NuiSkeletonPositionIndex.HipCenter}, {1, KinectWrapper.NuiSkeletonPositionIndex.Spine}, {2, KinectWrapper.NuiSkeletonPositionIndex.ShoulderCenter}, {3, KinectWrapper.NuiSkeletonPositionIndex.Head},
        {5, KinectWrapper.NuiSkeletonPositionIndex.ShoulderLeft}, {6, KinectWrapper.NuiSkeletonPositionIndex.ElbowLeft}, {7, KinectWrapper.NuiSkeletonPositionIndex.WristLeft}, {8, KinectWrapper.NuiSkeletonPositionIndex.HandLeft},
        {10, KinectWrapper.NuiSkeletonPositionIndex.ShoulderRight}, {11, KinectWrapper.NuiSkeletonPositionIndex.ElbowRight}, {12, KinectWrapper.NuiSkeletonPositionIndex.WristRight}, {13, KinectWrapper.NuiSkeletonPositionIndex.HandRight},
        {14, KinectWrapper.NuiSkeletonPositionIndex.HipLeft}, {15, KinectWrapper.NuiSkeletonPositionIndex.KneeLeft}, {16, KinectWrapper.NuiSkeletonPositionIndex.AnkleLeft}, {17, KinectWrapper.NuiSkeletonPositionIndex.FootLeft},
        {18, KinectWrapper.NuiSkeletonPositionIndex.HipRight}, {19, KinectWrapper.NuiSkeletonPositionIndex.KneeRight}, {20, KinectWrapper.NuiSkeletonPositionIndex.AnkleRight}, {21, KinectWrapper.NuiSkeletonPositionIndex.FootRight},
    };

    protected readonly Dictionary<int, List<KinectWrapper.NuiSkeletonPositionIndex>> specIndex2JointMap = new Dictionary<int, List<KinectWrapper.NuiSkeletonPositionIndex>>
    {
        {4, new List<KinectWrapper.NuiSkeletonPositionIndex> {KinectWrapper.NuiSkeletonPositionIndex.ShoulderLeft, KinectWrapper.NuiSkeletonPositionIndex.ShoulderCenter} },
        {9, new List<KinectWrapper.NuiSkeletonPositionIndex> {KinectWrapper.NuiSkeletonPositionIndex.ShoulderRight, KinectWrapper.NuiSkeletonPositionIndex.ShoulderCenter} },
    };

    protected readonly Dictionary<int, KinectWrapper.NuiSkeletonPositionIndex> boneIndex2MirrorJointMap = new Dictionary<int, KinectWrapper.NuiSkeletonPositionIndex>
    {
        {0, KinectWrapper.NuiSkeletonPositionIndex.HipCenter}, {1, KinectWrapper.NuiSkeletonPositionIndex.Spine}, {2, KinectWrapper.NuiSkeletonPositionIndex.ShoulderCenter}, {3, KinectWrapper.NuiSkeletonPositionIndex.Head},
        {5, KinectWrapper.NuiSkeletonPositionIndex.ShoulderRight}, {6, KinectWrapper.NuiSkeletonPositionIndex.ElbowRight}, {7, KinectWrapper.NuiSkeletonPositionIndex.WristRight}, {8, KinectWrapper.NuiSkeletonPositionIndex.HandRight},
        {10, KinectWrapper.NuiSkeletonPositionIndex.ShoulderLeft}, {11, KinectWrapper.NuiSkeletonPositionIndex.ElbowLeft}, {12, KinectWrapper.NuiSkeletonPositionIndex.WristLeft}, {13, KinectWrapper.NuiSkeletonPositionIndex.HandLeft},
        {14, KinectWrapper.NuiSkeletonPositionIndex.HipRight}, {15, KinectWrapper.NuiSkeletonPositionIndex.KneeRight}, {16, KinectWrapper.NuiSkeletonPositionIndex.AnkleRight}, {17, KinectWrapper.NuiSkeletonPositionIndex.FootRight},
        {18, KinectWrapper.NuiSkeletonPositionIndex.HipLeft}, {19, KinectWrapper.NuiSkeletonPositionIndex.KneeLeft}, {20, KinectWrapper.NuiSkeletonPositionIndex.AnkleLeft}, {21, KinectWrapper.NuiSkeletonPositionIndex.FootLeft},
    };

    protected readonly Dictionary<int, List<KinectWrapper.NuiSkeletonPositionIndex>> specIndex2MirrorJointMap = new Dictionary<int, List<KinectWrapper.NuiSkeletonPositionIndex>>
    {
        {4, new List<KinectWrapper.NuiSkeletonPositionIndex> {KinectWrapper.NuiSkeletonPositionIndex.ShoulderRight, KinectWrapper.NuiSkeletonPositionIndex.ShoulderCenter} },
        {9, new List<KinectWrapper.NuiSkeletonPositionIndex> {KinectWrapper.NuiSkeletonPositionIndex.ShoulderLeft, KinectWrapper.NuiSkeletonPositionIndex.ShoulderCenter} },
    };
}