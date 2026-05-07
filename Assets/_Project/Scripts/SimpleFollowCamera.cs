using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleFollowCamera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;

    [Header("Normal Follow Rotation")]
    [SerializeField] private float yaw = 0f;
    [SerializeField] private float rotateSpeed = 0.18f;
    [SerializeField] private float fixedPitch = 55f;

    [Header("Normal Follow Zoom")]
    [SerializeField] private float distance = 10f;
    [SerializeField] private float zoomSpeed = 1.5f;
    [SerializeField] private float minDistance = 4f;
    [SerializeField] private float maxDistance = 16f;

    [Header("Normal Follow")]
    [SerializeField] private float focusSmooth = 12f;
    [SerializeField] private float returnToFollowSmooth = 10f;

    [Header("Free Fly Mode")]
    [SerializeField] private bool enableFreeFly = true;
    [SerializeField] private float flyMoveSpeed = 12f;
    [SerializeField] private float flyFastMultiplier = 2.5f;
    [SerializeField] private float flyLookSpeed = 0.18f;
    [SerializeField] private float minFlyPitch = -80f;
    [SerializeField] private float maxFlyPitch = 80f;

    [Header("Free Fly Keys")]
    [SerializeField] private bool useQAndEForVerticalMovement = true;

    private Vector3 focusPoint;

    private bool isFreeFlying;
    private Vector3 freeFlyPosition;
    private float freeFlyYaw;
    private float freeFlyPitch;

    private bool isReturningToFollow;

    private void Start()
    {
        yaw = transform.eulerAngles.y;

        if (target != null)
            focusPoint = target.position;
        else
            focusPoint = transform.position;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        HandleZoomInput();

        if (enableFreeFly && Mouse.current != null && Mouse.current.middleButton.wasPressedThisFrame)
        {
            if (isFreeFlying)
                ExitFreeFlyMode();
            else
                EnterFreeFlyMode();
        }

        if (isFreeFlying)
        {
            HandleFreeFlyMode();
            return;
        }

        HandleRotationInput();
        HandleNormalFollow();
    }

    private void HandleNormalFollow()
    {
        focusPoint = Vector3.Lerp(
            focusPoint,
            target.position,
            focusSmooth * Time.deltaTime
        );

        Quaternion rotation = Quaternion.Euler(fixedPitch, yaw, 0f);
        Vector3 desiredPosition = focusPoint - rotation * Vector3.forward * distance;

        if (isReturningToFollow)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                desiredPosition,
                returnToFollowSmooth * Time.deltaTime
            );

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                rotation,
                returnToFollowSmooth * Time.deltaTime
            );

            float positionDistance = Vector3.Distance(transform.position, desiredPosition);
            float angleDistance = Quaternion.Angle(transform.rotation, rotation);

            if (positionDistance < 0.05f && angleDistance < 0.5f)
            {
                transform.position = desiredPosition;
                transform.rotation = rotation;
                isReturningToFollow = false;
            }

            return;
        }

        transform.position = desiredPosition;
        transform.rotation = rotation;
    }

    private void HandleRotationInput()
    {
        if (Mouse.current == null)
            return;

        if (Mouse.current.rightButton.isPressed)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            yaw += delta.x * rotateSpeed;
        }
    }

    private void HandleZoomInput()
    {
        if (Mouse.current == null)
            return;

        if (isFreeFlying)
            return;

        float scroll = Mouse.current.scroll.ReadValue().y;

        if (Mathf.Abs(scroll) > 0.01f)
        {
            distance -= scroll * 0.01f * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }
    }

    private void EnterFreeFlyMode()
    {
        isFreeFlying = true;
        isReturningToFollow = false;

        freeFlyPosition = transform.position;

        Vector3 euler = transform.eulerAngles;
        freeFlyYaw = euler.y;
        freeFlyPitch = NormalizeAngle(euler.x);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ExitFreeFlyMode()
    {
        isFreeFlying = false;
        isReturningToFollow = true;

        yaw = freeFlyYaw;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void HandleFreeFlyMode()
    {
        if (Mouse.current == null)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        freeFlyYaw += mouseDelta.x * flyLookSpeed;
        freeFlyPitch -= mouseDelta.y * flyLookSpeed;
        freeFlyPitch = Mathf.Clamp(freeFlyPitch, minFlyPitch, maxFlyPitch);

        Quaternion flyRotation = Quaternion.Euler(freeFlyPitch, freeFlyYaw, 0f);

        Vector3 movement = ReadFreeFlyMovementInput();

        float speed = flyMoveSpeed;

        if (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed)
            speed *= flyFastMultiplier;

        freeFlyPosition += flyRotation * movement * speed * Time.deltaTime;

        transform.position = freeFlyPosition;
        transform.rotation = flyRotation;
    }

    private Vector3 ReadFreeFlyMovementInput()
    {
        Vector3 movement = Vector3.zero;

        if (Keyboard.current == null)
            return movement;

        if (Keyboard.current.wKey.isPressed)
            movement.z += 1f;

        if (Keyboard.current.sKey.isPressed)
            movement.z -= 1f;

        if (Keyboard.current.dKey.isPressed)
            movement.x += 1f;

        if (Keyboard.current.aKey.isPressed)
            movement.x -= 1f;

        if (useQAndEForVerticalMovement)
        {
            if (Keyboard.current.eKey.isPressed)
                movement.y += 1f;

            if (Keyboard.current.qKey.isPressed)
                movement.y -= 1f;
        }
        else
        {
            if (Keyboard.current.spaceKey.isPressed)
                movement.y += 1f;

            if (Keyboard.current.leftCtrlKey.isPressed)
                movement.y -= 1f;
        }

        if (movement.sqrMagnitude > 1f)
            movement.Normalize();

        return movement;
    }

    private float NormalizeAngle(float angle)
    {
        while (angle > 180f)
            angle -= 360f;

        while (angle < -180f)
            angle += 360f;

        return angle;
    }
}