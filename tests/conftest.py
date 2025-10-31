import os
import time
from datetime import datetime, timedelta
from typing import Dict, Any

import pytest
import requests


# --- Basic config ---
DEFAULT_BASE_URL = os.environ.get("RESOURCELY_BASE_URL", "http://localhost:5130")


@pytest.fixture(scope="session")
def base_url() -> str:
    return DEFAULT_BASE_URL.rstrip("/")


@pytest.fixture(scope="session")
def http():
    """Session-wide requests session for connection reuse."""
    s = requests.Session()
    yield s
    s.close()


@pytest.fixture(scope="session", autouse=True)
def wait_for_server(base_url):
    """Wait briefly until the ASP.NET API responds to a simple GET (Swagger/health alternative).

    We ping a lightweight endpoint that should exist in most templates, else fall back to /.
    """
    timeout_s = 20
    start = time.time()
    last_err = None
    probe_paths = ["/swagger", "/api/buildings", "/"]
    while time.time() - start < timeout_s:
        for path in probe_paths:
            try:
                resp = requests.get(base_url + path, timeout=2)
                if resp.status_code < 500:
                    return
            except Exception as e:
                last_err = e
        time.sleep(0.5)
    if last_err:
        pytest.skip(f"Backend not reachable at {base_url}: {last_err}")
    else:
        pytest.skip(f"Backend not reachable at {base_url} within {timeout_s}s")


# --- Auth helpers (simple app: register/login returns user data; no JWT yet) ---

@pytest.fixture(scope="session")
def test_user_credentials() -> Dict[str, str]:
    return {
        "email": f"pytest.user+{int(time.time())}@example.com",
        "password": "pytest-Password1!",
        "username": "pytest_user",
    }


@pytest.fixture(scope="session")
def registered_user(http, base_url, test_user_credentials) -> Dict[str, Any]:
    # Try register; if already exists, ignore
    r = http.post(
        f"{base_url}/api/auth/register",
        json={
            "email": test_user_credentials["email"],
            "password": test_user_credentials["password"],
            "username": test_user_credentials["username"],
        },
        timeout=10,
    )
    # Accept 200 OK or 400 (already exists)
    assert r.status_code in (200, 400), r.text
    return test_user_credentials


@pytest.fixture(scope="session")
def login_user(http, base_url, registered_user) -> Dict[str, Any]:
    r = http.post(
        f"{base_url}/api/auth/login",
        json={
            "email": registered_user["email"],
            "password": registered_user["password"],
        },
        timeout=10,
    )
    assert r.status_code == 200, r.text
    data = r.json()
    assert data.get("message") == "Login successful."
    assert "user" in data
    return data


# --- Admin data helpers via public endpoints ---

@pytest.fixture(scope="session")
def ensure_building(http, base_url) -> int:
    # Create a building for tests
    name = f"Test Building {int(time.time())}"
    r = http.post(f"{base_url}/api/buildings", json={"name": name, "description": "pytest"}, timeout=10)
    assert r.status_code in (201, 200), r.text
    try:
        return r.json().get("id")
    except Exception:
        # fallback: list and pick the last
        lr = http.get(f"{base_url}/api/buildings", timeout=10)
        assert lr.status_code == 200
        buildings = lr.json()
        assert isinstance(buildings, list)
        return buildings[-1]["id"]


@pytest.fixture(scope="session")
def ensure_floor(http, base_url, ensure_building) -> int:
    name = f"Floor {int(time.time())}"
    r = http.post(
        f"{base_url}/api/floors",
        json={"name": name, "description": "pytest", "buildingId": ensure_building},
        timeout=10,
    )
    assert r.status_code in (201, 200), r.text
    return r.json()["id"]


@pytest.fixture(scope="session")
def ensure_block(http, base_url, ensure_floor) -> int:
    r = http.post(
        f"{base_url}/api/blocks",
        json={"name": f"Block {int(time.time())}", "description": "pytest", "floorId": ensure_floor},
        timeout=10,
    )
    assert r.status_code in (201, 200), r.text
    return r.json()["id"]


@pytest.fixture(scope="session")
def ensure_resource(http, base_url, ensure_block) -> int:
    r = http.post(
        f"{base_url}/api/resources",
        json={
            "name": f"Room {int(time.time())}",
            "type": "Room",
            "description": "pytest",
            "capacity": 10,
            "blockId": ensure_block,
            "isRestricted": False,
        },
        timeout=10,
    )
    assert r.status_code in (201, 200), r.text
    return r.json()["id"]


def make_timeslot(minutes_from_now: int = 30, duration_min: int = 60):
    start = datetime.utcnow() + timedelta(minutes=minutes_from_now)
    end = start + timedelta(minutes=duration_min)
    return {
        "date": start.strftime("%Y-%m-%d"),
        "time": start.strftime("%H:%M"),
        "endTime": end.strftime("%H:%M"),
        "start": start,
        "end": end,
    }
