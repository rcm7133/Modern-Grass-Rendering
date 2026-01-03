using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    [SerializeField] private Transform orientation;
    [SerializeField] public bool lookEnabled = true;
    public bool restrictPitch = false;
    
    private float xRotation;
    private float yRotation;
    
    void Update()
    {
        if (!lookEnabled)
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
            return;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * 100f;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * 100f;

        yRotation += mouseX;
        if (!restrictPitch)
            xRotation -= mouseY;

        xRotation = Mathf.Clamp(xRotation, -85f, 85f);
        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        orientation.rotation = Quaternion.Euler(0, yRotation, 0);
    
    }
}
