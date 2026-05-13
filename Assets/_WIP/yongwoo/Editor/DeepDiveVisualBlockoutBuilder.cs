using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;

public static class DeepDiveVisualBlockoutBuilder
{
    private const string Root = "Assets/_WIP/yongwoo";
    private const string ScenePath = Root + "/Scenes/DeepDive_VisualBlockout.unity";
    private const string YongwooStagePath = Root + "/Scenes/Yongwoo_Stage.unity";
    private const string GalleryRootName = "ZZ_DEEP_DIVE_ALL_ASSET_GALLERY";
    private const string SceneMockupsRootName = "ZZ_DEEP_DIVE_SCENE_MOCKUPS_REF_IMAGE";
    private const string RuleCandidatesRootName = "ZZ_DEEP_DIVE_RULE_BASED_SCENE_CANDIDATES";
    private const string CyberpunkRoot = "Assets/ThirdParty/Cyberpunk Platformer Asset Pixel Art";
    private const string BlockoutRoot = Root + "/Art/VisualBlockout";
    private const string TileCopyRoot = BlockoutRoot + "/Tiles";
    private const string TileAssetRoot = BlockoutRoot + "/TileAssets";
    private const int Ppu = 32;

    private static readonly string GhettoTiles =
        "Assets/ThirdParty/Cyberpunk Platformer Asset Pixel Art/Tilesets/craftpix-net-995156-ghetto-tileset-pixel-art/1 Tiles";

    private static readonly string LabTiles =
        "Assets/ThirdParty/Cyberpunk Platformer Asset Pixel Art/Tilesets/craftpix-net-104941-lab-game-tileset-pixel-art/1 Tiles";

    private static readonly string ChineseTiles =
        "Assets/ThirdParty/Cyberpunk Platformer Asset Pixel Art/Tilesets/craftpix-net-716407-chinese-street-tileset-pixel-art/1 Tiles";

    private static readonly string CityBg =
        "Assets/ThirdParty/Cyberpunk Platformer Asset Pixel Art/Backgrounds/craftpix-net-832833-free-scrolling-city-backgrounds-pixel-art/1 Backgrounds/8/Night";

    private static readonly string MarketTiles =
        "Assets/ThirdParty/Cyberpunk Platformer Asset Pixel Art/Tilesets/craftpix-net-153816-cyberpunk-market-street-pixel-art/1 Tiles";

    [MenuItem("DMS/Build Deep Dive Visual Blockout")]
    public static void Build()
    {
        EnsureFolders();
        var ghetto = PrepareTileSet("Ghetto", GhettoTiles, 80);
        var lab = PrepareTileSet("Lab", LabTiles, 64);
        var chinese = PrepareTileSet("ChineseStreet", ChineseTiles, 64);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "DeepDive_VisualBlockout";

        EnsureSortingLayers();
        BuildCamera();
        BuildBackground();
        BuildTilePreviewBoard("01_TILE_CANDIDATES_GHETTO", ghetto, new Vector3(-18f, 5.5f, 0f), 10);
        BuildTilePreviewBoard("02_TILE_CANDIDATES_LAB", lab, new Vector3(-18f, -2.5f, 0f), 10);
        BuildTilePreviewBoard("03_TILE_CANDIDATES_CHINESE", chinese, new Vector3(-18f, -10.5f, 0f), 10);

        BuildPlayableSample("04_SAMPLE_GHETTO_ALLEY_TILEMAP", ghetto, new Vector3(4f, 3.0f, 0f));
        BuildPlayableSample("05_SAMPLE_MEMORY_LAYER_TILEMAP", lab, new Vector3(4f, -5.5f, 0f));
        BuildPlayableSample("06_SAMPLE_STREET_PLATFORM_TILEMAP", chinese, new Vector3(4f, -14.0f, 0f));

        BuildPropStrip();
        AddLabel("CAMERA_FRAME_16_9: Game View에서 이 구역을 먼저 확인", new Vector3(-7.2f, 8.5f, 0f), 0.45f, TextAnchor.MiddleLeft);
        DrawCameraFrame();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorSceneManager.OpenScene(ScenePath);
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        Debug.Log($"[DeepDiveVisualBlockoutBuilder] Built {ScenePath}");
    }

    [MenuItem("DMS/Add Deep Dive Asset Gallery To Yongwoo Stage")]
    public static void AddGalleryToYongwooStage()
    {
        var scene = EditorSceneManager.OpenScene(YongwooStagePath, OpenSceneMode.Single);
        EnsureSortingLayers();

        var old = GameObject.Find(GalleryRootName);
        if (old != null)
        {
            UnityEngine.Object.DestroyImmediate(old);
        }

        var root = new GameObject(GalleryRootName);
        var reference = DetectReferenceSpriteSettings();
        var anchor = FindEmptyGalleryAnchor();
        root.transform.position = anchor;

        AddLabel(
            "DEEP DIVE ASSET GALLERY - 기존 Yongwoo_Stage 빈 공간에 모든 후보 스프라이트를 펼친 구역",
            anchor + new Vector3(0f, 2.4f, 0f),
            0.28f,
            TextAnchor.MiddleLeft,
            root.transform
        );
        AddLabel(
            $"기준 SpriteRenderer: SortingLayer={reference.sortingLayerName}, Order={reference.sortingOrder}, Material={reference.materialName}",
            anchor + new Vector3(0f, 1.9f, 0f),
            0.18f,
            TextAnchor.MiddleLeft,
            root.transform
        );

        var sprites = CollectCyberpunkSprites();
        var grouped = sprites
            .GroupBy(s => SectionName(s.path))
            .OrderBy(g => SectionOrder(g.Key))
            .ThenBy(g => g.Key)
            .ToList();

        var cursor = Vector3.zero;
        var total = 0;
        foreach (var group in grouped)
        {
            cursor = AddSpriteSection(root.transform, group.Key, group.ToList(), cursor, reference);
            total += group.Count();
        }

        AddLabel(
            $"TOTAL SPRITES PLACED: {total}  /  원본 에셋은 수정하지 않고 씬 인스턴스만 배치",
            anchor + cursor + new Vector3(0f, -1.4f, 0f),
            0.22f,
            TextAnchor.MiddleLeft,
            root.transform
        );

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = root;
        Debug.Log($"[DeepDiveVisualBlockoutBuilder] Added {total} sprites to {YongwooStagePath} under {GalleryRootName}");
    }

