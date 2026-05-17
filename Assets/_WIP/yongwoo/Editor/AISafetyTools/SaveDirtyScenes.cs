using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityCliConnector;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace AISafetyTools
{
    [UnityCliTool(
        Name = "save_dirty_scenes",
        Description = "현재 열린 씬 중 저장 안 된 것들만 일괄 저장 (경로 없는 새 씬은 건너뜀)",
        Group = "safety")]
    public static class SaveDirtyScenes
    {
        public static object HandleCommand(JObject parameters)
        {
            if (EditorApplication.isPlaying)
                return new ErrorResponse(
                    "플레이 모드 중에는 씬 저장이 불안정합니다. stop 후 다시 시도하세요.");

            var saved = new List<string>();
            var skipped = new List<string>();

            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                if (!scene.isDirty) continue;

                if (string.IsNullOrEmpty(scene.path))
                {
                    skipped.Add($"{scene.name} (한 번도 저장된 적 없음 — 에디터에서 직접 Save 필요)");
                    continue;
                }

                if (EditorSceneManager.SaveScene(scene))
                    saved.Add(scene.path);
                else
                    skipped.Add($"{scene.path} (저장 실패)");
            }

            if (saved.Count == 0 && skipped.Count == 0)
                return new SuccessResponse("저장할 dirty 씬 없음");

            return new SuccessResponse(
                $"저장 {saved.Count}개, 건너뜀 {skipped.Count}개",
                new { saved, skipped });
        }
    }
}
