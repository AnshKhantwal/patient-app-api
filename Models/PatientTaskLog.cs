using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace patient_api.Models;

// Patient daily self-checklist (Medicine/Diet/Exercise/...). Deliberately separate from the
// legacy dbo.Tasks table, which is an unrelated staff-to-staff CRM assignment system.
[Table("PatientTaskLog", Schema = "dbo")]
public class PatientTaskLog
{
    [Key]
    public int Id { get; set; }

    public int PatientId { get; set; }

    [Column(TypeName = "date")]
    public DateTime LogDate { get; set; }

    [MaxLength(50)]
    public string TaskKey { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Status { get; set; } = string.Empty; // complete | partial | none

    public DateTime SubmittedAtUtc { get; set; }

    public Patient? Patient { get; set; }
}
