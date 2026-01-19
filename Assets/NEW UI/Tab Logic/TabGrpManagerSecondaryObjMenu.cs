
using System;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Manages the tab group for the secondary menu of TTRPG_SceneObjectBase objects
/// </summary>
public class TabGrpManagerSecondaryObjMenu : TabGroupManagerBase
{
    public TabGroup tabGroup;
    public AnimatorHelper secondaryMenuTabAH;           // has animations for the secondary menu tabs (appearing, disappearing)
    public AnimatorHelper secondaryMenuAndGameViewAH;   // has animations and controls the size of the Secondary Menu and Game View GameObjects

    [SerializeField] private bool secondaryMenuHidden;

    protected override void OnStart()
    {
        if (tabGroup == null)
            ErrorOut.Throw(this, "tab group null");
        if (secondaryMenuTabAH == null)
            ErrorOut.Throw(this, "secondaryMenuAnimatorHelper null");
        if (secondaryMenuAndGameViewAH == null)
            ErrorOut.Throw(this, "secondaryMenuAndGameViewAH null");

        secondaryMenuHidden = true;
    }

    public void OnSelectedObjectChanged(Component component, object data)
    {
        if (data is ChangedObject changedObject)
        {
            TTRPG_SceneObjectBase selectedObj = changedObject.newSelectedObj;
            TTRPG_SceneObjectBase prevSelectedObj = changedObject.prevSelectedObj;

            // show the secondary menu tab & panel only when there was no selected object previously
            if (selectedObj != null && prevSelectedObj == null)
            {
                _selectedTabGrp = tabGroup;

                DebugOut.Log(this, $"triggering Reveal! at time: {Time.timeAsDouble}", debugDisabled);
                tabGroup.ActivateTabGroup();                        // the tab group class will call this class's `OnTabSelected()` function
                tabGroup.OnTabSelected(tabGroup.tabButtons[0]);     // select some arbitrary tab

                tabGroup.CheckAnimationTrigger("Reveal");
                secondaryMenuTabAH.CheckAnimationTrigger("Reveal");
            }
            // hide the secondary menu tab & panel when there is no selected object (ie selected the same Scene Object)
            else if (selectedObj == null)
            {
                DebugOut.Log(this, $"triggering hide! at time: {Time.timeAsDouble}", debugDisabled);
                tabGroup.CheckAnimationTrigger("Hide");
                secondaryMenuTabAH.CheckAnimationTrigger("Hide");
                _selectedTabGrp.selectedTab = null;     // manually reset the selected tab button 
                HideSecondaryMenu();
            }
        }
        else
        {
            WarningOut.Log(this, " - OnSelectedObjectChanged() - data is not ChangedObject"); // will be the case when there are other classes that inherit (extend) TTRPG_SceneObjectBase!
        }
    }

    /// <summary>
    /// Called by the "hide" animation when it finishes
    /// </summary>
    public override void OnHideAnimationFinish()
    {
        DebugOut.Log(this, $"finishing hide at time: {Time.timeAsDouble}!", debugDisabled);

        tabGroup.ExitTabGroup();
        _selectedTabGrp = null;
    }

    public override void OnRevealAnimationFinish()
    {
        // do nothing
        DebugOut.Log(this, $"finishing reveal at time: {Time.timeAsDouble}!", debugDisabled);
    }

    private void RevealSecondaryMenu()
    {
        if (!secondaryMenuHidden)   // secondary menu is already opened
            return;

        DebugOut.Log(this, $"RevealSecondaryMenu()", debugDisabled);
        //DebugOut.Log(this, $"RevealSecondaryMenu() | Stack trace: {Environment.StackTrace}", debugDisabled);
        secondaryMenuAndGameViewAH.CheckAnimationTrigger("Reveal");
        secondaryMenuHidden = false;
    }

    private void HideSecondaryMenu()
    {
        if (secondaryMenuHidden)    // secondary menu is already hidden
            return;

        DebugOut.Log(this, $"HideSecondaryMenu()", debugDisabled);
        secondaryMenuAndGameViewAH.CheckAnimationTrigger("Hide");
        secondaryMenuHidden = true;
    }

    public override void OnTabSelected(ChangedTabButton button)
    {
        if (button.prevTabButton == null && button.newTabButton == null)    // should never be called
            WarningOut.Log(this, "something wrong!!!");

        string str = "- OnTabSelected() -";

        if (button.prevTabButton != null && button.newTabButton != null)
        {
            DebugOut.Log(this, "-insert transition from one tab to the other-", debugDisabled);
            // transition from one tab to the other

            // Note: If you see the `swapToObject` of the prev button instantly disapearing when the same tab button is selected,
            // its because the button's Deselect() is called on it and sets its active status to false, so that's why.
        }
        // no new button was pressed, hide the secondary tab menu (ie selected the same button)
        else if (button.prevTabButton != null && button.newTabButton == null)
        {
            DebugOut.Log(this, $"{str} null button", debugDisabled);
            HideSecondaryMenu();
        }
        // otherwise reveal the secondary tab 
        else
        {
            DebugOut.Log(this, $"{str} new button clicked", debugDisabled);
            RevealSecondaryMenu();
        }

    }

    //public void OnSelectedObject(Component component, object data)
    //{
    //    _selectedGrp = tabGroup;
    //    if (_selectedGrp != null)
    //    {
    //        doingAnimation = true;
    //        DebugOut.Log(this, $"triggering Reveal! at time: {Time.timeAsDouble}");
    //        tabGroup.ActivateTabGroup();
    //        tabGroup.OnTabSelected(tabGroup.tabButtons[0]);     // select some arbitrary tab

    //        tabGroup.CheckAnimationTrigger("Reveal");
    //        secondaryMenuAnimatorHelper.CheckAnimationTrigger("Reveal");
    //    }
    //}

    //public void OnDeselectedObject(Component component, object data)
    //{
    //    if (_selectedGrp != null)
    //    {
    //        doingAnimation = true;
    //        DebugOut.Log(this, $"triggering hide! at time: {Time.timeAsDouble}");
    //        tabGroup.CheckAnimationTrigger("Hide");
    //        secondaryMenuAnimatorHelper.CheckAnimationTrigger("Hide");
    //    }
    //}

    ///// <summary>
    ///// Called by the "reveal" animation when it finishes
    ///// </summary>
    //public void FinishReveal()
    //{
    //    DebugOut.Log(this, $"finishing Reveal at time: {Time.timeAsDouble}!");

    //    doingAnimation = false;
    //}
}
