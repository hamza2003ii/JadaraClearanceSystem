# Software Requirements Specification (SRS)

Project: Jadara Clearance — Modern Clearance Management Platform

Date: 2026-08-30

Authors: System Architect / Hamza

---

## 1. Executive Summary
A modern, responsive, secure, and extensible web platform for managing clearance requests at Jadara University. System includes Admin Dashboard, Officer Portal, Student Portal, RESTful APIs, RBAC, audit logging, reporting, notifications, and backup/restore.

## 2. Scope
In-scope: Admin panel, student/officer portals, DB migration, API, auth (2FA), audit logs, reporting, export, notifications. Out-of-scope: native mobile apps, SSO integrations (optional).

## 3. Stakeholders
- System Administrator
- Registrar / Officer
- Student
- Auditor
- Developer / Maintainer
- Instructor / Product Owner

## 4. Glossary
- RBAC: Role Based Access Control
- 2FA: Two-Factor Authentication
- API: Application Programming Interface

## 5. Functional Requirements
F-1 Authentication & Account Management
- Secure login, logout, password reset
- 2FA (TOTP) enrollment and verification
- Session management, login history

F-2 Roles & Permissions
- Roles: Admin, Officer, Student, Auditor
- Permission management and role assignment

F-3 Clearance Workflow
- Submit request with attachments
- Multi-department approval stepper
- Request status timeline, reassign, escalate

F-4 Audit & Logging
- Append-only audit logs for sensitive actions
- Queryable by admin with export options

F-5 Content Management
- CRUD for articles/pages, category & media library
- SEO metadata editing

F-6 Reporting & Exports
- Prebuilt user and system reports
- Export to PDF/Excel, scheduled exports

F-7 Notifications
- Email templates, in-app notifications, admin alerts

F-8 System Management
- Site settings, SMTP config, backup & restore

## 6. Non-Functional Requirements
NFR-1 Accessibility: WCAG 2.1 AA
NFR-2 Performance: API p95 < 300ms under baseline load
NFR-3 Scalability: Horizontally scalable stateless APIs
NFR-4 Security: TLS, CSP, secure password hashing (Argon2id)
NFR-5 Reliability: Daily backups, monitoring & alerting
NFR-6 Maintainability: Clean architecture, tests, CI/CD

## 7. User Stories (Representative)
- As Admin, I can manage users and roles.
- As Officer, I can approve/reject requests and add comments.
- As Student, I can submit a clearance request with attachments.
- As Auditor, I can export audit logs for compliance.

## 8. System Modules
- Frontend: Admin SPA, User SPA (React + TypeScript)
- Backend: API server (ASP.NET Core / Node NestJS)
- DB: PostgreSQL (or MS SQL)
- Auth Service: JWT + Refresh + TOTP
- Notification Service: Email + In-App
- Storage: S3-compatible or Azure Blob
- Background Jobs: Redis queue + workers
- Monitoring: Prometheus + Grafana

## 9. Data Model Summary
See `../db/schema.sql` and `../design/erd.mmd` for full schema, relationships, constraints, and indexes.

## 10. API Summary
OpenAPI stub located at `../specs/openapi.yaml`. Versioned endpoints under `/api/v1/*` for auth, users, requests, content, reports, system.

## 11. Security Architecture
- HTTPS-only, HSTS
- CSP, secure cookies (HttpOnly, SameSite=strict)
- Rate limiting and account lockout
- RBAC enforced in middleware and service layer
- Audit logging for role changes, data exports, system restores

## 12. Deployment & Operations
- Containerized services (Docker), images pushed to registry
- Kubernetes or managed Web App for API and workers
- Postgres managed cluster with replicas and automated backups
- CI: GitHub Actions — build, test, docker push, deploy

## 13. Testing Strategy
- Unit tests for services and utils
- Integration tests for API endpoints (containerized DB)
- E2E tests for key user flows (Playwright/Cypress)
- Security scans (SAST, dependency checks)

## 14. Deliverables
- `docs/SRS.md` (this file)
- `design/*` — ERD + UML (Mermaid)
- `specs/openapi.yaml` — OpenAPI stub
- `db/schema.sql` — SQL schema & migrations
- Frontend prototype pages under `frontend/` (admin, officer, student)

## 15. Next Steps (Suggested)
1. Finalize DB schema and create SQL migrations.
2. Produce OpenAPI full spec.
3. Scaffold backend project and implement auth.
4. Build frontend Admin SPA using design system.

---

For details on any specific section (e.g., full OpenAPI, full SQL migrations, wireframes), tell me which to generate next and I'll produce it in the workspace.
