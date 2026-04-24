namespace Dashboard.Core.Entities;

public enum AlertIncidentState
{
    Firing = 0,
    Acknowledged = 1,
    Resolved = 2,
}

public sealed class AlertIncident
{
    public Guid Id { get; private set; }
    public Guid RuleId { get; private set; }
    public AlertIncidentState State { get; private set; }
    public double ObservedValue { get; private set; }
    public DateTimeOffset TriggeredAt { get; private set; }
    public DateTimeOffset? AcknowledgedAt { get; private set; }
    public Guid? AcknowledgedByUserId { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }

    private AlertIncident() { }

    public AlertIncident(Guid ruleId, double observedValue)
    {
        Id = Guid.NewGuid();
        RuleId = ruleId;
        ObservedValue = observedValue;
        State = AlertIncidentState.Firing;
        TriggeredAt = DateTimeOffset.UtcNow;
    }

    public void Acknowledge(Guid userId, DateTimeOffset when)
    {
        if (State != AlertIncidentState.Firing) return;
        State = AlertIncidentState.Acknowledged;
        AcknowledgedByUserId = userId;
        AcknowledgedAt = when;
    }

    public void Resolve(DateTimeOffset when)
    {
        if (State == AlertIncidentState.Resolved) return;
        State = AlertIncidentState.Resolved;
        ResolvedAt = when;
    }
}
