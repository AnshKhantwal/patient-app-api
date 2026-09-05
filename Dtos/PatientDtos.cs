namespace patient_api.Dtos;

public record PatientProfile(int PatientId, string Name, string? Phone);

public record UpdateProfileRequest(string Name, string? Phone);