    [MenuItem("DMS/Add Deep Dive Scene Mockups To Yongwoo Stage")]
    public static void AddSceneMockupsToYongwooStage()
    {
        var scene = EditorSceneManager.OpenScene(YongwooStagePath, OpenSceneMode.Single);
        EnsureSortingLayers();
        EnsureUtilitySprites();

        var old = GameObject.Find(SceneMockupsRootName);
        if (old != null)
        {
            UnityEngine.Object.DestroyImmediate(old);
        }

        var root = new GameObject(SceneMockupsRootName);
        root.transform.position = new Vector3(72f, 8f, 0f);

        AddLabel("REFERENCE-BASED SCENE MOCKUPS / 참고 이미지처럼 주거지 밀도, 검은 타이틀 보드, 청록 네온, 이어진 타일 바닥을 확인", root.transform.position + new Vector3(0f, 3.5f, 0f), 0.26f, TextAnchor.MiddleLeft, root.transform);

        BuildSceneMockup(root.transform, "01_TITLE_RESIDENTIAL_AREA", new Vector3(0f, 0f, 0f), "DEEP DIVE: HOME", "먼 도시 + 큰 검은 타이틀 보드");
        BuildSceneMockup(root.transform, "02_PROLOGUE_ACCESS_00", new Vector3(24f, 0f, 0f), "ACCESS 00: HOME", "튜토리얼: 이동/점프/대시/공격");
        BuildSceneMockup(root.transform, "03_PLAYER_ROOM", new Vector3(48f, 0f, 0f), "DEBT ROOM", "현실: 차가운 방과 단말기");
        BuildSceneMockup(root.transform, "04_CITY_PLAZA", new Vector3(72f, 0f, 0f), "RESIDENTIAL AREA", "광장: 밝지만 배제된 도시");

        BuildSceneMockup(root.transform, "05_BROKER_ALLEY", new Vector3(0f, -15f, 0f), "BACK ALLEY", "브로커와 칩 접속 장치");
        BuildSceneMockup(root.transform, "06_MEMORY_HOME", new Vector3(24f, -15f, 0f), "MEMORY: HOME", "집 기억 조각과 HOME 코어");
        BuildSceneMockup(root.transform, "07_RESIDUAL_BOSS", new Vector3(48f, -15f, 0f), "RESIDUAL 047", "보스방: 문지기와 깨진 공간");
        BuildSceneMockup(root.transform, "08_ENDING_DOOR", new Vector3(72f, -15f, 0f), "1KB LEFT", "엔딩: 문을 살짝 열어둠");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = root;
        Debug.Log($"[DeepDiveVisualBlockoutBuilder] Added reference scene mockups to {YongwooStagePath}");
    }

