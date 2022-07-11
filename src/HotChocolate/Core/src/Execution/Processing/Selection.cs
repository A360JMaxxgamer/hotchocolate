using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution.Properties;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// Represents a field selection during execution.
/// </summary>
public class Selection : ISelection
{
    private static readonly ArgumentMap _emptyArguments =
        new(new Dictionary<string, ArgumentValue>());

    private long[] _includeConditions;
    private Flags _flags;

    public Selection(
        int id,
        IObjectType declaringType,
        IObjectField field,
        IType type,
        FieldNode syntaxNode,
        string responseName,
        IArgumentMap? arguments = null,
        long includeCondition = 0,
        bool isInternal = false,
        bool isParallelExecutable = true,
        FieldDelegate? resolverPipeline = null,
        PureFieldDelegate? pureResolver = null)
    {
        Id = id;
        DeclaringType = declaringType;
        Field = field;
        Type = type;
        SyntaxNode = syntaxNode;
        ResponseName = responseName;
        Arguments = arguments ?? _emptyArguments;
        ResolverPipeline = resolverPipeline;
        PureResolver = pureResolver;
        Strategy = InferStrategy(!isParallelExecutable, pureResolver is not null);

        _includeConditions = includeCondition is 0
            ? Array.Empty<long>()
            : new[] { includeCondition };

        _flags = isInternal ? Flags.Internal : Flags.None;

        if (Type.IsListType())
        {
            _flags |= Flags.List;
        }
    }

    protected Selection(Selection selection)
    {
        if (selection is null)
        {
            throw new ArgumentNullException(nameof(selection));
        }

        Id = selection.Id;
        Strategy = selection.Strategy;
        DeclaringType = selection.DeclaringType;
        Field = selection.Field;
        Type = selection.Type;
        SyntaxNode = selection.SyntaxNode;
        ResponseName = selection.ResponseName;
        ResolverPipeline = selection.ResolverPipeline;
        PureResolver = selection.PureResolver;
        Arguments = selection.Arguments;
        _flags = selection._flags;

        _includeConditions =
            selection._includeConditions.Length == 0
                ? Array.Empty<long>()
                : selection._includeConditions.ToArray();
    }

    /// <inheritdoc />
    public int Id { get; }

    /// <inheritdoc />
    public SelectionExecutionStrategy Strategy { get; private set; }

    /// <inheritdoc />
    public IObjectType DeclaringType { get; }

    /// <inheritdoc />
    public IObjectField Field { get; }

    /// <inheritdoc />
    public IType Type { get; }

    /// <inheritdoc />
    public TypeKind TypeKind => Type.Kind;

    /// <inheritdoc />
    public bool IsList => (_flags & Flags.List) == Flags.List;

    /// <inheritdoc />
    public FieldNode SyntaxNode { get; private set; }

    public int SelectionSetId { get; private set; }

    /// <inheritdoc />
    public SelectionSetNode? SelectionSet => SyntaxNode.SelectionSet;

    /// <inheritdoc />
    public string ResponseName { get; }

    /// <inheritdoc />
    public FieldDelegate? ResolverPipeline { get; private set; }

    /// <inheritdoc />
    public PureFieldDelegate? PureResolver { get; private set; }

    /// <inheritdoc />
    public IArgumentMap Arguments { get; }

    public bool IsReadOnly => (_flags & Flags.Sealed) == Flags.Sealed;

    /// <inheritdoc />
    public bool IsInternal => (_flags & Flags.Internal) == Flags.Internal;

    /// <inheritdoc />
    public bool IsConditional
        => _includeConditions.Length > 0 || (_flags & Flags.Internal) == Flags.Internal;

    public bool IsIncluded(long includeFlags, bool allowInternals = false)
    {
        // in most case we do not have any include condition,
        // so we can take the easy way out here if we do not have any flags.
        if (_includeConditions.Length is 0)
        {
            return !IsInternal || allowInternals;
        }

        // if there are flags in most cases we just have one so we can
        // check the first and optimize for this.
        var includeCondition = _includeConditions[0];
        if ((includeFlags & includeCondition) == includeCondition)
        {
            return !IsInternal || allowInternals;
        }

        // if we just have one flag and the flags are not fulfilled we can just exit.
        if (_includeConditions.Length is 1)
        {
            return false;
        }

        // else, we will iterate over the rest of the conditions and validate them one by one.
        for (var i = 1; i < _includeConditions.Length; i++)
        {
            includeCondition = _includeConditions[i];
            if ((includeFlags & includeCondition) == includeCondition)
            {
                return !IsInternal || allowInternals;
            }
        }

        return false;
    }

