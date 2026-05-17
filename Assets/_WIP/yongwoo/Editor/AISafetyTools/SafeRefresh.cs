using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityCliConnector;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace AISafetyTools
{
    [UnityCliTool(
        Name = "safe_refresh",
        Description = "씬 dirty 또는 플레이/컴파일 중이면 거부하는 안전한 AssetDatabase.Refresh",
        Group = "safety")]
    public static class SafeRefresh
    {
        public class Parameters
        {
            [ToolParameter("스크립트 재컴파일 강제 (기본 false). true면 ForceUpdate 사용.")]
            public bool Compile { get; set; }
        }

        public static object HandleCommand(JObject parameters)
        {
            var p = new ToolParams(parameters);
            bool compile = p.GetBool("compile", false);

            if (EditorApplication.isPlaying)
                return new ErrorResponse(
                    "플레이 모드 중. `unity-cli editor stop` 후 다시 시도하세요.");

            if (EditorApplication.isCompiling)
                return new ErrorResponse(
                    "이미 컴파일 중. 끝날 때까지 기다리세요.");

            if (EditorApplication.isUpdating)
                return new ErrorResponse(
                    "에디터가 에셋 임포트 중. 끝날 때까지 기다리세요.");

            var dirty = new List<string>();
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                if (scene.isDirty)
                    dirty.Add(string.IsNullOrEmpty(scene.path) ? scene.name : scene.path);
            }

            if (dirty.Count > 0)
            {
                return new ErrorResponse(
                    $"저장 안 된 씬 {dirty.Count}개: {string.Join(", ", dirty)}. " +
                    "save_dirty_scenes로 저장하거나 사용자에게 확인 후 다시 시도하세요.",
                    new { dirty_scenes = dirty });
            }

            if (compile)
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            else
                AssetDatabase.Refresh();

            return new SuccessResponse(compile ? "Refresh (compile) 완료" : "Refresh 완료");
        }
    }
}
