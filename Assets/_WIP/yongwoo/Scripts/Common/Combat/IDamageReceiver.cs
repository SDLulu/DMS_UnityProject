using UnityEngine;

// 역할:
// - 플레이어/보스/향후 적이 공통으로 맞을 수 있는 최소 계약입니다.
// - 공격 판정 코드는 구체 클래스 대신 이 인터페이스만 보고 피해를 전달합니다.

public interface IDamageReceiver
{
    bool ReceiveHit(float damage, Vector2 knockback, GameObject source);
}
