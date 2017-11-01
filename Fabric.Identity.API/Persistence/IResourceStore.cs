namespace Fabric.Identity.API.Persistence
{
    public interface IResourceStore<T>
    {
        void AddResource(T resource);

        void UpdateResource(string id, T resource);

        T GetResource(string id);

        void DeleteResource(string id);
    }
}