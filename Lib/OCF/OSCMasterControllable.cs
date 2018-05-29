using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OSCMasterControllable : Controllable {

    public OSCMaster oscmaster;

    [OSCProperty]
    public int localPort;

    [OSCProperty(isInteractible = false)] public bool isConnected;

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

    [OSCMethod]
    public void SaveAllPresets()
    {
        ControllableMaster.SaveAllPresets();
    }


    public override void OnUiValueChanged(string name)
    {
        base.OnUiValueChanged(name);
        oscmaster.Connect();
    }
}
