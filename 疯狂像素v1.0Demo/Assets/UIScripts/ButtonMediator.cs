using UnityEngine;
using UnityEngine.UI;

public class ButtonMediator : MonoBehaviour
{
    public Button BtnExit { get; private set; }
    public Button BtnSetting { get; private set; }
    public Button BtnProducer { get; private set; }
    public Button BtnExitSetting { get; private set; }
    public Button BtnExitProducer { get; private set; }

    public GameObject SetPanel { get; private set; }
    public GameObject ProducerPanel { get; private set; }

    private bool _settingClicked = false;
    private float _lastSettingCallTime = 0f;
    private const float SETTING_CALL_COOLDOWN = 0.2f; // 200ms 冷却时间，防止重复调用

    public void Init(Button exit, Button setting, Button producer, Button exitSetting, Button exitProducer, GameObject setPanel, GameObject producerPanel)
    {
        BtnExit = exit;
        BtnSetting = setting;
        BtnProducer = producer;
        BtnExitSetting = exitSetting;
        BtnExitProducer = exitProducer;
        SetPanel = setPanel;
        ProducerPanel = producerPanel;

        if (SetPanel != null) SetPanel.SetActive(false);
        if (ProducerPanel != null) ProducerPanel.SetActive(false);

        if (BtnProducer != null) BtnProducer.interactable = true;
        _settingClicked = false;
    }

    // Called by the Facade when exit is requested (kept for symmetry)
    public void OnExit()
    {
        // nothing to manage for exit in UI
    }

    public void OnSetting()
    {
        // 防抖机制：如果距离上次调用时间太短，忽略此次调用
        float currentTime = Time.time;
        if (currentTime - _lastSettingCallTime < SETTING_CALL_COOLDOWN)
        {
            Debug.LogWarning($"ButtonMediator: OnSetting() 调用过于频繁，忽略此次调用（距离上次调用 {(currentTime - _lastSettingCallTime) * 1000f:F0}ms）");
            return;
        }
        _lastSettingCallTime = currentTime;
        
        Debug.Log($"ButtonMediator: OnSetting() 被调用，SetPanel = {(SetPanel != null ? SetPanel.name : "null")}");
        
        if (SetPanel == null)
        {
            Debug.LogError("ButtonMediator: SetPanel 为 null！请检查 BtnManagerControl 是否正确分配了 SetPanel 引用。");
            return;
        }
        
        bool isOpen = SetPanel.activeSelf;
        Debug.Log($"ButtonMediator: SetPanel 当前状态 = {(isOpen ? "打开" : "关闭")}");
        
        if (isOpen)
        {
            // close setting panel and re-enable producer
            SetPanel.SetActive(false);
            _settingClicked = false;
            if (BtnProducer != null) BtnProducer.interactable = true;
            Debug.Log("ButtonMediator: 关闭设置面板");
        }
        else
        {
            // open setting panel and disable producer
            _settingClicked = true;
            SetPanel.SetActive(true);
            if (ProducerPanel != null) ProducerPanel.SetActive(false);
            if (BtnProducer != null) BtnProducer.interactable = false;
            Debug.Log("ButtonMediator: 打开设置面板");
        }
    }

    public void OnProducer()
    {
        // If Setting is open, Producer should be disabled
        if (_settingClicked)
        {
            return;
        }

        bool isOpen = ProducerPanel != null && ProducerPanel.activeSelf;
        if (isOpen)
        {
            if (ProducerPanel != null) ProducerPanel.SetActive(false);
        }
        else
        {
            if (ProducerPanel != null) ProducerPanel.SetActive(true);
            if (SetPanel != null) SetPanel.SetActive(false);
        }
    }

    // Called when user clicks the Exit button inside the Setting panel
    public void OnExitSetting()
    {
        if (SetPanel != null && SetPanel.activeSelf)
        {
            SetPanel.SetActive(false);
        }
        _settingClicked = false;
        if (BtnProducer != null) BtnProducer.interactable = true;
    }

    // Called when user clicks the Exit button inside the Producer panel
    public void OnExitProducer()
    {
        if (ProducerPanel != null && ProducerPanel.activeSelf)
        {
            ProducerPanel.SetActive(false);
        }
    }
}
