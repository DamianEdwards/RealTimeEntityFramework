using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace RealTimeEntityFramework
{
    internal class PredicateExpressionValuesExtractor<TEntity> : ExpressionVisitor
    {
        private bool _evaluating;

        private readonly IDbContext _dbContext;

        private readonly Dictionary<string, object> _predicateFields = new Dictionary<string, object>();

        public PredicateExpressionValuesExtractor(IDbContext dbContext)
        {
            _dbContext = dbContext;
            IsValid = true;
        }

        public IDictionary<string, object> PredicateFields
        {
            get
            {
                return _predicateFields;
            }
        }

        public bool IsValid { get; private set; }

        public string ValidationError { get; private set; }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (_evaluating)
            {
                // TODO: Evaluate nested binary expressions

            }
            else
            {
                Expression target = null;
                string fieldName = null;

                // TODO: Change this to better capture details when the predicate is not supported

                // TODO: Support predicates that compare multiple entity properties, e.g. :
                //           post => post.CategoryId = categoryId && post.IsVisible || post.SuperVisible
                //       This would result in two distinct groups: CategoryId_[value]_IsVisible_true and SuperVisible

                if (IsNotificationSupportedEntityPropertyExpression(node.Left))
                {
                    fieldName = ((MemberExpression)node.Left).Member.Name;
                    target = node.Right;
                }
                else if (IsNotificationSupportedEntityPropertyExpression(node.Right))
                {
                    fieldName = ((MemberExpression)node.Right).Member.Name;
                    target = node.Left;
                }

                if (target != null)
                {
                    try
                    {
                        _evaluating = true;

                        var entityExpression = base.Visit(target) as ConstantExpression;

                        if (entityExpression != null)
                        {
                            _predicateFields.Add(fieldName, entityExpression.Value);
                        }
                        else
                        {
                            IsValid = false;
                            ValidationError = "The predicate has some issues.";
                        }
                    }
                    finally
                    {
                        _evaluating = false;
                    }
                }
            }

            return base.VisitBinary(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (_evaluating)
            {
                if (node.Member.MemberType == MemberTypes.Field &&
                    node.Expression.NodeType == ExpressionType.Constant)
                {
                    var value = ((FieldInfo)node.Member).GetValue(((ConstantExpression)node.Expression).Value);

                    return Expression.Constant(value);
                }
            }

            return base.VisitMember(node);
        }

        private bool IsNotificationSupportedEntityPropertyExpression(Expression expression)
        {
            if (expression.NodeType == ExpressionType.MemberAccess)
            {
                var memberExpression = (MemberExpression)expression;
                if (memberExpression.Member.DeclaringType == typeof(TEntity)
                    && memberExpression.Member.MemberType == MemberTypes.Property)
                {
                    // Get the EF meta model for the entity
                    var entityMetadata = _dbContext.GetEntityModelMetadata(typeof(TEntity));

                    // Determine if the property being accessed is supported as a notification trigger

                    // Foreign Keys
                    // TODO: This logic needs to more complex to support multi-part foreign keys, etc.
                    var isComparingToForeignKey = entityMetadata.NavigationProperties
                        .Any(np => np.GetDependentProperties()
                                     .Any(p => String.Equals(p.Name, memberExpression.Member.Name, StringComparison.Ordinal)));

                    if (isComparingToForeignKey)
                    {
                        return true;
                    }

                    // TODO: Support other property types, e.g. primitive types that are explicitly decorated as notification triggers
                }
            }

            return false;
        }
    }
}
