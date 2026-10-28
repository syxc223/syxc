using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Login & registration panel.
/// Calls AuthApi for login/register, notifies UIManager on success.
/// </summary>
public class LoginPanel : MonoBehaviour
{
    [Header("Login")]
    [SerializeField] TMP_InputField _usernameInput;
    [SerializeField] TMP_InputField _passwordInput;
    [SerializeField] Button _loginButton;
    [SerializeField] Button _registerButton;
    [SerializeField] Button _quitButton;
    [SerializeField] TMP_Text _messageText;

    [Header("Register")]
    [SerializeField] GameObject _registerPanel;
    [SerializeField] TMP_InputField _regUsernameInput;
    [SerializeField] TMP_InputField _regPasswordInput;
    [SerializeField] TMP_InputField _regConfirmInput;
    [SerializeField] Button _confirmRegisterButton;
    [SerializeField] Button _backButton;
    [SerializeField] TMP_Text _regMessageText;

    [Header("IP Display")]
    [SerializeField] TMP_Text _ipText;

    public System.Action<string> OnLoginSuccess;

    void Start()
    {
        if (_loginButton != null)
            _loginButton.onClick.AddListener(OnLoginClick);
        if (_registerButton != null)
            _registerButton.onClick.AddListener(OnRegisterClick);
        if (_quitButton != null)
            _quitButton.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlaySfx("UIClick");
                Application.Quit();
            });
        if (_confirmRegisterButton != null)
            _confirmRegisterButton.onClick.AddListener(OnConfirmRegister);
        if (_backButton != null)
            _backButton.onClick.AddListener(OnBackClick);

        if (_messageText != null) _messageText.text = "";
        if (_regMessageText != null) _regMessageText.text = "";
        if (_registerPanel != null) _registerPanel.SetActive(false);

        if (_ipText != null)
        {
            string ip = GetLocalIP();
            _ipText.text = string.IsNullOrEmpty(ip) ? "" : $"Your IP: {ip}";
        }
    }

    void OnLoginClick()
    {
        AudioManager.Instance?.PlaySfx("UIClick");

        string username = _usernameInput?.text?.Trim();
        string password = _passwordInput?.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowMessage("请输入用户名和密码");
            return;
        }

        SetButtonsInteractable(false);
        ShowMessage("登录中...");

        AuthApi.Login(username, password, (success, uname, error) =>
        {
            SetButtonsInteractable(true);
            if (success)
            {
                ShowMessage("");
                OnLoginSuccess?.Invoke(uname);
            }
            else
            {
                ShowMessage(error ?? "登录失败");
            }
        });
    }

    void OnRegisterClick()
    {
        AudioManager.Instance?.PlaySfx("UIClick");
        if (_registerPanel != null) _registerPanel.SetActive(true);
        if (_regMessageText != null) _regMessageText.text = "";
    }

    void OnConfirmRegister()
    {
        AudioManager.Instance?.PlaySfx("UIClick");

        string username = _regUsernameInput?.text?.Trim();
        string password = _regPasswordInput?.text;
        string confirm = _regConfirmInput?.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowRegMessage("请填写用户名和密码");
            return;
        }
        if (username.Length < 3 || username.Length > 20)
        {
            ShowRegMessage("用户名长度需为 3-20 个字符");
            return;
        }
        if (password.Length < 6 || password.Length > 32)
        {
            ShowRegMessage("密码长度需为 6-32 个字符");
            return;
        }
        if (password != confirm)
        {
            ShowRegMessage("两次输入的密码不一致");
            return;
        }

        SetRegButtonsInteractable(false);
        ShowRegMessage("注册中...");

        AuthApi.Register(username, password, (success, uname, error) =>
        {
            SetRegButtonsInteractable(true);
            if (success)
            {
                ShowRegMessage("");
                OnLoginSuccess?.Invoke(uname);
            }
            else
            {
                ShowRegMessage(error ?? "注册失败");
            }
        });
    }

    void OnBackClick()
    {
        AudioManager.Instance?.PlaySfx("UIClick");
        if (_registerPanel != null) _registerPanel.SetActive(false);
    }

    void ShowMessage(string msg)
    {
        if (_messageText != null) _messageText.text = msg;
    }

    void ShowRegMessage(string msg)
    {
        if (_regMessageText != null) _regMessageText.text = msg;
    }

    void SetButtonsInteractable(bool value)
    {
        if (_loginButton != null) _loginButton.interactable = value;
        if (_registerButton != null) _registerButton.interactable = value;
    }

    void SetRegButtonsInteractable(bool value)
    {
        if (_confirmRegisterButton != null) _confirmRegisterButton.interactable = value;
        if (_backButton != null) _backButton.interactable = value;
    }

    string GetLocalIP()
    {
        try
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                if (socket.LocalEndPoint is IPEndPoint endPoint)
                    return endPoint.Address.ToString();
            }
        }
        catch { }

        try
        {
            IPHostEntry ipEntry = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress item in ipEntry.AddressList)
            {
                if (item.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(item))
                    return item.ToString();
            }
        }
        catch { }

        return "";
    }
}
