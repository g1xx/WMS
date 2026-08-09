using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Warehouse.Infrastructure.Configurations;

// Npgsql.EntityFrameworkCore.PostgreSQL 10.0.2 does not ship UseXminAsConcurrencyToken();
// this reproduces it directly via Postgres's system "xmin" column.
public static class XminConcurrencyExtensions
{
    public static EntityTypeBuilder<TEntity> UseXminAsConcurrencyToken<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        builder.Property<uint>("xmin")
               .HasColumnType("xid")
               .ValueGeneratedOnAddOrUpdate()
               .IsRowVersion();

        return builder;
    }
}
