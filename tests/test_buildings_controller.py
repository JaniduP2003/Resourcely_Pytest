def test_buildings_create_and_get(http, base_url):
    r = http.post(
        f"{base_url}/api/buildings",
        json={"name": "Test Hall", "description": "pytest"},
        timeout=10,
    )
    assert r.status_code in (201, 200), r.text
    b = r.json()
    bid = b["id"]

    r2 = http.get(f"{base_url}/api/buildings", timeout=10)
    assert r2.status_code == 200
    assert isinstance(r2.json(), list)

    r3 = http.get(f"{base_url}/api/buildings/{bid}", timeout=10)
    assert r3.status_code == 200
    assert r3.json()["id"] == bid
