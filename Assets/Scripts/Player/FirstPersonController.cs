using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonController : MonoBehaviour
{
    public float mouseSensitivity = 200f;
    public Transform playerBody;

    private float xRotation;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        Vector2 look = Mouse.current.delta.ReadValue();

        float mouseX = look.x * mouseSensitivity * Time.deltaTime * 0.01f;
        float mouseY = look.y * mouseSensitivity * Time.deltaTime * 0.01f;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }
}