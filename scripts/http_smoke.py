"""Read-only HTTP smoke test: pages answer, assets are served, private pages redirect.

Interactive pages are rendered in the browser (no prerendering), so this script checks the
application shell; scripts/browser-audit.mjs checks what users actually see and do.
Usage: python3 scripts/http_smoke.py [base-url]
"""
import sys
import urllib.request

base = sys.argv[1] if len(sys.argv) > 1 else 'http://localhost:5180'

def get(path):
    with urllib.request.urlopen(base + path, timeout=20) as response:
        return response.status, response.url, response.read().decode(errors='replace')

pages = ['/', '/plan', '/commute', '/compare', '/about', '/homes/1', '/complex/1', '/area?lat=51.09&lon=71.42',
         '/catalog', '/apartment/1']  # old links still open the new pages
for path in pages:
    status, _, html = get(path)
    assert status == 200 and 'blazor.web' in html and 'Loading your plan' in html, f'Failed: {path}'
    print('PASS page', path)

for path, marker in {'/Account/Login': 'Log in', '/Account/Register': 'Register'}.items():
    status, _, html = get(path)
    assert status == 200 and marker in html, f'Failed: {path}'
    print('PASS account page', path)

for path in ['/js/map.js', '/js/dwellio.js', '/lib/leaflet/leaflet.js', '/lib/leaflet/leaflet.css', '/app.css']:
    status, _, body = get(path)
    assert status == 200 and len(body) > 100, f'Missing asset: {path}'
    print('PASS asset', path)

for path in ['/owner', '/admin', '/favorites']:
    _, url, _ = get(path)
    assert '/Account/Login' in url, f'Anonymous access was not redirected: {path}'
    print('PASS anonymous redirect', path)
