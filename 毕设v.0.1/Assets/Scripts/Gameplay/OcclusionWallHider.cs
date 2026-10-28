using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Hides the door-wall renderers while the local player has crossed beyond the wall
/// plane but the follow camera is still on this side — without this, the wall
/// occludes the player for the whole walk through the exit strip.
/// The pop happens while the player is inside the doorway gap, so it reads naturally.
/// </summary>
public class OcclusionWallHider : MonoBehaviour
{
    [Tooltip("墙体所在平面的 Z（玩家越过它后开始隐藏）")]
    [SerializeField] float _wallZ = 10.4555f;
    [Tooltip("回程时重新显示墙体所需的 Z（小回差防闪烁）")]
    [SerializeField] float _showOffset = 0.5f;
    [SerializeField] Renderer[] _renderers;

    readonly List<Renderer> _targets = new List<Renderer>();
    Transform _localPlayer;
    bool _hidden;

    void Awake()
    {
        if (_renderers != null)
        {
            foreach (var r in _renderers)
                if (r != null) _targets.Add(r);
        }
    }

    void Update()
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.SpawnManager == null) return;

        var playerObj = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        _localPlayer = playerObj != null ? playerObj.transform : null;
        if (_localPlayer == null) return;

        float z = _localPlayer.position.z;

        if (!_hidden && z > _wallZ)
            SetVisible(false);
        else if (_hidden && z < _wallZ - _showOffset)
            SetVisible(true);
    }

    void SetVisible(bool visible)
    {
        _hidden = !visible;
        foreach (var r in _targets)
            if (r != null) r.enabled = visible;
    }
}
