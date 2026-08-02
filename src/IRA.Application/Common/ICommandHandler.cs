namespace IRA.Application.Common;

/// <summary>
/// Marker for a command (an intent that changes state).
/// </summary>
public interface ICommand<TResult>
{
}

/// <summary>Marker for a query (a read that does not change state).</summary>
public interface IQuery<TResult>
{
}

/// <summary>Handles a single command type. Implements the write side of the CQRS split.</summary>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>Handles a single query type. Implements the read side of the CQRS split.</summary>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