    internal void AddSelection(FieldNode selectionSyntax, long includeCondition = 0)
    {
        if ((_flags & Flags.Sealed) == Flags.Sealed)
        {
            throw new NotSupportedException(Resources.PreparedSelection_ReadOnly);
        }

        if (includeCondition is not 0 &&
            Array.IndexOf(_includeConditions, includeCondition) == -1)
        {
            var next = _includeConditions.Length;
            Array.Resize(ref _includeConditions, next + 1);
            _includeConditions[next] = includeCondition;
        }

        if (!SyntaxNode.Equals(selectionSyntax, SyntaxComparison.Syntax))
        {
            SyntaxNode = MergeField(SyntaxNode, selectionSyntax);
        }
    }

    private static FieldNode MergeField(
        FieldNode first,
        FieldNode other)
    {
        var directives = first.Directives;

        if (other.Directives.Count > 0)
        {
            if (directives.Count == 0)
            {
                directives = other.Directives;
            }
            else
            {
                var temp = new DirectiveNode[directives.Count + other.Directives.Count];
                var next = 0;

                for (var i = 0; i < directives.Count; i++)
                {
                    temp[next++] = directives[i];
                }

                for (var i = 0; i < first.Directives.Count; i++)
                {
                    temp[next++] = first.Directives[i];
                }

                directives = temp;
            }
        }

        var selectionSet = first.SelectionSet;

        if (selectionSet is not null && other.SelectionSet is not null)
        {
            var selections = new ISelectionNode[
                selectionSet.Selections.Count +
                    other.SelectionSet.Selections.Count];
            var next = 0;

            for (var i = 0; i < selectionSet.Selections.Count; i++)
            {
                selections[next++] = selectionSet.Selections[i];
            }

            for (var i = 0; i < other.SelectionSet.Selections.Count; i++)
            {
                selections[next++] = other.SelectionSet.Selections[i];
            }

            selectionSet = selectionSet.WithSelections(selections);
        }

        return new FieldNode(
            first.Location,
            first.Name,
            first.Alias,
            first.Required,
            directives,
            first.Arguments,
            selectionSet);
    }

    internal void SetResolvers(
        FieldDelegate? resolverPipeline = null,
        PureFieldDelegate? pureResolver = null)
    {
        if ((_flags & Flags.Sealed) == Flags.Sealed)
        {
            throw new NotSupportedException(Resources.PreparedSelection_ReadOnly);
        }

        ResolverPipeline = resolverPipeline;
        PureResolver = pureResolver;
        Strategy = InferStrategy(hasPureResolver: pureResolver is not null);
    }

    internal void SetSelectionSetId(int selectionSetId)
    {
        if ((_flags & Flags.Sealed) == Flags.Sealed)
        {
            throw new NotSupportedException(Resources.PreparedSelection_ReadOnly);
        }

        SelectionSetId = selectionSetId;
    }

    internal void Seal()
    {
        if ((_flags & Flags.Sealed) != Flags.Sealed)
        {
            _flags |= Flags.Sealed;
        }
    }

    private SelectionExecutionStrategy InferStrategy(
        bool isSerial = false,
        bool hasPureResolver = false)
    {
        // once a field is marked serial it even with a pure resolver cannot become pure.
        if (Strategy is SelectionExecutionStrategy.Serial || isSerial)
        {
            return SelectionExecutionStrategy.Serial;
        }

        if (hasPureResolver)
        {
            return SelectionExecutionStrategy.Pure;
        }

        return SelectionExecutionStrategy.Default;
    }

    [Flags]
    private enum Flags
    {
        None = 0,
        Internal = 1,
        Sealed = 2,
        List = 4,
        Stream = 8
    }
}
