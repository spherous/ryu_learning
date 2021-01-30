using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

public class NetworkedObject : SerializedMonoBehaviour
{
    private Networker networker;
    public SyncType syncType;
    public int tickrate;
    private float updateAtTime;
    public Dictionary<GameObject, List<NetworkedComponent>> objectsToControl = new Dictionary<GameObject, List<NetworkedComponent>>();

    private void Awake() 
    {
        networker = GameObject.FindObjectOfType<Networker>();
        networker.RegisterNewNetworkObject(this);
    }

    private void Update() 
    {
        if(Time.timeSinceLevelLoad >= updateAtTime)
        {
            foreach(KeyValuePair<GameObject, List<NetworkedComponent>> kvp in objectsToControl)
            {
                foreach(NetworkedComponent comp in kvp.Value)
                {
                    if(!comp.isNetworked)
                        continue;

                    try {
                        byte[] snapshot = comp.GetSnapshot();
                        if(snapshot != comp.lastSnapshot)
                            comp.UpdateSnapshot(ref snapshot);
                    } catch(NotSupportedException e) {
                        Debug.LogWarning($"{comp.component} is marked as networked, but {e}.");
                    }
                }
            }
            updateAtTime = Time.timeSinceLevelLoad + 1f/tickrate;
        }
    }

    [Button]
    public void AddObjectsAndChildren() {
        Dictionary<GameObject, List<NetworkedComponent>> oldDict = new Dictionary<GameObject, List<NetworkedComponent>>(objectsToControl);
        ClearObjects();

        List<Component> components = gameObject.GetComponents<Component>().ToList();
        components.Remove((Component)this);
        List<NetworkedComponent> networkedObjects = GetNetworkedComponents(gameObject, components, oldDict);
        objectsToControl.Add(gameObject, networkedObjects);

        for(int i = 0; i < transform.childCount; i++) {
            GameObject child = transform.GetChild(i).gameObject;
            List<Component> childComponents = child.GetComponents<Component>().ToList();
            List<NetworkedComponent> childNCs = GetNetworkedComponents(child, childComponents, oldDict);
            objectsToControl.Add(child, childNCs);
        }
    }

    [Button]
    public void ClearObjects() => objectsToControl.Clear();

    public List<NetworkedComponent> GetNetworkedComponents(GameObject obj, List<Component> components, Dictionary<GameObject, List<NetworkedComponent>> oldDict)
    {
        List<NetworkedComponent> networkedComponents = new List<NetworkedComponent>();
        foreach(Component c in components)
        {
            TypeHash.TryRegisterHash(c.GetType());
            if(oldDict.ContainsKey(obj))
            {
                List<NetworkedComponent> oldComponents = oldDict[obj];
                int? foundAtIndex = null;
                for(int i = 0; i < oldComponents.Count; i++)
                {
                    if(oldComponents[i].component == c)
                    {
                        foundAtIndex = i;
                        break;
                    }
                }
                NetworkedComponent nc = foundAtIndex == null 
                    ? new NetworkedComponent(c, true, SyncType.Synced, 12) 
                    : new NetworkedComponent(c, oldComponents[foundAtIndex.Value].isNetworked, oldComponents[foundAtIndex.Value].syncType, oldComponents[foundAtIndex.Value].tickrate);
                networkedComponents.Add(nc);
            }
            else
                networkedComponents.Add(new NetworkedComponent(c, true, SyncType.Synced, 12));
        }
        return networkedComponents;
    }
}

public struct NetworkedComponent 
{
    public Component component;
    public bool isNetworked;
    [EnableIf(nameof(isNetworked)), ShowIf(nameof(isNetworked))]
    public SyncType syncType;
    [EnableIf(nameof(isNetworked)), ShowIf(nameof(isNetworked))]
    public int tickrate;

    public byte[] lastSnapshot;
    public bool snapshotHasChanged;

    public NetworkedComponent(Component component, bool isNetworked, SyncType syncType, int tickrate)
    {
        this.component = component;
        this.isNetworked = isNetworked;
        this.syncType = syncType;
        this.tickrate = tickrate;
        lastSnapshot = new byte[0];
        snapshotHasChanged = false;
    }

    public void UpdateSnapshot(ref byte[] snapshot)
    {
        snapshotHasChanged = true;
        lastSnapshot = snapshot;
    }

    public byte[] GetSnapshot()
    {
        byte[] snapshot;
        if(component is ISerializer)
        {
            ISerializer serializer = (ISerializer)component;
            snapshot = serializer.Serialize();
        }
        else if(component is Transform)
        {
            Transform t = (Transform)component;
            snapshot = t.Serialize();
        }
        else
            throw new NotSupportedException($"{component.GetType()} can not be serialized.");

        return snapshot;
    }

    public void MarkSent() => snapshotHasChanged = false;
}

public enum SyncType {Synced, RPC}