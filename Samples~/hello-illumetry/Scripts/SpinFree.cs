using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpinFree : MonoBehaviour
{
    public Vector3 directionAndSpeed;

    private void Update()
    {
        Vector3 step = directionAndSpeed * Time.deltaTime;
        transform.rotation *= Quaternion.Euler(step);
    }
}
