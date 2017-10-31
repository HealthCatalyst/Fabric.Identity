using IdentityServer4.Stores;

namespace Fabric.Identity.API.Stores
{
    public interface IResourceStore<T>
    {
        void AddResource(T resource);

        void UpdateResource(string id, T resource);

        T GetResource(string id);

        void DeleteResource(string id);
    }
}