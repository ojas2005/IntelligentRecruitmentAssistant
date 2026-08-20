using System.Reflection;
using IRA.Domain.Common;

namespace IRA.Infrastructure.Persistence.Cosmos;

/// <summary>
/// Restores the identity and creation timestamp of a rehydrated domain entity. The public
/// constructors generate a fresh <see cref="Entity.Id"/>/<see cref="Entity.CreatedAtUtc"/>, so
/// after mapping a persisted document back into an aggregate we reinstate the original values
/// via the protected setters (reflection is confined to this Infrastructure helper).
/// </summary>
internal static class EntityRestore
{
    private static readonly PropertyInfo IdProperty =
        typeof(Entity).GetProperty(nameof(Entity.Id))!;

    private static readonly PropertyInfo CreatedProperty =
        typeof(Entity).GetProperty(nameof(Entity.CreatedAtUtc))!;

    public static T With<T>(this T entity, Guid id, DateTimeOffset createdAtUtc) where T : Entity
    {
        IdProperty.SetValue(entity, id);
        CreatedProperty.SetValue(entity, createdAtUtc);
        return entity;
    }
}
