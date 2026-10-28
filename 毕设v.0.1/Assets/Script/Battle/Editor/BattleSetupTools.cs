#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 战斗重构辅助工具（仅编辑器）。顶部菜单“战斗重构”下：
/// ① 清理场景里因删除旧明雷脚本而残留的 Missing 组件；
/// ② 生成调试开战器（Play 后自动用第一个 Encounter 开战，最快验证逻辑闭环）；
/// ③ 正式搭建暗雷（玩家挂 PlayerEncounterTracker + 新建 EncounterZone 触发区）。
/// </summary>
public static class BattleSetupTools
{
    private const string Menu = "战斗重构/";

    [MenuItem(Menu + "① 清理场景残留的旧明雷(Missing脚本)")]
    public static void CleanMissingScripts()
    {
        int removed = 0;
        foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            removed += RecurseClean(root);

        MarkDirty();
        Debug.Log($"[战斗重构] 共清理 Missing 脚本组件 {removed} 个");
    }

    private static int RecurseClean(GameObject go)
    {
        int n = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        foreach (Transform child in go.transform)
            n += RecurseClean(child.gameObject);
        return n;
    }

    private static EncounterSO FindFirstEncounter()
    {
        string guid = AssetDatabase.FindAssets("t:EncounterSO").FirstOrDefault();
        if (string.IsNullOrEmpty(guid))
            return null;
        return AssetDatabase.LoadAssetAtPath<EncounterSO>(AssetDatabase.GUIDToAssetPath(guid));
    }

    [MenuItem(Menu + "② 生成调试开战器(Play后自动开战)")]
    public static void CreateDebugStarter()
    {
        var existing = Object.FindObjectOfType<BattleDebugStarter>();
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            EditorGUIUtility.PingObject(existing);
            Debug.Log("[战斗重构] 场景中已存在 BattleDebugStarter，已为你选中");
            return;
        }

        var go = new GameObject("BattleDebugStarter");
        var starter = go.AddComponent<BattleDebugStarter>();
        starter.Encounter = FindFirstEncounter();
        starter.StartOnPlay = true;
        Selection.activeGameObject = go;
        MarkDirty();
        Debug.Log("[战斗重构] 已生成调试开战器，进入 Play 即自动开战；可在 Inspector 更换 Encounter。");
    }

    [MenuItem(Menu + "③ 搭建暗雷(玩家Tracker + EncounterZone)")]
    public static void SetupRandomEncounter()
    {
        GameObject player = null;
        var interactor = Object.FindObjectOfType<PlayerInteractor>();
        if (interactor != null) player = interactor.gameObject;
        if (player == null)
        {
            var pc = Object.FindObjectOfType<PlayerController>();
            if (pc != null) player = pc.gameObject;
        }
        if (player == null)
        {
            EditorUtility.DisplayDialog("战斗重构", "当前场景没找到玩家（PlayerInteractor / PlayerController），请确认玩家在场景中。", "确定");
            return;
        }

        if (player.GetComponent<PlayerEncounterTracker>() == null)
            player.AddComponent<PlayerEncounterTracker>();

        var zoneGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        zoneGo.name = "EncounterZone";
        Object.DestroyImmediate(zoneGo.GetComponent<MeshRenderer>());
        var box = zoneGo.GetComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(12f, 3f, 12f);

        var zone = zoneGo.AddComponent<EncounterZone>();
        zone.MinEncounterDistance = 6f;
        zone.MaxEncounterDistance = 14f;
        zone.EncounterTable = new List<EncounterZone.WeightedEncounter>
        {
            new EncounterZone.WeightedEncounter { Encounter = FindFirstEncounter(), Weight = 1 }
        };

        zoneGo.transform.position = player.transform.position + new Vector3(0f, 0f, 3f);
        Selection.activeGameObject = zoneGo;
        MarkDirty();
        Debug.Log("[战斗重构] 玩家已挂 PlayerEncounterTracker，并新建 EncounterZone；调整 BoxCollider 范围覆盖遇敌区域，在 EncounterTable 里配置遭遇组。");
    }

    private static void MarkDirty()
    {
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}
#endif
