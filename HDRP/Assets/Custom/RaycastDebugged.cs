using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaycastDebugged : MonoBehaviour
{
    public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask, bool debugEnabled)
    {
        return Raycast(origin, direction, out hitInfo, maxDistance, layerMask, debugEnabled, Color.white);
    }

    public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask, bool debugEnabled, Color color)
    {
        bool value = Physics.Raycast(origin, direction, out hitInfo, maxDistance, layerMask);

        if(debugEnabled)
        {
            Debug.DrawLine(origin, value ? hitInfo.point : origin + direction * maxDistance, color);
        }

        return value;
    }

    public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask, bool debugEnabled, Color color, float debugDuration)
    {
        bool value = Physics.Raycast(origin, direction, out hitInfo, maxDistance, layerMask);

        if (debugEnabled)
        {
            Debug.DrawLine(origin, value ? hitInfo.point : origin + direction * maxDistance, color, debugDuration);
        }

        return value;
    }
}
