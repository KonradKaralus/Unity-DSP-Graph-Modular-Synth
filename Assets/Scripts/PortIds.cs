using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PortType
{
    Input,
    Output
}

public class PortIds: MonoBehaviour
{
    public int ModuleId;
    public int PortId;
    public PortType type;
}
