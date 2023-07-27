using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPCamera : MonoBehaviour
{
    [Header("References")] [SerializeField]
    private Movement movement;
    [SerializeField] private Transform camHolder;
    [SerializeField] private Transform body;
    [SerializeField] private Camera cam;
    
    [Space(10)]
    [Header("Camera")]
    [SerializeField] private float sensitivity = 3f;
    [SerializeField] private float yAngleClamp = 90f;
    [SerializeField] private float defaultFOV = 60f;

    [Space(10)] 
    [Header("Bobbing")]
    [SerializeField] private float bobbingSpeed = 14f;
    [SerializeField] private float bobbingAmount = 0.05f;

    private float _xRotation;
    private float _timer;
    private float _midpoint;

    private void Start()
    {
        if (!movement) movement = GetComponent<Movement>();
        cam.fieldOfView = defaultFOV;
        _midpoint = camHolder.localPosition.y;
    }

    private void Look(Vector2 lookInput)
    {
        float mouseX = lookInput.x * sensitivity * Time.deltaTime;
        float mouseY = lookInput.y * sensitivity * Time.deltaTime;

        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -yAngleClamp, yAngleClamp);
        
        camHolder.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        body.Rotate(Vector3.up * mouseX);
    }

    private void Update()
    {
        // Look:
        Vector2 lookInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        Look(lookInput);
        
        // Bobbing:
        if (movement.IsMoving())
        {
            _timer += Time.deltaTime * bobbingSpeed;
            camHolder.localPosition = new Vector3(camHolder.localPosition.x,
                _midpoint + Mathf.Sin(_timer) * bobbingAmount, camHolder.localPosition.z);
        }
        else
        {
            _timer = 0;
            camHolder.localPosition = new Vector3(camHolder.localPosition.x,
                Mathf.Lerp(camHolder.localPosition.y, _midpoint, bobbingSpeed * Time.deltaTime), camHolder.localPosition.z);
        }
    }
}