    [MenuItem("DMS/Add Rule Based Deep Dive Candidates To Yongwoo Stage")]
    public static void AddRuleBasedCandidatesToYongwooStage()
    {
        var scene = EditorSceneManager.OpenScene(YongwooStagePath, OpenSceneMode.Single);
        EnsureSortingLayers();
        EnsureUtilitySprites();

        DestroyIfExists(SceneMockupsRootName);
        DestroyIfExists(RuleCandidatesRootName);

        var samples = CollectReferenceRenderersFromCurrentStage();
        if (samples.Count == 0)
        {
            Debug.LogWarning("[DeepDiveVisualBlockoutBuilder] No source SpriteRenderers found in Yongwoo_Stage.");
            return;
        }

        var root = new GameObject(RuleCandidatesRootName);
        var anchor = FindEmptyGalleryAnchor() + new Vector3(0f, -10f, 0f);
        root.transform.position = anchor;

        AddLabel(
            "RULE BASED CANDIDATES - Yongwoo_Stage의 실제 배치/스케일/정렬을 복제한 후보 장면",
            anchor + new Vector3(0f, 3.1f, 0f),
            0.28f,
            TextAnchor.MiddleLeft,
            root.transform
        );
        AddLabel(
            $"Source SpriteRenderers: {samples.Count}. 기존 스테이지 샘플을 그대로 복제하고 장면별 포커스만 얹음.",
            anchor + new Vector3(0f, 2.65f, 0f),
            0.20f,
            TextAnchor.MiddleLeft,
            root.transform
        );

        var definitions = new[]
        {
            new { name = "01_TITLE", title = "DEEP DIVE: HOME", note = "타이틀: 참고 이미지처럼 큰 검은 타이틀 보드" },
            new { name = "02_PROLOGUE", title = "ACCESS 00: HOME", note = "튜토리얼: 기존 골목 레벨 배치 위 전투 시작점" },
            new { name = "03_PLAYER_ROOM", title = "DEBT ROOM", note = "주인공 방: 차갑고 좁은 실내 후보" },
            new { name = "04_PLAZA", title = "RESIDENTIAL AREA", note = "광장: 상위구역 접근 거부 후보" },
            new { name = "05_ALLEY", title = "BACK ALLEY", note = "브로커 골목: 칩 접속 장치 후보" },
            new { name = "06_MEMORY", title = "MEMORY: HOME", note = "기억층: 따뜻한 HOME 코어 후보" },
            new { name = "07_BOSS", title = "RESIDUAL 047", note = "보스방: 문지기와 깨진 기억 후보" },
            new { name = "08_ENDING", title = "1KB LEFT", note = "엔딩: 문을 살짝 열어둔 방 후보" },
        };

        for (var i = 0; i < definitions.Length; i++)
        {
            var x = (i % 4) * 24f;
            var y = -(i / 4) * 15f;
            BuildCandidateFromReference(root.transform, samples, new Vector3(x, y, 0f), definitions[i].name, definitions[i].title, definitions[i].note);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = root;
        Debug.Log($"[DeepDiveVisualBlockoutBuilder] Added rule-based scene candidates with {samples.Count} source renderers.");
    }

    private static void EnsureFolders()
    {
        CreateFolder("Assets/_WIP", "yongwoo");
        CreateFolder(Root, "Scenes");
        CreateFolder(Root, "Art");
        CreateFolder(Root + "/Art", "VisualBlockout");
        CreateFolder(BlockoutRoot, "Tiles");
        CreateFolder(BlockoutRoot, "TileAssets");
    }

    private static List<Tile> PrepareTileSet(string setName, string sourceFolder, int maxTiles)
    {
        CreateFolder(TileCopyRoot, setName);
        CreateFolder(TileAssetRoot, setName);

        var tilePaths = AssetDatabase.FindAssets("t:Texture2D", new[] { sourceFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => Path.GetFileNameWithoutExtension(p).StartsWith("Tile_", StringComparison.OrdinalIgnoreCase))
            .OrderBy(NaturalTileKey)
            .Take(maxTiles)
            .ToList();

        var result = new List<Tile>();
        foreach (var sourcePath in tilePaths)
        {
            var copyPath = $"{TileCopyRoot}/{setName}/{Path.GetFileName(sourcePath)}";
            if (!File.Exists(copyPath))
            {
                AssetDatabase.CopyAsset(sourcePath, copyPath);
            }

            ConfigureSpriteImporter(copyPath);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(copyPath);
            if (sprite == null) continue;

            var tilePath = $"{TileAssetRoot}/{setName}/{Path.GetFileNameWithoutExtension(sourcePath)}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, tilePath);
            }
            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.Sprite;
            EditorUtility.SetDirty(tile);
            result.Add(tile);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return result;
    }

    private static void ConfigureSpriteImporter(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = Ppu;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }

    private static void BuildCamera()
    {
        var cameraObj = new GameObject("Main Camera");
        var camera = cameraObj.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.2f;
        camera.backgroundColor = new Color(0.015f, 0.018f, 0.026f, 1f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        cameraObj.transform.position = new Vector3(0f, 0f, -10f);
        cameraObj.tag = "MainCamera";
    }

    private static void BuildBackground()
    {
        var parent = new GameObject("00_BACKGROUND_PREVIEW");
        var bgPaths = AssetDatabase.FindAssets("t:Texture2D", new[] { CityBg })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(p => p)
            .ToList();

        for (var i = 0; i < bgPaths.Count; i++)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(bgPaths[i]);
            if (sprite == null) continue;
            var obj = new GameObject($"BG_Layer_{i + 1}_{Path.GetFileNameWithoutExtension(bgPaths[i])}");
            obj.transform.SetParent(parent.transform);
            obj.transform.position = new Vector3(0f, 0f, 8f + i * 0.01f);
            obj.transform.localScale = Vector3.one * 2.4f;
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = "Background";
            sr.sortingOrder = i * 10;
            sr.color = new Color(0.62f, 0.68f, 0.8f, i == 0 ? 0.85f : 0.72f);
        }
    }

    private static void BuildTilePreviewBoard(string name, List<Tile> tiles, Vector3 origin, int columns)
    {
        var parent = new GameObject(name);
        AddLabel(name + "  /  번호 보고 마음에 드는 타일 조합 고르기", origin + new Vector3(0f, 1.2f, 0f), 0.24f, TextAnchor.MiddleLeft, parent.transform);

        for (var i = 0; i < tiles.Count; i++)
        {
            var sprite = tiles[i].sprite;
            if (sprite == null) continue;
            var x = i % columns;
            var y = i / columns;
            var obj = new GameObject($"{i:00}_{sprite.name}");
            obj.transform.SetParent(parent.transform);
            obj.transform.position = origin + new Vector3(x * 0.78f, -y * 0.78f, 0f);
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = "Ground";
            sr.sortingOrder = 50;

            if (i < 40)
            {
                AddLabel(i.ToString("00"), obj.transform.position + new Vector3(-0.25f, -0.34f, 0f), 0.12f, TextAnchor.MiddleCenter, parent.transform);
            }
        }
    }

    private static void BuildPlayableSample(string name, List<Tile> tiles, Vector3 origin)
    {
        if (tiles.Count < 12) return;

        var gridObj = new GameObject(name);
        gridObj.transform.position = origin;
        var grid = gridObj.AddComponent<Grid>();
        grid.cellSize = Vector3.one;

        var groundObj = new GameObject("Ground_Tilemap_actual_editable");
        groundObj.transform.SetParent(gridObj.transform);
        var tilemap = groundObj.AddComponent<Tilemap>();
        var renderer = groundObj.AddComponent<TilemapRenderer>();
        renderer.sortingLayerName = "Ground";
        renderer.sortingOrder = 100;

        PaintPlatform(tilemap, -8, -2, 15, tiles, 0);
        PaintPlatform(tilemap, -2, 1, 7, tiles, 10);
        PaintPlatform(tilemap, 7, 0, 5, tiles, 20);
        PaintPlatform(tilemap, -9, -5, 6, tiles, 30);
        PaintPlatform(tilemap, 2, -5, 10, tiles, 40);

        AddLabel(name + "  /  Grid + Tilemap. 직접 브러시로 수정 가능", origin + new Vector3(-8f, 2.8f, 0f), 0.24f, TextAnchor.MiddleLeft, gridObj.transform);

        var frame = new GameObject("Camera_Frame_Guide");
        frame.transform.SetParent(gridObj.transform);
        frame.transform.localPosition = Vector3.zero;
        var line = frame.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.widthMultiplier = 0.03f;
        line.positionCount = 4;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = new Color(0.2f, 0.95f, 1f, 0.7f);
        line.endColor = line.startColor;
        line.sortingLayerName = "Effect";
        line.sortingOrder = 500;
        line.SetPositions(new[]
        {
            new Vector3(-9.2f, -4.8f, 0f),
            new Vector3(9.2f, -4.8f, 0f),
            new Vector3(9.2f, 5.2f, 0f),
            new Vector3(-9.2f, 5.2f, 0f)
        });
    }

    private static void PaintPlatform(Tilemap map, int startX, int y, int width, List<Tile> tiles, int offset)
    {
        var topLeft = tiles[Mathf.Clamp(offset + 0, 0, tiles.Count - 1)];
        var topMid = tiles[Mathf.Clamp(offset + 1, 0, tiles.Count - 1)];
        var topRight = tiles[Mathf.Clamp(offset + 2, 0, tiles.Count - 1)];
        var fill = tiles[Mathf.Clamp(offset + 8, 0, tiles.Count - 1)];
        var bottom = tiles[Mathf.Clamp(offset + 9, 0, tiles.Count - 1)];

        for (var x = 0; x < width; x++)
        {
            var tile = x == 0 ? topLeft : x == width - 1 ? topRight : topMid;
            map.SetTile(new Vector3Int(startX + x, y, 0), tile);
            map.SetTile(new Vector3Int(startX + x, y - 1, 0), fill);
            map.SetTile(new Vector3Int(startX + x, y - 2, 0), bottom);
        }
    }

    private static void BuildPropStrip()
    {
        var parent = new GameObject("07_PROP_AND_CHARACTER_REFERENCE_STRIP");
        AddLabel("PROP / CHARACTER 후보. 여기서 보고 골라서 무대에 복사", new Vector3(4f, 8.0f, 0f), 0.28f, TextAnchor.MiddleLeft, parent.transform);

        var paths = new[]
        {
            "Assets/ThirdParty/Cyberpunk Platformer Asset Pixel Art/Tilesets/craftpix-net-995156-ghetto-tileset-pixel-art/3 Objects/Car.png",
            "Assets/ThirdParty/Cyberpunk Platformer Asset Pixel Art/Tilesets/craftpix-net-995156-ghetto-tileset-pixel-art/3 Objects/Barrel1.png",
            "Assets/ThirdParty/Cyberpunk Platformer Asset Pixel Art/Tilesets/craftpix-net-995156-ghetto-tileset-pixel-art/3 Objects/trash_can1.png",
            "Assets/ThirdParty/Cyberpunk Platformer Asset Pixel Art/Tilesets/craftpix-net-995156-ghetto-tileset-pixel-art/3 Objects/Box1.png",
            "Assets/ThirdParty/Cyberpunk Platformer Asset Pixel Art/Characters & Sprites/craftpix-net-598640-free-characters-with-melee-attack-pixel-art/2 Weapons/1 Idle/1.png",
            "Assets/ThirdParty/Cyberpunk Platformer Asset Pixel Art/Characters & Sprites/craftpix-net-545114-free-pixel-enemies-character-pack-for-seaport-location/6/Idle.png",
            "Assets/ThirdParty/Cyberpunk Platformer Asset Pixel Art/Characters & Sprites/craftpix-net-999713-cyberpunk-pixel-art-bosses-pack/3/Idle.png"
        };

        for (var i = 0; i < paths.Length; i++)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(paths[i]);
            if (sprite == null) continue;
            var obj = new GameObject($"{i:00}_{Path.GetFileNameWithoutExtension(paths[i])}");
            obj.transform.SetParent(parent.transform);
            obj.transform.position = new Vector3(4f + i * 1.6f, 7.0f, 0f);
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = i >= 4 ? "Player" : "Ground";
            sr.sortingOrder = 200;
        }
    }

    private struct SpriteEntry
    {
        public string path;
        public Sprite sprite;
    }

    private struct RendererReference
    {
        public string sortingLayerName;
        public int sortingOrder;
        public Material material;
        public string materialName;
    }

    private struct RendererSample
    {
        public string name;
        public Sprite sprite;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public Color color;
        public string sortingLayerName;
        public int sortingOrder;
        public Material material;
        public Vector3 boundsCenter;
    }

    private static List<RendererSample> CollectReferenceRenderersFromCurrentStage()
    {
        var renderers = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
            .Where(r => r != null && r.sprite != null)
            .Where(r => !r.transform.root.name.StartsWith("ZZ_DEEP_DIVE", StringComparison.OrdinalIgnoreCase))
            .Where(r => !r.transform.root.name.Contains("Canvas", StringComparison.OrdinalIgnoreCase))
            .Where(r => !r.gameObject.name.Contains("UI", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var camera = Camera.main;
        var source = new List<SpriteRenderer>();
        if (camera != null && camera.orthographic)
        {
            var h = camera.orthographicSize;
            var w = h * camera.aspect;
            var cx = camera.transform.position.x;
            var cy = camera.transform.position.y;
            source = renderers
                .Where(r => r.bounds.center.x >= cx - w - 3f && r.bounds.center.x <= cx + w + 3f)
                .Where(r => r.bounds.center.y >= cy - h - 3f && r.bounds.center.y <= cy + h + 3f)
                .OrderBy(r => r.sortingLayerID)
                .ThenBy(r => r.sortingOrder)
                .Take(220)
                .ToList();
        }

        if (source.Count < 24)
        {
            source = renderers
                .OrderBy(r => r.bounds.center.x)
                .ThenBy(r => r.bounds.center.y)
                .Take(220)
                .ToList();
        }

        var center = Vector3.zero;
        if (source.Count > 0)
        {
            center = source.Select(r => r.bounds.center).Aggregate(Vector3.zero, (a, b) => a + b) / source.Count;
        }

        return source.Select(r => new RendererSample
        {
            name = r.gameObject.name,
            sprite = r.sprite,
            position = r.transform.position - center,
            rotation = r.transform.rotation,
            scale = r.transform.lossyScale,
            color = r.color,
            sortingLayerName = r.sortingLayerName,
            sortingOrder = r.sortingOrder,
            material = r.sharedMaterial,
            boundsCenter = r.bounds.center - center
        }).ToList();
    }

    private static void BuildCandidateFromReference(Transform root, List<RendererSample> samples, Vector3 localOrigin, string id, string title, string note)
    {
        var parent = new GameObject("CANDIDATE_" + id);
        parent.transform.SetParent(root);
        parent.transform.localPosition = localOrigin;

        AddRect(parent.transform, "Candidate_Backdrop", new Vector3(0f, 0f, 0f), new Vector2(22f, 11.8f), new Color(0.02f, 0.02f, 0.03f, 0.92f), "Background", 900);

        foreach (var sample in samples)
        {
            if (sample.sprite == null) continue;
            var obj = new GameObject("REF_" + SanitizeName(sample.name));
            obj.transform.SetParent(parent.transform);
            obj.transform.localPosition = sample.position;
            obj.transform.rotation = sample.rotation;
            obj.transform.localScale = sample.scale;
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sample.sprite;
            sr.color = sample.color;
            sr.sortingLayerName = sample.sortingLayerName;
            sr.sortingOrder = sample.sortingOrder;
            if (sample.material != null) sr.sharedMaterial = sample.material;
        }

        AddReferenceTitleBoard(parent.transform, id, title);
        AddCandidateSceneFocus(parent.transform, id);
        AddLabel(note, root.transform.position + localOrigin + new Vector3(-10.7f, -6.4f, 0f), 0.18f, TextAnchor.MiddleLeft, parent.transform);
        DrawFrame(parent.transform, new Vector2(22f, 11.8f), "Foreground", 6000);
    }

    private static void AddReferenceTitleBoard(Transform parent, string id, string title)
    {
        var showBigBoard = id.Contains("01") || id.Contains("04");
        if (!showBigBoard)
        {
            AddLabel(title, parent.TransformPoint(new Vector3(-10.6f, 5.2f, 0f)), 0.26f, TextAnchor.MiddleLeft, parent);
            return;
        }

        AddRect(parent, "Reference_Black_Title_Board", new Vector3(0f, 4.35f, 0f), new Vector2(18.5f, 2.0f), Color.black, "Foreground", 5800);
        AddRect(parent, "Reference_Title_Cyan_Glow_Top", new Vector3(0f, 5.38f, 0f), new Vector2(18.8f, 0.08f), new Color(0.2f, 1f, 1f, 0.65f), "Foreground", 5801);
        AddLabel(title, parent.TransformPoint(new Vector3(-6.9f, 4.07f, 0f)), 0.52f, TextAnchor.MiddleLeft, parent);
    }

    private static void AddCandidateSceneFocus(Transform parent, string id)
    {
        if (id.Contains("02"))
        {
            AddRect(parent, "Access_Glitch_Portal", new Vector3(4.8f, -1.5f, 0f), new Vector2(1.0f, 3.5f), new Color(0.1f, 1f, 1f, 0.34f), "Foreground", 5600);
            AddLabel("[접속 중...] HOME", parent.TransformPoint(new Vector3(-5.5f, 2.6f, 0f)), 0.2f, TextAnchor.MiddleLeft, parent);
        }
        else if (id.Contains("03"))
        {
            AddRect(parent, "Room_Dark_Block", new Vector3(0f, -1.1f, 0f), new Vector2(9.5f, 5.2f), new Color(0.025f, 0.035f, 0.05f, 0.88f), "Foreground", 5400);
            AddRect(parent, "Debt_Terminal", new Vector3(2.5f, -1.0f, 0f), new Vector2(1.5f, 0.9f), new Color(0.1f, 0.9f, 1f, 0.75f), "Foreground", 5600);
            AddLabel("[채무 잔액] 83,420C", parent.TransformPoint(new Vector3(-4.2f, 1.2f, 0f)), 0.2f, TextAnchor.MiddleLeft, parent);
        }
        else if (id.Contains("05"))
        {
            AddRect(parent, "Broker_Device", new Vector3(5.4f, -3.3f, 0f), new Vector2(1.2f, 1.0f), new Color(1f, 0.08f, 0.5f, 0.9f), "Foreground", 5600);
            AddLabel("E : 원본 칩 접속", parent.TransformPoint(new Vector3(3.4f, -2.3f, 0f)), 0.18f, TextAnchor.MiddleLeft, parent);
        }
        else if (id.Contains("06"))
        {
            AddRect(parent, "Home_Core_Warmth", new Vector3(1.2f, -0.6f, 0f), new Vector2(4.0f, 3.0f), new Color(1f, 0.7f, 0.22f, 0.22f), "Foreground", 5450);
            AddRect(parent, "HOME_Core", new Vector3(1.2f, -0.4f, 0f), new Vector2(1.0f, 1.0f), new Color(0.75f, 1f, 0.92f, 0.95f), "Foreground", 5600);
            AddLabel("HOME", parent.TransformPoint(new Vector3(0.4f, 1.5f, 0f)), 0.25f, TextAnchor.MiddleLeft, parent);
        }
        else if (id.Contains("07"))
        {
            AddRect(parent, "Residual_Door", new Vector3(1.2f, -0.7f, 0f), new Vector2(1.8f, 4.0f), new Color(0.2f, 1f, 1f, 0.28f), "Foreground", 5550);
            AddRect(parent, "Boss_Warning_Line", new Vector3(1.2f, 1.7f, 0f), new Vector2(3.5f, 0.12f), new Color(1f, 0.08f, 0.45f, 0.9f), "Foreground", 5600);
            AddLabel("그건 팔 물건이 아니야", parent.TransformPoint(new Vector3(-3.7f, 2.5f, 0f)), 0.2f, TextAnchor.MiddleLeft, parent);
        }
        else if (id.Contains("08"))
        {
            AddRect(parent, "Ending_Door_Light", new Vector3(5.0f, -1.2f, 0f), new Vector2(1.2f, 4.3f), new Color(1f, 0.75f, 0.28f, 0.45f), "Foreground", 5600);
            AddLabel("[미전송 데이터: 1KB]", parent.TransformPoint(new Vector3(-3.8f, 2.2f, 0f)), 0.21f, TextAnchor.MiddleLeft, parent);
        }
    }

    private static void DestroyIfExists(string objectName)
    {
        var old = GameObject.Find(objectName);
        if (old != null)
        {
            UnityEngine.Object.DestroyImmediate(old);
        }
    }

    private static RendererReference DetectReferenceSpriteSettings()
    {
        var renderers = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
            .Where(r => r.sprite != null && !r.name.StartsWith("Background", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var mostCommon = renderers
            .GroupBy(r => r.sortingLayerName)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        var sample = mostCommon?.FirstOrDefault() ?? renderers.FirstOrDefault();
        return new RendererReference
        {
            sortingLayerName = sample != null ? sample.sortingLayerName : "Ground",
            sortingOrder = sample != null ? sample.sortingOrder : 0,
            material = sample != null ? sample.sharedMaterial : null,
            materialName = sample != null && sample.sharedMaterial != null ? sample.sharedMaterial.name : "default"
        };
    }

    private static Vector3 FindEmptyGalleryAnchor()
    {
        var renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
            .Where(r => r.gameObject.name != GalleryRootName)
            .ToList();
        if (renderers.Count == 0) return new Vector3(0f, -18f, 0f);

        var minY = renderers.Min(r => r.bounds.min.y);
        var minX = renderers.Min(r => r.bounds.min.x);
        return new Vector3(minX, minY - 9f, 0f);
    }

    private static List<SpriteEntry> CollectCyberpunkSprites()
    {
        return AssetDatabase.FindAssets("t:Texture2D", new[] { CyberpunkRoot })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => !Path.GetFileName(p).Contains("COUPON", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p)
            .SelectMany(p => AssetDatabase.LoadAllAssetsAtPath(p)
                .OfType<Sprite>()
                .OrderBy(s => s.name)
                .Select(s => new SpriteEntry { path = p, sprite = s }))
            .Where(e => e.sprite != null)
            .ToList();
    }

    private static Vector3 AddSpriteSection(Transform root, string sectionName, List<SpriteEntry> entries, Vector3 localOrigin, RendererReference reference)
    {
        var section = new GameObject("SECTION_" + SanitizeName(sectionName));
        section.transform.SetParent(root);
        section.transform.localPosition = localOrigin;

        AddLabel($"{sectionName}  ({entries.Count})", localOrigin, 0.24f, TextAnchor.MiddleLeft, root);

        const int columns = 36;
        const float cellW = 1.15f;
        const float cellH = 1.15f;
        const float maxVisual = 0.82f;

        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var col = i % columns;
            var row = i / columns;

            var obj = new GameObject($"{i:0000}_{SanitizeName(entry.sprite.name)}");
            obj.transform.SetParent(section.transform);
            obj.transform.localPosition = new Vector3(col * cellW, -1.0f - row * cellH, 0f);

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = entry.sprite;
            // Gallery sprites are for visual inspection, so keep them above the existing stage.
            // The existing stage reference is displayed in the label, but using it directly can hide
            // thousands of preview sprites behind already placed foreground/background objects.
            sr.sortingLayerName = "Foreground";
            sr.sortingOrder = 2000 + row;

            var size = entry.sprite.bounds.size;
            var largest = Mathf.Max(size.x, size.y);
            if (largest > 0.001f)
            {
                obj.transform.localScale = Vector3.one * Mathf.Min(1f, maxVisual / largest);
            }

            if (i % 25 == 0)
            {
                AddLabel(i.ToString("0000"), localOrigin + obj.transform.localPosition + new Vector3(-0.36f, -0.46f, 0f), 0.10f, TextAnchor.MiddleCenter, root);
            }
        }

        var rows = Mathf.CeilToInt(entries.Count / (float)columns);
        return localOrigin + new Vector3(0f, -(rows + 2.2f) * cellH, 0f);
    }

    private static void BuildSceneMockup(Transform root, string name, Vector3 localOrigin, string title, string note)
    {
        var parent = new GameObject(name);
        parent.transform.SetParent(root);
        parent.transform.localPosition = localOrigin;

        AddRect(parent.transform, "Backdrop", new Vector3(10.5f, -3.5f, 0f), new Vector2(21f, 11.5f), new Color(0.025f, 0.025f, 0.04f, 1f), "Background", 950);
        AddBackgroundLayers(parent.transform);
        AddDenseResidentialBlocks(parent.transform, name);
        AddConnectedGround(parent.transform, -9.5f, -8.7f, 20);
        AddUpperPlatforms(parent.transform, name);
        AddNeonProps(parent.transform, name);
        AddSceneSpecificFocus(parent.transform, name);

        AddRect(parent.transform, "Black_Title_Board", new Vector3(10.5f, 1.75f, 0f), new Vector2(16.8f, 2.15f), Color.black, "Foreground", 2600);
        AddLabel(title, root.transform.position + localOrigin + new Vector3(3.4f, 1.38f, 0f), 0.46f, TextAnchor.MiddleLeft, parent.transform);
        AddLabel(note, root.transform.position + localOrigin + new Vector3(0f, -9.65f, 0f), 0.18f, TextAnchor.MiddleLeft, parent.transform);

        DrawFrame(parent.transform, new Vector2(21f, 11.5f), "Foreground", 2800);
    }

    private static void AddBackgroundLayers(Transform parent)
    {
        var bgPaths = AssetDatabase.FindAssets("t:Texture2D", new[] { CityBg })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(p => p)
            .ToList();

        for (var i = 0; i < bgPaths.Count; i++)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(bgPaths[i]);
            if (sprite == null) continue;
            var obj = AddSpriteCover(parent, $"BG_{i + 1}", sprite, new Vector3(10.5f, -2.8f, 0f), new Vector2(21f, 11.5f), "Background", 960 + i * 3);
            var sr = obj.GetComponent<SpriteRenderer>();
            sr.color = new Color(0.42f, 0.48f, 0.64f, 0.4f + i * 0.07f);
        }
    }

    private static void AddDenseResidentialBlocks(Transform parent, string sceneName)
    {
        var wallTiles = LoadSprites(MarketTiles, 90)
            .Where(s => s != null)
            .ToList();
        if (wallTiles.Count == 0) return;

        var columns = new[]
        {
            new { x = 0.2f, w = 4, h = 8, o = 0 },
            new { x = 3.9f, w = 5, h = 6, o = 10 },
            new { x = 8.2f, w = 6, h = 9, o = 20 },
            new { x = 13.6f, w = 4, h = 7, o = 30 },
            new { x = 17.0f, w = 5, h = 8, o = 40 },
        };

        foreach (var c in columns)
        {
            for (var ix = 0; ix < c.w; ix++)
            {
                for (var iy = 0; iy < c.h; iy++)
                {
                    var sprite = wallTiles[Mathf.Abs(c.o + ix + iy * 3) % wallTiles.Count];
                    var obj = AddSpriteSized(parent, $"BuildingTile_{c.x}_{ix}_{iy}", sprite, new Vector3(c.x + ix, -7.6f + iy, 0f), new Vector2(1.02f, 1.02f), "Ground", 1300 + iy);
                    var sr = obj.GetComponent<SpriteRenderer>();
                    sr.color = new Color(0.46f, 0.34f + (ix % 3) * 0.04f, 0.62f + (iy % 2) * 0.08f, 0.92f);
                }
            }
        }

        var window = GetUtilitySprite("white");
        for (var i = 0; i < 18; i++)
        {
            var x = 1.1f + (i * 2.05f) % 18f;
            var y = -5.8f + (i % 5) * 1.15f;
            var color = i % 3 == 0 ? new Color(0.25f, 0.95f, 1f, 0.72f) : new Color(0.08f, 0.04f, 0.16f, 0.9f);
            AddRect(parent, $"Window_{i}", new Vector3(x, y, 0f), new Vector2(0.46f, 0.9f), color, "Foreground", 1850 + i);
        }
    }

    private static void AddConnectedGround(Transform parent, float startX, float y, int count)
    {
        var tiles = LoadSprites(MarketTiles, 120);
        if (tiles.Count == 0) return;

        var top = tiles.ElementAtOrDefault(0) ?? tiles[0];
        var mid = tiles.ElementAtOrDefault(1) ?? tiles[0];
        var fill = tiles.ElementAtOrDefault(8) ?? tiles[0];

        for (var i = 0; i < count; i++)
        {
            AddSpriteSized(parent, $"GroundTop_{i}", i == 0 || i == count - 1 ? top : mid, new Vector3(startX + i, y, 0f), new Vector2(1.02f, 1.02f), "Foreground", 2200);
            AddSpriteSized(parent, $"GroundFillA_{i}", fill, new Vector3(startX + i, y - 1f, 0f), new Vector2(1.02f, 1.02f), "Foreground", 2190);
            AddSpriteSized(parent, $"GroundFillB_{i}", fill, new Vector3(startX + i, y - 2f, 0f), new Vector2(1.02f, 1.02f), "Foreground", 2180);
        }
    }

    private static void AddUpperPlatforms(Transform parent, string sceneName)
    {
        AddConnectedGround(parent, -8.8f, -3.1f, 5);
        AddConnectedGround(parent, 1.8f, -4.2f, sceneName.Contains("BOSS") ? 4 : 7);
        AddConnectedGround(parent, 10.7f, -4.4f, 5);
    }

    private static void AddNeonProps(Transform parent, string sceneName)
    {
        var ads = new[]
        {
            "Assets/ThirdParty/Cyberpunk Platformer Asset Pixel Art/2D Game Objects/craftpix-net-154211-animated-ads-cyberpunk-pixel-art/1 Ads/1.png",
            "Assets/ThirdParty/Cyberpunk Platformer Asset Pixel Art/2D Game Objects/craftpix-net-154211-animated-ads-cyberpunk-pixel-art/1 Ads/6.png",
            "Assets/ThirdParty/Cyberpunk Platformer Asset Pixel Art/2D Game Objects/craftpix-net-154211-animated-ads-cyberpunk-pixel-art/2 Billboard/92x60.png",
            "Assets/ThirdParty/Cyberpunk Platformer Asset Pixel Art/2D Game Objects/craftpix-net-321524-city-signs-and-barriers-pixel-art/2 Road signs/1.png",
        };

        AddSpritePath(parent, "NeonSign_Left", ads[0], new Vector3(1.3f, -4.2f, 0f), 1.1f, "Foreground", 2450, new Color(1f, 0.45f, 0.85f, 1f));
        AddSpritePath(parent, "NeonSign_Center", ads[1], new Vector3(6.8f, -5.2f, 0f), 1.0f, "Foreground", 2455, Color.white);
        AddSpritePath(parent, "Billboard", ads[2], new Vector3(14.5f, -2.5f, 0f), 1.0f, "Foreground", 2460, Color.white);
        AddSpritePath(parent, "StreetSign", ads[3], new Vector3(17.4f, -7.3f, 0f), 0.75f, "Foreground", 2460, Color.white);
        AddRect(parent, "CyanGlow", new Vector3(10.5f, 2.85f, 0f), new Vector2(18f, 0.12f), new Color(0.2f, 1f, 1f, 0.45f), "Foreground", 2590);
        AddRect(parent, "MagentaFog", new Vector3(19.2f, -6.5f, 0f), new Vector2(2.2f, 5.8f), new Color(1f, 0.12f, 0.45f, 0.22f), "Foreground", 2100);
    }

    private static void AddSceneSpecificFocus(Transform parent, string sceneName)
    {
        if (sceneName.Contains("TITLE"))
        {
            AddPixelIconBuilding(parent, new Vector3(2.7f, 1.65f, 0f));
            return;
        }
        if (sceneName.Contains("PROLOGUE"))
        {
            AddRect(parent, "AccessPortal", new Vector3(10.6f, -3.2f, 0f), new Vector2(1.3f, 3.8f), new Color(0.2f, 1f, 1f, 0.35f), "Foreground", 2500);
            AddLabel("[접속 중...]  [회수 파일: HOME]", World(parent, new Vector3(4.0f, -1.6f, 0f)), 0.18f, TextAnchor.MiddleLeft, parent);
            AddCharacter(parent, new Vector3(3.3f, -7.7f, 0f), "Player");
            AddCharacter(parent, new Vector3(13.0f, -7.7f, 0f), "Enemy");
            return;
        }
        if (sceneName.Contains("ROOM"))
        {
            AddRect(parent, "RoomWall", new Vector3(10.5f, -5.0f, 0f), new Vector2(14f, 6.5f), new Color(0.06f, 0.09f, 0.13f, 0.95f), "Foreground", 2100);
            AddRect(parent, "Terminal", new Vector3(12.8f, -5.9f, 0f), new Vector2(1.6f, 1.0f), new Color(0.1f, 0.9f, 1f, 0.75f), "Foreground", 2500);
            AddCharacter(parent, new Vector3(9.5f, -7.7f, 0f), "Player");
            return;
        }
        if (sceneName.Contains("PLAZA"))
        {
            AddRect(parent, "AccessDeniedGate", new Vector3(16.7f, -5.5f, 0f), new Vector2(2.4f, 4.0f), new Color(0.1f, 0.9f, 1f, 0.35f), "Foreground", 2480);
            AddLabel("[접근 거부]", World(parent, new Vector3(15.8f, -2.8f, 0f)), 0.2f, TextAnchor.MiddleLeft, parent);
            AddCharacter(parent, new Vector3(6.6f, -7.7f, 0f), "Player");
            return;
        }
        if (sceneName.Contains("ALLEY"))
        {
            AddRect(parent, "ChipDevice", new Vector3(14.5f, -7.0f, 0f), new Vector2(1.4f, 1.2f), new Color(1f, 0.1f, 0.55f, 0.8f), "Foreground", 2510);
            AddCharacter(parent, new Vector3(6.0f, -7.7f, 0f), "Player");
            AddCharacter(parent, new Vector3(13.0f, -7.7f, 0f), "Broker");
            return;
        }
        if (sceneName.Contains("MEMORY"))
        {
            AddRect(parent, "WarmMemoryHouse", new Vector3(10.5f, -4.8f, 0f), new Vector2(5.4f, 3.2f), new Color(1f, 0.72f, 0.25f, 0.24f), "Foreground", 2470);
            AddRect(parent, "HomeCore", new Vector3(10.5f, -4.2f, 0f), new Vector2(1.2f, 1.2f), new Color(0.65f, 1f, 0.94f, 0.95f), "Foreground", 2550);
            AddLabel("HOME", World(parent, new Vector3(9.6f, -2.3f, 0f)), 0.24f, TextAnchor.MiddleLeft, parent);
            AddCharacter(parent, new Vector3(5.5f, -7.7f, 0f), "Player");
            return;
        }
        if (sceneName.Contains("BOSS"))
        {
            AddRect(parent, "BrokenDoor", new Vector3(10.5f, -4.8f, 0f), new Vector2(2.1f, 4.2f), new Color(0.1f, 0.95f, 1f, 0.35f), "Foreground", 2500);
            AddCharacter(parent, new Vector3(5.8f, -7.7f, 0f), "Player");
            AddCharacter(parent, new Vector3(12.3f, -7.7f, 0f), "Boss");
            AddRect(parent, "BossWarning", new Vector3(12.3f, -4.5f, 0f), new Vector2(3.2f, 0.15f), new Color(1f, 0.1f, 0.55f, 0.8f), "Foreground", 2600);
            return;
        }
        if (sceneName.Contains("ENDING"))
        {
            AddRect(parent, "OpenDoorLight", new Vector3(14.8f, -5.7f, 0f), new Vector2(1.2f, 4.4f), new Color(1f, 0.78f, 0.35f, 0.5f), "Foreground", 2500);
            AddLabel("[미전송 데이터: 1KB]", World(parent, new Vector3(6.7f, -2.2f, 0f)), 0.22f, TextAnchor.MiddleLeft, parent);
            AddCharacter(parent, new Vector3(9.0f, -7.7f, 0f), "Player");
        }
    }

    private static void AddCharacter(Transform parent, Vector3 localPosition, string role)
    {
        string path;
        float scale = 1.0f;
        Color tint = Color.white;
        if (role == "Enemy")
        {
            path = "Assets/ThirdParty/Cyberpunk Platformer Asset Pixel Art/Characters & Sprites/craftpix-net-545114-free-pixel-enemies-character-pack-for-seaport-location/6/Idle.png";
            scale = 1.7f;
            tint = new Color(0.9f, 1f, 0.92f, 1f);
        }
        else if (role == "Boss")
        {
            path = "Assets/ThirdParty/Cyberpunk Platformer Asset Pixel Art/Characters & Sprites/craftpix-net-999713-cyberpunk-pixel-art-bosses-pack/3/Idle.png";
            scale = 2.35f;
            tint = new Color(1f, 0.8f, 0.95f, 1f);
        }
        else if (role == "Broker")
        {
            path = "Assets/ThirdParty/Cyberpunk Platformer Asset Pixel Art/Characters & Sprites/craftpix-net-481510-free-townspeople-cyberpunk/1/Idle.png";
            scale = 1.7f;
            tint = new Color(0.75f, 1f, 0.95f, 1f);
        }
        else
        {
            path = "Assets/ThirdParty/Cyberpunk Platformer Asset Pixel Art/Characters & Sprites/craftpix-net-598640-free-characters-with-melee-attack-pixel-art/2 Weapons/1 Idle/1.png";
            scale = 1.7f;
        }

        AddSpritePath(parent, role, path, localPosition, scale, "Foreground", 2700, tint);
    }

    private static void AddPixelIconBuilding(Transform parent, Vector3 localPosition)
    {
        AddRect(parent, "IconBuildingMain", localPosition + new Vector3(0f, -0.05f, 0f), new Vector2(0.8f, 1.25f), new Color(0.25f, 1f, 1f, 1f), "Foreground", 2650);
        for (var x = 0; x < 3; x++)
        {
            for (var y = 0; y < 5; y++)
            {
                AddRect(parent, $"IconWindow_{x}_{y}", localPosition + new Vector3(-0.24f + x * 0.24f, 0.38f - y * 0.22f, 0f), new Vector2(0.08f, 0.08f), Color.black, "Foreground", 2660);
            }
        }
    }

    private static GameObject AddSpritePath(Transform parent, string name, string path, Vector3 localPosition, float scale, string sortingLayer, int order, Color tint)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        }
        if (sprite == null) return null;
        var obj = AddSpriteHeight(parent, name, sprite, localPosition, scale, sortingLayer, order);
        obj.GetComponent<SpriteRenderer>().color = tint;
        return obj;
    }

    private static GameObject AddSprite(Transform parent, string name, Sprite sprite, Vector3 localPosition, Vector3 scale, string sortingLayer, int order)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPosition;
        obj.transform.localScale = scale;
        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = order;
        return obj;
    }

