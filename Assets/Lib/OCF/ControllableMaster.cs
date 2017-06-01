using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllableMaster : MonoBehaviour
{
    public OSCMaster OscMaster;

    public Dictionary<string, Controllable> RegisteredControllables;

    public delegate void ControllableAddedEvent(Controllable controllable);
    public event ControllableAddedEvent controllableAdded;

    public delegate void ControllableRemovedEvent(Controllable controllable);
    public event ControllableRemovedEvent controllableRemoved;

    // Use this for initialization
    void Start()
    {
        RegisteredControllables = new Dictionary<string, Controllable>();
        OscMaster.valueUpdateReady += UpdateValue;
    }

    public void Register(Controllable candidate)
    {
        if (!RegisteredControllables.ContainsKey(candidate.id))
        {
            RegisteredControllables.Add(candidate.id, candidate);
            if (controllableAdded != null) controllableAdded(candidate);
            Debug.Log("Added " + candidate.id);
        }
        else
        {
            Debug.LogWarning("ControllerMaster already contains a Controllable named " + candidate.id);
        }
        
        
        //GameObject.Find("OSC").GetComponent<UIMaster>().CreateUI();
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