using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialCB : MonoBehaviour, IDial
{
    public delegate void Callback(float val, int id);

    public Callback cb;

    public void DialChanged(float dialvalue)
    {
        cb(dialvalue, GetComponent<ParameterId>().Id);
    }
}
