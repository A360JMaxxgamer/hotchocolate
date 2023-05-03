using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Data.ElasticSearch.Attributes;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Data.ElasticSearch.Filters;

/// <summary>
/// The default handler for all <see cref="FilterField"/> for the
/// <see cref="ElasticSearchFilterProvider"/>
/// </summary>
public class ElasticSearchDefaultFieldHandler
    : FilterFieldHandler<ElasticSearchFilterVisitorContext, ISearchOperation>
{
    /// <summary>
    /// Checks if the field not a filter operations field
    /// </summary>
    /// <param name="context">The current context</param>
    /// <param name="typeDefinition">The definition of the type that declares the field</param>
    /// <param name="fieldDefinition">The definition of the field</param>
    /// <returns>True in case the field can be handled</returns>
    public override bool CanHandle(
        ITypeCompletionContext context,
        IFilterInputTypeDefinition typeDefinition,
        IFilterFieldDefinition fieldDefinition) =>
        !(fieldDefinition is FilterOperationFieldDefinition);

    /// <inheritdoc />
    public override bool TryHandleEnter(
        ElasticSearchFilterVisitorContext context,
        IFilterField field,
        ObjectFieldNode node,
        [NotNullWhen(true)] out ISyntaxVisitorAction? action)
    {
        if (node.Value.IsNull())
        {
            context.ReportError(ErrorHelper.CreateNonNullError(field, node.Value, context));

            action = SyntaxVisitor.Skip;
            return true;
        }

        if (field.RuntimeType is null)
        {
            action = null;
            return false;
        }

        var fieldName = field.Member?.GetCustomAttribute<ElasticSearchFieldNameAttribute>()?.FieldName ?? field.Name;

        context.Path.Push(fieldName);
        context.RuntimeTypes.Push(field.RuntimeType);
        action = SyntaxVisitor.Continue;
        return true;
    }

    /// <inheritdoc />
    public override bool TryHandleLeave(
        ElasticSearchFilterVisitorContext context,
        IFilterField field,
        ObjectFieldNode node,
        [NotNullWhen(true)] out ISyntaxVisitorAction? action)
    {
        context.RuntimeTypes.Pop();
        context.Path.Pop();

        action = SyntaxVisitor.Continue;
        return true;
    }
}
