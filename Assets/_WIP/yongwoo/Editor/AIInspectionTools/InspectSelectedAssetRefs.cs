using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityCliConnector;
using UnityEditor;

namespace AIInspectionTools
{
    [UnityCliTool(
        Name = "inspect_selected_asset_refs",
        Description = "선택한 에셋들이 프로젝트 안에서 어디에 참조되는지 읽기 전용으로 검색",
        Group = "inspection")]
    public static class InspectSelectedAssetRefs
    {
        public class Parameters
        {
            [ToolParameter("폴더 선택 시 하위 에셋까지 확장. 기본 false.")]
            public bool ExpandFolders { get; set; }

            [ToolParameter("Packages 아래 에셋도 참조 검색 대상에 포함. 기본 false.")]
            public bool IncludePackages { get; set; }

            [ToolParameter("에셋별 참조 출력 제한. 기본 80.")]
            public int LimitPerAsset { get; set; }
        }

        public static object HandleCommand(JObject parameters)
        {
            var p = new ToolParams(parameters);
            bool expandFolders = p.GetBool("expand_folders", false);
            bool includePackages = p.GetBool("include_packages", false);
            int limitPerAsset = p.GetInt("limit_per_asset", 80) ?? 80;

            List<string> selectedPaths = InspectionUtility.GetSelectedAssetPaths(expandFolders)
                .Where(path => !AssetDatabase.IsValidFolder(path))
                .Distinct()
                .ToList();

            if (selectedPaths.Count == 0)
            {
                return new ErrorResponse("선택된 에셋이 없습니다. Project 창 에셋을 선택하거나 prefab instance를 선택하세요.");
            }

            string[] allPaths = AssetDatabase.GetAllAssetPaths()
                .Where(path => path.StartsWith("Assets/") || includePackages && path.StartsWith("Packages/"))
                .Where(path => !AssetDatabase.IsValidFolder(path))
                .ToArray();

            var results = new List<object>();
            foreach (string targetPath in selectedPaths)
            {
                var refs = new List<object>();
                int totalRefCount = 0;
                foreach (string candidatePath in allPaths)
                {
                    if (candidatePath == targetPath)
                    {
                        continue;
                    }

                    string[] dependencies = AssetDatabase.GetDependencies(candidatePath, true);
                    if (!dependencies.Contains(targetPath))
                    {
                        continue;
                    }

                    totalRefCount++;
                    if (refs.Count < limitPerAsset)
                    {
                        refs.Add(new
                        {
                            path = candidatePath,
                            type = AssetDatabase.GetMainAssetTypeAtPath(candidatePath)?.Name,
                            guid = AssetDatabase.AssetPathToGUID(candidatePath)
                        });
                    }
                }

                results.Add(new
                {
                    path = targetPath,
                    guid = AssetDatabase.AssetPathToGUID(targetPath),
                    type = AssetDatabase.GetMainAssetTypeAtPath(targetPath)?.Name,
                    reference_count = totalRefCount,
                    listed_count = refs.Count,
                    truncated_count = UnityEngine.Mathf.Max(0, totalRefCount - refs.Count),
                    references = refs
                });
            }

            return new SuccessResponse("Selected asset references inspected", new
            {
                selected_asset_count = selectedPaths.Count,
                include_packages = includePackages,
                expand_folders = expandFolders,
                results
            });
        }
    }
}
