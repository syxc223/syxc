using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InspectPanelController : PanelController
{
    [Header("Inspect Panel")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text contentText;
    [SerializeField] private Image previewImage;
    [SerializeField] private Button closeButton;

    public override Type PanelActionType => typeof(InspectableObject);

    public override void SetupPanel(ActionBase actionBase)
    {
        base.SetupPanel(actionBase);

        var inspectable = actionBase as InspectableObject;
        if (inspectable == null)
            return;

        if (titleText != null)
            titleText.text = inspectable.InspectTitle;
        if (contentText != null)
            contentText.text = inspectable.InspectDescription;
        if (previewImage != null)
        {
            previewImage.sprite = inspectable.InspectImage;
            previewImage.enabled = inspectable.InspectImage != null;
        }

        FirstButton = closeButton;
        RebindButtons(closeButton, OnCancel);
        SetDefaultSelection();
        GameModeManager.Instance.RequestChangeMode(GameMode.PuzzleInspect);
    }
}
