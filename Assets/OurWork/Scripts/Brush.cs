using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Brush", menuName = "ScriptableObjects/Brush", order = 0)]
public class Brush : ScriptableObject
{
    //List of brush textures
    public List<BrushTex> m_strokes;
    
    //Name of the brush
    public string m_name;

    //Allows for use of square brackets
    public Texture2D this[int key]
    {
        get
        {
            return m_strokes[key].m_texture;
        }
        set
        {
            m_strokes[key].m_texture = value;
        }
    }
}
