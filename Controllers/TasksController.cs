using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using patient_api.Auth;
using patient_api.Data;
using patient_api.Dtos;
using patient_api.Models;

namespace patient_api.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize]
[RequirePasswordChanged]
public class TasksController(AppDbContext db) : ControllerBase
{
    private static readonly HashSet<string> AllowedStatuses = ["complete", "partial", "none"];

    [HttpPost]
    public async Task<IActionResult> Submit(TasksSubmitRequest request)
    {
        if (request.Tasks.Any(t => !AllowedStatuses.Contains(t.Status)))
        {
            return BadRequest(new { error = "Invalid task status." });
        }

        var patientId = User.GetPatientId();
        var logDate = request.Date.Date;

        var existing = await db.PatientTaskLogs
            .Where(t => t.PatientId == patientId && t.LogDate == logDate)
            .ToListAsync();

        foreach (var entry in request.Tasks)
        {
            var row = existing.FirstOrDefault(e => e.TaskKey == entry.TaskKey);
            if (row is null)
            {
                db.PatientTaskLogs.Add(new PatientTaskLog
                {
                    PatientId = patientId,
                    LogDate = logDate,
                    TaskKey = entry.TaskKey,
                    Status = entry.Status,
                    SubmittedAtUtc = DateTime.UtcNow
                });
            }
            else
            {
                row.Status = entry.Status;
                row.SubmittedAtUtc = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("history")]
    public async Task<ActionResult<List<TasksHistoryEntry>>> GetHistory()
    {
        var patientId = User.GetPatientId();

        var rows = await db.PatientTaskLogs
            .Where(t => t.PatientId == patientId)
            .OrderByDescending(t => t.LogDate)
            .ToListAsync();

        var grouped = rows
            .GroupBy(r => r.LogDate)
            .Select(g => new TasksHistoryEntry(
                g.Key,
                g.Select(r => new TaskCheckIn(r.TaskKey, r.Status)).ToList(),
                g.Max(r => r.SubmittedAtUtc)))
            .ToList();

        return Ok(grouped);
    }

    [HttpGet]
    public async Task<ActionResult<TasksDayResponse>> GetByDate([FromQuery] DateTime date)
    {
        var patientId = User.GetPatientId();
        var logDate = date.Date;

        var rows = await db.PatientTaskLogs
            .Where(t => t.PatientId == patientId && t.LogDate == logDate)
            .ToListAsync();

        return Ok(new TasksDayResponse(logDate, rows.Select(r => new TaskCheckIn(r.TaskKey, r.Status)).ToList()));
    }
}
