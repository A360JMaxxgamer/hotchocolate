using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using HotChocolate.Subscriptions;
using HotChocolate.Subscriptions.RabbitMQ;
using RabbitMQ.Client;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// These helper methods allows to register the RabbitMQ subscription provider with the GraphQL configuration.
/// </summary>
public static class RabbitMQPubSubExtensions
{
    /// <summary>
    /// Adds support for using RabbitMQ as a subscription provider.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="connectionFactory">
    /// The RabbitMQ connection factory. This parameter is optional; the default value is <c>null</c>.
    /// The following properties will be overriden with <c>true</c> to make the connection recoverable.
    /// - <c>AutomaticRecoveryEnabled</c>
    /// - <c>DispatchConsumersAsync</c>
    /// </param>
    /// <param name="options">
    /// The subscription provider options. This parameter is optional; the default value is <c>null</c>.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddRabbitMQSubscriptions(
        this IRequestExecutorBuilder builder,
        ConnectionFactory? connectionFactory = null,
        SubscriptionOptions? options = null)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder
            .AddSubscriptionDiagnostics()
            .Services
            .AddRabbitMQSubscriptions(connectionFactory, options);

        return builder;
    }

    private static void AddRabbitMQSubscriptions(
        this IServiceCollection services,
        ConnectionFactory? connectionFactory = null,
        SubscriptionOptions? options = null)
    {
        services.TryAddSingleton(connectionFactory ?? new ConnectionFactory());
        services.TryAddSingleton<IRabbitMQConnection, RabbitMQConnection>();
        services.TryAddSingleton(options ?? new SubscriptionOptions());
        services.TryAddSingleton<IMessageSerializer, DefaultJsonMessageSerializer>();
        services.TryAddSingleton<RabbitMQPubSub>();
        services.TryAddSingleton<ITopicEventSender>(sp => sp.GetRequiredService<RabbitMQPubSub>());
        services.TryAddSingleton<ITopicEventReceiver>(sp => sp.GetRequiredService<RabbitMQPubSub>());
    }
}
