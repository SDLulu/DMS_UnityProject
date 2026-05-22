using UnityEngine;
using UnityEngine.InputSystem;

// 역할:
// - 디버그용 핫키(1/2/3)로 플레이어를 지정한 스폰 포인트로 즉시 이동시킵니다.
// - 빌드/검증용 임시 컴포넌트라 인스펙터에서 끌 수 있고, 정식 빌드에는 비활성화합니다.

public class DebugTeleportHotkeys : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("키 1 — 마을 시작(주인공집)")]
    [SerializeField] private Transform slot1Village;
    [Tooltip("키 2 — 튜토리얼 시작(접속구역)")]
    [SerializeField] private Transform slot2Tutorial;
    [Tooltip("키 3 — 보스 시작(재접속_047)")]
    [SerializeField] private Transform slot3Boss;

    [Header("Options")]
    [SerializeField] private bool enableHotkeys = true;
    [SerializeField] private bool logTeleport = true;

    private void Update()
    {
        if (!enableHotkeys)
        {
            return;
        }

        var kb = Keyboard.current;
        if (kb == null)
        {
            return;
        }

        if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
        {
            TeleportTo(slot1Village, "마을(주인공집)");
        }
        else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)
        {
            TeleportTo(slot2Tutorial, "튜토리얼(접속구역)");
        }
        else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame)
        {
            TeleportTo(slot3Boss, "보스(재접속_047)");
        }
    }

    private void TeleportTo(Transform target, string label)
    {
        if (target == null)
        {
            Debug.LogWarning($"[DebugTeleport] {label} 슬롯 비어있음", this);
            return;
        }

        var player = FindFirstObjectByType<SimplePlayerController>();
        if (player == null)
        {
            Debug.LogWarning("[DebugTeleport] SimplePlayerController 못 찾음", this);
            return;
        }

        Vector3 dest = target.position;
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position = dest;
            rb.linearVelocity = Vector2.zero;
        }
        player.transform.position = dest;

        if (logTeleport)
        {
            Debug.Log($"[DebugTeleport] → {label} @ {dest}", this);
        }
    }
}
