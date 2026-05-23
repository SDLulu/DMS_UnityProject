using UnityEngine;
using UnityEngine.UI;

// 역할:
// - 기존 Hp UI(Image 5칸)를 PlayerSlowMotion 자원 게이지로 재활용합니다.
// - 자원이 가득 차면 5칸 모두 켜지고, 0이면 모두 꺼집니다. 부분 칸은 알파로 보간합니다.
// - HP 표시는 일격사 룰로 의미가 없어 사용하지 않습니다.

[DisallowMultipleComponent]
public class SlowGaugeUI : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("동기화할 PlayerSlowMotion 컴포넌트입니다. 비워두면 씬에서 검색합니다.")]
    [SerializeField] private PlayerSlowMotion source;

    [Header("Targets")]
    [Tooltip("칸 한 개당 이미지. 인덱스 0이 첫 칸입니다.")]
    [SerializeField] private Image[] chargeImages;

    [Header("Visual")]
    [Tooltip("자원이 충분히 찬 칸의 알파 값입니다.")]
    [SerializeField, Range(0f, 1f)] private float filledAlpha = 1f;
    [Tooltip("빈 칸의 알파 값입니다. 0이면 완전히 보이지 않습니다.")]
    [SerializeField, Range(0f, 1f)] private float emptyAlpha = 0.15f;
    [Tooltip("부분 칸도 알파로 부드럽게 표현할지 여부. 끄면 칸 단위 on/off만 합니다.")]
    [SerializeField] private bool useFractionalFill = true;

    private void Reset()
    {
        source = Object.FindFirstObjectByType<PlayerSlowMotion>();
    }

    private void OnEnable()
    {
        if (source == null)
        {
            source = Object.FindFirstObjectByType<PlayerSlowMotion>();
        }
    }

    private void LateUpdate()
    {
        if (source == null || chargeImages == null || chargeImages.Length == 0)
        {
            return;
        }

        float charges = source.CurrentChargesRaw;
        for (int i = 0; i < chargeImages.Length; i++)
        {
            Image image = chargeImages[i];
            if (image == null)
            {
                continue;
            }

            float slotFill;
            if (useFractionalFill)
            {
                slotFill = Mathf.Clamp01(charges - i);
            }
            else
            {
                slotFill = (i < Mathf.FloorToInt(charges)) ? 1f : 0f;
            }

            float alpha = Mathf.Lerp(emptyAlpha, filledAlpha, slotFill);
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }
}
