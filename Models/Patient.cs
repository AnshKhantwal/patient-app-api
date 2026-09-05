using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace patient_api.Models;

[Table("Patient", Schema = "dbo")]
public class Patient
{
    [Key]
    [Column("row_id")]
    public int RowId { get; set; }

    [Column("KmedID")]
    public string? KmedId { get; set; }

    // Reused as the patient's login identifier (set to phone number on first provisioning).
    [Column("user_id")]
    public string? UserId { get; set; }

    // BCrypt hash lives here (column is VarChar(60), sized for exactly a BCrypt hash).
    [Column("password")]
    public string? PasswordHash { get; set; }

    [Column("FirstName")]
    public string FirstName { get; set; } = string.Empty;

    [Column("LastName")]
    public string? LastName { get; set; }

    [Column("mobile_no")]
    public decimal? MobileNo { get; set; }

    [Column("Patient_Status")]
    public int? PatientStatus { get; set; }
}
