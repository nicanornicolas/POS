using Domain.ValueObjects;
using MediatR;

namespace Application.Events;

public sealed record TransactionSettledEvent(Guid InternalTransactionId, string TerminalId, Money Amount) : INotification;
