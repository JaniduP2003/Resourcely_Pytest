import time
import pytest


def test_register_and_login_flow(http, base_url, test_user_credentials):
    # Register
    r = http.post(
        f"{base_url}/api/auth/register",
        json={
            "email": test_user_credentials["email"],
            "password": test_user_credentials["password"],
            "username": test_user_credentials["username"],
        },
        timeout=10,
    )
    assert r.status_code in (200, 400)

    # Login
    r2 = http.post(
        f"{base_url}/api/auth/login",
        json={
            "email": test_user_credentials["email"],
            "password": test_user_credentials["password"],
        },
        timeout=10,
    )
    assert r2.status_code == 200
    body = r2.json()
    assert body.get("message") == "Login successful."
    assert "user" in body
    assert body["user"]["email"].lower() == test_user_credentials["email"].lower()


@pytest.mark.parametrize(
    "email,password,expected_status",
    [
        ("", "secret", 400),
        ("user@example.com", "", 400),
        ("nouser@example.com", "wrongpass", 401),
    ],
)
def test_login_negative_cases(http, base_url, email, password, expected_status):
    r = http.post(
        f"{base_url}/api/auth/login",
        json={"email": email, "password": password},
        timeout=10,
    )
    assert r.status_code == expected_status
