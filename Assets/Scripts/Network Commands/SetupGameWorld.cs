using System.Collections.Generic;
using MsgPack.Serialization;

public class SetupGameWorld : ISerializer
{
    static readonly MessagePackSerializer<SetupGameWorld> serializer = MessagePackSerializer.Get<SetupGameWorld>();

    List<PaddleData> paddles = new List<PaddleData>();

    public void Deserialize(byte[] data)
    {
        
    }

    public byte[] Serialize()
    {
        byte[] ba = new byte[1];
        return ba;
    }
}