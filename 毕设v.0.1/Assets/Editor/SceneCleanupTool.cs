using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 场景/预制体清理工具（在编辑器手动执行一次）：
/// 1. 清理 SampleScene —— 菜单场景仅保留菜单内容，删除混入的玩法对象（Map / PuzzleManager / 旧遗留 Canvas）。
///    玩法内容以后在 GameScene 中重建（见 CODELY.md「GameScene 重建清单」）。
/// 2. 禁用传送门组件 —— 传送门尚未联网化（只在本地改 transform，会被网络同步撤销），
///    暂时从玩家预制体上禁用 PortalGun / PortalableObject。代码保留，联网化后可重新启用。
/// </summary>
public static class SceneCleanupTool
{
    static readonly string[] GameplayRootNames = { "Map", "PuzzleManager", "Canvas" };

    [MenuItem("Tools/毕设/清理 SampleScene（移除玩法内容）")]
    public static void CleanSampleScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "SampleScene")
        {
            EditorUtility.DisplayDialog("场景不对", $"当前打开的场景是「{scene.name}」，请先打开 SampleScene 再执行。", "知道了");
            return;
        }

        // 找出待删除的根物体
        var toRemove = new List<GameObject>();
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var name in GameplayRootNames)
            {
                if (root.name == name)
                {
                    toRemove.Add(root);
                    break;
                }
            }
        }

        if (toRemove.Count == 0)
        {
            EditorUtility.DisplayDialog("无需清理", "SampleScene 中没有找到玩法内容（Map / PuzzleManager / 旧 Canvas），可能已经清理过。", "好的");
            return;
        }

        var names = string.Join("、", toRemove.ConvertAll(go => go.name));
        bool ok = EditorUtility.DisplayDialog(
            "确认清理 SampleScene",
            $"将删除以下根物体（含其全部子物体）：\n{names}\n\n其余菜单内容（MainMenuCanvas / GameHUDCanvas / NetWorkManager 等）保留。此操作不可撤销（可保存后重开场景验证）。",
            "删除", "取消");
        if (!ok) return;

        foreach (var go in toRemove)
        {
            Object.DestroyImmediate(go);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[SceneCleanupTool] 已删除：{names}，并保存场景。");
    }

    [MenuItem("Tools/毕设/禁用玩家预制体传送门组件")]
    public static void DisablePortalComponents()
    {
        SetPortalComponentsEnabled(false);
    }

    [MenuItem("Tools/毕设/重新启用玩家预制体传送门组件")]
    public static void EnablePortalComponents()
    {
        SetPortalComponentsEnabled(true);
    }

    static void SetPortalComponentsEnabled(bool enabled)
    {
        string[] prefabPaths =
        {
            "Assets/Prefabs/player.prefab",
            "Assets/Prefabs/Player 1.prefab",
        };

        int changedPrefabs = 0;
        foreach (var path in prefabPaths)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                Debug.LogWarning($"[SceneCleanupTool] 找不到预制体：{path}");
                continue;
            }

            var contents = PrefabUtility.LoadPrefabContents(path);
            bool changed = false;

            foreach (var portalGun in contents.GetComponentsInChildren<PortalGun>(true))
            {
                if (portalGun.enabled != enabled)
                {
                    portalGun.enabled = enabled;
                    changed = true;
                }
            }
            foreach (var portalable in contents.GetComponentsInChildren<PortalableObject>(true))
            {
                if (portalable.enabled != enabled)
                {
                    portalable.enabled = enabled;
                    changed = true;
                }
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                changedPrefabs++;
                Debug.Log($"[SceneCleanupTool] {(enabled ? "启用" : "禁用")} 传送门组件：{path}");
            }
            PrefabUtility.UnloadPrefabContents(contents);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[SceneCleanupTool] 完成，修改了 {changedPrefabs} 个预制体。");
    }
}
