using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimeEntityFramework
{
    internal static class EntityFrameworkExtensions
    {
        public static EntityType GetEntityModelMetadata<TEntity>(this DbContext dbContext)
        {
            var objectContext = ((IObjectContextAdapter)dbContext).ObjectContext;
            return GetEntityModelMetadata(objectContext, typeof(TEntity));
        }

        public static EntityType GetEntityModelMetadata(this DbContext dbContext, Type entityType)
        {
            var objectContext = ((IObjectContextAdapter)dbContext).ObjectContext;
            return GetEntityModelMetadata(objectContext, entityType);
        }

        public static EntityType GetEntityModelMetadata<TEntity>(this ObjectContext objectContext)
        {
            return GetEntityModelMetadata(objectContext, typeof(TEntity));
        }

        public static EntityType GetEntityModelMetadata(this ObjectContext objectContext, Type entityType)
        {
            var conceptualSpaceItems = objectContext.MetadataWorkspace.GetItems<EntityType>(DataSpace.CSpace);
            var objectSpaceItems = (ObjectItemCollection)objectContext.MetadataWorkspace.GetItemCollection(DataSpace.OSpace);
            var entityMetadata = conceptualSpaceItems.FirstOrDefault(et =>
            {
                var objectSpaceType = (EntityType)objectContext.MetadataWorkspace.GetObjectSpaceType(et);
                var clrType = objectSpaceItems.GetClrType((StructuralType)objectSpaceType);
                return clrType == entityType;
            });

            return entityMetadata;
        }
    }
}
