using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BrushTex", menuName = "ScriptableObjects/BrushTex", order = 0)]
public class BrushTex : ScriptableObject
{
    //The brush texture
    public Texture2D m_texture;
    //The center of this stroke
    public Vector2 m_center = new Vector2(0.0f, 0.0f);
    //Angle of the brush for this texture
    public float m_angle;
}