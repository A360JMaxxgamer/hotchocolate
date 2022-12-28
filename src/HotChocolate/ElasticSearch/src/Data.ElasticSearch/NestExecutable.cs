﻿using System.Collections;
using System.Reflection;
using System.Text.Json;
using Elasticsearch.Net;
using HotChocolate.Data.ElasticSearch.Execution;
using HotChocolate.Data.ElasticSearch.Filters;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
using Nest;

namespace HotChocolate.Data.ElasticSearch;

public sealed class NestExecutable<T> : ElasticSearchExecutable<T> where T : class
{
    private readonly IElasticClient _elasticClient;

    /// <inheritdoc />
    public override object Source => this;

    public NestExecutable(IElasticClient elasticClient)
    {
        _elasticClient = elasticClient;
    }

    /// <inheritdoc />
    public override async ValueTask<object?> FirstOrDefaultAsync(CancellationToken cancellationToken)
    {
        var searchDescriptor = CreateQuery();
        searchDescriptor.Size = 1;
        var result = await _elasticClient.SearchAsync<T>(searchDescriptor, cancellationToken);
        return result.Hits.Select(hit => hit.Source).FirstOrDefault();
    }

    /// <inheritdoc />
    public override string GetName(IFilterField field)
    {
        IElasticFilterMetadata metadata = field.GetElasticMetadata();
        if (metadata.Name is { }) return metadata.Name;

        if (field.Member is PropertyInfo propertyInfo) return _elasticClient.Infer.Field(new Field(propertyInfo));

        if (field.Member is {Name: { } memberName}) return memberName;

        return field.Name;
    }

    /// <inheritdoc />
    public override string GetName(ISortField field)
    {
        if (field.Member is PropertyInfo propertyInfo)
        {
            return AddKeywordSuffix(_elasticClient.Infer.Field(new Field(propertyInfo)));
        }

        if (field.Member is { Name: { } memberName })
        {
            return AddKeywordSuffix(memberName);
        }

        return AddKeywordSuffix(field.Name);
    }

    /// <inheritdoc />
    public override string Print()
    {
        MemoryStream stream = new();
        SerializableData<SearchRequest> data = new(CreateQuery());
        data.Write(stream, new ConnectionSettings(new InMemoryConnection()));
        stream.Position = 0;
        JsonDocument deserialized = JsonDocument.Parse(stream);
        JsonSerializerOptions options = new() { WriteIndented = true };
        return JsonSerializer.Serialize(deserialized, options);
    }

    /// <inheritdoc />
    public override async ValueTask<object?> SingleOrDefaultAsync(CancellationToken cancellationToken)
    {
        var searchDescriptor = CreateQuery();
        searchDescriptor.Size = 1;
        var result = await _elasticClient.SearchAsync<T>(searchDescriptor, cancellationToken);
        return result.Hits.Select(hit => hit.Source).FirstOrDefault();
    }

    /// <inheritdoc />
    public override async ValueTask<IList> ToListAsync(CancellationToken cancellationToken)
    {
        var searchDescriptor = CreateQuery();
        var result = await _elasticClient.SearchAsync<T>(searchDescriptor, cancellationToken);
        return result.Hits.Select(hit => hit.Source).ToList();
    }

    private string AddKeywordSuffix(string val) => $"{val}.keyword";

    private SearchRequest<T> CreateQuery()
    {
        var searchRequest = new SearchRequest<T>
        {
            Query = new MatchAllQuery()
        };

        if (Filters is ISearchOperation filters)
        {
            var query = (QueryBase)ElasticSearchOperationRewriter.Instance.Rewrite(filters);
            searchRequest.Query = query;
        }

        if (Sorting is {Count: > 0} sortOperations)
        {
            searchRequest.Sort = sortOperations
                .Select(sortOperation => new FieldSort
                {
                    Field = new Field(AddKeywordSuffix(sortOperation.Path)),
                    Order = sortOperation.Direction == ElasticSearchSortDirection.Ascending
                        ? SortOrder.Ascending
                        : SortOrder.Descending
                })
                .OfType<ISort>()
                .ToList();
        }

        if (Take is not null)
        {
            searchRequest.Size = Take;
        }

        if (Skip is not null)
        {
            searchRequest.From = Skip;
        }

        return searchRequest;
    }
}

