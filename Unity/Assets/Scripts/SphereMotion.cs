﻿using UnityEngine;
using System.Collections;

public class SphereMotion : MonoBehaviour
{

    bool pressed = false;
    bool sphere_move = false;
    Vector3 offset;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            pressed = true;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Vector3.Cross(ray.direction, transform.position - ray.origin).magnitude < 2.5f) sphere_move = true;
            else sphere_move = false;
            offset = Input.mousePosition - Camera.main.WorldToScreenPoint(transform.position);
        }
        if (Input.GetMouseButtonUp(0))
            pressed = false;

        if (pressed)
        {
            if (sphere_move)
            {
                Vector3 mouse = Input.mousePosition;
                mouse -= offset;
                mouse.z = Camera.main.WorldToScreenPoint(transform.position).z;
                transform.position = Camera.main.ScreenToWorldPoint(mouse);
                if (transform.position.y < -12.5)
                    transform.position = new Vector3(transform.position.x, -12.5f, transform.position.z);
            }
        }
    }
}
