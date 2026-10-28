using System.Collections;
using System.Collections.Generic;
using Game;
using UnityEngine;

/// <summary>
/// 玩家暗雷遇敌追踪器（教程 092）：挂在玩家身上。
/// 进入 EncounterZone 后只累计 XZ 水平位移，达到随机阈值就锁输入、短暂停顿、抽敌并开战；
/// 传送/切场景后应调用 ResetEncounterTracking，避免把位移误算进步数。
/// </summary>
public class PlayerEncounterTracker : MonoBehaviour
{
    [Tooltip("达到阈值后、真正切战斗前的停顿（锁移动），单位秒")]
    [SerializeField] private float encounterDelay = 0.35f;

    private EncounterZone _currentZone;
    private Vector3 _lastPosition;
    private float _distanceTraveled;
    private float _encounterThreshold;
    private bool _encounterInProgress;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<EncounterZone>(out var zone))
        {
            _currentZone = zone;
            ResetEncounterTracking();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (_currentZone != null && other.TryGetComponent<EncounterZone>(out var zone) && zone == _currentZone)
        {
            _currentZone = null;
            ResetEncounterTracking();
        }
    }

    private void Update()
    {
        // 只在自由探索时累计；菜单/对话/战斗中不遇敌
        if (_currentZone == null || _encounterInProgress)
            return;
        if (GameModeManager.Instance != null && GameModeManager.Instance.CurrentGameMode != GameMode.Explore)
            return;

        Vector3 current = transform.position;
        Vector3 delta = current - _lastPosition;
        delta.y = 0f; // 只计水平位移，跳跃/下坡不计
        _distanceTraveled += delta.magnitude;
        _lastPosition = current;

        if (_distanceTraveled >= _encounterThreshold)
            StartCoroutine(TriggerEncounter());
    }

    private IEnumerator TriggerEncounter()
    {
        _encounterInProgress = true;

        // 先切到交互菜单模式锁住玩家移动（输入系统会切离 Player map）
        if (GameModeManager.Instance != null)
            GameModeManager.Instance.RequestChangeMode(GameMode.InteractionMenu);

        yield return new WaitForSeconds(encounterDelay);

        EncounterSO encounter = _currentZone != null ? _currentZone.GetRandomEncounter() : null;
        if (encounter == null || encounter.Enemies == null || encounter.Enemies.Count == 0)
        {
            ResetEncounterTracking();
            _encounterInProgress = false;
            if (GameModeManager.Instance != null)
                GameModeManager.Instance.RequestChangeMode(GameMode.Explore);
            yield break;
        }

        // 队友快照（教程 092）
        var allies = new List<CharacterRuntimeData>();
        if (PartyManager.Instance != null)
            allies = new List<CharacterRuntimeData>(PartyManager.Instance.PartyMembers);

        var enemies = new List<EnemyDefinitionSO>(encounter.Enemies);

        BattleService.Ensure().StartBattle(allies, enemies, encounter, _currentZone.BattleSceneReference);

        ResetEncounterTracking();
        _encounterInProgress = false;
    }

    public void ResetEncounterDistance()
    {
        _distanceTraveled = 0f;
        _lastPosition = transform.position;
    }

    public void ResetEncounterTracking()
    {
        ResetEncounterDistance();
        if (_currentZone != null)
            _encounterThreshold = _currentZone.GetRandomEncounterDistance();
    }
}
