using System;
using System.Collections.Generic;
using UnityEngine;

// 역할:
// - Yongwoo 씬의 배경 루트 오브젝트에 붙어 카메라 움직임에 따라 레이어를 서로 다른 비율로 이동시킵니다.
// - 전투 규칙과 무관한 순수 시각 보조 컴포넌트입니다.
//
// 구조 포인트:
// - 시스템 매니저가 아니라 Yongwoo 씬 표현용 배경 오브젝트에 직접 붙는 뷰 컴포넌트입니다.

[DisallowMultipleComponent]
public class SimpleParallaxBackground : MonoBehaviour
{
    [SerializeField] private Transform targetCamera;
    [SerializeField] private float farLayerHorizontal = 0.05f;
    [SerializeField] private float nearLayerHorizontal = 0.28f;
    [SerializeField] private float farLayerVertical = 1f;
    [SerializeField] private float nearLayerVertical = 1f;
    [SerializeField] private bool autoRefreshChildren = true;

    private readonly List<LayerState> _layers = new();
    private Vector3 _cameraStartPosition;
    private int _cachedChildCount = -1;
    private bool _isInitialized;

    private void OnEnable()
    {
        RebuildLayers();
    }

    private void LateUpdate()
    {
        if (autoRefreshChildren && transform.childCount != _cachedChildCount)
        {
            RebuildLayers();
        }

        if (!TryInitialize())
        {
            return;
        }

        Vector3 cameraOffset = targetCamera.position - _cameraStartPosition;

        for (int i = 0; i < _layers.Count; i++)
        {
            LayerState layer = _layers[i];
            if (layer.Transform == null)
            {
                continue;
            }

            Vector3 parallaxOffset = new Vector3(
                cameraOffset.x * layer.HorizontalMultiplier,
                cameraOffset.y * layer.VerticalMultiplier,
                0f);

            layer.Transform.localPosition = layer.StartLocalPosition + parallaxOffset;
        }
    }

    [ContextMenu("Refresh Parallax Layers")]
    private void RebuildLayers()
    {
        _layers.Clear();
        _cachedChildCount = transform.childCount;

        List<Transform> directChildren = new(transform.childCount);
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child != null)
            {
                directChildren.Add(child);
            }
        }

        directChildren.Sort(CompareLayers);

        int lastIndex = Mathf.Max(1, directChildren.Count - 1);
        for (int i = 0; i < directChildren.Count; i++)
        {
            Transform child = directChildren[i];
            float t = directChildren.Count == 1 ? 1f : i / (float)lastIndex;
            _layers.Add(new LayerState(
                child,
                child.localPosition,
                Mathf.Lerp(farLayerHorizontal, nearLayerHorizontal, t),
                Mathf.Lerp(farLayerVertical, nearLayerVertical, t)));
        }

        _isInitialized = false;
    }

    private bool TryInitialize()
    {
        if (targetCamera == null && Camera.main != null)
        {
            targetCamera = Camera.main.transform;
        }

        if (targetCamera == null || _layers.Count == 0)
        {
            return false;
        }

        if (_isInitialized)
        {
            return true;
        }

        _cameraStartPosition = targetCamera.position;
        for (int i = 0; i < _layers.Count; i++)
        {
            LayerState layer = _layers[i];
            if (layer.Transform == null)
            {
                continue;
            }

            layer.StartLocalPosition = layer.Transform.localPosition;
            _layers[i] = layer;
        }

        _isInitialized = true;
        return true;
    }

    private static int CompareLayers(Transform a, Transform b)
    {
        int aOrder = GetLayerOrder(a);
        int bOrder = GetLayerOrder(b);

        if (aOrder != bOrder)
        {
            return aOrder.CompareTo(bOrder);
        }

        return a.GetSiblingIndex().CompareTo(b.GetSiblingIndex());
    }

    private static int GetLayerOrder(Transform layer)
    {
        if (layer == null)
        {
            return int.MaxValue;
        }

        string name = layer.name;
        int digitStart = name.Length;

        while (digitStart > 0 && char.IsDigit(name[digitStart - 1]))
        {
            digitStart--;
        }

        if (digitStart < name.Length && int.TryParse(name.Substring(digitStart), out int parsedValue))
        {
            return parsedValue;
        }

        return int.MaxValue;
    }

    [Serializable]
    private struct LayerState
    {
        public Transform Transform;
        public Vector3 StartLocalPosition;
        public float HorizontalMultiplier;
        public float VerticalMultiplier;

        public LayerState(Transform transform, Vector3 startLocalPosition, float horizontalMultiplier, float verticalMultiplier)
        {
            Transform = transform;
            StartLocalPosition = startLocalPosition;
            HorizontalMultiplier = horizontalMultiplier;
            VerticalMultiplier = verticalMultiplier;
        }
    }
}
