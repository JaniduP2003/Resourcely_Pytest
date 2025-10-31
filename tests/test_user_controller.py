import pytest


def test_get_user_stats(http, base_url):
    r = http.get(f"{base_url}/api/user/stats", timeout=10)
    assert r.status_code == 200
    data = r.json()
    assert {"upcomingBookings", "totalBookings", "availableRooms", "favoriteRooms"}.issubset(data.keys())


def test_get_all_users(http, base_url, login_user):
    r = http.get(f"{base_url}/api/user", timeout=10)
    assert r.status_code == 200
    assert isinstance(r.json(), list)


def test_delete_user_not_found(http, base_url):
    r = http.delete(f"{base_url}/api/user/999999", timeout=10)
    assert r.status_code in (404, 400)


@pytest.mark.parametrize("role", ["student", "lecturer"])
def test_update_user_role_and_lists(http, base_url, login_user, role):
    user_id = login_user["user"]["id"]
    # Update role
    r = http.put(f"{base_url}/api/user/{user_id}/role", json={"role": role}, timeout=10)
    # Could be 200 if allowed, 400 if admin disallowed, or 404 if user missing
    assert r.status_code in (200, 400, 404), r.text

    # Lists should be reachable regardless
    r_students = http.get(f"{base_url}/api/user/students", timeout=10)
    assert r_students.status_code == 200
    r_lecturers = http.get(f"{base_url}/api/user/lecturers", timeout=10)
    assert r_lecturers.status_code == 200
