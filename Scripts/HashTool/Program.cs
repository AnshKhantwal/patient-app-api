// Dev utility: prints a BCrypt hash for a password passed as argv[0] (defaults to "medcross").
// Used to seed dbo.Patient.password for local testing / the real enrollment backfill.
var password = args.Length > 0 ? args[0] : "medcross";
Console.WriteLine(BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12));
