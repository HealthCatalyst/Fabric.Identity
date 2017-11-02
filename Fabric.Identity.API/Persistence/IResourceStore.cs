using System.Threading.Tasks;

namespace Fabric.Identity.API.Persistence
{
    public interface IResourceStore<T>
    {
        void AddResource(T resource);

        void UpdateResource(string id, T resource);

        T GetResource(string id);

        void DeleteResource(string id);

        Task AddResourceAsync(T resource);
        Task UpdateResourceAsync(string id, T resource);
        Task<T> GetResourceAsync(string id);
        Task DeleteResourceAsync(string id);

    }
}