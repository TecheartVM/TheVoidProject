using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Maths
{
    public static Vector3 GetVectorIgnoreY(Vector3 vector)
    {
        vector.y = 0;
        return vector;
    }
}
