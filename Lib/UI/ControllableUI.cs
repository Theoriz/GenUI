using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllableUI : MonoBehaviour {

    public Controllable LinkedControllable;
    
    public bool IsInteractible;

    public bool ShowDebug;

    public void CreateUI(Controllable target)
    {

    }

    public virtual void RemoveUI()
    {
        LinkedControllable.controllableValueChanged -= HandleTargetChange;

        Destroy(this.gameObject);
    }

    public virtual void HandleTargetChange(string name)
    {
    }
}
