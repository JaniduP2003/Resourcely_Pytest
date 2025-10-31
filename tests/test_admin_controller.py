import pytest


def test_admin_overview(http, base_url):
    r = http.get(f"{base_url}/api/admin/overview", timeout=10)
    # IsAdmin() currently returns true in code, so this should be accessible
    assert r.status_code in (200, 403)
    if r.status_code == 200:
        data = r.json()
        assert "totalBookings" in data


def test_admin_pending_approved_rejected_lists(http, base_url):
    for path in ("pending", "approved", "rejected"):
        r = http.get(f"{base_url}/api/admin/bookings/{path}", timeout=10)
        assert r.status_code in (200, 403)
        if r.status_code == 200:
            assert isinstance(r.json(), list)
