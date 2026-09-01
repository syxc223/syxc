using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 弹药显示 UI：监听 GunShooting 弹药变化并更新文本
/// </summary>
public class AmmoUI : MonoBehaviour
{
    [SerializeField] private GunShooting gun;   // 关联的枪械脚本（可拖场景实例或玩家预制体）
    [SerializeField] private Text ammoText;     // 显示弹药的 Text

    private GunShooting boundGun;

    void OnEnable()
    {
        TryBind();
    }

    void OnDisable()
    {
        Unbind();
    }

    /// <summary>
    /// 外部在玩家实例化后可调用，重新绑定（适配拖预制体场景）
    /// </summary>
    public void RefreshBinding()
    {
        Unbind();
        TryBind();
    }

    // 支持场景实例或玩家预制体：若拖的是预制体则在场景中查找实例
    private void TryBind()
    {
        // 确保有 Text 引用
        if (ammoText == null)
        {
            ammoText = GetComponent<Text>();
        }

        // 如果拖的是预制体（不在场景），尝试查找场景中的实例
        if (gun != null && !gun.gameObject.scene.IsValid())
        {
            var found = FindFirstObjectByType<GunShooting>();
            if (found != null) gun = found;
        }

        // 如果仍未指定，自动查找场景中的第一把枪
        if (gun == null)
        {
            gun = FindFirstObjectByType<GunShooting>();
        }

        // 绑定事件
        if (gun != null)
        {
            boundGun = gun;
            boundGun.OnAmmoChanged += HandleAmmoChanged;
            HandleAmmoChanged(boundGun.CurrentAmmo, boundGun.MaxAmmo);
        }
    }

    private void Unbind()
    {
        if (boundGun != null)
        {
            boundGun.OnAmmoChanged -= HandleAmmoChanged;
            boundGun = null;
        }
    }

    private void HandleAmmoChanged(int current, int max)
    {
        if (ammoText != null)
        {
            ammoText.text = $"{current}/{max}";
        }
    }
}

