using UnityEngine;

public class CameraController : MonoBehaviour {
    [SerializeField] Camera cam;
    Transform targetCamera;

    public InputActions InputActions { get; private set; }

    [Header("Movement")]
    [SerializeField] float normalSpeed;
    [SerializeField] float fastSpeed;
    [SerializeField] float moveTime;
    [SerializeField] float normalSensitivity;
    [SerializeField] float fastSensitivity;
    float moveSpeed;
    float sensitivity;

    [Header("Rotation")]
    [SerializeField] float rotationAmount;
    [SerializeField] float rotationSpeed;
    [SerializeField] float rotationNormalSensitivity = 0.2f;
    [SerializeField] float rotationFastSensitivity = 0.6f;
    [SerializeField] AnimationCurve upwardPanCurve;
    float rotationSensitivity;

    [Header("Zoom")]
    [SerializeField] float zoomSpeed;
    [SerializeField] AnimationCurve fovCurve;
    [SerializeField] Vector2 minMaxZoom;

    [SerializeField] float zoomAmount;
    Vector3 newPos;
    Vector3 newZoom;

    Vector3 camStartRot;
    Quaternion newRot;

    bool enabled = false;

    //public Quaternion newCamRot; // Make x rotation smaller so camera can see further when zooming in

    // Start is called before the first frame update
    void Awake() {
        InputActions = new();
        InputActions.Default.Enable();

        targetCamera = cam.transform;
        newPos = transform.position;
        newRot = transform.rotation;
        camStartRot = targetCamera.localEulerAngles;
        newZoom = targetCamera.localPosition;

        WorldGenerationEvents.Regenerate += () => { enabled = true; };
        WorldGenerationEvents.BeginGeneration += () => { enabled = true; };
    }

    // Update is called once per frame
    void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) enabled  = !enabled;

        HandleMovement();
    }

    public void HandleMovement() {
        if (!enabled) return;

        if (Input.GetKey(KeyCode.LeftShift)) {
            sensitivity = fastSensitivity;
            rotationSensitivity = rotationFastSensitivity;
        }
        else {
            moveSpeed = normalSpeed;
            sensitivity = normalSensitivity;
            rotationSensitivity = rotationNormalSensitivity;
        }


        //Move camera
        Vector2 movementVector = InputActions.Default.MoveCam.ReadValue<Vector2>();
        Vector3 newMoveVector = movementVector.x * transform.right + movementVector.y * transform.forward;
        newPos += newMoveVector * sensitivity * Time.deltaTime;

        //Rotate Camera
        Vector2 rotationVector = InputActions.Default.SpinCam.ReadValue<Vector2>();
        newRot *= Quaternion.Euler(Vector3.up * (rotationAmount * -rotationVector.x) * rotationSensitivity);

        //ZoomCamera
        Vector2 scrollVector = InputActions.Default.ZoomCam.ReadValue<Vector2>();

        if (scrollVector.y != 0f) {

            Vector3 zoomAmountV3 = new(0, -zoomAmount, zoomAmount);

            if (scrollVector.y > 0f)
                newZoom += zoomAmountV3;
            else
                newZoom -= zoomAmountV3;

            newZoom.y = Mathf.Clamp(newZoom.y, minMaxZoom.x, minMaxZoom.y);
            newZoom.z = Mathf.Clamp(newZoom.z, -minMaxZoom.y, -minMaxZoom.x);
        }

        transform.SetPositionAndRotation(Vector3.Lerp(transform.position, newPos, Time.deltaTime * moveSpeed),
            Quaternion.Lerp(transform.rotation, newRot, Time.deltaTime * rotationSensitivity * rotationSpeed));

        targetCamera.localPosition = Vector3.Lerp(targetCamera.localPosition, newZoom, Time.deltaTime * zoomSpeed * rotationSensitivity);

        //Get cameras current normalized position between the min and max positions
        Vector3 maxPos = new(0, minMaxZoom.y, -minMaxZoom.y);
        Vector3 minPos = new(0, minMaxZoom.x, -minMaxZoom.x);

        Vector3 ab = minPos - maxPos;
        Vector3 ac = targetCamera.localPosition - minPos;
        float distAlong = Vector3.Dot(ac, ab.normalized) / ab.magnitude;
        //=========================================================================

        cam.fieldOfView = fovCurve.Evaluate(-distAlong);
        targetCamera.localEulerAngles = new(camStartRot.x + -upwardPanCurve.Evaluate(-distAlong), camStartRot.y, camStartRot.z);
    }
}
