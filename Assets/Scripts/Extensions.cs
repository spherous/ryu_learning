using UnityEngine;
using MsgPack.Serialization;
using System;

public static class Extensions
{
    static readonly MessagePackSerializer<Vector3> v3s = MessagePackSerializer.Get<Vector3>();
    
    public static byte[] Serialize(this Transform t) 
    {
        byte[] ba = new byte[48];
        Buffer.BlockCopy(v3s.PackSingleObject(t.position), 0, ba, 0, 16);
        Buffer.BlockCopy(v3s.PackSingleObject(t.eulerAngles), 0, ba, 16, 16);
        Buffer.BlockCopy(v3s.PackSingleObject(t.localScale), 0, ba, 32, 16);
        return ba;
    }

    public static void Deserialize<T>(this byte[] ba, GameObject obj) where T : Transform
    {
        byte[] p = new byte[16];
        byte[] r = new byte[16];
        byte[] s = new byte[16];
        Buffer.BlockCopy(ba, 0, p, 0, 16);
        Buffer.BlockCopy(ba, 16, r, 0, 16);
        Buffer.BlockCopy(ba, 32, s, 0, 16);

        obj.transform.position = v3s.UnpackSingleObject(p);
        obj.transform.eulerAngles = v3s.UnpackSingleObject(r);
        obj.transform.localScale = v3s.UnpackSingleObject(s);
    }
}