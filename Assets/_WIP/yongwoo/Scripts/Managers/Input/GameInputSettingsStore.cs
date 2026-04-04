using System;
using System.IO;
using UnityEngine;

// 역할:
// - 입력 오버라이드와 감도 값을 JSON으로 저장하고 다시 불러옵니다.
// - 런타임 입력 계층이 파일 저장 포맷을 직접 알지 않도록 분리한 저장소입니다.
//
// 구조 포인트:
// - 입력 시스템의 영속성 책임만 따로 떼어낸 파일입니다.

public static class GameInputSettingsStore
{
    [Serializable]
    private class GameInputSettingsData
    {
        public string bindingOverridesJson = string.Empty;
        public float lookSensitivity = 1f;
    }

    private static readonly string SavePath = Path.Combine(Application.persistentDataPath, "game-input-settings.json");

    public static float LookSensitivity { get; private set; } = 1f;

    public static void Load(GameInput input)
    {
        LookSensitivity = 1f;
        if (!File.Exists(SavePath))
        {
            return;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            GameInputSettingsData data = JsonUtility.FromJson<GameInputSettingsData>(json);
            if (data == null)
            {
                return;
            }

            LookSensitivity = Mathf.Max(0.1f, data.lookSensitivity);
            if (!string.IsNullOrWhiteSpace(data.bindingOverridesJson))
            {
                input.LoadBindingOverridesFromJson(data.bindingOverridesJson);
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"GameInput settings could not be loaded: {exception.Message}");
        }
    }

    public static void Save(GameInput input)
    {
        try
        {
            GameInputSettingsData data = new GameInputSettingsData
            {
                bindingOverridesJson = input.SaveBindingOverridesAsJson(),
                lookSensitivity = LookSensitivity
            };

            string directory = Path.GetDirectoryName(SavePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(SavePath, JsonUtility.ToJson(data, prettyPrint: true));
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"GameInput settings could not be saved: {exception.Message}");
        }
    }

    public static void SetLookSensitivity(float value)
    {
        LookSensitivity = Mathf.Max(0.1f, value);
    }

    public static void ResetToDefaults(GameInput input)
    {
        LookSensitivity = 1f;
        input.RemoveAllBindingOverrides();
        Save(input);
    }
}
