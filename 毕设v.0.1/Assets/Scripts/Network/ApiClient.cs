using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Base HTTP client for REST API communication.
/// Uses UnityWebRequest with coroutines and callbacks.
/// JWT token is stored in PlayerPrefs for persistence across sessions.
/// </summary>
public static class ApiClient
{
    // Change this to your cloud server's public IP
    public const string ServerUrl = "http://8.130.65.176:3000";
    public const string ApiBase = ServerUrl + "/api";

    const string TokenKey = "jwt_token";
    const string UsernameKey = "username";

    public static string Token => PlayerPrefs.GetString(TokenKey, "");
    public static string Username => PlayerPrefs.GetString(UsernameKey, "");
    public static bool IsLoggedIn => !string.IsNullOrEmpty(Token);

    public static void SetToken(string token)
    {
        PlayerPrefs.SetString(TokenKey, token);
        PlayerPrefs.Save();
    }

    public static void SetUsername(string username)
    {
        PlayerPrefs.SetString(UsernameKey, username);
        PlayerPrefs.Save();
    }

    public static void ClearAuth()
    {
        PlayerPrefs.DeleteKey(TokenKey);
        PlayerPrefs.DeleteKey(UsernameKey);
        PlayerPrefs.Save();
    }

    // GET
    public static void Get(string path, Action<bool, string> callback)
    {
        var req = UnityWebRequest.Get(ApiBase + path);
        AttachAuth(req);
        StaticCoroutine.Start(SendRequest(req, callback));
    }

    // POST with JSON body
    public static void Post(string path, string jsonBody, Action<bool, string> callback)
    {
        var req = new UnityWebRequest(ApiBase + path, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody ?? "{}");
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        AttachAuth(req);
        StaticCoroutine.Start(SendRequest(req, callback));
    }

    // DELETE
    public static void Delete(string path, Action<bool, string> callback)
    {
        var req = UnityWebRequest.Delete(ApiBase + path);
        AttachAuth(req);
        StaticCoroutine.Start(SendRequest(req, callback));
    }

    static void AttachAuth(UnityWebRequest req)
    {
        if (!string.IsNullOrEmpty(Token))
            req.SetRequestHeader("Authorization", "Bearer " + Token);
    }

    static IEnumerator SendRequest(UnityWebRequest req, Action<bool, string> callback)
    {
        req.timeout = 10;
        yield return req.SendWebRequest();

        bool success = req.result == UnityWebRequest.Result.Success;
        string body = success ? req.downloadHandler.text : "";

        if (!success)
        {
            // 令牌失效/未授权：清除本地认证并回到登录界面
            if (req.responseCode == 401)
            {
                ClearAuth();
                if (UIManager.Instance != null)
                    UIManager.Instance.ShowLogin();
            }

            // Try to extract error message from response body
            if (!string.IsNullOrEmpty(req.downloadHandler?.text))
                body = req.downloadHandler.text;
            else
                body = req.error ?? "网络请求失败";
        }

        req.Dispose();
        callback?.Invoke(success, body);
    }
}

/// <summary>
/// Helper to run coroutines from static context.
/// Auto-creates a hidden GameObject if needed.
/// </summary>
public class StaticCoroutine : MonoBehaviour
{
    static StaticCoroutine _instance;

    public static Coroutine Start(IEnumerator routine)
    {
        if (_instance == null)
        {
            var go = new GameObject("[StaticCoroutine]");
            _instance = go.AddComponent<StaticCoroutine>();
            DontDestroyOnLoad(go);
        }
        return _instance.StartCoroutine(routine);
    }
}
