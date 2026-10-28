using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Room lobby panel: list rooms, create room, join room.
/// </summary>
public class RoomPanel : MonoBehaviour
{
    [Header("Header")]
    [SerializeField] TMP_Text _welcomeText;
    [SerializeField] Button _logoutButton;
    [SerializeField] Button _quitButton;

    [Header("Room List")]
    [SerializeField] Transform _roomListContent;
    [SerializeField] GameObject _roomItemPrefab;
    [SerializeField] Button _refreshButton;

    [Header("Create Room")]
    [SerializeField] Button _createRoomButton;
    [SerializeField] GameObject _createRoomDialog;
    [SerializeField] TMP_InputField _roomNameInput;
    [SerializeField] Button _confirmCreateButton;
    [SerializeField] Button _cancelCreateButton;

    [Header("Direct Connect")]
    [SerializeField] TMP_InputField _directConnectInput;
    [SerializeField] Button _directConnectButton;

    public System.Action OnLogout;
    public System.Action OnEnterGame;

    void OnEnable()
    {
        if (_welcomeText != null && ApiClient.IsLoggedIn)
        {
            string ip = GetLocalIP();
            _welcomeText.text = string.IsNullOrEmpty(ip)
                ? $"欢迎, {ApiClient.Username}"
                : $"欢迎, {ApiClient.Username}  |  本机IP: {ip}";
        }

        // Re-enable all buttons when returning to lobby (e.g. after ExitRoom)
        SetInteractable(true);

        RefreshRooms();
    }

