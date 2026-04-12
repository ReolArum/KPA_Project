using UnityEngine;

/// <summary>
/// 쿼터뷰 탐사 씬에서 캐릭터를 부드럽게 추약하는 카메라 컨트롤러입니다.
/// </summary>
public class ExplorationCameraController : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private bool isFollowing = false;
    
    // [ADD] 우리가 원하는 표준 쿼터뷰 거리 (예: 위로 10, 뒤로 10)
    [SerializeField] private Vector3 preferredOffset = new Vector3(0, 14f, -10f);

    private Vector3 _currentVelocity = Vector3.zero;

    private void LateUpdate()
    {
        if (!isFollowing || target == null) return;

        // 목표 위치 계산 (캐릭터의 현재 위치 + 우리가 설정한 고정 거리)
        Vector3 targetPosition = target.position + preferredOffset;

        // 부드럽게 이동
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _currentVelocity, smoothTime);
    }

    /// <summary>
    /// 추적할 대상을 설정합니다.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    /// <summary>
    /// 추적 활성화 여부를 설정합니다.
    /// </summary>
    public void SetFollowing(bool follow)
    {
        isFollowing = follow;
        if (follow && target != null)
        {
            // 추적 시작 시 부드러운 전환을 위해 초기 오프셋 재계산 유도 가능
            // _hasOffset = false; 
        }
    }

    /// <summary>
    /// 카메라를 타겟 뒤로 즉시 순간이동 시킵니다.
    /// </summary>
    public void WarpToTarget()
    {
        if (target == null) return;

        transform.position = target.position + preferredOffset;
        _currentVelocity = Vector3.zero;
    }
}
