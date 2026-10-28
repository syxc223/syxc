using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System;

using UnityEngine;
public class PanelController : MonoBehaviour
{
    [Header("Action")]
    public ActionBase CurrentAction;

    [Header("Focus Navigation")]
    public Button FirstButton;

    [Header("Action Icon")]
    [SerializeField] private Image actionIcon;

    public virtual Type PanelActionType => null;

    public virtual void SetupPanel(ActionBase actionBase)
    {
        CurrentAction = actionBase;
        if (actionIcon != null)
        
            actionIcon.sprite = actionBase.CommandInfo.Icon;
        
    }
    
    public virtual void ClosePanel()
    {
        gameObject.SetActive(false);
    }

    protected void OnCancel()
    {
        GameModeManager.Instance.RequestChangeMode(GameMode.Explore);
        ClosePanel();
    }

    protected void CloseWithoutModeChange()
    {
        ClosePanel();
    }

    protected virtual void OnConfirm()
    {
        CurrentAction.Execute();
        ClosePanel();
    }


    public virtual bool HandleCancelInput()
    {
        if (!gameObject.activeSelf)
            return false;

        OnCancel();
        return true;
    }


    public void SetDefaultSelection()
    {
        if (FirstButton == null)
            return;

        FirstButton.Select();
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(FirstButton.gameObject);

        if (transform is RectTransform rectTransform)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }
    protected void RebindButtons(Button button,UnityAction unityAction)
    {
        if(button is null)
         return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(unityAction);
    }
}