    private static void AddRect(Transform parent, string name, Vector3 localPosition, Vector2 size, Color color, string sortingLayer, int order)
    {
        var sprite = GetUtilitySprite("white");
        var obj = AddSpriteSized(parent, name, sprite, localPosition, size, sortingLayer, order);
        obj.GetComponent<SpriteRenderer>().color = color;
    }

    private static GameObject AddSpriteSized(Transform parent, string name, Sprite sprite, Vector3 localPosition, Vector2 worldSize, string sortingLayer, int order)
    {
        var bounds = sprite.bounds.size;
        var scale = new Vector3(
            bounds.x > 0.001f ? worldSize.x / bounds.x : 1f,
            bounds.y > 0.001f ? worldSize.y / bounds.y : 1f,
            1f
        );
        return AddSprite(parent, name, sprite, localPosition, scale, sortingLayer, order);
    }

    private static GameObject AddSpriteHeight(Transform parent, string name, Sprite sprite, Vector3 localPosition, float targetHeight, string sortingLayer, int order)
    {
        var h = sprite.bounds.size.y;
        var uniform = h > 0.001f ? targetHeight / h : 1f;
        return AddSprite(parent, name, sprite, localPosition, Vector3.one * uniform, sortingLayer, order);
    }

    private static GameObject AddSpriteCover(Transform parent, string name, Sprite sprite, Vector3 localPosition, Vector2 area, string sortingLayer, int order)
    {
        var bounds = sprite.bounds.size;
        var uniform = Mathf.Max(
            bounds.x > 0.001f ? area.x / bounds.x : 1f,
            bounds.y > 0.001f ? area.y / bounds.y : 1f
        );
        return AddSprite(parent, name, sprite, localPosition, Vector3.one * uniform, sortingLayer, order);
    }

