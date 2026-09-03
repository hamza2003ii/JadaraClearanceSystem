# CI / Secrets & Local Staging Setup

This document explains the secrets and local test setup used by the CI workflows added earlier.

Required GitHub Secrets
- `STAGING_URL` — Public URL of the staging deployment used by DAST (OWASP ZAP) scans, e.g. `https://staging.example.com`.
- `DOCKERHUB_USERNAME` and `DOCKERHUB_TOKEN` (or registry credentials) — for pushing images from CI.
- `AZURE_CREDENTIALS` or `GCP_SERVICE_ACCOUNT` — for cloud deploy steps if used.
- `DB_CONNECTION_STRING` (optional) — connection string used by CI integration tests when not using `docker-compose`.
- `SNYK_TOKEN` / `TRIVY_TOKEN` (optional) — if using external scanning services.

Local environment notes
- A simple local staging can be started with `docker-compose.test.yml` at repo root.
- Example command to run locally (from repository root):

```bash
# Start dependent services
docker compose -f docker-compose.test.yml up -d --build

# (Optional) Build and run the app in a container (requires Dockerfile)
# docker compose -f docker-compose.test.yml up --build app
```

Environment variables for local run
- For local run without containers, set the following in your shell or a `.env.test` file:

```
ASPNETCORE_ENVIRONMENT=Testing
ConnectionStrings__DefaultConnection=Server=localhost,1433;Database=JadaraClearance;User Id=sa;Password=Your_strong!Passw0rd;
STAGING_URL=http://localhost:5000
```

CI tips
- `STAGING_URL` should point to an environment reachable by the GitHub Actions runners when running DAST. If you run the app in GitHub Actions, expose it using `ngrok` or deploy to a temporary staging host.
- Keep secret values small and rotate regularly.
- Limit who can modify repository secrets in GitHub settings.

Security reminder
- Never commit real credentials into the repository. Use GitHub Secrets or your CI's secure secret store.

