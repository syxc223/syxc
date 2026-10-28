using MFramework.Event;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExaminePanelController : PanelController, IEventReceiver<ExamineRequestEvent>
{
    [Header("Examine Panel")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text contentText;
    [SerializeField] private Image previewImage;
    [SerializeField] private Button closeButton;

    private void OnEnable()
    {
        EventBus.Subscribe<ExamineRequestEvent>(this);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<ExamineRequestEvent>(this);
    }

    public void OnEvent(ExamineRequestEvent evt)
    {
        gameObject.SetActive(true);

        if (titleText != null)
            titleText.text = evt.Title;
        if (contentText != null)
            contentText.text = evt.Description;
        if (previewImage != null)
        {
            previewImage.sprite = evt.PreviewImage;
            previewImage.enabled = evt.PreviewImage != null;
        }

        FirstButton = closeButton;
        RebindButtons(closeButton, OnCancel);
        SetDefaultSelection();
        GameModeManager.Instance?.RequestChangeMode(GameMode.PuzzleInspect);
    }
}
