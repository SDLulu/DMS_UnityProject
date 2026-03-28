using UnityEngine;

public static class PrototypeRuntimeSpriteUtility
{
    private static Sprite _whiteSprite;
    private static Sprite _circleSprite;

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
}
