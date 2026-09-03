# Database Design — Jadara Clearance Platform

## Overview
Relational database (Postgres recommended) for transactional data. Analytical store recommended for reporting (ETL to OLAP).

## Normalization Strategy
- Third normal form for transactional entities.
- JSONB columns for flexible metadata (requests.meta, articles.seo).
- Append-only `audit_logs` to preserve history.

## Core Entities (brief)
- `users` (PK id)
- `roles`, `permissions`, `role_permissions`
- `sessions`
- `departments`
- `clearance_requests`
- `clearance_approvals`
- `media`
- `categories`, `articles`
- `notifications`
- `audit_logs`
- `exports`

## Indexing Strategy
- Unique index on users.email
- Composite indexes on clearance_requests(user_id, status)
- Index on audit_logs(timestamp) desc for recent queries
- GIN index on JSONB fields that are queried (e.g., meta->'tags')

## Constraints
- FK constraints for referential integrity
- NOT NULL where fields are required
- Check constraints for enums (status values)

## Data Retention & Archival
- Archive old audit logs to cold storage after configurable retention (e.g., 1 year)
- Purge process with admin-controlled policies

## Backup & Restore
- Automated daily snapshots; point-in-time recovery enabled
- Test restores periodically in staging

## Sample DDL
See `db/schema.sql` for migrations and seed data.

## Data Dictionary (sample rows)
- `users.email` — primary contact
- `clearance_requests.status` — enum: draft, submitted, in_review, approved, rejected, cancelled
- `audit_logs.details` — JSON with event-specific payload

## Next Steps
- Create migration scripts for chosen DB (EF Core/TypeORM migrations)
- Define OLAP schema and ETL pipelines
