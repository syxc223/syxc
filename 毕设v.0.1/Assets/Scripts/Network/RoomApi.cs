using System;
using UnityEngine;

/// <summary>
/// Room management API: list, create, join, leave, close.
/// </summary>
public static class RoomApi
{
    public static void GetRooms(Action<bool, NetworkModels.RoomInfo[], string> callback)
    {
        ApiClient.Get("/rooms", (success, body) =>
        {
            if (!success)
            {
                Debug.LogWarning($"[RoomApi] GetRooms 请求失败: {body}");
                callback?.Invoke(false, null, "获取房间列表失败，请检查网络连接");
                return;
            }

            NetworkModels.RoomListResponse resp;
            try
            {
                resp = JsonUtility.FromJson<NetworkModels.RoomListResponse>(body);
            }
            catch
            {
                callback?.Invoke(false, null, "服务器响应格式错误");
                return;
            }

            if (resp.success)
            {
                callback?.Invoke(true, resp.rooms ?? Array.Empty<NetworkModels.RoomInfo>(), null);
            }
            else
            {
                callback?.Invoke(false, null, resp.error ?? "获取房间列表失败");
            }
        });
    }

    public static void CreateRoom(string name, string hostIp, Action<bool, int, string> callback)
    {
        var req = new NetworkModels.CreateRoomRequest { name = name, hostIp = hostIp };
        string json = JsonUtility.ToJson(req);

        ApiClient.Post("/rooms", json, (success, body) =>
        {
            if (!success)
            {
                Debug.LogWarning($"[RoomApi] CreateRoom 请求失败: {body}");
                callback?.Invoke(false, 0, TryExtractError(body) ?? "创建房间失败");
                return;
            }

            NetworkModels.CreateRoomResponse resp;
            try
            {
                resp = JsonUtility.FromJson<NetworkModels.CreateRoomResponse>(body);
            }
            catch
            {
                callback?.Invoke(false, 0, "服务器响应格式错误");
                return;
            }

            if (resp.success)
                callback?.Invoke(true, resp.roomId, null);
            else
                callback?.Invoke(false, 0, resp.error ?? "创建房间失败");
        });
    }

    public static void JoinRoom(int roomId, Action<bool, string, string, string> callback)
    {
        ApiClient.Post($"/rooms/{roomId}/join", "{}", (success, body) =>
        {
            if (!success)
            {
                Debug.LogWarning($"[RoomApi] JoinRoom 请求失败: {body}");
                callback?.Invoke(false, null, null, TryExtractError(body) ?? "加入房间失败");
                return;
            }

            NetworkModels.JoinRoomResponse resp;
            try
            {
                resp = JsonUtility.FromJson<NetworkModels.JoinRoomResponse>(body);
            }
            catch
            {
                callback?.Invoke(false, null, null, "服务器响应格式错误");
                return;
            }

            if (resp.success)
                callback?.Invoke(true, resp.hostIp, resp.host_username, null);
            else
                callback?.Invoke(false, null, null, resp.error ?? "加入房间失败");
        });
    }

    public static void LeaveRoom(int roomId, Action<bool> callback)
    {
        ApiClient.Post($"/rooms/{roomId}/leave", "{}", (success, body) =>
        {
            callback?.Invoke(success);
        });
    }

    public static void CloseRoom(int roomId, Action<bool> callback)
    {
        ApiClient.Delete($"/rooms/{roomId}", (success, body) =>
        {
            callback?.Invoke(success);
        });
    }

    static string TryExtractError(string body)
    {
        if (string.IsNullOrEmpty(body)) return null;
        try
        {
            var resp = JsonUtility.FromJson<NetworkModels.SimpleResponse>(body);
            return resp.error;
        }
        catch
        {
            return body;
        }
    }
}
