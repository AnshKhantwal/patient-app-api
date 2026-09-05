-- Dev-only: backfill PatientAuthState for every patient that already has a password hash.
-- Login (AuthController.Login) rejects any patient without a PatientAuthState row, even
-- when Patient.password holds a valid BCrypt hash -- this is what makes a bulk
-- "UPDATE Patient SET password = @Hash" alone insufficient.
--
-- Safe to re-run: only inserts rows that are missing.

SET QUOTED_IDENTIFIER ON;

INSERT INTO PatientAuthState (PatientId, MustChangePassword, FailedLoginCount, LockedUntilUtc, LastLoginAtUtc)
SELECT p.row_id, 1, 0, NULL, NULL
FROM Patient p
WHERE p.password IS NOT NULL
  AND LEN(p.password) = 60
  AND NOT EXISTS (SELECT 1 FROM PatientAuthState a WHERE a.PatientId = p.row_id);

-- Clear any lockouts / failed counters accumulated while login was failing for the wrong reason.
UPDATE PatientAuthState
SET FailedLoginCount = 0,
    LockedUntilUtc = NULL;
