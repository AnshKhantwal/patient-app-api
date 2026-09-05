using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace patient_api.Models;

[Table("Visit", Schema = "dbo")]
public class Visit
{
    [Key]
    [Column("row_id")]
    public int RowId { get; set; }

    [Column("patient_id")]
    public int PatientId { get; set; }

    [Column("visit_on")]
    public DateTime VisitOn { get; set; }

    [Column("practice_name")]
    public string PracticeName { get; set; } = string.Empty;

    [Column("doctor_id")]
    public int DoctorId { get; set; }

    [Column("practice_id")]
    public int PracticeId { get; set; }

    [Column("is_visit_over")]
    public bool? IsVisitOver { get; set; }
}