    void Start()
    {
        if (_logoutButton != null)
            _logoutButton.onClick.AddListener(OnLogoutClick);
        if (_quitButton != null)
            _quitButton.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlaySfx("UIClick");
                Application.Quit();
            });
        if (_refreshButton != null)
            _refreshButton.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlaySfx("UIClick");
                RefreshRooms();
            });
        if (_createRoomButton != null)
            _createRoomButton.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlaySfx("UIClick");
                if (_createRoomDialog != null) _createRoomDialog.SetActive(true);
            });
        if (_confirmCreateButton != null)
            _confirmCreateButton.onClick.AddListener(OnConfirmCreate);
        if (_cancelCreateButton != null)
            _cancelCreateButton.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlaySfx("UIClick");
                if (_createRoomDialog != null) _createRoomDialog.SetActive(false);
            });

        if (_createRoomDialog != null) _createRoomDialog.SetActive(false);

        // Direct connect: bypass server, connect directly to a host IP
        if (_directConnectButton != null)
        {
            _directConnectButton.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlaySfx("UIClick");
                OnDirectConnect();
            });
        }
    }

    void RefreshRooms()
    {
        // Clear existing items
        if (_roomListContent != null)
        {
            for (int i = _roomListContent.childCount - 1; i >= 0; i--)
                Destroy(_roomListContent.GetChild(i).gameObject);
        }

        RoomApi.GetRooms((success, rooms, error) =>
        {
            if (!success)
            {
                Debug.LogWarning($"获取房间列表失败: {error}");
                ShowEmptyRooms("\u6682\u65E0\u53EF\u7528\u623F\u95F4 (\u670D\u52A1\u5668\u672A\u8FDE\u63A5)"); // 暂无可用房间 (服务器未连接)
                return;
            }

            if (rooms == null || rooms.Length == 0)
            {
                ShowEmptyRooms("\u6682\u65E0\u53EF\u7528\u623F\u95F4"); // 暂无可用房间
                return;
            }

            foreach (var room in rooms)
            {
                if (_roomItemPrefab == null || _roomListContent == null) continue;

                var item = Instantiate(_roomItemPrefab, _roomListContent);

                // Set room name text
                var nameText = FindChildText(item, "RoomNameText");
                if (nameText != null) nameText.text = room.name;

                // Set host text
                var hostText = FindChildText(item, "HostNameText");
                if (hostText != null) hostText.text = $"Host: {room.host_username}";

                // Set player count
                var countText = FindChildText(item, "PlayerCountText");
                if (countText != null) countText.text = $"{room.current_players}/{room.max_players}";

                // Set join button
                var joinButton = item.GetComponentInChildren<Button>();
                if (joinButton != null)
                {
                    int roomId = room.id;
                    string hostIp = room.host_ip;

                    if (!room.CanJoin)
                    {
                        joinButton.interactable = false;
                        var label = joinButton.GetComponentInChildren<TMP_Text>();
                        if (label != null) label.text = "已满";
                    }
                    else
                    {
                        joinButton.onClick.AddListener(() =>
                        {
                            AudioManager.Instance?.PlaySfx("UIClick");
                            OnJoinRoom(roomId, hostIp);
                        });
                    }
                }
            }
        });
    }

    void OnJoinRoom(int roomId, string hostIp)
    {
        SetInteractable(false);

        RoomApi.JoinRoom(roomId, (success, ip, hostName, error) =>
        {
            if (success)
            {
                GameManager.Instance.CurrentRoomId = roomId;
                if (GameManager.Instance.Connect(ip))
                    TransitionToGame();
                else
                    SetInteractable(true);
            }
            else
            {
                SetInteractable(true);
                Debug.LogWarning($"加入房间失败: {error}");
            }
        });
    }

    /// <summary>
    /// Direct connect: skip room API, connect to a manually entered host IP.
    /// Useful when auto-detected IP is wrong or server is unreachable.
    /// </summary>
    void OnDirectConnect()
    {
        string ip = _directConnectInput?.text?.Trim();
        if (string.IsNullOrEmpty(ip))
        {
            Debug.LogWarning("请输入主机 IP");
            return;
        }

        SetInteractable(false);
        GameManager.Instance.CurrentRoomId = -1; // no server room tracking

        if (GameManager.Instance.Connect(ip))
            TransitionToGame();
        else
            SetInteractable(true);
    }

    void OnConfirmCreate()
    {
        AudioManager.Instance?.PlaySfx("UIClick");

        string roomName = _roomNameInput?.text?.Trim();
        if (string.IsNullOrEmpty(roomName))
        {
            Debug.LogWarning("请输入房间名");
            return;
        }

        string localIp = GetLocalIP();
        if (string.IsNullOrEmpty(localIp))
        {
            Debug.LogWarning("无法获取本地 IP");
            return;
        }

        SetInteractable(false);

        RoomApi.CreateRoom(roomName, localIp, (success, roomId, error) =>
        {
            if (success)
            {
                GameManager.Instance.CurrentRoomId = roomId;
                if (GameManager.Instance.StartHost())
                    TransitionToGame();
                else
                    SetInteractable(true);
            }
            else
            {
                SetInteractable(true);
                Debug.LogWarning($"创建房间失败: {error}");
            }
        });
    }

    void OnLogoutClick()
    {
        AudioManager.Instance?.PlaySfx("UIClick");
        ApiClient.ClearAuth();
        OnLogout?.Invoke();
    }

    void TransitionToGame()
    {
        AudioManager.Instance?.PlaySfx("Connect");
        OnEnterGame?.Invoke();
    }

    void SetInteractable(bool value)
    {
        if (_refreshButton != null) _refreshButton.interactable = value;
        if (_createRoomButton != null) _createRoomButton.interactable = value;
        if (_logoutButton != null) _logoutButton.interactable = value;
        if (_directConnectButton != null) _directConnectButton.interactable = value;
    }

    /// <summary>Placeholder row shown when the room list is empty or the server is unreachable.</summary>
    void ShowEmptyRooms(string message)
    {
        if (_roomItemPrefab == null || _roomListContent == null) return;

        var item = Instantiate(_roomItemPrefab, _roomListContent);
        var nameText = FindChildText(item, "RoomNameText");
        if (nameText != null) nameText.text = message;
        var hostText = FindChildText(item, "HostNameText");
        if (hostText != null) hostText.text = "";
        var countText = FindChildText(item, "PlayerCountText");
        if (countText != null) countText.text = "";
        var btn = item.GetComponentInChildren<Button>();
        if (btn != null)
        {
            btn.interactable = false;
            btn.gameObject.SetActive(false); // hide the join slot in empty state
        }
    }

    TMP_Text FindChildText(GameObject parent, string childName)
    {
        var t = parent.transform.Find(childName);
        return t?.GetComponent<TMP_Text>();
    }

    string GetLocalIP()
    {
        // Method 1: UDP connect — determines the interface used for outbound traffic
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

        // Method 2: Enumerate network interfaces via System.Net.NetworkInformation
        try
        {
            foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up)
                    continue;
                if (ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                    continue;
                // Skip virtual adapters (VPN, Docker, Hyper-V, etc.)
                if (ni.Description.Contains("Virtual") || ni.Description.Contains("VMware") ||
                    ni.Description.Contains("Hyper-V") || ni.Description.Contains("Docker"))
                    continue;

                var props = ni.GetIPProperties();
                foreach (var ua in props.UnicastAddresses)
                {
                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(ua.Address))
                        return ua.Address.ToString();
                }
            }
        }
        catch { }

        // Method 3: DNS fallback (may fail on machines with non-ASCII hostnames)
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
