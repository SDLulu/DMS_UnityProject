using UnityEngine;

// 역할:
// - 보스 패턴 한 종류가 한 사이클을 어떻게 진행하는지를 정의합니다.
// - 실제 발사체/장판/대시베기는 각 구현체에서 처리합니다.
// - BossPatternRunner가 패턴을 순환 호출합니다.

public interface IBossPattern
{
    string PatternId { get; }
    bool IsActive { get; }

    void BeginPattern(BossPatternContext context);
    void TickPattern(float deltaTime);
    void EndPattern();
}

// 패턴이 보스 본체/플레이어 정보를 알아야 할 때 통과시킬 컨테이너.
public struct BossPatternContext
{
    public Transform boss;
    public Transform player;
    public BossInteraction interaction;
    public BossTeleporter teleporter;
}
