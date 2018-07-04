using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OSCMasterControllable : Controllable {

    public OSCMaster oscmaster;
    public UIMaster MyUIMaster;

    [Header("OSC settings")]
    [OSCProperty]
    public int localPort;

    [OSCProperty(isInteractible = false)] public bool isConnected;

    //[Header("UI settings")]
    //[OSCProperty] public bool HideUIAtStart;
    //[OSCProperty] public bool HideCursorWithUI;
    
    [OSCMethod]
    public void SaveAll()
    {
        ControllableMaster.SaveAllPresets();
    }

    [OSCMethod]
    public void SaveAsAll()
    {
        ControllableMaster.SaveAsAllPresets();
    }

    [OSCMethod]
    public void LoadAll()
    {
        ControllableMaster.LoadAllPresets();
    }

    public override void Awake()
    {
        if (oscmaster == null)
            oscmaster = FindObjectOfType<OSCMaster>();

        if (oscmaster == null)
        {
            Debug.LogWarning("Can't find " + this.GetType().Name + " script to control !");
            return;
        }

        TargetScript = oscmaster;
        base.Awake();
        oscmaster.Connect();
    }

    public override void OnUiValueChanged(string name)
    {
        base.OnUiValueChanged(name);
        oscmaster.Connect();
        //MyUIMaster.AutoHideCursor = HideCursorWithUI;
        //MyUIMaster.HideUIAtStart = HideUIAtStart;
    }
}
