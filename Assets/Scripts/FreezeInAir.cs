using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreezeInAir : MonoBehaviour
{
    public List<SampleForFreeze> jointsToFreeze;
    public RBFollowTransform brushFollower;

    public bool isFrozen;

    //TODO: Don't check this every frame lol
    private void Start()
    {
        foreach (var joint in jointsToFreeze)
        {
            joint.SetMaster(this);
        }
    }

    public void TryFreeze()
    {
        if (isFrozen)
            return;

        isFrozen = true;
        SetAllJoints(ConfigurableJointMotion.Locked);
        brushFollower.moveWithPhysics = false;
    }

    public void TryThaw()
    {
        if (!isFrozen)
            return;
        isFrozen = false;
        SetAllJoints(ConfigurableJointMotion.Free);
        brushFollower.moveWithPhysics = true;
    }

    public bool GetIfNodesGrounded()
    {
        foreach (var joint in jointsToFreeze)
        {
            if (!joint.GetInAir())
                return true;
        }
        return false;
    }

    public void SetAllJoints(ConfigurableJointMotion newMotion)
    {
        foreach (var joint in jointsToFreeze)
        {
            joint.joint.angularXMotion = newMotion;
            joint.joint.angularYMotion = newMotion;
            joint.joint.angularZMotion = newMotion;
        }
    }
}
