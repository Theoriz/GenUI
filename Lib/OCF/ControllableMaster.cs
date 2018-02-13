using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ControllableMaster 
{
    public static bool debug;

    public static Dictionary<string, Controllable> RegisteredControllables = new Dictionary<string, Controllable>();

    public delegate void ControllableAddedEvent(Controllable controllable);
    public static event ControllableAddedEvent controllableAdded;

    public delegate void ControllableRemovedEvent(Controllable controllable);
    public static event ControllableRemovedEvent controllableRemoved;

    public static void Register(Controllable candidate)
    {
        if (!RegisteredControllables.ContainsKey(candidate.id))
        {
            RegisteredControllables.Add(candidate.id, candidate);
            if(controllableAdded != null) controllableAdded(candidate);

            //Debug.Log("Added " + candidate.id);
        }
        else
        {
            Debug.LogWarning("ControllerMaster already contains a Controllable named " + candidate.id);
        }
    }

    public static void UnRegister(Controllable candidate)
    {
        if (RegisteredControllables.ContainsKey(candidate.id))
        {
            RegisteredControllables.Remove(candidate.id);
            if (controllableRemoved != null) controllableRemoved(candidate);
        }
    }

    public static void UpdateValue(string target, string property, List<object> values)
    {
        if (RegisteredControllables.ContainsKey(target))
            RegisteredControllables[target].setProp(property, values);
        else
            Debug.LogWarning("Target : \"" + target + "\" is unknown !");
    }

    public static void LoadEveryPresets()
    {
        foreach (var controllable in RegisteredControllables)
        {
            controllable.Value.LoadLatestUsedPreset();
        }
    }
}