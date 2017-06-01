using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllableMaster : MonoBehaviour
{
    public bool debug;
    private bool isReadyBool;
    public OSCMaster OscMaster;

    public Dictionary<string, Controllable> RegisteredControllables;

    public delegate void ControllableAddedEvent(Controllable controllable);
    public event ControllableAddedEvent controllableAdded;

    public delegate void ControllableRemovedEvent(Controllable controllable);
    public event ControllableRemovedEvent controllableRemoved;

    public bool isReady()
    {
        return isReadyBool;
    }
    // Use this for initialization
    void Awake()
    {
        RegisteredControllables = new Dictionary<string, Controllable>();
        OscMaster.valueUpdateReady += UpdateValue;
        isReadyBool = true;
    }

    public void Register(Controllable candidate)
    {
        if (!RegisteredControllables.ContainsKey(candidate.id))
        {
            RegisteredControllables.Add(candidate.id, candidate);
            if (controllableAdded != null) controllableAdded(candidate);
            if(debug)
                Debug.Log("Added " + candidate.id);
        }
        else
        {
            Debug.LogWarning("ControllerMaster already contains a Controllable named " + candidate.id);
        }
    }

    public void UnRegister(Controllable candidate)
    {
        if(RegisteredControllables.ContainsKey(candidate.id)) RegisteredControllables.Remove(candidate.id);
        if (controllableAdded != null) controllableRemoved(candidate);
    }

    public void UpdateValue(string target, string property, List<object> values)
    {
        if (RegisteredControllables.ContainsKey(target))
            RegisteredControllables[target].setProp(property, values);
        else
            Debug.LogWarning("Target : \"" + target + "\" is unknown !");
    }
}