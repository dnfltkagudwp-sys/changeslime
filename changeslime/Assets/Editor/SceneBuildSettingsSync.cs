using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Assets/01_Scenes 폴더의 .unity 파일 목록을 Build Settings(Scenes In Build)와 항상 동기화함.
/// 새 레벨 씬을 추가/삭제/이동할 때마다 수동으로 Build Settings에 등록해줄 필요가 없도록 자동화.
/// </summary>
[InitializeOnLoad]
public static class SceneBuildSettingsSync
{
    private const string ScenesFolder = "Assets/01_Scenes";

    static SceneBuildSettingsSync()
    {
        SyncScenes();
    }

    [MenuItem("Tools/씬 빌드 목록 동기화")]
    private static void SyncScenesMenuItem()
    {
        SyncScenes();
    }

    private static void SyncScenes()
    {
        if (!Directory.Exists(ScenesFolder)) return;

        EditorBuildSettingsScene[] scenes = Directory
            .GetFiles(ScenesFolder, "*.unity", SearchOption.TopDirectoryOnly) // 하위 폴더(예: _Archive)는 빌드 목록에서 제외
            .Select(path => path.Replace('\\', '/'))
            .OrderBy(path => path)
            .Select(path => new EditorBuildSettingsScene(path, true))
            .ToArray();

        // 이미 동일한 목록이면 건드리지 않음 (불필요한 변경/저장 방지)
        EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
        if (current.Length == scenes.Length && current.Select(s => s.path).SequenceEqual(scenes.Select(s => s.path)))
            return;

        EditorBuildSettings.scenes = scenes;
        Debug.Log($"[SceneBuildSettingsSync] 빌드 씬 목록을 {ScenesFolder} 기준으로 동기화함 ({scenes.Length}개).");
    }

    private class ScenesFolderWatcher : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            bool relevantChange = importedAssets
                .Concat(deletedAssets)
                .Concat(movedAssets)
                .Concat(movedFromAssetPaths)
                .Any(path => path.StartsWith(ScenesFolder) && path.EndsWith(".unity"));

            if (relevantChange)
                SyncScenes();
        }
    }
}
