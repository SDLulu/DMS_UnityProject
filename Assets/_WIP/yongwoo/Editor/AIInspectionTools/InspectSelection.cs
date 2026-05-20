using Newtonsoft.Json.Linq;
using UnityCliConnector;
using UnityEditor;

namespace AIInspectionTools
{
    [UnityCliTool(
        Name = "inspect_selection",
        Description = "현재 선택한 오브젝트/에셋을 Inspector 요약 형태로 읽기 전용 출력. Hierarchy GameObject가 선택돼 있으면 그게 우선. 그 외에는 Project 창 왼쪽 트리 활성 폴더가 우선이고, 그게 없으면 Selection의 에셋. 자식은 이름과 컴포넌트 타입만 반환 — 더 깊이 보려면 자식을 선택하고 다시 호출",
        Group = "inspection")]
    public static class InspectSelection
    {
        public class Parameters
        {
            [ToolParameter("자식 출력 제한. 기본 80.")]
            public int ChildLimit { get; set; }

            [ToolParameter("컴포넌트당 SerializedField 출력 제한. 기본 32.")]
            public int FieldLimit { get; set; }
        }

        public static object HandleCommand(JObject parameters)
        {
            var p = new ToolParams(parameters);
            int childLimit = p.GetInt("child_limit", 80) ?? 80;
            int fieldLimit = p.GetInt("field_limit", 32) ?? 32;

            UnityEngine.Object selected = Selection.activeObject;
            string source = null;

            bool selectionIsSceneObject = selected != null
                && InspectionUtility.GetSelectedGameObject(selected) != null
                && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(selected));

            if (selectionIsSceneObject)
            {
                source = "selection_scene";
            }
            else
            {
                string folderPath = InspectionUtility.TryGetActiveProjectFolderPath();
                if (!string.IsNullOrEmpty(folderPath))
                {
                    UnityEngine.Object folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folderPath);
                    if (folder != null)
                    {
                        selected = folder;
                        source = "project_browser_folder";
                    }
                }

                if (source == null && selected != null)
                {
                    source = "selection_asset";
                }
            }

            if (selected == null)
            {
                return new ErrorResponse("선택된 오브젝트나 에셋이 없습니다.");
            }

            var gameObject = InspectionUtility.GetSelectedGameObject(selected);
            object data = gameObject != null
                ? InspectionUtility.DescribeGameObject(gameObject, childLimit, fieldLimit)
                : InspectionUtility.DescribeAsset(selected);

            return new SuccessResponse("Selection inspected", new
            {
                active = InspectionUtility.DescribeObject(selected),
                source,
                selection_count = Selection.objects.Length,
                data
            });
        }
    }
}
