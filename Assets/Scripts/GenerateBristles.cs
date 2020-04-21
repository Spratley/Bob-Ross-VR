using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(RBFollowTransform))]
public class GenerateBristles : MonoBehaviour
{
    public Vector3 originOffset;
    public Vector2Int bristleCount;
    public Vector2 area;
    public GameObject bristlePrefab;

    private void Start()
    {
        float xUnit = area.x / bristleCount.x;
        float zUnit = area.y / bristleCount.y;

        for (int x = 0; x < bristleCount.x; x++) {
            for (int z = 0; z < bristleCount.y; z++) {
                GameObject bristle = Instantiate(bristlePrefab);
                bristle.transform.parent = transform;

                ConfigurableJoint joint = bristle.GetComponent<ConfigurableJoint>();
                joint.connectedBody = GetComponent<Rigidbody>();
                float xPos = (xUnit * x + xUnit / 2) - area.x / 2;
                float zPos = (zUnit * z + zUnit / 2) - area.y / 2;
                joint.connectedAnchor = new Vector3(xPos + originOffset.x, originOffset.y, zPos + originOffset.z);

                FreezeInAir fir = bristle.GetComponent<FreezeInAir>();
                fir.brushFollower = GetComponent<RBFollowTransform>();
            }
        }
    }
}
