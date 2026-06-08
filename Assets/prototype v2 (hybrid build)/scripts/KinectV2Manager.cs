using UnityEngine;
using Windows.Kinect;

public class KinectV2Manager : MonoBehaviour
{
    [Header("Rig Target Connection")]
    public KinectRigController rigController;

    // Native SDK Components
    private KinectSensor _sensor;
    private BodyFrameReader _reader;
    private Body[] _bodyData = null;

    void Start()
    {
        // 1. Get the default connected sensor hardware instance
        _sensor = KinectSensor.GetDefault();

        if (_sensor != null)
        {
            // 2. Open the body data reader stream frame source
            _reader = _sensor.BodyFrameSource.OpenReader();
            
            // Allocate the native data buffer size space (Kinect v2 tracking max limit is 6 bodies)
            _bodyData = new Body[_sensor.BodyFrameSource.BodyCount];

            // 3. Start the device sensor
            if (!_sensor.IsOpen)
            {
                _sensor.Open();
            }
            Debug.Log("Kinect v2 Sensor Initialized successfully.");
        }
        else
        {
            Debug.LogError("No Kinect v2 Sensor Hardware detected connected to this system.");
        }
    }

    void Update()
    {
        if (_reader == null) return;

        // Acquire the latest available tracking frame stream from the API
        using (var frame = _reader.AcquireLatestFrame())
        {
            if (frame != null)
            {
                // Refresh the frame buffer data container array cleanly
                frame.GetAndRefreshBodyData(_bodyData);

                // Process the body index loops
                foreach (var body in _bodyData)
                {
                    if (body == null) continue;

                    // If a valid person skeleton structure layout is actively within frame
                    if (body.IsTracked)
                    {
                        ProcessTrackedSkeleton(body);
                        
                        // Limit control tracking processing to the primary active target body frame space 
                        break; 
                    }
                }
            }
        }
    }

    void ProcessTrackedSkeleton(Body body)
    {
        if (rigController == null) return;

        // Run updates across all 25 joints mapped into the SDK Enum collection structures
        foreach (JointType jointType in System.Enum.GetValues(typeof(JointType)))
        {
            Joint joint = body.Joints[jointType];

            if (joint.TrackingState == TrackingState.Tracked)
            {
                // Extract metrics out of standard meter scale values cleanly
                Vector3 rawKinectPosition = new Vector3(joint.Position.X, joint.Position.Y, joint.Position.Z);
                int jointID = (int)jointType;

                // Push values to our custom rig mapper component structure
                rigController.UpdateJointPosition(jointID, rawKinectPosition);

                // OPTIONAL: Pull orientation rotation data if you require active bone twists
                JointOrientation orientation = body.JointOrientations[jointType];
                Quaternion rawKinectRotation = new Quaternion(
                    orientation.Orientation.X, 
                    orientation.Orientation.Y, 
                    orientation.Orientation.Z, 
                    orientation.Orientation.W
                );
                rigController.UpdateJointRotation(jointID, rawKinectRotation);
            }
        }
    }

    // Crucial clean memory disposal routine handling to avoid backend Windows background thread leaks
    void OnApplicationQuit()
    {
        if (_reader != null)
        {
            _reader.Dispose();
            _reader = null;
        }

        if (_sensor != null)
        {
            if (_sensor.IsOpen)
            {
                _sensor.Close();
            }
            _sensor = null;
        }
        Debug.Log("Kinect v2 Native hardware tracking stream cleanly disposed.");
    }
}