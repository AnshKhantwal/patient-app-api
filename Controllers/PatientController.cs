using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using patient_api.Auth;
using patient_api.Data;
using patient_api.Dtos;

namespace patient_api.Controllers;

[ApiController]
[Route("api/patient")]
[Authorize]
[RequirePasswordChanged]
public class PatientController(AppDbContext db) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<PatientProfile>> Me()
    {
        var patientId = User.GetPatientId();
        var patient = await db.Patients.FirstOrDefaultAsync(p => p.RowId == patientId);
        if (patient is null)
        {
            return NotFound();
        }

        var name = $"{patient.FirstName} {patient.LastName}".Trim();
        var phone = patient.MobileNo?.ToString("0");
        return Ok(new PatientProfile(patient.RowId, name, phone));
    }

    [HttpPut("me")]
    public async Task<ActionResult<PatientProfile>> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        var patientId = User.GetPatientId();
        var patient = await db.Patients.FirstOrDefaultAsync(p => p.RowId == patientId);
        if (patient is null)
        {
            return NotFound();
        }

        var trimmedName = request.Name?.Trim();
        if (string.IsNullOrEmpty(trimmedName))
        {
            return BadRequest(new { error = "Name is required." });
        }

        // The legacy dbo.Patient stores the name split across two columns. Single free-text
        // field in, so first token is the given name and the remainder is the surname.
        var spaceIndex = trimmedName.IndexOf(' ');
        if (spaceIndex < 0)
        {
            patient.FirstName = trimmedName;
            patient.LastName = null;
        }
        else
        {
            patient.FirstName = trimmedName[..spaceIndex];
            patient.LastName = trimmedName[(spaceIndex + 1)..].Trim();
        }

        // Phone doubles as the login identifier (AuthController matches on mobile_no), so it
        // must stay unique across patients and the caller has to log in with the new number next time.
        var digitsOnly = new string((request.Phone ?? string.Empty).Where(char.IsDigit).ToArray());
        if (string.IsNullOrEmpty(digitsOnly) || !decimal.TryParse(digitsOnly, out var mobileNo))
        {
            return BadRequest(new { error = "A valid phone number is required." });
        }

        var phoneTaken = await db.Patients.AnyAsync(p => p.MobileNo == mobileNo && p.RowId != patientId);
        if (phoneTaken)
        {
            return Conflict(new { error = "That phone number is already in use by another account." });
        }

        patient.MobileNo = mobileNo;
        patient.UserId = digitsOnly;

        await db.SaveChangesAsync();

        var name = $"{patient.FirstName} {patient.LastName}".Trim();
        return Ok(new PatientProfile(patient.RowId, name, patient.MobileNo?.ToString("0")));
    }
}
