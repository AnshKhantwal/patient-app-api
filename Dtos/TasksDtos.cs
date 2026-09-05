namespace patient_api.Dtos;

public record TaskCheckIn(string TaskKey, string Status);

public record TasksSubmitRequest(DateTime Date, List<TaskCheckIn> Tasks);

public record TasksDayResponse(DateTime Date, List<TaskCheckIn> Tasks);

public record TasksHistoryEntry(DateTime Date, List<TaskCheckIn> Tasks, DateTime LastSubmittedAtUtc);
