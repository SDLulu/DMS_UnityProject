using UnityEngine;
using UnityEngine.UI;

// 역할:
// - 타이틀 심전도 라인. 매 주기마다 랜덤 파형 1덩어리가 왼쪽으로 쭉 지나감.
// - 조절: Scroll Speed(이동 속도), Cycle Seconds(다음 랜덤 파형까지 시간).

public enum TitleWaveStyle
{
    Random = 0,
    AngelBeatRipple = 1,
    ClinicalEcg = 2
}

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasRenderer))]
[ExecuteAlways]
public class TitleEcgLineGraphic : MaskableGraphic
{
    [Header("Wave Style")]
    [SerializeField] private TitleWaveStyle waveStyle = TitleWaveStyle.Random;

    [Header("Scroll & Cycle")]
    [Tooltip("파형 덩어리가 왼쪽으로 이동하는 속도 (픽셀/초)")]
    [SerializeField] private float scrollSpeed = 110f;
    [Tooltip("새 랜덤 파형이 나오는 간격 (초). Speed×CycleSeconds ≥ 라인 너비면 한 바퀴 다 지나감")]
    [SerializeField] private float cycleSeconds = 1.35f;
    [Tooltip("0이면 라인 가로폭 = 파형 1덩어리 길이")]
    [SerializeField] private float wavePacketWidth;
    [SerializeField] private float waveAmplitude = 0.36f;
    [SerializeField] private int sampleCount = 180;
    [SerializeField] private int smoothPasses = 3;

    [Header("Random Wave")]
    [SerializeField] private int controlPointsPerCycle = 9;
    [SerializeField] private int patternSeed = 20260523;
    [SerializeField] private float randomMin = -0.15f;
    [SerializeField] private float randomMax = 1f;

    [Header("Angel Beat Ripple")]
    [SerializeField] private float pulseCenter = 0.70f;
    [SerializeField] private float pulseWidth = 0.11f;
    [SerializeField] private float rippleDelay1 = 0.09f;
    [SerializeField] private float rippleDelay2 = 0.17f;
    [SerializeField] private float rippleStrength1 = 0.28f;
    [SerializeField] private float rippleStrength2 = 0.14f;

    [Header("Clinical ECG")]
    [SerializeField] private bool clinicalSmoothing = false;
    [SerializeField] private int clinicalSmoothPasses = 1;

    [Header("Line")]
    [SerializeField] private float lineThickness = 5f;
    [SerializeField] private float glowThickness = 14f;
    [SerializeField] private Color lineColor = new Color(0.20f, 0.96f, 0.86f, 0.92f);
    [SerializeField] private Color glowColor = new Color(0.16f, 0.92f, 0.80f, 0.18f);

    private float[] _sampleBuffer;
    private float[] _cycleControls;
    private int _activeCycleIndex = int.MinValue;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = rectTransform.rect;
        if (rect.width <= 1f || rect.height <= 1f)
        {
            return;
        }

        int count = Mathf.Max(64, sampleCount);
        EnsureBuffers(count);

        float width = rect.width;
        float height = rect.height;
        float centerY = rect.center.y;
        float amplitudePixels = height * waveAmplitude;

        float elapsed = Time.unscaledTime;
        float packetWidth = wavePacketWidth > 1f ? wavePacketWidth : width;
        int cycleIndex = Mathf.FloorToInt(elapsed / Mathf.Max(0.05f, cycleSeconds));
        float offset = (elapsed % Mathf.Max(0.05f, cycleSeconds)) * scrollSpeed;
        EnsureCycleControls(cycleIndex);

        for (int i = 0; i <= count; i++)
        {
            float t = i / (float)count;
            float x = rect.xMin + width * t;
            float localX = x - rect.xMin;
            _sampleBuffer[i] = SampleWaveAtScreen(localX, offset, packetWidth) * amplitudePixels;
        }

        ApplySmoothing(count);

