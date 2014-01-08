using System.Collections.Generic;
using System.Linq.Expressions;

namespace System.Web.Mvc.Html
{
    public static class HtmlHelperExtensions
    {
        public static IHtmlString DisplayNameFor<TModel, TOriginal, TRetVal>(this HtmlHelper<TOriginal> helper, IEnumerable<TModel> dummy, Expression<Func<TModel, TRetVal>> expression)
        {
            var metadata = ModelMetadata.FromLambdaExpression(expression, new ViewDataDictionary<TModel>());
            return new HtmlString(metadata.DisplayName ?? metadata.PropertyName);
        }
    }
}