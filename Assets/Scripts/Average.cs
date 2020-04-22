using System.Collections;
using System.Collections.Generic;
using UnityEngine;

static class ExtraMath
{
    public static Vector3 AveragePosition(List<Transform> input)
    {
        Vector3 average = Vector3.zero;
        foreach (var trans in input)
            average += trans.position;
        return average / input.Count;
    }
}