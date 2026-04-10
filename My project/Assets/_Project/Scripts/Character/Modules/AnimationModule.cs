using UnityEngine;
using UnityEngine.AI;
using KPA.Character;

public class AnimationModule : ICharacterModule
{
    private CharacterBase _owner;
    private Animator _animator;
    private NavMeshAgent _agent;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    public void Initialize(CharacterBase owner)
    {
        _owner = owner;
        _animator = owner.GetComponentInChildren<Animator>();
        _agent = owner.GetComponent<NavMeshAgent>();
    }

    public void OnUpdate()
    {
        if (_animator == null)
        {
            _animator = _owner.GetComponentInChildren<Animator>();
            return;
        }

        if (_agent != null)
        {
            // NavMeshAgent의 속도를 Animator의 Speed 파라미터에 전달
            // float speed = _agent.velocity.magnitude;
            // _animator.SetFloat(SpeedHash, speed);
            
            // 임시: 모델링의 달리기 애니메이션 트리거를 위한 속도 전달
            float curSpeed = _agent.velocity.magnitude / _agent.speed; 
            _animator.SetFloat(SpeedHash, curSpeed);
        }
    }

    public void OnFixedUpdate() { }
}
