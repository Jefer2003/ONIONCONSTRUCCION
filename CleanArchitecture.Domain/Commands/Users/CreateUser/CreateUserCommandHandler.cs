using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;
using CleanArchitecture.Domain.Errors;
using CleanArchitecture.Domain.Events.User;
using CleanArchitecture.Domain.Interfaces;
using CleanArchitecture.Domain.Interfaces.Repositories;
using CleanArchitecture.Domain.Notifications;
using MediatR;
using BC = BCrypt.Net.BCrypt;

namespace CleanArchitecture.Domain.Commands.Users.CreateUser;

public sealed class CreateUserCommandHandler : CommandHandlerBase,
    IRequestHandler<CreateUserCommand>
{
    private readonly IUserRepository _userRepository;

    public CreateUserCommandHandler(
        IMediatorHandler bus,
        IUnitOfWork unitOfWork,
        INotificationHandler<DomainNotification> notifications,
        IUserRepository userRepository) : base(bus, unitOfWork, notifications)
    {
        _userRepository = userRepository;
    }

    public async Task Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        if (!await TestValidityAsync(request))
        {
            return;
        }

        var existingUser = await _userRepository.GetByIdAsync(request.UserId);

        if (existingUser is not null)
        {
            await Bus.RaiseEventAsync(
                new DomainNotification(
                    request.MessageType,
                    $"There is already a user with Id {request.UserId}",
                    DomainErrorCodes.User.UserAlreadyExists));
            return;
        }

        existingUser = await _userRepository.GetByEmailAsync(request.Email);

        if (existingUser is not null)
        {
            await Bus.RaiseEventAsync(
                new DomainNotification(
                    request.MessageType,
                    $"There is already a user with email {request.Email}",
                    DomainErrorCodes.User.UserAlreadyExists));
            return;
        }

        var passwordHash = BC.HashPassword(request.Password);

        var user = new User(
            request.UserId,
            request.TenantId,
            request.Email,
            request.FirstName,
            request.LastName,
            passwordHash,
            UserRole.User);

        _userRepository.Add(user);

        if (await CommitAsync())
        {
            await Bus.RaiseEventAsync(new UserCreatedEvent(user.Id));
        }
    }
}