        BuildWaveMesh(vh, rect, width, centerY, count, glowThickness, glowColor);
        BuildWaveMesh(vh, rect, width, centerY, count, lineThickness, lineColor);
    }

    private void EnsureBuffers(int count)
    {
        if (_sampleBuffer == null || _sampleBuffer.Length < count + 1)
        {
            _sampleBuffer = new float[count + 1];
        }

        int controlCount = Mathf.Max(3, controlPointsPerCycle);
        if (_cycleControls == null || _cycleControls.Length != controlCount)
        {
            _cycleControls = new float[controlCount];
            _activeCycleIndex = int.MinValue;
        }
    }

    private void EnsureCycleControls(int cycleIndex)
    {
        if (cycleIndex == _activeCycleIndex)
        {
            return;
        }

        _activeCycleIndex = cycleIndex;
        Random.State previous = Random.state;
        Random.InitState(patternSeed + cycleIndex * 7919);

        for (int i = 0; i < _cycleControls.Length; i++)
        {
            _cycleControls[i] = Random.Range(randomMin, randomMax);
        }

        Random.state = previous;
    }

    private float SampleWaveAtScreen(float localX, float offset, float packetWidth)
    {
        if (waveStyle == TitleWaveStyle.Random)
        {
            float u = (localX + offset) / packetWidth;
            if (u < 0f || u > 1f)
            {
                return 0f;
            }

            return SampleRandomCycle(u);
        }

        float scroll = Time.unscaledTime * scrollSpeed;
        float phase = (localX + scroll) / packetWidth;
        float localPhase = phase - Mathf.Floor(phase);
        return waveStyle switch
        {
            TitleWaveStyle.ClinicalEcg => SampleClinicalEcg(localPhase),
            _ => SampleAngelBeatWave(localPhase)
        };
    }

    private float SampleRandomCycle(float localPhase)
    {
        if (_cycleControls == null || _cycleControls.Length < 2)
        {
            return 0f;
        }

        float scaled = localPhase * (_cycleControls.Length - 1);
        int index = Mathf.FloorToInt(scaled);
        int next = Mathf.Min(index + 1, _cycleControls.Length - 1);
        float t = scaled - index;
        t = t * t * (3f - 2f * t);
        return Mathf.Lerp(_cycleControls[index], _cycleControls[next], t);
    }

    private void ApplySmoothing(int count)
    {
        int passes = waveStyle switch
        {
            TitleWaveStyle.ClinicalEcg => clinicalSmoothing ? Mathf.Max(0, clinicalSmoothPasses) : 0,
            TitleWaveStyle.Random => Mathf.Max(0, smoothPasses),
            _ => Mathf.Max(0, smoothPasses)
        };

        SmoothSamples(count, passes);
    }

    private void BuildWaveMesh(
        VertexHelper vh,
        Rect rect,
        float width,
        float centerY,
        int count,
        float thickness,
        Color color)
    {
        Vector2 prev = default;
        bool hasPrev = false;

        for (int i = 0; i <= count; i++)
        {
            float t = i / (float)count;
            float x = rect.xMin + width * t;
            Vector2 point = new Vector2(x, centerY + _sampleBuffer[i]);

            if (hasPrev)
            {
                AddLineSegment(vh, prev, point, thickness, color);
            }

            prev = point;
            hasPrev = true;
        }
    }

    private void SmoothSamples(int count, int passes)
    {
        for (int pass = 0; pass < passes; pass++)
        {
            float prev = _sampleBuffer[0];
            for (int i = 1; i < count; i++)
            {
                float next = _sampleBuffer[i + 1];
                float smoothed = (_sampleBuffer[i] * 2f + prev + next) * 0.25f;
                prev = _sampleBuffer[i];
                _sampleBuffer[i] = smoothed;
            }
        }
    }

    private float SampleAngelBeatWave(float localPhase)
    {
        float main = SampleSoftPulse(localPhase, pulseCenter, pulseWidth);
        float ripple1 = SampleSoftPulse(localPhase - rippleDelay1, pulseCenter, pulseWidth) * rippleStrength1;
        float ripple2 = SampleSoftPulse(localPhase - rippleDelay2, pulseCenter, pulseWidth) * rippleStrength2;
        return main + ripple1 + ripple2;
    }

    private static float SampleSoftPulse(float phase, float center, float width)
    {
        float dist = Mathf.Abs(phase - center) / width;
        if (dist >= 1f)
        {
            return 0f;
        }

        float falloff = Mathf.Cos(dist * Mathf.PI * 0.5f);
        return falloff * falloff;
    }

    private static float SampleClinicalEcg(float t)
    {
        if (t < 0.08f)
        {
            return 0f;
        }

        if (t < 0.12f)
        {
            return Mathf.Sin((t - 0.08f) / 0.04f * Mathf.PI) * 0.12f;
        }

        if (t < 0.18f)
        {
            return 0f;
        }

        if (t < 0.20f)
        {
            return -0.08f;
        }

        if (t < 0.24f)
        {
            return Mathf.Lerp(-0.08f, 1f, (t - 0.20f) / 0.04f);
        }

        if (t < 0.28f)
        {
            return Mathf.Lerp(1f, -0.25f, (t - 0.24f) / 0.04f);
        }

        if (t < 0.42f)
        {
            return Mathf.Lerp(-0.25f, 0f, (t - 0.28f) / 0.14f);
        }

        if (t < 0.52f)
        {
            return Mathf.Sin((t - 0.42f) / 0.10f * Mathf.PI) * 0.22f;
        }

        return 0f;
    }

    private void Update()
    {
        SetVerticesDirty();
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        SetVerticesDirty();
    }

    private static void AddLineSegment(VertexHelper vh, Vector2 a, Vector2 b, float thickness, Color color)
    {
        Vector2 dir = b - a;
        if (dir.sqrMagnitude <= 0.000001f)
        {
            return;
        }

        Vector2 normal = new Vector2(-dir.y, dir.x).normalized * (thickness * 0.5f);
        int start = vh.currentVertCount;

        vh.AddVert(a - normal, color, Vector2.zero);
        vh.AddVert(a + normal, color, Vector2.zero);
        vh.AddVert(b + normal, color, Vector2.zero);
        vh.AddVert(b - normal, color, Vector2.zero);

        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }
}
