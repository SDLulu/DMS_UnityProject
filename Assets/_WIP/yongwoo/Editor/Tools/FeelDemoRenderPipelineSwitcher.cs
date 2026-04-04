using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

// 역할:
// - Feel 데모 자산이 요구하는 렌더 파이프라인 세팅을 빠르게 전환합니다.
// - 데모 확인용 설정 변경을 프로젝트 전체 설정과 분리해 다루기 위한 도구입니다.
//
// 구조 포인트:
// - 실제 게임 로직과 무관한 외부 자산 테스트용 에디터 유틸리티입니다.

public static class FeelDemoRenderPipelineSwitcher
{
    private const string FeelDemoPipelinePath = "Assets/Settings/FeelDemo_URP.asset";
    private const string ProjectPipelinePath = "Assets/Settings/UniversalRP.asset";

    [MenuItem("Tools/Feel/Use Demo Forward Renderer")]
    private static void UseFeelDemoPipeline()
    {
        SwitchPipeline(FeelDemoPipelinePath, "Feel demo forward renderer");
    }

    [MenuItem("Tools/Feel/Restore Project 2D Renderer")]
    private static void RestoreProjectPipeline()
    {
        SwitchPipeline(ProjectPipelinePath, "project 2D renderer");
    }

    private static void SwitchPipeline(string assetPath, string label)
    {
        RenderPipelineAsset pipelineAsset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(assetPath);
        if (pipelineAsset == null)
        {
            Debug.LogError($"Feel renderer switch failed. Missing asset at {assetPath}");
            return;
        }

        GraphicsSettings.defaultRenderPipeline = pipelineAsset;

        int currentQuality = QualitySettings.GetQualityLevel();
        string[] qualityNames = QualitySettings.names;
        for (int i = 0; i < qualityNames.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = pipelineAsset;
        }

        QualitySettings.SetQualityLevel(currentQuality, false);
        QualitySettings.renderPipeline = pipelineAsset;

        AssetDatabase.SaveAssets();
        SceneView.RepaintAll();
        InternalEditorUtility.RepaintAllViews();

        Debug.Log($"Switched render pipeline to {label}: {assetPath}");
    }
}
