using BearWordsAPI.Shared.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BearWordsAPI.Shared.Helpers;

public class CascadeConfiguration
{
    private readonly CascadeRuleBuilder _builder;

    internal CascadeConfiguration(CascadeRuleBuilder builder)
    {
        _builder = builder;
    }

    public IEnumerable<ISoftDeletable> GetDeleteTargets(ISoftDeletable entity, DbContext context)
    {
        return _builder.GetDeleteTargets(entity, context);
    }

    public IEnumerable<ISoftDeletable> GetRestoreTargets(ISoftDeletable entity, DbContext context)
    {
        return _builder.GetRestoreTargets(entity, context);
    }

    public bool HasDeleteRules(Type entityType)
    {
        return _builder.HasDeleteRules(entityType);
    }

    public bool HasRestoreRules(Type entityType)
    {
        return _builder.HasRestoreRules(entityType);
    }
}