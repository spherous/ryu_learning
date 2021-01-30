using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor;

public static class TypeHash 
{
    public static BidirectionalDictionary<Type, byte[]> typeHash = new BidirectionalDictionary<Type, byte[]>();

    public static byte[] GetHash(Type t)
    {
        if(typeHash.ContainsKey(t))
            return typeHash[t];
        
        TryRegisterHash(t);
        return typeHash[t];
    }

    public static Type GetType(byte[] hash)
    {
        if(typeHash.ContainsKey(hash))
            return typeHash[hash];
        return null;
    }

    public static void TryRegisterHash(Type t)
    {
        if(typeHash.ContainsKey(t))
            return;

        SHA1 sha = SHA1.Create();
        Byte[] ba = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(t.AssemblyQualifiedName));
        typeHash.Add(t, ba);
    }

    [MenuItem("Networking/Clear Type Hash")]
    public static void ClearTypeHash() => typeHash.Clear();
}