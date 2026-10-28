using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using System;
using UnityEngine;
public class InquirePanelController : PanelController
{
    [Header("Inquire Panel")]
    [SerializeField] private TMP_Text npcNameText;
    [SerializeField] private Image npcAvatar;
    [SerializeField] private TMP_Text messageTitleText;
    [SerializeField] private TMP_Text messageContentText;

    private InquireAction _currentAction;
    private int _currentIndex = -1;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;

    public override Type PanelActionType => typeof(InquireAction);

    public override void SetupPanel(ActionBase actionBase)
    {
        base.SetupPanel(actionBase);

        FirstButton = confirmButton;
        BindButtons();
        SetDefaultSelection();
        _currentAction = (InquireAction)CurrentAction;
        ApplyMessage(_currentAction.PickRandomMessageIndex());
    }

    private void ApplyMessage(int messageIndex)
    {

        _currentAction.GetInquireActionData(messageIndex,out InquireActionData data);
        _currentIndex = messageIndex;

        npcNameText.text = data.personName;
        npcAvatar.sprite = data.portraitOverride;
        messageTitleText.text = data.title;
        messageContentText.text = data.message;
}

    private void BindButtons()
{
    RebindButtons(confirmButton, OnCancel);
}
}