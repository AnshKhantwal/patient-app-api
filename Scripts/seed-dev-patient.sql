-- Dev-only helper: seed one real patient row with a BCrypt("medcross") hash + auth state,
-- so login can be tested end-to-end without running the full backfill across all patients.
--
-- Generate the hash yourself with the same work factor the API uses:
--   cd Scripts/HashTool && dotnet run -- medcross
-- Never paste a hash you didn't just generate -- work factor / salt must match what
-- AuthController.ChangePassword uses (BCrypt.Net.BCrypt.HashPassword(pw, workFactor: 12)).

SET QUOTED_IDENTIFIER ON;

DECLARE @TargetRowId INT = 1; -- Patient.row_id to seed (row_id=1 on PolyClinic062626 dev snapshot)
DECLARE @Hash VARCHAR(60) = '$2a$12$vgf6ESMKe463yyBnN5JDUOJ9ecvYYW.tNjRNQGLKoXloATB75.jwW'; -- BCrypt("medcross")

UPDATE Patient
SET password = @Hash
WHERE row_id = @TargetRowId;

IF NOT EXISTS (SELECT 1 FROM PatientAuthState WHERE PatientId = @TargetRowId)
BEGIN
    INSERT INTO PatientAuthState (PatientId, MustChangePassword, FailedLoginCount, LockedUntilUtc, LastLoginAtUtc)
    VALUES (@TargetRowId, 1, 0, NULL, NULL);
END
