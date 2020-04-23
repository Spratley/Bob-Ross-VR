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

    private void OnTriggerEnter(Collider other)
    {
        ColorPicker tryCP;
        if (other.TryGetComponent(out tryCP))
        {
            baxter3D.rgb = tryCP.GetColorVec3();
            baxter3D.RefillPaint();
        }
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

    private void OnDestroy()
    {
        if (baxter3D == null)
            return;

        baxter3D.pointsOnCanvas.Remove(transform);
        
    }
}
