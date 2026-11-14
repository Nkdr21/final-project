"""Read-only HTTP smoke tests; not a replacement for browser interaction tests."""
import urllib.request
import sys
base = sys.argv[1] if len(sys.argv) > 1 else 'http://localhost:5180'
checks = {'/market': 'Real complexes. Published rental prices.', '/': 'Find a home that fits your life.', '/catalog': 'Apartments in Astana',
          '/preferences': 'What makes a home yours', '/complexes': 'neighbourhood',
          '/complex/1': 'Inside the complex', '/apartment/1': 'MONTHLY RENT',
          '/compare': 'Three places', '/about': 'Behind your matches',
          '/Account/Login': 'Log in', '/Account/Register': 'Register'}
for path, marker in checks.items():
    with urllib.request.urlopen(base + path, timeout=20) as response:
        html = response.read().decode()
        assert response.status == 200 and marker in html, f'Failed: {path}'
        print('PASS', path)
for path in ['/owner', '/admin', '/favorites']:
    with urllib.request.urlopen(base + path, timeout=20) as response:
        assert '/Account/Login' in response.url, f'Anonymous access was not redirected: {path}'
        print('PASS anonymous redirect', path)
