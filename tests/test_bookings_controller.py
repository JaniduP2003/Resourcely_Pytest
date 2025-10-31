import pytest
from datetime import datetime, timedelta


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


def create_booking(http, base_url, resource_id, minutes_from_now=40, duration_min=60):
    ts = make_timeslot(minutes_from_now=minutes_from_now, duration_min=duration_min)
    payload = {
        "resourceId": resource_id,
        "date": ts["date"],
        "time": ts["time"],
        "endTime": ts["endTime"],
        "reason": "pytest meeting",
        "capacity": 2,
        "contact": "+1234567890",
    }
    r = http.post(f"{base_url}/api/bookings", json=payload, timeout=10)
    return r


def test_create_and_get_booking(http, base_url, ensure_resource):
    r = create_booking(http, base_url, ensure_resource)
    assert r.status_code == 201, r.text
    body = r.json()
    booking_id = body.get("id") or body.get("Id")
    assert booking_id is not None

    # Get by id
    g = http.get(f"{base_url}/api/bookings/{booking_id}", timeout=10)
    assert g.status_code == 200
    details = g.json()
    assert details["id"] == booking_id
    assert details["status"] in ("Pending", "Approved", "Rejected")


def test_pending_list_and_overlap_negative(http, base_url, ensure_resource):
    # Create a booking
    r = create_booking(http, base_url, ensure_resource, minutes_from_now=90, duration_min=45)
    assert r.status_code == 201
    created = r.json()

    # Pending list
    p = http.get(f"{base_url}/api/bookings/pending", timeout=10)
    assert p.status_code == 200
    assert isinstance(p.json(), list)

    # Create overlapping booking (should be 400)
    # Use same times as previous
    payload = {
        "resourceId": ensure_resource,
        "date": created["bookingAt"][:10],
        "time": created["bookingAt"][11:16],
        "endTime": created["endAt"][11:16],
        "reason": "overlap attempt",
        "capacity": 2,
        "contact": "+1234567890",
    }
    r2 = http.post(f"{base_url}/api/bookings", json=payload, timeout=10)
    assert r2.status_code in (400, 409)


def test_delete_booking_rules(http, base_url, ensure_resource):
    # Create a future booking so deletion is allowed
    r = create_booking(http, base_url, ensure_resource, minutes_from_now=180, duration_min=30)
    assert r.status_code == 201
    bid = r.json()["id"]

    d = http.delete(f"{base_url}/api/bookings/{bid}", timeout=10)
    # Endpoint returns 204 NoContent on success, or 400 for past bookings
    assert d.status_code in (204, 200)
