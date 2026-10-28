
/// <summary>
/// 单例基类。联机时需 Host 单例的管理器：
/// PuzzleProgressService, DialogueService, InventoryManager, PartyManager, SceneLoadManager
/// </summary>
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    public static T Instance => _instance;
    protected virtual void Awake() {
        if(_instance is null) {
            _instance = this as T;
        } 
        if(_instance != this)
        {
            Destroy(gameObject);
        }
    }
}
