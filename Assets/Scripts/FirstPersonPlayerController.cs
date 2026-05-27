using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class FirstPersonPlayerController : MonoBehaviour
{
    [Header("Camera / eyes")]
    public Transform playerCamera;
    public float eyeHeightMeters = 1.6256f;

    [Header("Visible body")]
    public bool createVisibleBody = true;
    public float bodyHeightMeters = 1.6256f;
    public float bodyRadiusMeters = 0.28f;
    public Color bodyColor = new Color(0.2f, 0.35f, 0.9f);

    [Header("Movement")]
    public float walkSpeed = 3.0f;
    public float sprintSpeed = 6.0f;

    [Header("Mouse look")]
    public float mouseSensitivity = 0.08f;
    public bool invertMouseY = false;
    public float minLookAngle = -80f;
    public float maxLookAngle = 80f;

    [Header("Sky")]
    public Color skyColor = new Color(0.50f, 0.75f, 1.0f);

    private float yaw;
    private float pitch;
    private GameObject visibleBody;

    private void Start()
    {
        SetupCamera();
        SetupVisibleBody();

        yaw = transform.eulerAngles.y;
        pitch = 0f;

        transform.position = new Vector3(transform.position.x, 0f, transform.position.z);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        LockCursor(true);
    }

    private void Update()
    {
        HandleCursor();
        HandleMouseLook();
        HandleMovement();

        LockBodyToGround();
        LockCameraToBody();
    }

    private void LateUpdate()
    {
        LockBodyToGround();
        LockCameraToBody();
    }

    private void SetupCamera()
    {
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }

        if (playerCamera == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
            playerCamera = camObj.transform;
        }

        playerCamera.SetParent(transform, false);

        playerCamera.localPosition = new Vector3(0f, eyeHeightMeters, 0f);
        playerCamera.localRotation = Quaternion.identity;

        Camera cameraComponent = playerCamera.GetComponent<Camera>();
        if (cameraComponent != null)
        {
            cameraComponent.clearFlags = CameraClearFlags.SolidColor;
            cameraComponent.backgroundColor = skyColor;
        }

        RenderSettings.ambientLight = Color.white;
    }

    private void SetupVisibleBody()
    {
        if (!createVisibleBody)
        {
            return;
        }

        Transform existing = transform.Find("Visible Body");
        if (existing != null)
        {
            visibleBody = existing.gameObject;
            return;
        }

        visibleBody = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visibleBody.name = "Visible Body";
        visibleBody.transform.SetParent(transform);

        visibleBody.transform.localPosition = new Vector3(0f, bodyHeightMeters / 2f, 0f);

        visibleBody.transform.localScale = new Vector3(
            bodyRadiusMeters * 2f,
            bodyHeightMeters / 2f,
            bodyRadiusMeters * 2f
        );

        Collider bodyCollider = visibleBody.GetComponent<Collider>();
        if (bodyCollider != null)
        {
            Destroy(bodyCollider);
        }

        Renderer renderer = visibleBody.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", bodyColor);
            renderer.sharedMaterial = mat;
        }
    }

    private void HandleCursor()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            LockCursor(false);
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            LockCursor(true);
        }
#else
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            LockCursor(false);
        }

        if (Input.GetMouseButtonDown(0))
        {
            LockCursor(true);
        }
#endif
    }

    private void HandleMouseLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked || playerCamera == null)
        {
            return;
        }

        Vector2 mouse = GetMouseDelta();

        yaw += mouse.x * mouseSensitivity;

        if (invertMouseY)
        {
            pitch += mouse.y * mouseSensitivity;
        }
        else
        {
            pitch -= mouse.y * mouseSensitivity;
        }

        pitch = Mathf.Clamp(pitch, minLookAngle, maxLookAngle);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        playerCamera.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleMovement()
    {
        Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        Vector3 right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;

        Vector3 move = Vector3.zero;

#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current;
        if (k == null)
        {
            return;
        }

        if (k.wKey.isPressed) move += forward;
        if (k.sKey.isPressed) move -= forward;
        if (k.aKey.isPressed) move -= right;
        if (k.dKey.isPressed) move += right;

        bool sprinting = k.leftShiftKey.isPressed || k.rightShiftKey.isPressed;
#else
        if (Input.GetKey(KeyCode.W)) move += forward;
        if (Input.GetKey(KeyCode.S)) move -= forward;
        if (Input.GetKey(KeyCode.A)) move -= right;
        if (Input.GetKey(KeyCode.D)) move += right;

        bool sprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif

        if (move.sqrMagnitude > 1f)
        {
            move.Normalize();
        }

        float speed = sprinting ? sprintSpeed : walkSpeed;

        Vector3 nextPosition = transform.position + move * speed * Time.deltaTime;

        nextPosition.y = 0f;

        transform.position = nextPosition;
    }

    private void LockBodyToGround()
    {
        Vector3 pos = transform.position;
        pos.y = 0f;
        transform.position = pos;
    }

    private void LockCameraToBody()
    {
        if (playerCamera == null)
        {
            return;
        }

        playerCamera.localPosition = new Vector3(0f, eyeHeightMeters, 0f);
    }

    private Vector2 GetMouseDelta()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null)
        {
            return Vector2.zero;
        }

        return Mouse.current.delta.ReadValue();
#else
        return new Vector2(
            Input.GetAxisRaw("Mouse X") * 10f,
            Input.GetAxisRaw("Mouse Y") * 10f
        );
#endif
    }

    private void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
