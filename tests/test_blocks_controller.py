def test_blocks_by_floor_and_get(http, base_url, ensure_floor, ensure_block):
    r = http.get(f"{base_url}/api/blocks/by-floor/{ensure_floor}", timeout=10)
    assert r.status_code == 200
    blocks = r.json()
    assert isinstance(blocks, list)

    r2 = http.get(f"{base_url}/api/blocks/{ensure_block}", timeout=10)
    assert r2.status_code == 200
    data = r2.json()
    assert data["id"] == ensure_block
