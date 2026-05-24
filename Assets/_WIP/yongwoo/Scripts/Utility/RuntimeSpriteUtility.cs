using UnityEngine;

// 역할:
// - 런타임 디버그/프로토타이핑에 필요한 기본 스프라이트와 머티리얼을 지연 생성합니다.
// - 플레이어/보스 공격 시각화에서 재사용할 공통 시각 리소스를 제공합니다.
//
// 구조 포인트:
// - 임시 비주얼 자산을 코드에서 빠르게 구성할 때 보는 공용 유틸리티입니다.

public static class RuntimeSpriteUtility
{
    private static Sprite _whiteSprite;
    private static Sprite _circleSprite;
    private static Sprite _ringSprite;
    private static Material _unlitSpriteMaterial;
    private static Shader _unlitColorShader;

    public static Sprite WhiteSprite
    {
        get
        {
            if (_whiteSprite == null)
            {
                _whiteSprite = Sprite.Create(
                    Texture2D.whiteTexture,
                    new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }

            return _whiteSprite;
        }
    }

    public static Sprite CircleSprite
    {
        get
        {
            if (_circleSprite == null)
            {
                const int size = 128;
                Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Bilinear;

                Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
                float radius = size * 0.5f - 2f;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), center);
                        float alpha = distance <= radius ? 1f : 0f;
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }

                texture.Apply();
                _circleSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, size, size),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }

            return _circleSprite;
        }
    }

    public static Sprite RingSprite
    {
        get
        {
            if (_ringSprite == null)
            {
                const int size = 128;
                Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Bilinear;

                Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
                float outerRadius = size * 0.5f - 3f;
                float innerRadius = outerRadius - 8f;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), center);
                        float alpha = distance <= outerRadius && distance >= innerRadius ? 1f : 0f;
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }

                texture.Apply();
                _ringSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, size, size),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }

            return _ringSprite;
        }
    }

    public static Material UnlitSpriteMaterial
    {
        get
        {
            if (_unlitSpriteMaterial != null)
            {
                return _unlitSpriteMaterial;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                ?? Shader.Find("Sprites/Default");
            if (shader == null)
            {
                return null;
            }

            _unlitSpriteMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            return _unlitSpriteMaterial;
        }
    }

    public static Vector3 WorldSizeToLocalScale(Sprite sprite, Vector2 worldSize)
    {
        if (sprite == null)
        {
            return new Vector3(worldSize.x, worldSize.y, 1f);
        }

        Vector2 spriteSize = sprite.bounds.size;
        return new Vector3(
            worldSize.x / Mathf.Max(0.0001f, spriteSize.x),
            worldSize.y / Mathf.Max(0.0001f, spriteSize.y),
            1f);
    }

    public static Vector3 UniformWorldScale(Sprite sprite, float worldDiameter)
    {
        if (sprite == null)
        {
            return Vector3.one * worldDiameter;
        }

        float maxAxis = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
        float scale = worldDiameter / Mathf.Max(0.0001f, maxAxis);
        return new Vector3(scale, scale, 1f);
    }

    public static Material CreateUnlitColorMaterial(Color color)
    {
        _unlitColorShader ??= Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color")
            ?? Shader.Find("Sprites/Default");
        if (_unlitColorShader == null)
        {
            return null;
        }

        Material material = new Material(_unlitColorShader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        return material;
    }
}
