using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class RobotMaidAnimationBuilder
{
    private const string SourceRoot = "Assets/ThirdParty/Robot Maid/SpriteSheet_Separates_80x80";
    private const string OutputRoot = "Assets/_WIP/yongwoo/Resources/RobotMaid/Animations";

    private readonly struct ClipDefinition
    {
        public ClipDefinition(string stateName, string sourceFile, float frameRate, bool loop, int startFrame = 0, int frameCount = -1)
        {
            StateName = stateName;
            SourceFile = sourceFile;
            FrameRate = frameRate;
            Loop = loop;
            StartFrame = startFrame;
            FrameCount = frameCount;
        }

        public string StateName { get; }
        public string SourceFile { get; }
        public float FrameRate { get; }
        public bool Loop { get; }
        public int StartFrame { get; }
        public int FrameCount { get; }
    }

    [MenuItem("Tools/Prototype/Build Robot Maid Animation Assets")]
    public static void Build()
    {
        EnsureFolders();

        Dictionary<string, AnimationClip> playerClips = BuildClips(
            "Player",
            new[]
            {
                new ClipDefinition("Idle", "1_Idle.png", 10f, true),
                new ClipDefinition("CrouchEnter", "2_Down.png", 10f, false, 0, 3),
                new ClipDefinition("CrouchHold", "2_Down.png", 10f, false, 2, 1),
                new ClipDefinition("CrouchExit", "2_Down.png", 10f, false, 2, 3),
                new ClipDefinition("Run", "3_Run.png", 14f, true),
                new ClipDefinition("Jump", "7_Jump.png", 14f, true),
                new ClipDefinition("Fall", "8_Fall.png", 14f, true),
                new ClipDefinition("Dash", "9_DashJump.png", 18f, false),
                new ClipDefinition("Roll", "11_Rolling.png", 14f, false),
                new ClipDefinition("Attack", "12_Attack1.png", 18f, false)
            });

        Dictionary<string, AnimationClip> bossClips = BuildClips(
            "Boss",
            new[]
            {
                new ClipDefinition("Idle", "1_Idle.png", 10f, true),
                new ClipDefinition("Telegraph", "24_Push.png", 14f, false),
                new ClipDefinition("Dash", "4_Dash.png", 18f, false),
                new ClipDefinition("Leap", "7_Jump.png", 14f, false),
                new ClipDefinition("Shoot", "14_Throw.png", 14f, false),
                new ClipDefinition("Hit", "19_Hit.png", 16f, false)
            });

        BuildController("Player", "RobotMaidPlayer", "Idle", playerClips);
        BuildController("Boss", "RobotMaidBoss", "Idle", bossClips);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Robot Maid animation assets rebuilt.");
    }

    private static Dictionary<string, AnimationClip> BuildClips(string category, IEnumerable<ClipDefinition> definitions)
    {
        Dictionary<string, AnimationClip> result = new Dictionary<string, AnimationClip>();

        foreach (ClipDefinition definition in definitions)
        {
            string sourcePath = Path.Combine(SourceRoot, definition.SourceFile).Replace("\\", "/");
            Sprite[] sprites = LoadSprites(sourcePath);
            if (sprites.Length == 0)
            {
                Debug.LogWarning($"Robot Maid animation source missing sprites: {sourcePath}");
                continue;
            }

            int startFrame = Mathf.Clamp(definition.StartFrame, 0, sprites.Length - 1);
            int frameCount = definition.FrameCount > 0
                ? Mathf.Min(definition.FrameCount, sprites.Length - startFrame)
                : sprites.Length - startFrame;
            Sprite[] selectedSprites = sprites.Skip(startFrame).Take(frameCount).ToArray();
            if (selectedSprites.Length == 0)
            {
                Debug.LogWarning($"Robot Maid animation source has no selected sprites: {sourcePath}");
                continue;
            }

            string outputPath = $"{OutputRoot}/{category}/{definition.StateName}.anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(outputPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, outputPath);
            }

            clip.frameRate = definition.FrameRate;

            ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[selectedSprites.Length];
            for (int i = 0; i < selectedSprites.Length; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i / definition.FrameRate,
                    value = selectedSprites[i]
                };
            }

            EditorCurveBinding spriteBinding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = string.Empty,
                propertyName = "m_Sprite"
            };

            AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);
            SetLoop(clip, definition.Loop);
            EditorUtility.SetDirty(clip);
            result[definition.StateName] = clip;
        }

        return result;
    }

    private static void BuildController(string category, string controllerName, string defaultStateName, Dictionary<string, AnimationClip> clips)
    {
        string controllerPath = $"{OutputRoot}/{category}/{controllerName}.controller";
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
        {
            AssetDatabase.DeleteAsset(controllerPath);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        foreach (KeyValuePair<string, AnimationClip> pair in clips)
        {
            AnimatorState state = stateMachine.AddState(pair.Key);
            state.motion = pair.Value;
            state.writeDefaultValues = true;
            if (pair.Key == defaultStateName)
            {
                stateMachine.defaultState = state;
            }
        }

        EditorUtility.SetDirty(controller);
    }

    private static Sprite[] LoadSprites(string path)
    {
        return AssetDatabase
            .LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .OrderBy(sprite => ExtractFrameIndex(sprite.name))
            .ThenBy(sprite => sprite.name, StringComparer.Ordinal)
            .ToArray();
    }

    private static int ExtractFrameIndex(string spriteName)
    {
        int separator = spriteName.LastIndexOf('_');
        if (separator >= 0 && int.TryParse(spriteName.Substring(separator + 1), out int parsed))
        {
            return parsed;
        }

        return int.MaxValue;
    }

    private static void SetLoop(AnimationClip clip, bool loop)
    {
        SerializedObject serializedClip = new SerializedObject(clip);
        SerializedProperty settings = serializedClip.FindProperty("m_AnimationClipSettings");
        if (settings != null)
        {
            SerializedProperty loopTime = settings.FindPropertyRelative("m_LoopTime");
            if (loopTime != null)
            {
                loopTime.boolValue = loop;
            }
        }

        serializedClip.ApplyModifiedProperties();
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/_WIP/yongwoo/Resources");
        EnsureFolder("Assets/_WIP/yongwoo/Resources/RobotMaid");
        EnsureFolder(OutputRoot);
        EnsureFolder($"{OutputRoot}/Player");
        EnsureFolder($"{OutputRoot}/Boss");
    }

    private static void EnsureFolder(string folderPath)
    {
        string normalized = folderPath.Replace("\\", "/");
        if (AssetDatabase.IsValidFolder(normalized))
        {
            return;
        }

        string[] parts = normalized.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }
}
