using System;
using System.Collections;
using System.Collections.Generic;
using MsgPack.Serialization;
using UnityEngine;

public struct PaddleData : ISerializer
{
    public Color color;
    public float angle;
    public int id;

    static readonly MessagePackSerializer<PaddleData> serializer = MessagePackSerializer.Get<PaddleData>();
    
    public PaddleData(Color color, float angle, int id)
    {
        this.color = color;
        this.angle = angle;
        this.id = id;
    }

    public override string ToString() => $"{color}, {angle}, {id}";

    public void Deserialize(byte[] data)
    {
        PaddleData pd = serializer.UnpackSingleObject(data);
        color = pd.color;
        angle = pd.angle;
        id = pd.id;
    }

    public byte[] Serialize() => serializer.PackSingleObject(this);
}