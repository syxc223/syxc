using UnityEngine;

/// <summary>
/// Main menu screen manager.
/// Switches between LoginPanel and RoomPanel.
/// Persists across scenes via DontDestroyOnLoad.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] GameObject _loginPanel;
    [SerializeField] GameObject _roomPanel;
    [SerializeField] CanvasGroup _menuGroup;

    LoginPanel _login;
    RoomPanel _room;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        _login = _loginPanel?.GetComponent<LoginPanel>();
        _room = _roomPanel?.GetComponent<RoomPanel>();

        if (_login != null)
            _login.OnLoginSuccess += OnLoginSuccess;
        if (_room != null)
        {
            _room.OnLogout += ShowLogin;
            _room.OnEnterGame += TransitionToGame;
        }

        if (ApiClient.IsLoggedIn)
            ShowRoom();
        else
            ShowLogin();

        AudioManager.Instance?.PlayBgm("MenuBGM");
    }

    public void ShowLogin()
    {
        if (_menuGroup != null) _menuGroup.alpha = 1f;
        gameObject.SetActive(true);
        if (_loginPanel != null) _loginPanel.SetActive(true);
        if (_roomPanel != null) _roomPanel.SetActive(false);
    }

    public void ShowRoom()
    {
        if (_menuGroup != null) _menuGroup.alpha = 1f;
        gameObject.SetActive(true);
        if (_loginPanel != null) _loginPanel.SetActive(false);
        if (_roomPanel != null) _roomPanel.SetActive(true);
    }

    /// <summary>
    /// Return to lobby from game (ESC exit room).
    /// </summary>
    public void ShowLobby()
    {
        if (_menuGroup != null) _menuGroup.alpha = 1f;
        gameObject.SetActive(true);
        if (_loginPanel != null) _loginPanel.SetActive(false);
        if (_roomPanel != null) _roomPanel.SetActive(true);
    }

    void OnLoginSuccess(string username)
    {
        ShowRoom();
    }

    void TransitionToGame()
    {
        if (_menuGroup != null)
            StartCoroutine(FadeOutMenu());
        else
            gameObject.SetActive(false);
    }

    System.Collections.IEnumerator FadeOutMenu()
    {
        float duration = 0.5f;
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            _menuGroup.alpha = 1f - t / duration;
            yield return null;
        }
        _menuGroup.alpha = 0f;
        gameObject.SetActive(false);
    }
}
