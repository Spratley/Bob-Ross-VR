using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RBFollowTransform : MonoBehaviour
{
    private Rigidbody rb;
    public Transform target;
    public Vector3 offset;
    public Vector3 rotOffset;
    public bool moveWithPhysics;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (moveWithPhysics)
        {
            rb.MovePosition(target.position + offset);
            rb.MoveRotation(target.rotation * Quaternion.Euler(rotOffset));
        }
        else
        {
            transform.position = target.position + offset;
            transform.rotation = target.rotation * Quaternion.Euler(rotOffset);
        }
    }
}
