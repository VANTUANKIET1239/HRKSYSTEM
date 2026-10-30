using Activity.Domain;
using Core.Messaging.Contracts;
using System.Text.Json;

namespace Activity.Application;

public interface IActivityRecorder
{
    Task RecordAsync(RecordPlayerActivityCommand command, CancellationToken ct);
}
