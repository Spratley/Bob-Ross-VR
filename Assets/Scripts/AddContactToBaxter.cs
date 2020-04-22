using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddContactToBaxter : MonoBehaviour
{
    public Baxter3D baxter3D;

    private void Start()
    {
        baxter3D = FindObjectOfType<Baxter3D>();
    }

    private void OnCollisionEnter(Collision collision) {
        if (baxter3D == null)
            return;

        if(collision.gameObject == baxter3D.gameObject) {
            baxter3D.pointsOnCanvas.Add(transform);
        }
    }

    private void OnCollisionExit(Collision collision) {
        if (baxter3D == null)
            return;

        if (collision.gameObject == baxter3D.gameObject) {
            baxter3D.pointsOnCanvas.Remove(transform);
        }
    }
}
