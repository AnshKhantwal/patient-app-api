namespace patient_api.Dtos;

public record VitalsSubmitRequest(
    DateTime Date,
    decimal? HeightCm,
    int? WeightKg,
    decimal? TemperatureF,
    int? BpSystolic,
    int? BpDiastolic,
    int? RespiratoryRate,
    int? Pulse,
    decimal? Spo2);

public record VitalsRecord(
    int Id,
    DateTime Date,
    decimal? HeightCm,
    int? WeightKg,
    decimal? TemperatureF,
    int? BpSystolic,
    int? BpDiastolic,
    int? RespiratoryRate,
    int? Pulse,
    decimal? Spo2);
