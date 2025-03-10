using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Extensions;
using BananaCakePop.Middleware;
using static HotChocolate.AspNetCore.MiddlewareRoutingType;
using static Microsoft.AspNetCore.Routing.Patterns.RoutePatternFactory;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Provides GraphQL extensions to the <see cref="IEndpointConventionBuilder"/>.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    private const string _graphQLHttpPath = "/graphql";
    private const string _graphQLWebSocketPath = "/graphql/ws";
    private const string _graphQLSchemaPath = "/graphql/sdl";
    private const string _graphQLToolPath = "/graphql/ui";
    private const string _graphQLToolRelativeRequestPath = "..";

    /// <summary>
    /// Adds a GraphQL endpoint to the endpoint configurations.
    /// </summary>
    /// <param name="endpointRouteBuilder">
    /// The <see cref="IEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="path">
    /// The path to which the GraphQL endpoint shall be mapped.
    /// </param>
    /// <param name="schemaName">
    /// The name of the schema that shall be used by this endpoint.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    public static GraphQLEndpointConventionBuilder MapGraphQL(
        this IEndpointRouteBuilder endpointRouteBuilder,
        string path = _graphQLHttpPath,
        string? schemaName = default)
        => MapGraphQL(endpointRouteBuilder, new PathString(path), schemaName);

    /// <summary>
    /// Adds a GraphQL endpoint to the endpoint configurations.
    /// </summary>
    /// <param name="endpointRouteBuilder">
    /// The <see cref="IEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="path">
    /// The path to which the GraphQL endpoint shall be mapped.
    /// </param>
    /// <param name="schemaName">
    /// The name of the schema that shall be used by this endpoint.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="endpointRouteBuilder" /> is <c>null</c>.
    /// </exception>
    public static GraphQLEndpointConventionBuilder MapGraphQL(
        this IEndpointRouteBuilder endpointRouteBuilder,
        PathString path,
        string? schemaName = default)
    {
        if (endpointRouteBuilder is null)
        {
            throw new ArgumentNullException(nameof(endpointRouteBuilder));
        }

        path = path.ToString().TrimEnd('/');

        var pattern = Parse(path + "/{**slug}");
        var requestPipeline = endpointRouteBuilder.CreateApplicationBuilder();
        var schemaNameOrDefault = schemaName ?? Schema.DefaultName;

        requestPipeline
            .UseCancellation()
            .UseMiddleware<WebSocketSubscriptionMiddleware>(schemaNameOrDefault)
            .UseMiddleware<HttpPostMiddleware>(schemaNameOrDefault)
            .UseMiddleware<HttpMultipartMiddleware>(schemaNameOrDefault)
            .UseMiddleware<HttpGetMiddleware>(schemaNameOrDefault, path)
            .UseMiddleware<HttpGetSchemaMiddleware>(schemaNameOrDefault, Integrated)
            .UseBananaCakePop(path)
            .Use(_ => context =>
            {
                context.Response.StatusCode = 404;
                return Task.CompletedTask;
            });

        return new GraphQLEndpointConventionBuilder(
            endpointRouteBuilder
                .Map(pattern, requestPipeline.Build())
                .WithDisplayName("Hot Chocolate GraphQL Pipeline"));
    }

    /// <summary>
    /// Adds a GraphQL HTTP endpoint to the endpoint configurations.
    /// </summary>
    /// <param name="endpointRouteBuilder">
    /// The <see cref="IEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="pattern">
    /// The path to which the GraphQL HTTP endpoint shall be mapped.
    /// </param>
    /// <param name="schemaName">
    /// The name of the schema that shall be used by this endpoint.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="endpointRouteBuilder" /> is <c>null</c>.
    /// </exception>
    public static GraphQLHttpEndpointConventionBuilder MapGraphQLHttp(
        this IEndpointRouteBuilder endpointRouteBuilder,
        string pattern = _graphQLHttpPath,
        string? schemaName = default)
        => MapGraphQLHttp(endpointRouteBuilder, Parse(pattern), schemaName);

    /// <summary>
    /// Adds a GraphQL HTTP endpoint to the endpoint configurations.
    /// </summary>
    /// <param name="endpointRouteBuilder">
    /// The <see cref="IEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="pattern">
    /// The path to which the GraphQL HTTP endpoint shall be mapped.
    /// </param>
    /// <param name="schemaName">
    /// The name of the schema that shall be used by this endpoint.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="endpointRouteBuilder" /> is <c>null</c>.
    /// </exception>
    public static GraphQLHttpEndpointConventionBuilder MapGraphQLHttp(
        this IEndpointRouteBuilder endpointRouteBuilder,
        RoutePattern pattern,
        string? schemaName = default)
    {
        if (endpointRouteBuilder is null)
        {
            throw new ArgumentNullException(nameof(endpointRouteBuilder));
        }

        if (pattern is null)
        {
            throw new ArgumentNullException(nameof(pattern));
        }

        var requestPipeline = endpointRouteBuilder.CreateApplicationBuilder();
        var schemaNameOrDefault = schemaName ?? Schema.DefaultName;

        requestPipeline
            .UseCancellation()
            .UseMiddleware<HttpPostMiddleware>(schemaNameOrDefault)
            .UseMiddleware<HttpMultipartMiddleware>(schemaNameOrDefault)
            .UseMiddleware<HttpGetMiddleware>(schemaNameOrDefault, default(PathString))
            .Use(_ => context =>
            {
                context.Response.StatusCode = 404;
                return Task.CompletedTask;
            });

        return new GraphQLHttpEndpointConventionBuilder(
            endpointRouteBuilder
                .Map(pattern, requestPipeline.Build())
                .WithDisplayName("Hot Chocolate GraphQL HTTP Pipeline"));
    }

    /// <summary>
    /// Adds a GraphQL WebSocket endpoint to the endpoint configurations.
    /// </summary>
    /// <param name="endpointRouteBuilder">
    /// The <see cref="IEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="pattern">
    /// The path to which the GraphQL WebSocket endpoint shall be mapped.
    /// </param>
    /// <param name="schemaName">
    /// The name of the schema that shall be used by this endpoint.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="endpointRouteBuilder" /> is <c>null</c>.
    /// </exception>
    public static WebSocketEndpointConventionBuilder MapGraphQLWebSocket(
        this IEndpointRouteBuilder endpointRouteBuilder,
        string pattern = _graphQLWebSocketPath,
        string? schemaName = default)
        => MapGraphQLWebSocket(endpointRouteBuilder, Parse(pattern), schemaName);

    /// <summary>
    /// Adds a GraphQL WebSocket endpoint to the endpoint configurations.
    /// </summary>
    /// <param name="endpointRouteBuilder">
    /// The <see cref="IEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="pattern">
    /// The path to which the GraphQL WebSocket endpoint shall be mapped.
    /// </param>
    /// <param name="schemaName">
    /// The name of the schema that shall be used by this endpoint.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="endpointRouteBuilder" /> is <c>null</c>.
    /// </exception>
    public static WebSocketEndpointConventionBuilder MapGraphQLWebSocket(
        this IEndpointRouteBuilder endpointRouteBuilder,
        RoutePattern pattern,
        string? schemaName = default)
    {
        if (endpointRouteBuilder is null)
        {
            throw new ArgumentNullException(nameof(endpointRouteBuilder));
        }

        if (pattern is null)
        {
            throw new ArgumentNullException(nameof(pattern));
        }

        var requestPipeline = endpointRouteBuilder.CreateApplicationBuilder();
        var schemaNameOrDefault = schemaName ?? Schema.DefaultName;

        requestPipeline
            .UseCancellation()
            .UseMiddleware<WebSocketSubscriptionMiddleware>(schemaNameOrDefault)
            .Use(_ => context =>
            {
                context.Response.StatusCode = 404;
                return Task.CompletedTask;
            });

        var builder = new GraphQLEndpointConventionBuilder(
            endpointRouteBuilder
                .Map(pattern, requestPipeline.Build())
                .WithDisplayName("Hot Chocolate GraphQL WebSocket Pipeline"));

        return new WebSocketEndpointConventionBuilder(builder);
    }

    /// <summary>
    /// Adds a GraphQL schema SDL endpoint to the endpoint configurations.
    /// </summary>
    /// <param name="endpointRouteBuilder">
    /// The <see cref="IEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="pattern">
    /// The path to which the GraphQL schema SDL endpoint shall be mapped.
    /// </param>
    /// <param name="schemaName">
    /// The name of the schema that shall be used by this endpoint.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="endpointRouteBuilder" /> is <c>null</c>.
    /// </exception>
    public static IEndpointConventionBuilder MapGraphQLSchema(
        this IEndpointRouteBuilder endpointRouteBuilder,
        string pattern = _graphQLSchemaPath,
        string? schemaName = default)
        => MapGraphQLSchema(endpointRouteBuilder, Parse(pattern), schemaName);

    /// <summary>
    /// Adds a GraphQL schema SDL endpoint to the endpoint configurations.
    /// </summary>
    /// <param name="endpointRouteBuilder">
    /// The <see cref="IEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="pattern">
    /// The path to which the GraphQL schema SDL endpoint shall be mapped.
    /// </param>
    /// <param name="schemaName">
    /// The name of the schema that shall be used by this endpoint.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="endpointRouteBuilder" /> is <c>null</c>.
    /// </exception>
    public static IEndpointConventionBuilder MapGraphQLSchema(
        this IEndpointRouteBuilder endpointRouteBuilder,
        RoutePattern pattern,
        string? schemaName = default)
    {
        if (endpointRouteBuilder is null)
        {
            throw new ArgumentNullException(nameof(endpointRouteBuilder));
        }

        if (pattern is null)
        {
            throw new ArgumentNullException(nameof(pattern));
        }

        var requestPipeline = endpointRouteBuilder.CreateApplicationBuilder();
        var schemaNameOrDefault = schemaName ?? Schema.DefaultName;

        requestPipeline
            .UseCancellation()
            .UseMiddleware<HttpGetSchemaMiddleware>(schemaNameOrDefault, Explicit)
            .Use(_ => context =>
            {
                context.Response.StatusCode = 404;
                return Task.CompletedTask;
            });

        return new GraphQLEndpointConventionBuilder(
            endpointRouteBuilder
                .Map(pattern, requestPipeline.Build())
                .WithDisplayName("Hot Chocolate GraphQL Schema Pipeline"));
    }

    /// <summary>
    /// Adds a Banana Cake Pop endpoint to the endpoint configurations.
    /// </summary>
    /// <param name="endpointRouteBuilder">
    /// The <see cref="IEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="toolPath">
    /// The path to which Banana Cake Pop is mapped.
    /// </param>
    /// <param name="relativeRequestPath">
    /// The relative path on which the server is listening for GraphQL requests.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    public static BananaCakePopEndpointConventionBuilder MapBananaCakePop(
        this IEndpointRouteBuilder endpointRouteBuilder,
        string toolPath = _graphQLToolPath,
        string? relativeRequestPath = _graphQLToolRelativeRequestPath)
        => MapBananaCakePop(endpointRouteBuilder, new PathString(toolPath), relativeRequestPath);

    /// <summary>
    /// Adds a Banana Cake Pop endpoint to the endpoint configurations.
    /// </summary>
    /// <param name="endpointRouteBuilder">
    /// The <see cref="IEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="toolPath">
    /// The path to which Banana Cake Pop is mapped.
    /// </param>
    /// <param name="relativeRequestPath">
    /// The relative path on which the server is listening for GraphQL requests.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    public static BananaCakePopEndpointConventionBuilder MapBananaCakePop(
        this IEndpointRouteBuilder endpointRouteBuilder,
        PathString toolPath,
        string? relativeRequestPath = _graphQLToolRelativeRequestPath)
    {
        if (endpointRouteBuilder is null)
        {
            throw new ArgumentNullException(nameof(endpointRouteBuilder));
        }

        toolPath = toolPath.ToString().TrimEnd('/');
        relativeRequestPath ??= _graphQLToolRelativeRequestPath;

        var pattern = Parse(toolPath + "/{**slug}");
        var requestPipeline = endpointRouteBuilder.CreateApplicationBuilder();

        requestPipeline
            .UseBananaCakePop(toolPath)
            .Use(_ => context =>
            {
                context.Response.StatusCode = 404;
                return Task.CompletedTask;
            });

        var builder = endpointRouteBuilder
            .Map(pattern, requestPipeline.Build())
            .WithDisplayName("Banana Cake Pop Pipeline")
            .WithMetadata(new BananaCakePopOptions { GraphQLEndpoint = relativeRequestPath });

        return new BananaCakePopEndpointConventionBuilder(builder);
    }

    /// <summary>
    /// Specifies the GraphQL server options.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="GraphQLEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="serverOptions">
    /// The GraphQL server options.
    /// </param>
    /// <returns>
    /// Returns the <see cref="GraphQLEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    public static GraphQLEndpointConventionBuilder WithOptions(
        this GraphQLEndpointConventionBuilder builder,
        GraphQLServerOptions serverOptions) 
        => builder
            .WithMetadata(serverOptions)
            .WithMetadata(serverOptions.Tool.ToBcpOptions());

    /// <summary>
    /// Specifies the GraphQL HTTP request options.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="GraphQLEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="httpOptions">
    /// The GraphQL HTTP request options.
    /// </param>
    /// <returns>
    /// Returns the <see cref="GraphQLEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    public static GraphQLHttpEndpointConventionBuilder WithOptions(
        this GraphQLHttpEndpointConventionBuilder builder,
        GraphQLHttpOptions httpOptions) =>
        builder.WithMetadata(new GraphQLServerOptions
        {
            AllowedGetOperations = httpOptions.AllowedGetOperations,
            EnableGetRequests = httpOptions.EnableGetRequests,
            EnableMultipartRequests = httpOptions.EnableMultipartRequests
        });

    /// <summary>
    /// Specifies the Banana Cake Pop tooling options.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="BananaCakePopEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="toolOptions">
    /// The Banana Cake Pop tooling options.
    /// </param>
    /// <returns>
    /// Returns the <see cref="BananaCakePopEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    public static BananaCakePopEndpointConventionBuilder WithOptions(
        this BananaCakePopEndpointConventionBuilder builder,
        GraphQLToolOptions toolOptions)
    {
        builder.Add(c => c.Metadata.Add(toolOptions.ToBcpOptions()));
        return builder;
    }

    /// <summary>
    /// Specifies the GraphQL over Websocket options.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="WebSocketEndpointConventionBuilder"/>.
    /// </param>
    /// <param name="socketOptions">
    /// The GraphQL socket options.
    /// </param>
    /// <returns>
    /// Returns the <see cref="WebSocketEndpointConventionBuilder"/> so that
    /// configuration can be chained.
    /// </returns>
    public static WebSocketEndpointConventionBuilder WithOptions(
        this WebSocketEndpointConventionBuilder builder,
        GraphQLSocketOptions socketOptions) =>
        builder.WithMetadata(new GraphQLServerOptions { Sockets = socketOptions });

    private static IApplicationBuilder UseCancellation(this IApplicationBuilder builder)
        => builder.Use(next => async context =>
        {
            try
            {
                await next(context);
            }
            catch (OperationCanceledException)
            {
                // we just catch cancellations here and do nothing.
            }
        });

    internal static BananaCakePopOptions ToBcpOptions(this GraphQLToolOptions options)
        => new()
        {
            ServeMode = ServeMode.Version(options.ServeMode.Mode),
            Title = options.Title,
            Document = options.Document,
            UseBrowserUrlAsGraphQLEndpoint = options.UseBrowserUrlAsGraphQLEndpoint,
            GraphQLEndpoint = options.GraphQLEndpoint,
            IncludeCookies = options.IncludeCookies,
            HttpHeaders = options.HttpHeaders,
            UseGet = options.HttpMethod == DefaultHttpMethod.Get,
            Enable = options.Enable,
            GaTrackingId = options.GaTrackingId,
            DisableTelemetry = options.DisableTelemetry,
        };
}
