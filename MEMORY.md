# MEMORY.md — Architecture & Decision Log

---

## 2026-06-07, US-AUTH-02 MFA Implementation

**What was decided:**
- TOTP secret is NOT persisted during `POST /auth/mfa/setup`. It is only stored after `POST /auth/mfa/enable` confirms a valid OTP from the user's authenticator app.
- AES-256-CBC is used to encrypt the TOTP secret at rest, with a key from `Mfa:EncryptionKey` (64-char hex) in app configuration.
- The MFA login flow is session-based (not token-based): after password validation, a `MfaPending` session key holds the user ID until `POST /auth/mfa/login` completes.
- Admin MFA toggle (`PATCH /admin/users/{id}/mfa`) sets `mfa_enabled` flag only; the user must complete setup themselves when admin enables MFA.
- Endpoint rename: `/auth/mfa/verify` split into `/auth/mfa/enable` (setup confirm) and `/auth/mfa/login` (login challenge). Old `VerifyMfaAsync` on `IAuthService` removed and replaced with `EnableMfaAsync` + `VerifyMfaLoginAsync`.

**Why:**
- Separating setup confirmation from login verification prevents a race condition where a partially set-up MFA blocks login.
- AES-256-CBC with per-encryption random IV ensures that even identical secrets produce distinct ciphertexts (prevents correlation attacks against the DB).
- Session-based pending state avoids storing short-lived tokens in Redis, keeping the architecture simple and consistent with the existing session-auth pattern.

**What was rejected:**
- Redis-based `pendingToken` approach (as originally spec'd): rejected because the project already has session infrastructure and it would add complexity for no privacy benefit in a self-hosted deployment.
- Persisting the TOTP secret immediately during setup: rejected because the user may never complete setup (scans QR but never verifies), polluting the DB with unvalidated secrets.
- JWT short-lived token for MFA pending: rejected to keep auth uniformly session-based (no Bearer tokens in localStorage per CLAUDE.md).

---
