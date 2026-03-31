using UnityEngine;

public class P_Effect_Raycast_Laser_2D : MonoBehaviour
{
    public GameObject Raybody;         // 레이저 시작 위치용 오브젝트
    public GameObject ScaleDistance;   // 레이저 길이 표현용 오브젝트
    public GameObject RayResult;       // 충돌 위치에 표시할 오브젝트

    public float maxDistance = 200f;
    public LayerMask hitLayer;         // 레이저가 맞을 레이어

    void Update()
    {
        // 2D에서는 forward 대신 방향 벡터를 직접 정해야 함
        // 오른쪽으로 쏘고 싶으면 transform.right
        // 위쪽으로 쏘고 싶으면 transform.up
        Vector2 origin = Raybody != null ? (Vector2)Raybody.transform.position : (Vector2)transform.position;
        Vector2 direction = transform.right;

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, maxDistance, hitLayer);

        if (hit.collider != null)
        {
            // 레이저 길이를 충돌 거리만큼 조절
            ScaleDistance.transform.localScale = new Vector3(1f, hit.distance, 1f);

            // 충돌 위치로 결과 오브젝트 이동
            RayResult.transform.position = hit.point;

            // 2D에서는 LookRotation 대신 각도를 직접 계산
            // hit.normal 방향을 기준으로 회전값 설정
            float angle = Mathf.Atan2(hit.normal.y, hit.normal.x) * Mathf.Rad2Deg;
            RayResult.transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }
        else
        {
            // 아무것도 맞지 않았으면 최대 거리까지 레이저 표시
            ScaleDistance.transform.localScale = new Vector3(1f, maxDistance, 1f);

            // 최대 거리 끝 지점으로 결과 오브젝트 이동
            Vector2 endPoint = origin + direction * maxDistance;
            RayResult.transform.position = endPoint;

            // 방향만 유지
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            RayResult.transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }

        // 디버그용으로 씬 뷰에 레이 표시
        Debug.DrawRay(origin, direction * maxDistance, Color.red);
    }
}