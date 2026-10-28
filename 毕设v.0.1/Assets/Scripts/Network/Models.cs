using System;

namespace NetworkModels
{
    [Serializable]
    public class AuthRequest
    {
        public string username;
        public string password;
    }

    [Serializable]
    public class AuthResponse
    {
        public bool success;
        public string token;
        public string username;
        public string error;
    }

    [Serializable]
    public class CreateRoomRequest
    {
        public string name;
        public string hostIp;
    }

    [Serializable]
    public class CreateRoomResponse
    {
        public bool success;
        public int roomId;
        public string error;
    }

    [Serializable]
    public class RoomInfo
    {
        public int id;
        public string name;
        public string host_username;
        public string host_ip;
        public int current_players;
        public int max_players;
        public string status;

        public string DisplayText => $"{name}  (Host: {host_username}  {current_players}/{max_players})";
        public bool CanJoin => current_players < max_players && status == "waiting";
    }

    [Serializable]
    public class RoomListResponse
    {
        public bool success;
        public RoomInfo[] rooms;
        public string error;
    }

    // JsonUtility needs a wrapper for arrays
    [Serializable]
    public class RoomArrayWrapper
    {
        public RoomInfo[] rooms;
    }

    [Serializable]
    public class JoinRoomResponse
    {
        public bool success;
        public string hostIp;
        public string host_username;
        public string error;
    }

    [Serializable]
    public class SimpleResponse
    {
        public bool success;
        public string error;
    }
}
