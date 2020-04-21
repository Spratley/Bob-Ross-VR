using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;

public class AdjustFriction : MonoBehaviour
{
    public PhysicMaterial physicMaterial;
    //public PhysicMaterialCombine physicMaterialCombine;
    public Text debugText;
    public float frictionAmount;
    public SteamVR_Action_Vector2 scrollChange;

    private Vector2 previousInput = Vector2.zero;
    private void Update()
    {
        var stickInput = scrollChange.GetAxis(SteamVR_Input_Sources.LeftHand);
        
        physicMaterial.dynamicFriction += stickInput.y * Time.deltaTime * frictionAmount;
        physicMaterial.staticFriction = physicMaterial.dynamicFriction;

        if (Mathf.Abs(stickInput.x) >= 0.8f && Mathf.Abs(previousInput.x) < 0.8f) {
            int currentCombine = (int)physicMaterial.frictionCombine;
            currentCombine += ((int)Mathf.Sign(stickInput.x) * 2) - 1;
            currentCombine %= 4;
            physicMaterial.frictionCombine = (PhysicMaterialCombine)currentCombine;
        }

        debugText.text = "Friction: " + physicMaterial.dynamicFriction 
                       + "\nCombination mode" + physicMaterial.frictionCombine.ToString() 
                       + "\nInput is at: " + stickInput.ToString()
                       + "\nFPS - " + 1.0f/Time.deltaTime;


        previousInput = stickInput;
    }
}
