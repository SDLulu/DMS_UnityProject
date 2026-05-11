using UnityEditor;
using UnityEngine;

public sealed class DefaultTextureImportSettings : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        var textureImporter = (TextureImporter)assetImporter;
        textureImporter.filterMode = FilterMode.Point;
    }
}
