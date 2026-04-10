namespace KPA.Character
{
    public interface ICharacterModule
    {
        void Initialize(CharacterBase owner);
        void OnUpdate(); // 필요한 경우 매 프레임 업데이트
        void OnFixedUpdate();
    }
}
