using IdentityServer4.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Identity.API.Persistence.SqlServer
{
    public static class ModelBuilderExtensions
    {
        private static EntityTypeBuilder<TEntity> ToTable<TEntity>(this EntityTypeBuilder<TEntity> entityTypeBuilder, TableConfiguration configuration)
            where TEntity : class
        {
            return string.IsNullOrWhiteSpace(configuration.Schema) ? entityTypeBuilder.ToTable(configuration.Name) : entityTypeBuilder.ToTable(configuration.Name, configuration.Schema);
        }

       

    }
}
