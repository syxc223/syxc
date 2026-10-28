using System.Collections;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// TCP Echo 测试客户端：连接服务器 → 发送3条消息 → 接收回显 → 断开。
/// 用于排查跨设备连通性问题。
/// </summary>
public class EchoTester : MonoBehaviour
{
    [SerializeField] TMP_InputField _ipInput;
    [SerializeField] TMP_Text _outputText;
    [SerializeField] Button _startButton;
    [SerializeField] Button _clearButton;

    const int ECHO_PORT = 3001;

    void Start()
    {
        if (_startButton != null)
            _startButton.onClick.AddListener(() => StartCoroutine(RunEchoTest()));
        if (_clearButton != null)
            _clearButton.onClick.AddListener(() => { if (_outputText != null) _outputText.text = ""; });
    }

    IEnumerator RunEchoTest()
    {
        if (_ipInput == null || string.IsNullOrWhiteSpace(_ipInput.text))
        {
            LogLine("<color=red>请输入服务器 IP</color>");
            yield break;
        }

        string ip = _ipInput.text.Trim();
        if (_startButton != null) _startButton.interactable = false;

        LogLine($"<color=yellow>═══ Echo 测试开始 ═══</color>");
        LogLine($"目标: {ip}:{ECHO_PORT}");

        TcpClient client = null;
        NetworkStream stream = null;

        // Step 1: 连接
        LogLine("<color=#88ccff>[1/4] 正在连接...</color>");
        bool connected = false;
        bool connectFailed = false;

        var connectThread = new System.Threading.Thread(() =>
        {
            try
            {
                client = new TcpClient();
                var result = client.BeginConnect(ip, ECHO_PORT, null, null);
                if (result.AsyncWaitHandle.WaitOne(5000))
                {
                    client.EndConnect(result);
                    connected = client.Connected;
                }
                else
                {
                    connectFailed = true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EchoTester] 连接异常: {e.Message}");
                connectFailed = true;
            }
        });
        connectThread.IsBackground = true;
        connectThread.Start();

        // 等待连接完成（最多 6 秒）
        float waitTimer = 0f;
        while (connectThread.IsAlive && waitTimer < 6f)
        {
            waitTimer += Time.deltaTime;
            yield return null;
        }

        if (!connected || connectFailed)
        {
            LogLine("<color=red>[1/4] 连接失败！请检查 IP 和防火墙</color>");
            LogLine($"<color=red>  IP: {ip}, 端口: {ECHO_PORT}</color>");
            LogLine("<color=red>  服务器是否已启动？端口是否放行？</color>");
            if (_startButton != null) _startButton.interactable = true;
            yield break;
        }

        LogLine("<color=green>[1/4] 连接成功！</color>");
        stream = client.GetStream();

        // 接收欢迎消息
        yield return ReceiveLine(stream, 2f);

        // Step 2-4: 发送3条消息并接收回显
        string[] messages = { "Test1", "Unity Echo", "exit" };
        for (int i = 0; i < messages.Length; i++)
        {
            int step = i + 2;
            LogLine($"<color=#88ccff>[{step}/4] 发送: \"{messages[i]}\"</color>");

            byte[] data = Encoding.UTF8.GetBytes(messages[i] + "\n");
            try
            {
                stream.Write(data, 0, data.Length);
                stream.Flush();
                LogLine($"  已发送 ({data.Length} bytes)");
            }
            catch (System.Exception e)
            {
                LogLine($"<color=red>  发送失败: {e.Message}</color>");
                break;
            }

            // 接收回显
            yield return ReceiveLine(stream, 3f);
        }

        // 清理
        LogLine("<color=yellow>═══ 测试完成 ═══</color>");
        try { stream?.Close(); } catch { }
        try { client?.Close(); } catch { }

        if (_startButton != null) _startButton.interactable = true;
    }

    IEnumerator ReceiveLine(NetworkStream stream, float timeout)
    {
        float timer = 0f;
        byte[] buffer = new byte[1024];
        int totalRead = 0;

        while (timer < timeout)
        {
            if (stream.DataAvailable)
            {
                int read = stream.Read(buffer, totalRead, buffer.Length - totalRead);
                if (read > 0)
                {
                    totalRead += read;
                    string received = Encoding.UTF8.GetString(buffer, 0, totalRead);
                    if (received.Contains("\n"))
                    {
                        string line = received.TrimEnd('\r', '\n');
                        LogLine($"<color=green>  收到: \"{line}\"</color>");
                        yield break;
                    }
                }
            }
            timer += Time.deltaTime;
            yield return null;
        }

        LogLine($"<color=red>  接收超时 ({timeout}s)</color>");
    }

    void LogLine(string msg)
    {
        if (_outputText != null)
            _outputText.text += msg + "\n";
        Debug.Log($"[EchoTester] {msg}");
    }
}