    private static void DrawFrame(Transform parent, Vector2 size, string sortingLayer, int order)
    {
        AddRect(parent, "Frame_Top", new Vector3(size.x * 0.5f, 2.25f, 0f), new Vector2(size.x, 0.04f), new Color(0.2f, 1f, 1f, 0.7f), sortingLayer, order);
        AddRect(parent, "Frame_Bottom", new Vector3(size.x * 0.5f, -9.25f, 0f), new Vector2(size.x, 0.04f), new Color(0.2f, 1f, 1f, 0.7f), sortingLayer, order);
        AddRect(parent, "Frame_Left", new Vector3(0f, -3.5f, 0f), new Vector2(0.04f, size.y), new Color(0.2f, 1f, 1f, 0.7f), sortingLayer, order);
        AddRect(parent, "Frame_Right", new Vector3(size.x, -3.5f, 0f), new Vector2(0.04f, size.y), new Color(0.2f, 1f, 1f, 0.7f), sortingLayer, order);
    }

    private static List<Sprite> LoadSprites(string folder, int take)
    {
        return AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(NaturalTileKey)
            .Take(take)
            .SelectMany(p => AssetDatabase.LoadAllAssetsAtPath(p).OfType<Sprite>())
            .Where(s => s != null)
            .ToList();
    }

