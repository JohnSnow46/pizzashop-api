using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PizzaShop.Application.Common.Messaging;
using ValidationException = PizzaShop.Application.Common.Exceptions.ValidationException;

namespace PizzaShop.Application.Tests.Common.Messaging;

public class DispatcherTests
{
    private sealed record PingCommand(string Text) : ICommand<string>;

    private sealed class PingCommandHandler : ICommandHandler<PingCommand, string>
    {
        public Task<string> Handle(PingCommand command, CancellationToken cancellationToken) =>
            Task.FromResult($"pong:{command.Text}");
    }

    private sealed class PingCommandValidator : AbstractValidator<PingCommand>
    {
        public PingCommandValidator()
        {
            RuleFor(c => c.Text).NotEmpty();
        }
    }

    private sealed record PingQuery(string Text) : IQuery<string>;

    private sealed class PingQueryHandler : IQueryHandler<PingQuery, string>
    {
        public Task<string> Handle(PingQuery query, CancellationToken cancellationToken) =>
            Task.FromResult($"query:{query.Text}");
    }

    private static IDispatcher BuildDispatcher()
    {
        var services = new ServiceCollection();
        services.AddScoped<ICommandHandler<PingCommand, string>, PingCommandHandler>();
        services.AddScoped<IValidator<PingCommand>, PingCommandValidator>();
        services.AddScoped<IQueryHandler<PingQuery, string>, PingQueryHandler>();
        services.AddScoped<IDispatcher, Dispatcher>();

        return services.BuildServiceProvider().GetRequiredService<IDispatcher>();
    }

    [Fact]
    public async Task Send_ValidCommand_InvokesHandlerAndReturnsResult()
    {
        var dispatcher = BuildDispatcher();

        var result = await dispatcher.Send<string>(new PingCommand("hello"));

        result.Should().Be("pong:hello");
    }

    [Fact]
    public async Task Send_InvalidCommand_ThrowsValidationExceptionWithoutInvokingHandler()
    {
        var dispatcher = BuildDispatcher();

        var act = () => dispatcher.Send<string>(new PingCommand(""));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Send_Query_InvokesHandlerAndReturnsResult()
    {
        var dispatcher = BuildDispatcher();

        var result = await dispatcher.Send<string>(new PingQuery("hi"));

        result.Should().Be("query:hi");
    }
}
