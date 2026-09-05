-- Dev-only: why can't patient X log in? Run each block and compare against what the API needs.

SET QUOTED_IDENTIFIER ON;

-- 1. Coverage: patients that can actually reach a password check.
SELECT
    total          = COUNT(*),
    with_hash      = SUM(CASE WHEN LEN(ISNULL(p.password, '')) = 60 THEN 1 ELSE 0 END),
    truncated_hash = SUM(CASE WHEN p.password IS NOT NULL AND LEN(p.password) <> 60 THEN 1 ELSE 0 END),
    with_authstate = SUM(CASE WHEN a.PatientId IS NOT NULL THEN 1 ELSE 0 END),
    no_mobile      = SUM(CASE WHEN p.mobile_no IS NULL THEN 1 ELSE 0 END)
FROM Patient p
LEFT JOIN PatientAuthState a ON a.PatientId = p.row_id;

-- 2. Duplicate phone numbers: Login uses FirstOrDefault on mobile_no, so duplicates
--    resolve to whichever row SQL returns first -- the other patient can never log in.
SELECT mobile_no, dupes = COUNT(*)
FROM Patient
WHERE mobile_no IS NOT NULL
GROUP BY mobile_no
HAVING COUNT(*) > 1
ORDER BY dupes DESC;

-- 3. Locked-out accounts (5 bad attempts => 15 min lock, returns HTTP 423 not 401).
SELECT PatientId, FailedLoginCount, LockedUntilUtc
FROM PatientAuthState
WHERE LockedUntilUtc IS NOT NULL AND LockedUntilUtc > GETUTCDATE();

-- 4. Single-patient check. mobile_no is decimal -- the API strips non-digits from the
--    submitted phone and parses it, so '+91 98765 43210' must equal 919876543210 here.
DECLARE @Phone DECIMAL(18,0) = 9876543210;
SELECT p.row_id, p.FirstName, p.LastName, p.mobile_no,
       hash_len = LEN(ISNULL(p.password, '')),
       has_auth_state = CASE WHEN a.PatientId IS NULL THEN 0 ELSE 1 END,
       a.MustChangePassword, a.FailedLoginCount, a.LockedUntilUtc
FROM Patient p
LEFT JOIN PatientAuthState a ON a.PatientId = p.row_id
WHERE p.mobile_no = @Phone;
