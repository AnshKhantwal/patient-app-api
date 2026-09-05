using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace patient_api.Models;

// Volatile patient-auth security state. Kept separate from dbo.Patient (which already
// holds password/user_id) so this new table's presence/absence never risks the legacy schema.
[Table("PatientAuthState", Schema = "dbo")]
public class PatientAuthState
{
    [Key]
    public int PatientId { get; set; }

    public bool MustChangePassword { get; set; } = true;

    public int FailedLoginCount { get; set; }

    public DateTime? LockedUntilUtc { get; set; }

    public DateTime? LastLoginAtUtc { get; set; }

    public Patient? Patient { get; set; }
}
