using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainPanelController : PanelController
{
    [Header("Main Panel")]
    [SerializeField] private Button itemButton;
    [SerializeField] private Button equipmentButton;
    [SerializeField] private TMP_Text currencyAmountText;

    [Header("Panels")]
    [SerializeField] private ItemPanelController itemPanelController;
    [SerializeField] private EquipmentPanelController equipmentPanelController;
    [SerializeField] private CanvasGroup leftPartCanvasGroup;
    [SerializeField] private GameObject candidatePanel;

    private GameObject _currentPanel;
    private Button _lastButton;

    private void Awake()
    {
        RebindButtons(itemButton, OpenItemPanel);
        RebindButtons(equipmentButton, OpenEquipmentPanel);

        if (candidatePanel == null)
        {
            Transform candidateTransform = transform.Find("Candidate Panel");
            if (candidateTransform != null)
                candidatePanel = candidateTransform.gameObject;
        }

        if (candidatePanel != null)
            candidatePanel.SetActive(false);
    }

    void OnEnable()
    {
        FirstButton = itemButton;
        SetDefaultSelection();
        UpdateCurrencyDisplay();
    }

    void OnDisable()
    {
        if (candidatePanel != null)
            candidatePanel.SetActive(false);

        if (_currentPanel != null)
            _currentPanel.SetActive(false);

        leftPartCanvasGroup.interactable = true;
        _currentPanel = null;
    }

    private void OpenItemPanel()
    {
        OpenPanel(itemPanelController.gameObject, itemButton);
        itemPanelController.SetupPanel(PanelType.Item);
    }

    private void OpenEquipmentPanel()
    {
        OpenPanel(equipmentPanelController.gameObject, equipmentButton);
        equipmentPanelController.SetupWithPartyMember(PartyManager.Instance != null ? PartyManager.Instance.PartyMembers : null);
        equipmentPanelController.OnPanelOpened();
    }

    private void OpenPanel(GameObject panel, Button sourceButton)
    {
        _lastButton = sourceButton;
        panel.SetActive(true);
        _currentPanel = panel;
        leftPartCanvasGroup.interactable = false;
    }

    public void OpenCandidatePanel()
    {
        if (candidatePanel == null)
            return;

        candidatePanel.SetActive(true);
        equipmentPanelController?.SetOverlayMode(true);
    }

    public void CloseCandidatePanel()
    {
        if (candidatePanel == null || !candidatePanel.activeSelf)
            return;

        candidatePanel.SetActive(false);
        equipmentPanelController?.SetOverlayMode(false);
        equipmentPanelController?.RestoreDefaultSelection();
    }

    public override bool HandleCancelInput()
    {
        if (_currentPanel == null || !_currentPanel.activeSelf)
            return false;

        if (_currentPanel == equipmentPanelController.gameObject
            && equipmentPanelController.HandleCancelInput())
            return true;

        CloseCurrentPanel();
        return true;
    }

    private void CloseCurrentPanel()
    {
        FirstButton = _lastButton;
        _currentPanel.SetActive(false);
        _currentPanel = null;

        leftPartCanvasGroup.interactable = true;
        SetDefaultSelection();
    }

    private void UpdateCurrencyDisplay()
    {
        if (currencyAmountText == null)
            return;

        int currentMoney = InventoryManager.Instance.Currency;
        currencyAmountText.text = $"持有金额: {currentMoney}";
    }
}
