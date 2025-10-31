def test_departments_list_and_create(http, base_url):
    # List
    r = http.get(f"{base_url}/api/department", timeout=10)
    assert r.status_code == 200
    assert isinstance(r.json(), list)

    # Create
    r2 = http.post(
        f"{base_url}/api/department",
        json={"name": "Computer Science", "description": "pytest"},
        timeout=10,
    )
    assert r2.status_code in (201, 200), r2.text
    dep = r2.json()
    assert dep["name"].lower() == "computer science"
