namespace Application
{
    public interface ISaveStorage<T>
    {
        bool Save(T data);
        T Load();
    }
}
