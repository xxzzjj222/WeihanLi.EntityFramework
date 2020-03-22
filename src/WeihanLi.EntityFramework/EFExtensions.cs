﻿using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using WeihanLi.Extensions;

namespace WeihanLi.EntityFramework
{
    public static class EFExtensions
    {
        internal static readonly Version EFCoreVersion = typeof(DbContext)
            .Assembly.GetName().Version;

        public static EntityEntry<TEntity> Remove<TEntity>(this DbContext dbContext, params object[] keyValues) where TEntity : class
        {
            var entity = dbContext.Find<TEntity>(keyValues);
            if (entity == null)
            {
                return null;
            }

            return dbContext.Remove(entity);
        }

        public static EntityEntry<TEntity> Update<TEntity>(this DbContext dbContext, TEntity entity, params string[] propNames) where TEntity : class
        {
            if (propNames == null || propNames.Length == 0)
            {
                return dbContext.Update(entity);
            }

            var entry = dbContext.GetEntityEntry(entity, out var existBefore);
            if (existBefore)
            {
                foreach (var propEntry in entry.Properties)
                {
                    if (!propNames.Contains(propEntry.Metadata.Name))
                    {
                        propEntry.IsModified = false;
                    }
                }
            }
            else
            {
                entry.State = EntityState.Unchanged;
                foreach (var propName in propNames)
                {
                    entry.Property(propName).IsModified = true;
                }
            }

            return entry;
        }

        public static EntityEntry<TEntity> UpdateWithout<TEntity>(this DbContext dbContext, TEntity entity, params string[] propNames) where TEntity : class
        {
            if (propNames == null || propNames.Length == 0)
            {
                return dbContext.Update(entity);
            }

            var entry = dbContext.GetEntityEntry(entity, out _);
            entry.State = EntityState.Modified;
            foreach (var expression in propNames)
            {
                entry.Property(expression).IsModified = false;
            }

            return entry;
        }

        public static EntityEntry<TEntity> Update<TEntity>(this DbContext dbContext, TEntity entity, params Expression<Func<TEntity, object>>[] propertyExpressions) where TEntity : class
        {
            if (propertyExpressions == null || propertyExpressions.Length == 0)
            {
                return dbContext.Update(entity);
            }

            var entry = dbContext.GetEntityEntry(entity, out var existBefore);

            if (existBefore)
            {
                var propNames = propertyExpressions.Select(x => x.GetMemberName()).ToArray();

                foreach (var propEntry in entry.Properties)
                {
                    if (!propNames.Contains(propEntry.Metadata.Name))
                    {
                        propEntry.IsModified = false;
                    }
                }
            }
            else
            {
                entry.State = EntityState.Unchanged;
                foreach (var expression in propertyExpressions)
                {
                    entry.Property(expression).IsModified = true;
                }
            }

            return entry;
        }

        public static EntityEntry<TEntity> UpdateWithout<TEntity>(this DbContext dbContext, TEntity entity, params Expression<Func<TEntity, object>>[] propertyExpressions) where TEntity : class
        {
            if (propertyExpressions == null || propertyExpressions.Length == 0)
            {
                return dbContext.Update(entity);
            }

            var entry = dbContext.GetEntityEntry(entity, out _);

            entry.State = EntityState.Modified;
            foreach (var expression in propertyExpressions)
            {
                entry.Property(expression).IsModified = false;
            }

            return entry;
        }

        private static EntityEntry<TEntity> GetEntityEntry<TEntity>(this DbContext dbContext, TEntity entity, out bool existBefore)
      where TEntity : class
        {
            var type = typeof(TEntity);

            var entityType = dbContext.Model.FindEntityType(type);

            var keysGetter = entityType.FindPrimaryKey().Properties
                .Select(x => x.PropertyInfo.GetValueGetter<TEntity>())
                .ToArray();

            var keyValues = keysGetter
                .Select(x => x.Invoke(entity))
                .ToArray();

            var originalEntity = dbContext.Set<TEntity>().Local
                .FirstOrDefault(x => GetEntityKeyValues(keysGetter, x).SequenceEqual(keyValues));

            EntityEntry<TEntity> entityEntry;
            if (null == originalEntity)
            {
                existBefore = false;
                entityEntry = dbContext.Attach(entity);
            }
            else
            {
                existBefore = true;
                entityEntry = dbContext.Entry(originalEntity);
                entityEntry.CurrentValues.SetValues(entity);
            }

            return entityEntry;
        }

        private static object[] GetEntityKeyValues<TEntity>(Func<TEntity, object>[] keyValueGetter, TEntity entity)
        {
            var keyValues = keyValueGetter.Select(x => x.Invoke(entity)).ToArray();
            return keyValues;
        }
    }
}