# Security boundaries

Observe is the only executable mode. There is no infrastructure-write interface and no mode-switch API. Local platform mutations (drafts/imports/reviews) do not modify managed infrastructure.

Identity uses ASP.NET Core salted password hashes, 14-character minimum, complexity rules, five-failure lockout and 30-minute non-sliding session cookies. Login is limited to 10 attempts/minute/IP; other API traffic is limited to 300/minute/principal or IP. Cookie security stamps revalidate each minute. CSRF validation applies to all unsafe API methods, including login/logout. No CORS policy is enabled. All product data APIs require authentication. Role checks are enforced server-side; UI visibility is not the security boundary.

| Role | Granted capability |
|---|---|
| PlatformAdministrator | Import, changes, analysis, audit; **not** approval by role implication |
| IntegrationAdministrator | Inventory and relationship ingestion |
| ChangeManager | Draft/edit/submit/analyze |
| RiskAnalyst | Analyze; no draft edit, import or execution |
| ChangeApprover | Review decisions; own change forbidden |
| DeploymentOperator | Read only in this version |
| Auditor | Read and audit history |
| ReadOnlyUser | Read product data |

Initial accounts and role assignments are local operator CLI actions. No default password or public bootstrap endpoint exists. Production accounts must be provisioned intentionally. `.local/credentials.json` is a generated development artifact restricted by Windows ACL, never shipped.

Production Data Protection keys require persistent storage. Windows uses machine DPAPI; Linux encrypts keys using a separate PKCS#12 certificate. Private key material, certificate password and database password must be held in protected secret files, independently from the encrypted key ring. Docker secret mounts are not a substitute for host access controls. Back up the certificate and key ring securely.

HTML report values are encoded; CSV formula prefixes are escaped. Configuration JSON is inert data. No dynamic code, SQL or shell expressions can be submitted. Request body and collection limits reject excessive input. Logs omit credentials and source response errors. Runtime Docker processes run as non-root with read-only root filesystems.

Database triggers and EF reject audit/evidence mutation. Runtime role does not own tables or have DDL privileges; migration owner credentials never reach API/worker containers. DBA/superuser compromise remains outside this application control. A separate immutable audit sink/signing service is not implemented.

## Outstanding production gates

- OIDC/AD authentication, MFA and account lifecycle UI are not implemented.
- Rate limiting is process-local. Forwarded client IP and HTTPS scheme are accepted only from the explicitly configured proxy IP, with a one-hop limit. Keep the proxy address and network allocation consistent; HA-wide limiting remains required before enterprise exposure.
- Live connector secrets, endpoint allowlists, rotation and production collection are not yet managed by the UI.
- Audit retention/partitioning, cryptographic audit-chain signing, penetration testing and dependency scanning in CI remain required.
- Windows Application Control blocked a newly built SDK DLL during this session. No protection was disabled; native deployment needs organization-approved signing/allowlisting by the system owner.

Report vulnerabilities privately to the repository owner; do not attach credentials or enterprise inventory to public issues.
