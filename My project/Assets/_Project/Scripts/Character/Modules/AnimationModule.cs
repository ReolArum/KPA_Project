using UnityEngine;
using UnityEngine.AI;
using KPA.Character;

public class AnimationModule : ICharacterModule
{
    private CharacterBase _owner;
    private Animator _animator;
    private NavMeshAgent _agent;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private float _lastLogTime;

    public void Initialize(CharacterBase owner)
    {
        _owner = owner;
        _animator = owner.GetComponentInChildren<Animator>(true);
        _agent = owner.GetComponent<NavMeshAgent>();

        Debug.Log($"<color=cyan>[Animation] Module Initialized on {owner.gameObject.name}</color>");
        if (_animator != null) Debug.Log($"<color=green>[Animation] Animator initially found: {_animator.gameObject.name}</color>");
        if (_agent == null) Debug.LogWarning("[Animation] NavMeshAgent NOT found! Movement animation won't work.");
    }

    public void OnUpdate()
    {
        if (_animator == null)
        {
            var visual = _owner.GetModule<VisualModule>();
            var model = visual?.GetModel();
            if (model != null) _animator = model.GetComponentInChildren<Animator>(true);
            if (_animator == null) _animator = _owner.GetComponentInChildren<Animator>(true);

            if (_animator == null) return;
        }

        if (_agent != null)
        {
            // [FIX] 이동하면 즉시 걷도록 하되, 댐핑(0.15f)을 주어 미세한 떨림 방지
            float targetSpeed = (_agent.velocity.magnitude > 0.1f) ? 1.0f : 0f;
            
            // 댐핑을 0.15f로 조절하여 덝그덕거리는 현상 해결
            _animator.SetFloat(SpeedHash, targetSpeed, 0.15f, Time.deltaTime);
        }
    }

    /// <summary>
    /// 외부에서 애니메이션 트리거를 재생하고 싶을 때 사용 (예: 공격, 피격, 기뻐하기)
    /// </summary>
    public void PlayTrigger(string triggerName)
    {
        if (_animator != null)
        {
            _animator.SetTrigger(triggerName);
        }
    }

    public void OnFixedUpdate() { }
}
