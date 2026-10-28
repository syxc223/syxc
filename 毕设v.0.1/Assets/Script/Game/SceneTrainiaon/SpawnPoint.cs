using UnityEngine;

/// <summary>
/// 场景出生点。挂载在场景中的 GameObject 上，通过 spawnPointId 查找。
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    [Tooltip("出生点唯一标识，与 SceneLoadRequest.SpawnPointId 对应")]
    [SerializeField] private string spawnPointId = "default";

    public string SpawnPointId => spawnPointId;

    /// <summary>
    /// 在当前已加载的场景中查找指定 ID 的出生点。
    /// </summary>
    public static SpawnPoint Find(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        var allPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
        foreach (var point in allPoints)
        {
            if (point.spawnPointId == id)
                return point;
        }
        return null;
    }

    /// <summary>
    /// 获取出生点的世界坐标位置。
    /// </summary>
    public Vector3 Position => transform.position;

    /// <summary>
    /// 获取出生点的旋转。
    /// </summary>
    public Quaternion Rotation => transform.rotation;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        Gizmos.DrawRay(transform.position, transform.forward * 0.8f);
    }
}
