import pytest


def test_buildings_crud_minimal(http, base_url):
    # Create
    r = http.post(f"{base_url}/api/buildings", json={"name": "Test Hall", "description": "pytest"}, timeout=10)
    assert r.status_code in (201, 200), r.text
    b = r.json()
    assert b["name"]

    # List
    r2 = http.get(f"{base_url}/api/buildings", timeout=10)
    assert r2.status_code == 200
    assert isinstance(r2.json(), list)

    # Get
    rid = b["id"]
    r3 = http.get(f"{base_url}/api/buildings/{rid}", timeout=10)
    assert r3.status_code == 200
    assert r3.json()["id"] == rid


def test_floors_blocks_resources_flow(http, base_url, ensure_building, ensure_floor, ensure_block, ensure_resource):
    # Floors by building
    r = http.get(f"{base_url}/api/floors/by-building/{ensure_building}", timeout=10)
    assert r.status_code == 200
    assert isinstance(r.json(), list)

    # Blocks by floor
    r2 = http.get(f"{base_url}/api/blocks/by-floor/{ensure_floor}", timeout=10)
    assert r2.status_code == 200
    blocks = r2.json()
    assert isinstance(blocks, list)

    # Block details
    if blocks:
        blk_id = blocks[0]["id"]
        r3 = http.get(f"{base_url}/api/blocks/{blk_id}", timeout=10)
        assert r3.status_code == 200

    # Resource get
    r4 = http.get(f"{base_url}/api/resources/{ensure_resource}", timeout=10)
    assert r4.status_code == 200
    res = r4.json()
    assert res["id"] == ensure_resource

    # Availability
    r5 = http.get(f"{base_url}/api/resources/{ensure_resource}/availability", params={"date": "2030-01-01"}, timeout=10)
    assert r5.status_code == 200
    avail = r5.json()
    assert avail["ResourceId"] == ensure_resource


def test_negative_get_missing_resource(http, base_url):
    r = http.get(f"{base_url}/api/resources/999999", timeout=10)
    assert r.status_code == 404
