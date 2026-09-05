using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using patient_api.Auth;
using patient_api.Data;
using patient_api.Dtos;
using patient_api.Models;
using patient_api.Services;

namespace patient_api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, JwtTokenService tokens, ILogger<AuthController> logger) : ControllerBase
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var normalizedPhone = NormalizePhone(request.PhoneNumber);
        if (normalizedPhone is null || string.IsNullOrEmpty(request.Password))
        {
            return Unauthorized(new { error = "Invalid phone number or password." });
        }

        var patient = await db.Patients
            .FirstOrDefaultAsync(p => p.MobileNo == normalizedPhone);

        if (patient is null)
        {
            logger.LogInformation("Login failed: no patient for supplied phone.");
            return Unauthorized(new { error = "Invalid phone number or password." });
        }

        var authState = await db.PatientAuthStates.FirstOrDefaultAsync(a => a.PatientId == patient.RowId);
        if (authState is null || string.IsNullOrEmpty(patient.PasswordHash))
        {
            logger.LogWarning("Login failed: patient {PatientId} has no credential provisioned.", patient.RowId);
            return Unauthorized(new { error = "Invalid phone number or password." });
        }

        if (authState.LockedUntilUtc is { } lockedUntil && lockedUntil > DateTime.UtcNow)
        {
            return StatusCode(StatusCodes.Status423Locked, new { error = "Account temporarily locked. Try again later." });
        }

        var passwordOk = BCrypt.Net.BCrypt.Verify(request.Password, patient.PasswordHash);
        if (!passwordOk)
        {
            authState.FailedLoginCount++;
            if (authState.FailedLoginCount >= MaxFailedAttempts)
            {
                authState.LockedUntilUtc = DateTime.UtcNow.Add(LockoutDuration);
                authState.FailedLoginCount = 0;
            }
            await db.SaveChangesAsync();
            return Unauthorized(new { error = "Invalid phone number or password." });
        }

        authState.FailedLoginCount = 0;
        authState.LockedUntilUtc = null;
        authState.LastLoginAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var accessToken = tokens.CreateAccessToken(patient.RowId, authState.MustChangePassword);
        var name = $"{patient.FirstName} {patient.LastName}".Trim();
        return Ok(new LoginResponse(accessToken, authState.MustChangePassword, name));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<ChangePasswordResponse>> ChangePassword(ChangePasswordRequest request)
    {
        if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 8)
        {
            return BadRequest(new { error = "New password must be at least 8 characters." });
        }

        var patientId = User.GetPatientId();
        var patient = await db.Patients.FirstOrDefaultAsync(p => p.RowId == patientId);
        var authState = await db.PatientAuthStates.FirstOrDefaultAsync(a => a.PatientId == patientId);

        if (patient is null || authState is null || string.IsNullOrEmpty(patient.PasswordHash))
        {
            return Unauthorized();
        }

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, patient.PasswordHash))
        {
            return BadRequest(new { error = "Current password is incorrect." });
        }

        patient.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
        authState.MustChangePassword = false;
        await db.SaveChangesAsync();

        var accessToken = tokens.CreateAccessToken(patient.RowId, authState.MustChangePassword);
        return Ok(new ChangePasswordResponse(accessToken));
    }

    // Accepts either a raw 10-digit number or one with country code / formatting; stored as a
    // decimal in dbo.ContactDetail.mobile_no exactly like the existing CPS schema.
    private static decimal? NormalizePhone(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var digitsOnly = new string(raw.Where(char.IsDigit).ToArray());
        return decimal.TryParse(digitsOnly, out var value) ? value : null;
    }
}
