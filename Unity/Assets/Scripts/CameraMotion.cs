using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMotion : MonoBehaviour
{
    Transform myCamera;
    float moveSpeed = 10.0f;
    float minDistance = 0.5f;

    void Start()
    {
        myCamera = gameObject.transform;
    }

    void Update()
    {
        Vector3 direction;
        Vector3 speedForward = Vector3.zero;
        Vector3 speedBack = Vector3.zero;
        Vector3 speedLeft = Vector3.zero;
        Vector3 speedRight = Vector3.zero;
        Vector3 speedUp = Vector3.zero;
        Vector3 speedDown = Vector3.zero;
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) speedForward = myCamera.forward;
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) speedBack = -myCamera.forward;
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) speedLeft = -myCamera.right;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) speedRight = myCamera.right;
        if (Input.GetKey(KeyCode.E)) speedUp = Vector3.up;
        if (Input.GetKey(KeyCode.Q)) speedDown = Vector3.down;
        direction = speedForward + speedBack + speedLeft + speedRight + speedUp + speedDown;

        if (Input.GetMouseButton(1))
        {
            float h = 2.0f * Input.GetAxis("Mouse X");
            Camera.main.transform.RotateAround(new Vector3(0, 0, 0), Vector3.up, h);
        }

        if (Input.GetAxis("Mouse ScrollWheel") < 0)
        {
            if (Camera.main.fieldOfView <= 100)
                Camera.main.fieldOfView += 2;
            if (Camera.main.orthographicSize <= 20)
                Camera.main.orthographicSize += 0.5F;
        }
        if (Input.GetAxis("Mouse ScrollWheel") > 0)
        {
            if (Camera.main.fieldOfView > 2)
                Camera.main.fieldOfView -= 2;
            if (Camera.main.orthographicSize >= 1)
                Camera.main.orthographicSize -= 0.5F;
        }

        RaycastHit hit;
        while (Physics.Raycast(myCamera.position, direction, out hit, minDistance))
        {
            float angel = Vector3.Angle(direction, hit.normal);
            float magnitude = Vector3.Magnitude(direction) * Mathf.Cos(Mathf.Deg2Rad * (180 - angel));
            direction += hit.normal * magnitude;
        }
        myCamera.Translate(direction * moveSpeed * Time.deltaTime, Space.World);
    }
}
