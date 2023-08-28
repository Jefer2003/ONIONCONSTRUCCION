using System;
using CleanArchitecture.Domain.Commands.Users.CreateUser;
using CleanArchitecture.Domain.Errors;
using CleanArchitecture.Domain.Events.User;
using Xunit;

namespace CleanArchitecture.Domain.Tests.CommandHandler.User.CreateUser;

public sealed class CreateUserCommandHandlerTests
{
    private readonly CreateUserCommandTestFixture _fixture = new();

    [Fact]
    public void Should_Create_User()
    {
        _fixture.SetupUser();

        var command = new CreateUserCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "test@email.com",
            "Test",
            "Email",
            "Po=PF]PC6t.?8?ks)A6W");

        _fixture.CommandHandler.Handle(command, default).Wait();

        _fixture
            .VerifyNoDomainNotification()
            .VerifyCommit()
            .VerifyRaisedEvent<UserCreatedEvent>(x => x.AggregateId == command.UserId);
    }

    [Fact]
    public void Should_Not_Create_Already_Existing_User()
    {
        var user = _fixture.SetupUser();

        var command = new CreateUserCommand(
            user.Id,
            Guid.NewGuid(),
            "test@email.com",
            "Test",
            "Email",
            "Po=PF]PC6t.?8?ks)A6W");

        _fixture.CommandHandler.Handle(command, default).Wait();

        _fixture
            .VerifyNoCommit()
            .VerifyNoRaisedEvent<UserCreatedEvent>()
            .VerifyAnyDomainNotification()
            .VerifyExistingNotification(
                DomainErrorCodes.User.UserAlreadyExists,
                $"There is already a user with Id {command.UserId}");
    }
}