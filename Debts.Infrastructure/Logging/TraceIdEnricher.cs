using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace Debts.Infrastructure.Logging;

public class TraceIdEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? "no-trace";

        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty("TraceId", traceId)
        );
    }
}