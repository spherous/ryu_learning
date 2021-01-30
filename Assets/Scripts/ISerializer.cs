using MsgPack.Serialization;

interface ISerializer
{
    byte[] Serialize();
    void Deserialize(byte[] data);
}