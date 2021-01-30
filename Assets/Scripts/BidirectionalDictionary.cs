using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

public class BidirectionalDictionary<T, K> : ICollection<KeyValuePair<T, K>>, IEnumerable<KeyValuePair<T, K>>, IEnumerable, IDictionary<T, K>, IReadOnlyCollection<KeyValuePair<T, K>>, IReadOnlyDictionary<T, K>, ICollection, IDictionary, IDeserializationCallback, ISerializable
{
    private Dictionary<T, K> forwardDict = new Dictionary<T, K>();
    private Dictionary<K, T> backwardsDict = new Dictionary<K, T>();

    public K this[T key] { get => forwardDict[key]; set{} }
    public T this[K key] { get => backwardsDict[key]; set{} }

    public object this[object key] { get {
        if(key is T) 
            return forwardDict[(T)key];
        else if(key is K)
            return backwardsDict[(K)key];
        else
            return null;
        } 
        set {} 
    }

    public int Count => forwardDict.Count;
    public bool IsFixedSize => false;

    public ICollection<T> Keys => forwardDict.Keys;
    public ICollection<K> Values => forwardDict.Values;

    public bool IsReadOnly => false;
    public bool IsSynchronized => false;
    public object SyncRoot => null;

    IEnumerable<T> IReadOnlyDictionary<T, K>.Keys => forwardDict.Keys;
    ICollection IDictionary.Keys => forwardDict.Keys;
    IEnumerable<K> IReadOnlyDictionary<T, K>.Values => forwardDict.Values;
    ICollection IDictionary.Values => forwardDict.Values;

    public void Add(T key, K value)
    {
        if(!ContainsKey(key) && !ContainsKey(value))
        {
            forwardDict.Add(key, value);
            backwardsDict.Add(value, key);
            return;
        }
        throw new InvalidOperationException("Matching key found.");
    }
    public void Add(K key, T value) => Add(value, key);
    public void Add(KeyValuePair<T, K> item) => Add(item.Key, item.Value);
    public void Add(KeyValuePair<K, T> item) => Add(item.Value, item.Key);
    public void Add(object key, object value)
    {
        if(key is T && value is K)
            Add((T)key, (K)value);
        else if(key is K && value is T)
            Add((K)value, (T)key);
    }

    public bool Remove(KeyValuePair<T, K> item) => forwardDict.Remove(item.Key) && backwardsDict.Remove(item.Value);
    public bool Remove(KeyValuePair<K, T> item) => backwardsDict.Remove(item.Key) && forwardDict.Remove(item.Value);
    public bool Remove(T key) => throw new InvalidOperationException("Requires both key and value to remove.");
    public void Remove(object key) => throw new InvalidOperationException("Requires both key and value to remove.");

    public void Clear()
    {
        forwardDict.Clear();
        backwardsDict.Clear();
    }

    public bool ContainsKey(T key) => forwardDict.ContainsKey(key);
    public bool ContainsKey(K key) => backwardsDict.ContainsKey(key);
    public bool Contains(KeyValuePair<T, K> item) => ContainsKey(item.Key);
    public bool Contains(KeyValuePair<K, T> item) => ContainsKey(item.Key);

    public bool Contains(object key)
    {
        if(key is T)
            return ContainsKey((T)key);
        else if(key is K)
            return ContainsKey((K)key);
        else
            return false;
    }

    public bool TryGetValue(T key, out K value) => forwardDict.TryGetValue(key, out value);
    public bool TryGetValue(K key, out T value) => backwardsDict.TryGetValue(key, out value);

    public void CopyTo(KeyValuePair<T, K>[] array, int arrayIndex) => throw new System.NotImplementedException();
    public void CopyTo(KeyValuePair<K, T>[] array, int arrayIndex) => throw new NotImplementedException();
    public void CopyTo(Array array, int index) => throw new NotImplementedException();
    public void GetObjectData(SerializationInfo info, StreamingContext context) => throw new NotImplementedException();
    public void OnDeserialization(object sender) => throw new NotImplementedException();
    
    public IEnumerator<KeyValuePair<T, K>> GetEnumerator() => forwardDict.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    IDictionaryEnumerator IDictionary.GetEnumerator() => forwardDict.GetEnumerator();
}