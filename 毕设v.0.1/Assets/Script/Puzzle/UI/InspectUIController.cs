using MFramework.Event;

using UnityEngine;
public class InspectUIController : MonoBehaviour, IEventReceiver<InspectRequestEvent>
{
    [SerializeField] private InspectPanelController inspectPanel;

    private void Awake()
    {
        if (inspectPanel == null)
            inspectPanel = GetComponentInChildren<InspectPanelController>(true);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<InspectRequestEvent>(this);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<InspectRequestEvent>(this);
    }

    public void OnEvent(InspectRequestEvent evt)
    {
        if (inspectPanel == null || evt.Target == null)
            return;

        inspectPanel.gameObject.SetActive(true);
        inspectPanel.SetupPanel(evt.Target);
    }
}
