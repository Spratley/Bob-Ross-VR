using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "3DBrush", menuName = "ScriptableObjects/Brush3D", order = 0)]
public class Brush3D : Brush
{
    // Prefab used to summon the 3D counterpart to the brush
    public GameObject m_brushPrefab;
}
