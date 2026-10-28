using System.Collections.Generic;
using MFramework.Event;
using TMPro;
using UnityEngine;

public class EvidenceJournalController : MonoBehaviour, IEventReceiver<EvidenceCollectedEvent>, IEventReceiver<ClueCollectedEvent>
{
    [Header("Evidence Journal")]
    [SerializeField] private GameObject journalRoot;
    [SerializeField] private TMP_Text journalContentText;

    private void Awake()
    {
        if (journalRoot != null)
            journalRoot.SetActive(false);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<EvidenceCollectedEvent>(this);
        EventBus.Subscribe<ClueCollectedEvent>(this);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<EvidenceCollectedEvent>(this);
        EventBus.Unsubscribe<ClueCollectedEvent>(this);
    }

    private void Update()
    {
        if (journalRoot == null || !journalRoot.activeSelf)
            return;

        if (InputSystemController.Instance != null && InputSystemController.Instance.GetUICancelPressed())
            CloseJournal();
    }

    public void ToggleJournal()
    {
        if (journalRoot == null)
            return;

        bool nextActive = !journalRoot.activeSelf;
        journalRoot.SetActive(nextActive);

        if (nextActive)
        {
            RefreshJournal();
            GameModeManager.Instance?.RequestChangeMode(GameMode.Pause);
        }
        else
        {
            GameModeManager.Instance?.RequestChangeMode(GameMode.Explore);
        }
    }

    public void OnEvent(EvidenceCollectedEvent evt) => RefreshJournalIfOpen();
    public void OnEvent(ClueCollectedEvent evt) => RefreshJournalIfOpen();

    private void RefreshJournalIfOpen()
    {
        if (journalRoot != null && journalRoot.activeSelf)
            RefreshJournal();
    }

    private void RefreshJournal()
    {
        if (journalContentText == null)
            return;

        var evidenceList = InvestigationService.Instance?.GetCollectedEvidenceDefinitions();
        if (evidenceList == null || evidenceList.Count == 0)
        {
            journalContentText.text = "（暂无证物）\n去调查场景、与角色对话来收集线索。";
            return;
        }

        var lines = new List<string> { "—— 证物手册 ——" };
        for (int i = 0; i < evidenceList.Count; i++)
        {
            var evidence = evidenceList[i];
            lines.Add($"• {evidence.ItemName}");
            if (!string.IsNullOrEmpty(evidence.DetailDescription))
                lines.Add($"  {evidence.DetailDescription}");
            else if (!string.IsNullOrEmpty(evidence.ItemDescription))
                lines.Add($"  {evidence.ItemDescription}");
        }

        journalContentText.text = string.Join("\n", lines);
    }

    private void CloseJournal()
    {
        if (journalRoot != null)
            journalRoot.SetActive(false);
        GameModeManager.Instance?.RequestChangeMode(GameMode.Explore);
    }
}
