using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorPicker : MonoBehaviour
{
    public Material mat;

    public Vector3 GetColorVec3()
    {
        return new Vector3(mat.color.r, mat.color.g, mat.color.b);
    }
}
