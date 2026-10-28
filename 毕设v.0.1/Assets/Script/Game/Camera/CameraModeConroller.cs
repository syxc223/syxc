using MFramework.Event;

using UnityEngine;
public class CameraModeController : MonoBehaviour, IEventReceiver<GameModeChangedEvent>
{
    [Header("Cameras")]
    [SerializeField] private GameObject followCamera;

    void OnEnable()
    {
        EventBus.Subscribe<GameModeChangedEvent>(this);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<GameModeChangedEvent>(this);
    }

    public void OnEvent(GameModeChangedEvent evt)
    {
        SetCameraView(CameraView.Explore);
    }

    private void SetCameraView(CameraView view)
    {
        if (followCamera != null)
            followCamera.SetActive(view == CameraView.Explore);
    }
}
