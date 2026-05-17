using Newtonsoft.Json.Linq;
using UnityCliConnector;
using UnityEditor;

namespace AIInspectionTools
{
    [UnityCliTool(
        Name = "inspect_selection",
        Description = "현재 선택한 오브젝트 또는 에셋 하나를 Inspector 요약 형태로 읽기 전용 출력",
        Group = "inspection")]
    public static class InspectSelection
    {
        public class Parameters
        {
            [ToolParameter("자식 계층 출력 깊이. 기본 2.")]
            public int Depth { get; set; }

            [ToolParameter("Transform 하나당 자식 출력 제한. 기본 40.")]
            public int ChildLimit { get; set; }

            [ToolParameter("컴포넌트당 SerializedField 출력 제한. 기본 32.")]
            public int FieldLimit { get; set; }
        }

        public static object HandleCommand(JObject parameters)
        {
            var p = new ToolParams(parameters);
            int depth = p.GetInt("depth", 2) ?? 2;
            int childLimit = p.GetInt("child_limit", 40) ?? 40;
            int fieldLimit = p.GetInt("field_limit", 32) ?? 32;

            UnityEngine.Object selected = Selection.activeObject;
            if (selected == null)
            {
                return new ErrorResponse("선택된 오브젝트나 에셋이 없습니다.");
            }

            var gameObject = InspectionUtility.GetSelectedGameObject(selected);
            object data = gameObject != null
                ? InspectionUtility.DescribeGameObject(gameObject, depth, childLimit, fieldLimit)
                : InspectionUtility.DescribeAsset(selected);

            return new SuccessResponse("Selection inspected", new
            {
                active = InspectionUtility.DescribeObject(selected),
                selection_count = Selection.objects.Length,
                data
            });
        }
    }
}
