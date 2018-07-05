using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelUI : ControllableUI
{
    List<ControllableUI> _uiElements;

    public bool IsExpanded = true;

    public void AddUIElement(ControllableUI newElement)
    {
        if(_uiElements == null)
        {
            _uiElements = new List<ControllableUI>();
        }
        _uiElements.Add(newElement);
    }

    public override void RemoveUI()
    {
        foreach (var element in _uiElements)
            element.RemoveUI();
    }

    public void HandleClickOnButton()
    {
        IsExpanded = !IsExpanded;
        for(var i=1; i<transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(IsExpanded);
        }

        if(IsExpanded)
        {
            foreach(var element in _uiElements)
                element.HandleTargetChange("");
        }
        
        this.transform.GetChild(0).GetChild(1).rotation = Quaternion.Euler(new Vector3(0, 0, IsExpanded ? -90 : 0));
    }
    //public override void CreateUI(Controllable target)
    //{

    //}

    //public override void RemoveUI()
    //{

    //}
}
