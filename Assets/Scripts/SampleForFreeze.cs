using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ConfigurableJoint))]
public class SampleForFreeze : MonoBehaviour
{
    List<GameObject> collisions;
    FreezeInAir master = null;

    public ConfigurableJoint joint;

    private void Start()
    {
        joint = GetComponent<ConfigurableJoint>();
        collisions = new List<GameObject>();
    }

    public void SetMaster(FreezeInAir master)
    {
        this.master = master;
    }

    public bool GetInAir()
    {
        return collisions.Count == 0;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (master == null)
            return;

        // If the object is now grounded
        if (GetInAir())
            master.TryThaw();
        if (!collisions.Contains(collision.gameObject))
            collisions.Add(collision.gameObject);
    }

    private void OnCollisionExit(Collision collision)
    {
        if (!GetInAir())
            master.TryFreeze();
        if (collisions.Contains(collision.gameObject))
            collisions.Remove(collision.gameObject);
    }
}
