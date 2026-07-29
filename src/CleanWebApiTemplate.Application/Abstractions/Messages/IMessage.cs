namespace CleanWebApiTemplate.Application.Abstractions.Messages;

/// <summary>
/// Marker interface for every CQRS message (commands and queries).
/// Useful to constrain shared infrastructure (e.g. base validators) to messages only.
/// </summary>
public interface IMessage;
