using UnityEngine;
using Cinemachine;

public class DimensionSwitcher : MonoBehaviour
{
    [Header("Cinemachine Cameras")]
    public CinemachineVirtualCamera cam3D;
    public CinemachineVirtualCamera cam2D;

    bool Is2D = false;


    void Start()
    {
        // 初始化状态，向本体和子对象发送函数调用消息
        BroadcastMessage("SwitchTo3D");
    }
    void SwitchTo2D()
    {
        // Cinemachine 逻辑：优先级高的相机会夺取控制权
        cam2D.Priority = 11;
        cam3D.Priority = 10;
    }

    private void SwitchTo3D()
    {
        cam3D.Priority = 11;
        cam2D.Priority = 10;
    }
    void Update()
    {
        // 切换按键 鼠标左键
        if (Input.GetMouseButtonDown(0))
        {
            Is2D = !Is2D;
            if (Is2D)
                BroadcastMessage("SwitchTo2D");
            else
                BroadcastMessage("SwitchTo3D");
        }
    }
}