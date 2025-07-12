using BearWordsAPI.Shared.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BearWordsAPI.Shared.Helpers;

public class CascadeRuleBuilder
{
    private readonly Dictionary<Type, List<Func<ISoftDeletable, DbContext, IEnumerable<ISoftDeletable>>>>
        _deleteRules = [];
    private readonly Dictionary<Type, List<Func<ISoftDeletable, DbContext, IEnumerable<ISoftDeletable>>>>
        _restoreRules = [];

    public bool HasDeleteRules(Type entityType) => _deleteRules.ContainsKey(entityType);
    public bool HasRestoreRules(Type entityType) => _restoreRules.ContainsKey(entityType);

    public CascadeRuleBuilder OnDelete<T, TChild>(Func<T, DbContext, IQueryable<TChild>> selector)
        where T : class, ISoftDeletable
        where TChild : class, ISoftDeletable
    {
        var entityType = typeof(T);
        if (!_deleteRules.ContainsKey(entityType))
            _deleteRules[entityType] = [];

        _deleteRules[entityType].Add((entity, context) => selector((T)entity, context));
        return this;
    }

    public CascadeRuleBuilder OnRestore<T, TChild>(Func<T, DbContext, IQueryable<TChild>> selector)
        where T : class, ISoftDeletable
        where TChild : class, ISoftDeletable
    {
        var entityType = typeof(T);
        if (!_restoreRules.ContainsKey(entityType))
            _restoreRules[entityType] = [];

        _restoreRules[entityType].Add((entity, context) => selector((T)entity, context));
        return this;
    }

    public IEnumerable<ISoftDeletable> GetDeleteTargets(ISoftDeletable entity, DbContext context)
    {
        if (!_deleteRules.TryGetValue(entity.GetType(), out var rules))
            return [];

        var allTargets = new List<ISoftDeletable>();
        foreach (var rule in rules)
        {
            var targets = rule.Invoke(entity, context);
            allTargets.AddRange(targets);
        }
        return allTargets;
    }

    public IEnumerable<ISoftDeletable> GetRestoreTargets(ISoftDeletable entity, DbContext context)
    {
        if (!_restoreRules.TryGetValue(entity.GetType(), out var rules))
            return [];

        var allTargets = new List<ISoftDeletable>();
        foreach (var rule in rules)
        {
            var targets = rule.Invoke(entity, context);
            allTargets.AddRange(targets);
        }
        return allTargets;
    }

    public CascadeConfiguration Build()
    {
        return new CascadeConfiguration(this);
    }
}
