using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleFollowCamera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;

    [Header("Rotation")]
    [SerializeField] private float yaw = 0f;
    [SerializeField] private float rotateSpeed = 0.18f;
    [SerializeField] private float fixedPitch = 55f;

    [Header("Zoom")]
    [SerializeField] private float distance = 10f;
    [SerializeField] private float zoomSpeed = 1.5f;
    [SerializeField] private float minDistance = 4f;
    [SerializeField] private float maxDistance = 16f;

    [Header("Follow")]
    [SerializeField] private float focusSmooth = 12f;

    private Vector3 focusPoint;

    private void Start()
    {
        if (target == null)
            return;

        yaw = transform.eulerAngles.y;
        focusPoint = target.position;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        HandleRotationInput();
        HandleZoomInput();

        focusPoint = Vector3.Lerp(
            focusPoint,
            target.position,
            focusSmooth * Time.deltaTime
        );

        Quaternion rotation = Quaternion.Euler(fixedPitch, yaw, 0f);
        Vector3 desiredPosition = focusPoint - rotation * Vector3.forward * distance;

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

        float scroll = Mouse.current.scroll.ReadValue().y;

        if (Mathf.Abs(scroll) > 0.01f)
        {
            distance -= scroll * 0.01f * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }
    }
}