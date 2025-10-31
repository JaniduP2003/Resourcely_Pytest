# PyTest API Tests for Backend-Resourcely

This folder adds a small, real-HTTP PyTest suite to exercise your ASP.NET backend (Backend-Resourcely) via requests.

## Why PyTest
- Simple fixtures and parametrization for clean tests
- Huge plugin ecosystem, CI-friendly output and reports
- Fast, readable, minimal boilerplate

## Prereqs
- Python 3.10+
- .NET SDK installed and your backend can run with `dotnet run`

## Install test dependencies

```bash
python -m venv .venv
source .venv/bin/activate  # on Linux/macOS
pip install -r requirements.txt
```

## Start the backend
In one terminal:
```bash
cd Backend-Resourcely/Backend-Resourcely
ASPNETCORE_ENVIRONMENT=Development dotnet run
```
By default the app exposes `http://localhost:5130` (see `Properties/launchSettings.json`). If your app runs on a different port, export the base URL for tests:
```bash
export RESOURCELY_BASE_URL=http://localhost:5000
```

## Run tests
In another terminal at repo root:
```bash
pytest tests -v
```

You should see clear pass/fail output. Example (truncated):

```
collected 12 items

tests/test_auth_controller.py::test_register_and_login_flow PASSED
tests/test_auth_controller.py::test_login_negative_cases[email-empty] PASSED
tests/test_user_controller.py::test_get_user_stats PASSED
...
```

## What the tests do
- Auth: registers a random test user and logs in (no JWT required in current backend)
- Structure: creates building/floor/block/resource, fetches lists and details
- Bookings: creates a booking, fetches by id, lists pending, checks basic overlap behavior, and deletion rules
- Admin: reads overview/pending/approved/rejected (allowed since `IsAdmin()` is stubbed to true)
- Department: lists and creates departments

## Core PyTest features shown
- Fixtures: `base_url`, `http` session, `registered_user`, and hierarchy builders live in `tests/conftest.py`
- Parametrize: negative login cases, role updates
- Assertions: status codes and selected JSON keys/values
- Reporting: `-v` increases verbosity; add `-q` for quiet; integrate with CI easily

## Troubleshooting
- If tests skip with "Backend not reachable", ensure your .NET app is listening at the URL in `RESOURCELY_BASE_URL` (default `http://localhost:5130`).
- If your backend requires a real DB, make sure it's available and configured via `appsettings.json`.
- If certain endpoints are protected in your environment, adjust or export a token fixture and add headers in tests.

## Optional: HTML report
Install plugin and run:
```bash
pip install pytest-html
pytest tests -v --html=report.html --self-contained-html
```

