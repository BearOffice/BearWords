using BearWordsAPI.Shared.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Reflection;

namespace BearWordsAPI.Shared.Helpers;

public static class SoftDeleteCascadeExtensions
{
    public static void CascadeSoftDeleteAndRestore(this DbContext context,
        CascadeConfiguration? cascadeConfig = null)
    {
        var modifiedEntries = context.ChangeTracker.Entries<ISoftDeletable>()
            .Where(e => e.State == EntityState.Modified && e.Property(p => p.DeleteFlag).IsModified)
            .ToList();

        var deletedEntries = context.ChangeTracker.Entries<ISoftDeletable>()
            .Where(e => e.State == EntityState.Deleted)
            .ToList();

        foreach (var entry in modifiedEntries)
        {
            var entity = entry.Entity;
            if (entity.DeleteFlag)
            {
                LoadNaviPropertiesIfNeeded(entry, cascadeConfig, isDelete: true);
                CascadeBase(context, entity, isDelete: true, cascadeConfig);
            }
            else
            {
                LoadNaviPropertiesIfNeeded(entry, cascadeConfig, isDelete: false);
                CascadeBase(context, entity, isDelete: false, cascadeConfig);
            }
        }

        foreach (var entry in deletedEntries)
        {
            var entity = entry.Entity;
            LoadNaviPropertiesIfNeeded(entry, cascadeConfig, isDelete: true);

            entry.State = EntityState.Modified;
            entity.DeleteFlag = true;

            CascadeBase(context, entity, isDelete: true, cascadeConfig);
        }
    }

    private static void LoadNaviPropertiesIfNeeded(EntityEntry<ISoftDeletable> entry,
        CascadeConfiguration? cascadeConfig, bool isDelete)
    {
        if (isDelete)
        {
            if (cascadeConfig?.HasDeleteRules(entry.Entity.GetType()) != true)
            {
                LoadAttributeBasedNaviProperties(entry, c => c.OnDelete);
            }
        }
        else
        {
            if (cascadeConfig?.HasRestoreRules(entry.Entity.GetType()) != true)
            {
                LoadAttributeBasedNaviProperties(entry, c => c.OnRestore);
            }
        }
    }

    private static void LoadAttributeBasedNaviProperties(EntityEntry<ISoftDeletable> entry,
        Func<CascadeSoftDeleteAttribute, bool> cascadeMember)
    {
        var entityType = entry.Entity.GetType();
        var cascadeProperties = entityType.GetProperties()
            .Where(p => p.GetCustomAttribute<CascadeSoftDeleteAttribute>() is not null &&
                       cascadeMember.Invoke(p.GetCustomAttribute<CascadeSoftDeleteAttribute>()!));

        var entityMetadata = entry.Metadata;

        foreach (var property in cascadeProperties)
        {
            var navigation = entityMetadata.FindNavigation(property.Name);
            if (navigation is null) continue;

            if (navigation.IsCollection)
            {
                entry.Collection(property.Name).Load();
            }
            else
            {
                entry.Reference(property.Name).Load();
            }
        }
    }

    private static void CascadeBase(DbContext context, ISoftDeletable entity, bool isDelete,
        CascadeConfiguration? cascadeConfig)
    {
        var shouldUseAttributeBased = cascadeConfig is null ||
            (isDelete ? !cascadeConfig.HasDeleteRules(entity.GetType())
                      : !cascadeConfig.HasRestoreRules(entity.GetType()));

        if (shouldUseAttributeBased)
        {
            ProcessAttributeBasedCascade(context, entity, isDelete, cascadeConfig);
            return;
        }

        var targets = isDelete
             ? cascadeConfig!.GetDeleteTargets(entity, context)
             : cascadeConfig!.GetRestoreTargets(entity, context);

        foreach (var target in targets.Where(t => t.DeleteFlag != isDelete))
        {
            target.DeleteFlag = isDelete;
            MarkAsModified(context, target);
            CascadeBase(context, target, isDelete, cascadeConfig);
        }
    }

    private static void ProcessAttributeBasedCascade(DbContext context, ISoftDeletable entity,
        bool isDelete, CascadeConfiguration? cascadeConfig)
    {
        var entityType = entity.GetType();
        var properties = entityType.GetProperties()
            .Where(p => p.GetCustomAttribute<CascadeSoftDeleteAttribute>() is not null);

        foreach (var property in properties)
        {
            var attribute = property.GetCustomAttribute<CascadeSoftDeleteAttribute>()!;
            bool shouldCascade = isDelete ? attribute.OnDelete : attribute.OnRestore;
            if (!shouldCascade) continue;

            var value = property.GetValue(entity);
            if (value is null) continue;

            if (value is IEnumerable<ISoftDeletable> collection)
            {
                var childrenToProcess = collection.Where(c => c.DeleteFlag != isDelete);

                foreach (var child in childrenToProcess)
                {
                    child.DeleteFlag = isDelete;
                    MarkAsModified(context, child);
                    CascadeBase(context, child, isDelete, cascadeConfig);
                }
            }
            else if (value is ISoftDeletable singleChild)
            {
                if (singleChild.DeleteFlag != isDelete)
                {
                    singleChild.DeleteFlag = isDelete;
                    MarkAsModified(context, singleChild);
                    CascadeBase(context, singleChild, isDelete, cascadeConfig);
                }
            }
        }
    }

    private static void MarkAsModified(DbContext context, ISoftDeletable entity)
    {
        var entry = context.Entry(entity);
        if (entry.State == EntityState.Unchanged)
        {
            entry.State = EntityState.Modified;
        }
    }
}