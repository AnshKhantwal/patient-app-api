using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace patient_api.Models;

[Table("VitalSign", Schema = "dbo")]
public class VitalSign
{
    [Key]
    [Column("row_id")]
    public int RowId { get; set; }

    [Column("visit_id")]
    public int VisitId { get; set; }

    [Column("patient_id")]
    public int? PatientId { get; set; }

    [Column("date")]
    public DateTime Date { get; set; }

    [Column("height")]
    public decimal? Height { get; set; }

    [Column("weight")]
    public int? Weight { get; set; }

    [Column("temperature")]
    public decimal? Temperature { get; set; }

    [Column("blood_pressure_systolic")]
    public int? BloodPressureSystolic { get; set; }

    [Column("blood_pressure_distolic")]
    public int? BloodPressureDiastolic { get; set; }

    [Column("respiratory_rate")]
    public int? RespiratoryRate { get; set; }

    [Column("pulse")]
    public int? Pulse { get; set; }

    [Column("spo2")]
    public decimal? Spo2 { get; set; }

    [Column("ip_address")]
    public string? IpAddress { get; set; }
}
