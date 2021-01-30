using System;
using System.Net.NetworkInformation;
using System.Collections.Generic;

/// <summary>
/// Represents a network interface as configured by the OS
/// </summary>
/// <remarks>This is just a wrapper for the .NET System.Net.NetworkInformation.NetworkInterface object 
/// to simplify some aspects of the code.  If this object does not suit your purposes, feel free to change it.</remarks>
public class RNetworkInterface
{
    NetworkInterface _netI;
    
    #region Properties
    public bool SupportsIP4
    {
        get { return _netI.Supports(NetworkInterfaceComponent.IPv4); }
    }
    public bool SupportsIP6
    {
        get { return _netI.Supports(NetworkInterfaceComponent.IPv6); }
    }

    public string Description
    {
        get { return _netI.Description; }
    }
    
    public IPv4InterfaceProperties GetIPv4Properties()
    {
        return _netI.GetIPProperties().GetIPv4Properties();
    }
    public IPv6InterfaceProperties GetIPv6Properties()
    {
        return _netI.GetIPProperties().GetIPv6Properties();
    }

    public IPv4InterfaceStatistics GetIPv4Statistics()
    {
        return _netI.GetIPv4Statistics();
    }

    public string Id
    {
        get { return _netI.Id; }
    }

    public bool IsReceiveOnly
    {
        get { return _netI.IsReceiveOnly; }
    }

    public string Name
    {
        get { return _netI.Name; }
    }
    
    public OperationalStatus OperationalStatus
    {
        get { return _netI.OperationalStatus; }
    }

    public long Speed
    {
        get { return _netI.Speed; }
    }
    
    public bool SupportsMulticast
    {
        get { return _netI.SupportsMulticast; }
    }

    public NetworkInterfaceType InterfaceType
    {
        get { return _netI.NetworkInterfaceType; }
    }
    #endregion

    internal RNetworkInterface(NetworkInterface ni)
    {
        _netI = ni;
    }
}