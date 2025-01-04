using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vector3Byte : MonoBehaviour
{
    public static void Vector3ToByte(Vector3 vect, ref byte[] bytes)
    {
        bytes[0] = Convert.ToByte(vect.x);
        bytes[1] = Convert.ToByte(vect.y);
        bytes[2] = Convert.ToByte(vect.z);
    }

    public static Vector3Int ByteToVector3(byte[] bytes)
    {
        Vector3 vect = Vector3.zero;
        vect.x = Convert.ToSingle(bytes[0]);
        vect.y = Convert.ToSingle(bytes[1]);
        vect.z = Convert.ToSingle(bytes[2]);

        return Vector3Int.FloorToInt(vect);
    }
}
