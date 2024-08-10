using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CalculateDistances : MonoBehaviour
{

    public float CalculatexyYDistance(Vector3 basexyYCoordinate, Vector3 xyYcoordinate)
    {
        return Vector3.Distance(basexyYCoordinate, xyYcoordinate);
    }

    public float CalculateP3Distance(Color baseP3, Color P3)
    {
        Vector3 baseP3Vector = new Vector3(baseP3[0], baseP3[1], baseP3[2]);
        Vector3 P3Vector = new Vector3(P3[0], P3[1], P3[2]);
        return Vector3.Distance(baseP3Vector, P3Vector);
    }

}
