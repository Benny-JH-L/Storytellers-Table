using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour 
{
    private CameraControlActions cameraActions;
    private InputAction movement;   // for xz plane movement
    private InputAction elevation;  // for y-axis movement
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Camera cameraRigged;                 // camera that's rigged

    [Header("Horizontal motion")]
    [SerializeField] private float maxSpeed = 5f;
    private float speed;
    [SerializeField] private float acceleration = 10f;  // how quickly we should speed to up `taragetPos`
    [SerializeField] private float damping = 15f;       // how quickly we will slow down after reaching `targetPos`

    [Header("Vertical motion - zooming")]
    [SerializeField] private float stepSize = 3f;           // how much we move up and down
    [SerializeField] private float zoomDampening = 7.5f;    
    [SerializeField] private float minHeight = 10f;
    [SerializeField] private float maxHeight = 30f;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float orthoMinHeight = 30f;     // for the pseudo ortho graphic camera
    [SerializeField] private float orthoMaxHeight = 50f;    // for the pseudo ortho graphic camera

    [Header("Rotation")]
    [Range(0.1f, 1f)]
    [SerializeField] private float maxRotationalSpeed = 1f;

    [Header("Screen edge motion")]
    [Range(0f, 0.1f)]
    [SerializeField] private float edgeTolerance = 0.05f; // percentage
    [SerializeField] private bool useScreenEdge = true;


    [Header("Elevation motion")]
    [SerializeField] private float elevationStep = 2f;

    [Header("Other")]
    [SerializeField] private bool onPseudoOrthographic = false;    // toggle to change camera to a pseudo `orthographic`
    [Range(20f, 120f)]
    [SerializeField] private float cameraFOV = 60f;

    // used to update the position of the camera base object, where our target is moving to
    private Vector3 targetPos;

    private float zoomHeight; // height we want the camera to be at

    // used to track and maintain velocity without a rigid body
    private Vector3 horizontalVelocity;
    private Vector3 lastPosition;

    // track where the dragging action started
    private Vector3 startDrag;
    private Vector3 elevationStartDrag;

    private void Awake()
    {
        cameraActions = new CameraControlActions();
        cameraRigged = this.GetComponentInChildren<Camera>();
        cameraTransform = cameraRigged.transform;
    }

    private void OnEnable()
    {
        // set initial values
        zoomHeight = cameraTransform.localPosition.y;
        cameraTransform.LookAt(this.transform);

        lastPosition = this.transform.position;
        movement = cameraActions.Camera.Movement;   // cache
        elevation = cameraActions.Camera.Elevation; // cache
        cameraActions.Camera.RotateCamera.performed += RotateCamera;    // subscribe a call back to when `performed` is called by Unity.
        cameraActions.Camera.ZoomCamera.performed += ZoomCamera;
            // NOTE when a value changes Unity will call events; `started`, `performed`, and `canceled`
            // started, when control moves away from default val. ex, gampad stick moving away from (0,0).
            // performed will be called each time the value changes.
            // canceled, when control moves back to default val. ex, gampad stick going back to (0,0).

        cameraActions.Camera.Enable();              // ensure the action map is enabled
    }

    private void OnDisable()
    {
        cameraActions.Camera.RotateCamera.performed -= RotateCamera;    // unsubscribe
        cameraActions.Camera.ZoomCamera.performed -= ZoomCamera;        // unsubscribe
        cameraActions.Disable();
    }

    private void Update()
    {
        GetKeyboardMovement();
        if (useScreenEdge)
            CheckMouseAtScreenEdge();
        DragCamera();
        ElevationDragCamera();

        UpdateVelocity();
        UpdateCameraPosition();
        UpdateBasePosition();
    }

    private void UpdateVelocity()
    {
        horizontalVelocity = (this.transform.position - lastPosition) / Time.deltaTime;
        horizontalVelocity.y = 0;   // only move in the horizontal plane
        lastPosition = this.transform.position;
    }

    /// <summary>
    /// Gets the keyboard movment from the InputAction, updates `targetPos`.
    /// </summary>
    private void GetKeyboardMovement()
    {
        // Want the camera to move relative to where the camera is facing
        Vector3 inputVal = movement.ReadValue<Vector2>().x * GetCameraRight()
                            + movement.ReadValue<Vector2>().y * GetCameraForward();

        inputVal = inputVal.normalized; // the `y` value will be 0, bc the `y` from the movement, vec2, will be the `z` in the vector3

        if (inputVal.sqrMagnitude > 0.1f)
            targetPos += inputVal;

        // elevation movement, along y-axis
        float elevationVal = elevation.ReadValue<float>();
        //Debug.Log("elev: " + elevationVal); // prints a float: 0, -1, 1, for no change, negative, and positive respectively.
        if (elevationVal < 0)
            targetPos += new Vector3(0f, -elevationStep);
        else if (elevationVal > 0)
            targetPos += new Vector3(0f, elevationStep);
    }

    /// <summary>
    /// gets the `right` vector relative to where the camera's facing
    /// </summary>
    /// <returns></returns>
    private Vector3 GetCameraRight()
    {
        Vector3 right = cameraTransform.right;
        right.y = 0;
        return right;
    }

    /// <summary>
    /// gets the `forward` vector relative to where the camera's facing
    /// </summary>
    /// <returns></returns>
    private Vector3 GetCameraForward()
    {
        Vector3 forward = cameraTransform.forward; 
        forward.y = 0;
        return forward;
    }

    /// <summary>
    /// Update the position of the rig base.
    /// </summary>
    private void UpdateBasePosition()
    {
        // check if we have a target position
        if (targetPos.sqrMagnitude > 0.1f)  
        {
            // ramp up speed
            speed = Mathf.Lerp(speed, maxSpeed, Time.deltaTime * acceleration); // linear interpolation from starting poing 'a' to 'b' by 't' 
            transform.position += targetPos * speed * Time.deltaTime;
        }
        // if it is near 0
        else
        {
            // slow down
            horizontalVelocity = Vector3.Lerp(horizontalVelocity, Vector3.zero, Time.deltaTime * damping);
            transform.position += horizontalVelocity * Time.deltaTime;
        }

        targetPos = Vector3.zero;
    }

    /// <summary>
    /// Callback to rotate the camera (about the y axis), when middle mouse button is pressed (and shift, ctr, command, are not pressed).
    /// </summary>
    /// <param name="context"></param>
    private void RotateCamera(InputAction.CallbackContext inputVal)
    {
        if (!Mouse.current.middleButton.isPressed || Keyboard.current.shiftKey.isPressed || (Keyboard.current.ctrlKey.isPressed || (Keyboard.current.leftCommandKey.isPressed || Keyboard.current.rightCommandKey.isPressed)))
            return;

        float val = inputVal.ReadValue<Vector2>().x;
        transform.rotation = Quaternion.Euler(0f, val * maxRotationalSpeed + transform.rotation.eulerAngles.y, 0f);
    }

    /// <summary>
    /// Callback to zoom the camera
    /// </summary>
    /// <param name="inputVal"></param>
    private void ZoomCamera(InputAction.CallbackContext inputVal)
    {
        float val = -inputVal.ReadValue<Vector2>().y;

        if (Mathf.Abs(val) > 0.1f)
        {
            zoomHeight = cameraTransform.localPosition.y + val * (onPseudoOrthographic ? stepSize * 5f: stepSize);
            ClampZoomHeight();
        }
    }

    /// <summary>
    /// Clamps the zoom height of the camera
    /// </summary>
    private void ClampZoomHeight()
    {
        // clamp height
        if (onPseudoOrthographic)
            zoomHeight = Mathf.Clamp(zoomHeight, orthoMinHeight, orthoMaxHeight);
        else
            zoomHeight = Mathf.Clamp(zoomHeight, minHeight, maxHeight);
    }

    /// <summary>
    /// Updates the camera's zoom, and local position relative to the rig.
    /// </summary>
    private void UpdateCameraPosition()
    {
        Vector3 zoomTarget = new Vector3(cameraTransform.localPosition.x, zoomHeight, cameraTransform.localPosition.z);
        zoomTarget -= zoomSpeed * (zoomHeight - cameraTransform.localPosition.y) * Vector3.forward;

        cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, zoomTarget, Time.deltaTime * zoomDampening);
        cameraTransform.LookAt(this.transform);
    }


    /// <summary>
    /// Check if the mouse is within some % of the screen, and moves the camera toward that directioin, updates the `targetPos`.
    /// </summary>
    private void CheckMouseAtScreenEdge()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 moveDirection = Vector3.zero;

        if (mousePos.x < edgeTolerance * Screen.width)              // if pos is under <5% of the side of the screen (left-to-right)
            moveDirection += -GetCameraRight();         // move camera left
        else if (mousePos.x > (1 - edgeTolerance) * Screen.width)   // if pos is over >95% of the screen (left-to-right)
            moveDirection += GetCameraRight();         // move camera right

        if (mousePos.y < edgeTolerance * Screen.height)              // if pos is under <5% of the side of the screen (bottom-top)
            moveDirection += -GetCameraForward();         // move camera up
        else if (mousePos.y > (1 - edgeTolerance) * Screen.height)   // if pos is over over >95% of the screen (bottom-top)
            moveDirection += GetCameraForward();            // move camera down

        targetPos += moveDirection;
    }

    /// <summary>
    /// Check camera drag when the left shift key and middle mouse button are pressed, updates the `targetPos`.
    /// </summary>
    private void DragCamera()
    {
        if (!Keyboard.current.shiftKey.isPressed || !Mouse.current.middleButton.isPressed)
            return;

        Plane plane = new Plane(Vector3.up, this.transform.position); // 1st param, perpendicular vector, 2nd param, point on the plane (ie goes through wherever the rig position is)
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        
        if (plane.Raycast(ray, out float distance))
        {
            if (Mouse.current.middleButton.wasPressedThisFrame)
                startDrag = ray.GetPoint(distance);
            else
                targetPos += startDrag - ray.GetPoint(distance);
        }
    }

    /// <summary>
    /// Check camera drag when the ctr/command key and middle mouse button are preswsed, updates the `targetPos`.
    /// </summary>
    private void ElevationDragCamera()
    {
        if ((!Keyboard.current.ctrlKey.isPressed && (!Keyboard.current.leftCommandKey.isPressed || !Keyboard.current.rightCommandKey.isPressed)) || !Mouse.current.middleButton.isPressed)
            return;

        Plane plane = new Plane(Vector3.up, this.transform.position); // 1st param, perpendicular vector, 2nd param, point on the plane (ie goes through wherever rig position is)
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (plane.Raycast(ray, out float distance))
        {
            if (Mouse.current.middleButton.wasPressedThisFrame)
                elevationStartDrag = ray.GetPoint(distance);
            else
                targetPos.y += (elevationStartDrag - ray.GetPoint(distance)).z;
        }
    }

    /// <summary>
    /// Toggles between a normal persepctive and a pseudo orthographic projection for the rigged camera.
    /// </summary>
    public void TogglePeseudoOrthographic()
    {
        onPseudoOrthographic = !onPseudoOrthographic;

        if (onPseudoOrthographic)
        {
            cameraRigged.fieldOfView = 10f;
            zoomHeight = cameraTransform.localPosition.y + 20f;
        }
        else
        {
            cameraRigged.fieldOfView = cameraFOV;
            zoomHeight = cameraTransform.localPosition.y - 20f;
        }
        ClampZoomHeight();
    }
}

