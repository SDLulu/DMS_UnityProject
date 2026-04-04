using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;

public static class SetBoldPixelsDefaultTmpFont
{
    private const string SourceFontPath = "Assets/Fonts/BoldPixels.ttf";
    private const string FontAssetPath = "Assets/Fonts/BoldPixels SDF.asset";
    private const string MenuPath = "Tools/DMS/Fonts/Set BoldPixels As Default TMP Font";

    [MenuItem(MenuPath)]
    public static void Apply()
    {
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont == null)
        {
            Debug.LogError($"Could not find source font at '{SourceFontPath}'.");
            return;
        }

        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (fontAsset == null)
        {
            fontAsset = CreateFontAsset(sourceFont);
            if (fontAsset == null)
            {
                Debug.LogError("Failed to create the BoldPixels TMP font asset.");
                return;
            }
        }

        TMP_Settings settings = TMP_Settings.instance;
        if (settings == null)
        {
            Debug.LogError("TMP Settings.asset could not be loaded. Import TMP Essential Resources first.");
            return;
        }

        TMP_Settings.defaultFontAsset = fontAsset;
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = fontAsset;
        Debug.Log($"TMP default font is now set to '{fontAsset.name}'.");
    }

    private static TMP_FontAsset CreateFontAsset(Font sourceFont)
    {
        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            90,
            9,
            GlyphRenderMode.SDFAA,
            1024,
            1024);

        if (fontAsset == null)
        {
            return null;
        }

        fontAsset.name = "BoldPixels SDF";
        fontAsset.atlasTexture.name = "BoldPixels Atlas";
        fontAsset.material.name = "BoldPixels Material";

        AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
        AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
        AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

        EditorUtility.SetDirty(fontAsset);
        EditorUtility.SetDirty(fontAsset.atlasTexture);
        EditorUtility.SetDirty(fontAsset.material);
        AssetDatabase.SaveAssets();

        return fontAsset;
    }
}
