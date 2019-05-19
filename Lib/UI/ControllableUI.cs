using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

public class ControllableUI : MonoBehaviour {

    public Controllable LinkedControllable;
    public FieldInfo Property;
    public MethodInfo Method;

    public bool IsInteractible;

    public bool ShowDebug;

    public virtual void RemoveUI()
    {
        LinkedControllable.controllableValueChanged -= HandleTargetChange;

        Destroy(this.gameObject);
    }

    public virtual void HandleTargetChange(string name)
    {
    }

    public void CopyAddressToClipboard()
    {

        GUIUtility.systemCopyBuffer = "/" + ControllableMaster.instance.RootOSCAddress + "/" + LinkedControllable.id + "/" + (Property == null ? Method.Name : Property.Name) ;
    }
}
