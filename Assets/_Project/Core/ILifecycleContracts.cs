namespace Core
{
    public interface IResettable
    {
        void ResetState();
    }

    public interface ISpawnable
    {
        void OnSpawned();
    }

    public interface IDespawnable
    {
        void OnDespawned();
    }
}
