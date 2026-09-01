using UnityEngine;
using UnityEngine.SceneManagement;

public class TeleportToScene : MonoBehaviour
{
    [Tooltip("碰到玩家后要跳转到的场景名字（必须已加入 Build Settings）")]
    public string sceneName = "YourSceneName";

    [Tooltip("玩家 GameObject 的 Tag，例如：Player")]
    public string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    
}