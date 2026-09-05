using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using patient_api.Auth;
using patient_api.Data;
using patient_api.Dtos;
using patient_api.Models;

namespace patient_api.Controllers;

[ApiController]
[Route("api/vitals")]
[Authorize]
[RequirePasswordChanged]
public class VitalsController(AppDbContext db, IConfiguration config) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<VitalsRecord>> Submit(VitalsSubmitRequest request)
    {
        if (!IsPlausible(request))
        {
            return BadRequest(new { error = "One or more vitals are outside a plausible range." });
        }

        var patientId = User.GetPatientId();

        // Legacy rows store local time (clsEpr uses DateTime.Now), and a date-only
        // submission would otherwise render as 12:00 AM in the EPR grid.
        var submittedOn = request.Date == default
            ? DateTime.Now
            : request.Date.TimeOfDay == TimeSpan.Zero
                ? request.Date.Add(DateTime.Now.TimeOfDay)
                : request.Date;

        await using var transaction = await db.Database.BeginTransactionAsync();

        var visit = await ResolveVisitAsync(patientId, submittedOn);

        var vital = new VitalSign
        {
            VisitId = visit.RowId,
            PatientId = patientId,
            Date = submittedOn,
            Height = request.HeightCm,
            Weight = request.WeightKg,
            Temperature = request.TemperatureF,
            BloodPressureSystolic = request.BpSystolic,
            BloodPressureDiastolic = request.BpDiastolic,
            RespiratoryRate = request.RespiratoryRate,
            Pulse = request.Pulse,
            Spo2 = request.Spo2,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };
        db.VitalSigns.Add(vital);
        await db.SaveChangesAsync();

        await transaction.CommitAsync();

        return Ok(ToRecord(vital));
    }

    [HttpGet]
    public async Task<ActionResult<List<VitalsRecord>>> GetHistory()
    {
        var patientId = User.GetPatientId();

        var records = await db.VitalSigns
            .Where(v => v.PatientId == patientId)
            .OrderByDescending(v => v.Date)
            .ToListAsync();

        return Ok(records.Select(ToRecord).ToList());
    }

    // ChiefComplaints.aspx renders vitals via proc_GetVitalSign(Session["visit_id"]), which
    // filters strictly by visit_id -- so a self-reported vital is only visible to the clinician
    // if it hangs off the same visit row the EPR resolves. clsEpr.Visit.VisitStatus picks the
    // newest still-open visit for the patient at that practice, and CreateNewVisit only opens a
    // fresh one (is_visit_over = false) when none exists. Mirror that exactly: reuse an open
    // visit if there is one, otherwise leave the new visit open so the EPR adopts it later.
    private async Task<Visit> ResolveVisitAsync(int patientId, DateTime visitOn)
    {
        var lastVisit = await db.Visits
            .Where(v => v.PatientId == patientId)
            .OrderByDescending(v => v.RowId)
            .FirstOrDefaultAsync();

        var doctorId = lastVisit?.DoctorId ?? config.GetValue("SelfReportedVisit:FallbackDoctorId", 0);
        var practiceId = lastVisit?.PracticeId ?? config.GetValue("SelfReportedVisit:FallbackPracticeId", 0);
        var practiceName = lastVisit?.PracticeName
            ?? config["SelfReportedVisit:PracticeName"]
            ?? "Patient Self-Reported";

        var openVisit = await db.Visits
            .Where(v => v.PatientId == patientId && v.PracticeId == practiceId && v.IsVisitOver == false)
            .OrderByDescending(v => v.RowId)
            .FirstOrDefaultAsync();

        if (openVisit is not null)
        {
            return openVisit;
        }

        var visit = new Visit
        {
            PatientId = patientId,
            VisitOn = visitOn,
            PracticeName = practiceName,
            DoctorId = doctorId,
            PracticeId = practiceId,
            IsVisitOver = false
        };
        db.Visits.Add(visit);
        await db.SaveChangesAsync();

        return visit;
    }

    private static bool IsPlausible(VitalsSubmitRequest r) =>
        (r.HeightCm is null or (> 20 and < 300)) &&
        (r.WeightKg is null or (> 0 and < 400)) &&
        (r.TemperatureF is null or (> 80 and < 115)) &&
        (r.BpSystolic is null or (> 40 and < 300)) &&
        (r.BpDiastolic is null or (> 20 and < 200)) &&
        (r.RespiratoryRate is null or (> 0 and < 100)) &&
        (r.Pulse is null or (> 0 and < 300)) &&
        (r.Spo2 is null or (> 0 and <= 100));

    private static VitalsRecord ToRecord(VitalSign v) => new(
        v.RowId, v.Date, v.Height, v.Weight, v.Temperature,
        v.BloodPressureSystolic, v.BloodPressureDiastolic, v.RespiratoryRate, v.Pulse, v.Spo2);
}
