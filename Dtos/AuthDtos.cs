namespace patient_api.Dtos;

public record LoginRequest(string PhoneNumber, string Password);

public record LoginResponse(string AccessToken, bool MustChangePassword, string PatientName);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record ChangePasswordResponse(string AccessToken);
