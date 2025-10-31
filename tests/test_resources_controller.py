def test_resources_get_and_availability(http, base_url, ensure_block, ensure_resource):
    # by-block
    r = http.get(f"{base_url}/api/resources/by-block/{ensure_block}", timeout=10)
    assert r.status_code == 200
    assert isinstance(r.json(), list)

    # get one
    r2 = http.get(f"{base_url}/api/resources/{ensure_resource}", timeout=10)
    assert r2.status_code == 200
    res = r2.json()
    assert res["id"] == ensure_resource

    # availability
    r3 = http.get(
        f"{base_url}/api/resources/{ensure_resource}/availability",
        params={"date": "2030-01-01"},
        timeout=10,
    )
    assert r3.status_code == 200
    avail = r3.json()
    assert avail["ResourceId"] == ensure_resource

    # negative: not found
    r4 = http.get(f"{base_url}/api/resources/999999", timeout=10)
    assert r4.status_code == 404
