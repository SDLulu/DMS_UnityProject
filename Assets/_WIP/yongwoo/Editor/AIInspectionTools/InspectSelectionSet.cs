using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityCliConnector;
using UnityEditor;
using UnityEngine;

namespace AIInspectionTools
{
    [UnityCliTool(
        Name = "inspect_selection_set",
        Description = "현재 선택한 여러 오브젝트/에셋을 타입, 컴포넌트, 부모, 폴더 기준으로 분류",
        Group = "inspection")]
    public static class InspectSelectionSet
    {
        public class Parameters
        {
            [ToolParameter("개별 항목 출력 제한. 기본 120.")]
            public int Limit { get; set; }
        }

        public static object HandleCommand(JObject parameters)
        {
            var p = new ToolParams(parameters);
            int limit = p.GetInt("limit", 120) ?? 120;

            Object[] selected = Selection.objects;
            if (selected.Length == 0)
            {
                return new ErrorResponse("선택된 오브젝트나 에셋이 없습니다.");
            }

            var items = new List<object>();
            var byKind = new Dictionary<string, int>();
            var byType = new Dictionary<string, int>();
            var byComponent = new Dictionary<string, int>();
            var byParent = new Dictionary<string, int>();
            var byFolder = new Dictionary<string, int>();

            int count = 0;
            foreach (Object obj in selected)
            {
                if (obj == null)
                {
                    continue;
                }

                string assetPath = AssetDatabase.GetAssetPath(obj);
                GameObject gameObject = InspectionUtility.GetSelectedGameObject(obj);
                string kind = gameObject != null && string.IsNullOrEmpty(assetPath) ? "scene_object" : "asset";
                Increment(byKind, kind);
                Increment(byType, obj.GetType().Name);

                if (gameObject != null && string.IsNullOrEmpty(assetPath))
                {
                    string parent = gameObject.transform.parent != null
                        ? InspectionUtility.GetHierarchyPath(gameObject.transform.parent)
                        : "<scene_root>";
                    Increment(byParent, parent);

                    var componentTypes = new List<string>();
                    foreach (Component component in gameObject.GetComponents<Component>())
                    {
                        string componentType = component != null ? component.GetType().Name : "MissingScript";
                        componentTypes.Add(componentType);
                        Increment(byComponent, componentType);
                    }

                    if (count < limit)
                    {
                        items.Add(new
                        {
                            kind,
                            name = gameObject.name,
                            scene = gameObject.scene.name,
                            hierarchy_path = InspectionUtility.GetHierarchyPath(gameObject.transform),
                            parent,
                            layer = LayerMask.LayerToName(gameObject.layer),
                            tag = gameObject.tag,
                            component_types = componentTypes
                        });
                    }
                }
                else
                {
                    string folder = string.IsNullOrEmpty(assetPath) ? "<no_asset_path>" : Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
                    Increment(byFolder, string.IsNullOrEmpty(folder) ? "<root>" : folder);

                    if (count < limit)
                    {
                        items.Add(new
                        {
                            kind,
                            name = obj.name,
                            type = obj.GetType().Name,
                            asset_path = assetPath,
                            folder,
                            extension = string.IsNullOrEmpty(assetPath) ? null : Path.GetExtension(assetPath),
                            is_folder = !string.IsNullOrEmpty(assetPath) && AssetDatabase.IsValidFolder(assetPath)
                        });
                    }
                }

                count++;
            }

            return new SuccessResponse("Selection set inspected", new
            {
                selection_count = selected.Length,
                listed_count = items.Count,
                truncated_count = Mathf.Max(0, count - items.Count),
                by_kind = ToRows(byKind),
                by_type = ToRows(byType),
                by_component = ToRows(byComponent),
                by_parent = ToRows(byParent),
                by_folder = ToRows(byFolder),
                items
            });
        }

        private static void Increment(Dictionary<string, int> map, string key)
        {
            key ??= "<null>";
            map.TryGetValue(key, out int count);
            map[key] = count + 1;
        }

        private static List<object> ToRows(Dictionary<string, int> map)
        {
            var rows = new List<object>();
            foreach (var pair in map)
            {
                rows.Add(new { key = pair.Key, count = pair.Value });
            }

            rows.Sort((a, b) =>
            {
                int ac = (int)a.GetType().GetProperty("count").GetValue(a);
                int bc = (int)b.GetType().GetProperty("count").GetValue(b);
                return bc.CompareTo(ac);
            });
            return rows;
        }
    }
}