    private static Vector3 World(Transform parent, Vector3 local)
    {
        return parent.TransformPoint(local);
    }

    private static Sprite GetUtilitySprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{BlockoutRoot}/Utility/{name}.png");
    }

    private static void EnsureUtilitySprites()
    {
        CreateFolder(Root + "/Art", "VisualBlockout");
        CreateFolder(BlockoutRoot, "Utility");
        var path = $"{BlockoutRoot}/Utility/white.png";
        if (!File.Exists(path))
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
        ConfigureSpriteImporter(path);
    }

    private static string SectionName(string path)
    {
        var relative = path.Replace(CyberpunkRoot + "/", string.Empty).Replace("\\", "/");
        var parts = relative.Split('/');
        if (parts.Length == 0) return "Other";
        if (parts[0] == "Tilesets" && parts.Length > 1) return "Tilesets / " + TrimCraftpix(parts[1]);
        if (parts[0] == "Characters & Sprites" && parts.Length > 1) return "Characters / " + TrimCraftpix(parts[1]);
        if (parts[0] == "2D Game Objects" && parts.Length > 1) return "Objects / " + TrimCraftpix(parts[1]);
        if (parts[0] == "Backgrounds" && parts.Length > 1) return "Backgrounds / " + TrimCraftpix(parts[1]);
        return parts[0];
    }

    private static int SectionOrder(string section)
    {
        if (section.StartsWith("Tilesets")) return 0;
        if (section.StartsWith("Objects")) return 1;
        if (section.StartsWith("Characters")) return 2;
        if (section.StartsWith("Backgrounds")) return 3;
        return 4;
    }

    private static string TrimCraftpix(string text)
    {
        return text
            .Replace("craftpix-net-", string.Empty)
            .Replace("-pixel-art", string.Empty)
            .Replace("-for-platformer-game", string.Empty);
    }

    private static string SanitizeName(string text)
    {
        var invalid = Path.GetInvalidFileNameChars().Concat(new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' }).ToHashSet();
        var chars = text.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        return new string(chars);
    }

    private static void DrawCameraFrame()
    {
        var obj = new GameObject("00_GAME_VIEW_FRAME_16_9");
        var line = obj.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = true;
        line.widthMultiplier = 0.035f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = new Color(0f, 0.9f, 1f, 0.85f);
        line.endColor = line.startColor;
        line.sortingLayerName = "Effect";
        line.sortingOrder = 1000;
        line.positionCount = 4;
        line.SetPositions(new[]
        {
            new Vector3(-9.24f, -5.2f, 0f),
            new Vector3(9.24f, -5.2f, 0f),
            new Vector3(9.24f, 5.2f, 0f),
            new Vector3(-9.24f, 5.2f, 0f)
        });
    }

    private static TextMesh AddLabel(string text, Vector3 position, float size, TextAnchor anchor, Transform parent = null)
    {
        var obj = new GameObject("Label_" + text.Substring(0, Mathf.Min(text.Length, 24)));
        if (parent != null) obj.transform.SetParent(parent);
        obj.transform.position = position;
        var mesh = obj.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.characterSize = size;
        mesh.anchor = anchor;
        mesh.alignment = TextAlignment.Left;
        mesh.color = new Color(0.65f, 1f, 0.95f, 1f);
        var renderer = obj.GetComponent<MeshRenderer>();
        renderer.sortingLayerName = "Effect";
        renderer.sortingOrder = 1200;
        return mesh;
    }

    private static int NaturalTileKey(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var parts = name.Split('_');
        if (parts.Length > 1 && int.TryParse(parts.Last(), out var n)) return n;
        return int.MaxValue;
    }

    private static void CreateFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + child))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static void EnsureSortingLayers()
    {
        foreach (var name in new[] { "Background", "Ground", "Player", "Enemy", "Effect", "Foreground" })
        {
            TryAddSortingLayer(name);
        }
    }

    private static void TryAddSortingLayer(string name)
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("m_SortingLayers");
        for (var i = 0; i < layers.arraySize; i++)
        {
            if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == name) return;
        }
        layers.InsertArrayElementAtIndex(layers.arraySize);
        var layer = layers.GetArrayElementAtIndex(layers.arraySize - 1);
        layer.FindPropertyRelative("name").stringValue = name;
        layer.FindPropertyRelative("uniqueID").intValue = UnityEngine.Random.Range(100000, int.MaxValue);
        tagManager.ApplyModifiedProperties();
    }
}
