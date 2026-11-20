namespace SafeAlert.Models;

public enum EmergencyType
{
    Panic,
    Medical,
    Harassment,
    Robbery,
    Police,
    Silent,
    WalkSafe,
    Test
}

public enum AlertStatus
{
    InProgress,
    Resolved,
    FalseAlarm
}

public enum HistoryFilter
{
    All,
    Resolved,
    FalseAlarm,
    InProgress
}
