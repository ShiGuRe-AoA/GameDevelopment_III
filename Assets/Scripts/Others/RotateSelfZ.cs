using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class RotateSelfZ : MonoBehaviour
{
    public float rotateSpeed = 180f; // Ã¿ÃëÐý×ª½Ç¶È

    void Update()
    {
        transform.Rotate(0f, 0f, -rotateSpeed * Time.deltaTime, Space.Self);
    }
}