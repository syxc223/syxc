using System.Collections;
using System.Collections.Generic;
using Game;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 调试用开战器：不用配置暗雷区域即可验证“战斗逻辑闭环”。
/// 挂到场景任意物体、指定一个 EncounterSO：
/// - 勾选 StartOnPlay：进入 Play 后自动延迟开战；
/// - 运行中按 J（新 Input System 的键盘）也可随时开战。
/// 正式接入暗雷后可删除本组件。
/// </summary>
public class BattleDebugStarter : MonoBehaviour
{
    public EncounterSO Encounter;
    public bool StartOnPlay = true;
    public KeyCode LegacyKey = KeyCode.J; // 仅占位说明，实际读取下方新输入系统按键
    public Key DebugKey = Key.J;
    [Min(0f)] public float StartDelay = 1f;

    private void Start()
    {
        if (StartOnPlay)
            StartCoroutine(DelayedBegin(StartDelay));
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current[DebugKey].wasPressedThisFrame)
            BeginBattle();
    }

    private IEnumerator DelayedBegin(float delay)
    {
        yield return new WaitForSeconds(delay);
        BeginBattle();
    }

    public void BeginBattle()
    {
        if (Encounter == null)
        {
            Debug.LogWarning("[BattleDebugStarter] 未指定 EncounterSO，无法开战");
            return;
        }
        if (BattleService.HasInstance && BattleService.Instance.IsBattleActive)
            return;

        var allies = PartyManager.Instance != null
            ? new List<CharacterRuntimeData>(PartyManager.Instance.PartyMembers)
            : new List<CharacterRuntimeData>();

        if (allies.Count == 0)
        {
            Debug.LogWarning("[BattleDebugStarter] 当前队伍为空（PartyManager 无成员），无法开战");
            return;
        }

        var enemies = new List<EnemyDefinitionSO>(Encounter.Enemies);
        BattleService.Ensure().StartBattle(allies, enemies, Encounter);
    }
}
