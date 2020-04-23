using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrushAngle : MonoBehaviour
{
    public BrushPicker brushPicker;
    public Transform canvas;

    private void Update()
    {
        brushPicker.m_angle = 90 - Mathf.Clamp(Vector3.Angle(-transform.up, canvas.forward), 0, 90);

        brushPicker.m_brushScript.m_brushAngle = Vector3.Angle(transform.right, canvas.right);
    }
}
