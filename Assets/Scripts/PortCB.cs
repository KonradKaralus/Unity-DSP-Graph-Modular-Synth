using UnityEngine;


public interface IPort
{
    void PortTouched();
}

public class PortCB : MonoBehaviour, IPort
{
    public delegate void Callback(int id, int port, PortType type, Vector3 transform);

    public Callback cb;

    public void PortTouched()
    {

        Debug.Log("Port touched");
        cb(GetComponent<PortIds>().ModuleId, GetComponent<PortIds>().PortId, GetComponent<PortIds>().type, GetComponent<Transform>().position);
    }
}
