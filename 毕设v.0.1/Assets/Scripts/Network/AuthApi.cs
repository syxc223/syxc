using System;
using UnityEngine;

/// <summary>
/// Authentication API: register and login.
/// </summary>
public static class AuthApi
{
    public static void Register(string username, string password, Action<bool, string, string> callback)
    {
        var req = new NetworkModels.AuthRequest { username = username, password = password };
        string json = JsonUtility.ToJson(req);

        ApiClient.Post("/auth/register", json, (success, body) =>
        {
            if (!success)
            {
                Debug.LogWarning($"[AuthApi] Register 请求失败: {body}");
                string errorMsg = TryExtractError(body) ?? "注册失败，请检查网络连接";
                callback?.Invoke(false, null, errorMsg);
                return;
            }

            NetworkModels.AuthResponse resp;
            try
            {
                resp = JsonUtility.FromJson<NetworkModels.AuthResponse>(body);
            }
            catch
            {
                callback?.Invoke(false, null, "服务器响应格式错误");
                return;
            }

            if (resp.success)
            {
                ApiClient.SetToken(resp.token);
                ApiClient.SetUsername(resp.username);
                callback?.Invoke(true, resp.username, null);
            }
            else
            {
                callback?.Invoke(false, null, resp.error ?? "注册失败");
            }
        });
    }

    public static void Login(string username, string password, Action<bool, string, string> callback)
    {
        var req = new NetworkModels.AuthRequest { username = username, password = password };
        string json = JsonUtility.ToJson(req);

        ApiClient.Post("/auth/login", json, (success, body) =>
        {
            if (!success)
            {
                Debug.LogWarning($"[AuthApi] Login 请求失败: {body}");
                string errorMsg = TryExtractError(body) ?? "登录失败，请检查网络连接";
                callback?.Invoke(false, null, errorMsg);
                return;
            }

            NetworkModels.AuthResponse resp;
            try
            {
                resp = JsonUtility.FromJson<NetworkModels.AuthResponse>(body);
            }
            catch
            {
                callback?.Invoke(false, null, "服务器响应格式错误");
                return;
            }

            if (resp.success)
            {
                ApiClient.SetToken(resp.token);
                ApiClient.SetUsername(resp.username);
                callback?.Invoke(true, resp.username, null);
            }
            else
            {
                callback?.Invoke(false, null, resp.error ?? "登录失败");
            }
        });
    }

    static string TryExtractError(string body)
    {
        if (string.IsNullOrEmpty(body)) return null;
        try
        {
            var resp = JsonUtility.FromJson<NetworkModels.AuthResponse>(body);
            return resp.error;
        }
        catch
        {
            return body;
        }
    }
}
