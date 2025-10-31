def test_floors_by_building_and_get(http, base_url, ensure_building, ensure_floor):
    r = http.get(f"{base_url}/api/floors/by-building/{ensure_building}", timeout=10)
    assert r.status_code == 200
    floors = r.json()
    assert isinstance(floors, list)

    r2 = http.get(f"{base_url}/api/floors/{ensure_floor}", timeout=10)
    assert r2.status_code == 200
    data = r2.json()
    assert data["id"] == ensure_floor
