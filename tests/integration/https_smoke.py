"""TLS Compose smoke test. Uses only stdlib, explicit CA trust and test-only data."""
import http.cookiejar
import json
import pathlib
import ssl
import time
import urllib.request
import urllib.error
import uuid

root = pathlib.Path(__file__).resolve().parents[2]
credentials = json.loads((root / '.local/compose-credentials.json').read_text(encoding='utf-8-sig'))
context = ssl.create_default_context(cafile=str(root / '.local/compose-secrets/tls.crt'))
jar = http.cookiejar.CookieJar()
opener = urllib.request.build_opener(urllib.request.HTTPSHandler(context=context), urllib.request.HTTPCookieProcessor(jar))
base = 'https://127.0.0.1:8443/api/v1'
csrf = None

def request(path, body=None):
    headers = {'Content-Type': 'application/json'}
    if body is not None:
        headers['X-CSRF-TOKEN'] = csrf
        headers['Idempotency-Key'] = str(uuid.uuid4())
    data = None if body is None else json.dumps(body).encode()
    with opener.open(urllib.request.Request(base + path, data=data, headers=headers), timeout=15) as response:
        return json.load(response)

for attempt in range(30):
    try:
        if request('/health/ready')['status'] == 'ready':
            break
    except urllib.error.HTTPError as error:
        if error.code not in (502, 503):
            raise
    time.sleep(1)
else:
    raise AssertionError('HTTPS API did not become ready within 30 checks')
csrf = request('/auth/csrf')['token']
assert request('/auth/login', credentials)['authenticated']
csrf = request('/auth/csrf')['token']
session = next(cookie for cookie in jar if cookie.name == 'BerImpact.Session')
assert session.secure and session.has_nonstandard_attr('httponly')
source = 'compose-test-' + uuid.uuid4().hex
request('/assets/import', {'source': source, 'isTest': True, 'records': [dict(externalId='guid-1', name='TLS-TEST-01', kind='Computer', os='Windows 11', model='M1', location='Test', critical=False, online=True, evidence='Verified', observedAt=time.strftime('%Y-%m-%dT%H:%M:%SZ', time.gmtime()))]})
change_id = str(uuid.uuid4())
request('/changes', dict(id=change_id, title='TLS Compose test', description='Test-only smoke check', type='Custom', before={}, after={'test': True}, target={'source': source, 'isTest': True}, rollbackPlan='Restore test fixture', rollbackDifficulty=None, technicalSensitivity=None, factorEvidence='', successCriteria='Worker returns evidence', version=1))
job = request(f'/changes/{change_id}/assessments', {'scenarios': []})
for attempt in range(30):
    state = request('/jobs/' + job['id'])
    if state['status'] in ('Completed', 'Failed'):
        break
    time.sleep(1)
assert state['status'] == 'Completed', state
assessment = request('/risk/assessments/' + state['assessmentId'])
result = json.loads(assessment['resultJson'])[0]
assert result['directCount'] == 1 and result['reviewRequired']
assert request('/system/status')['totalAssets'] == 0
assert request('/system/status')['activeChanges'] == 0
print(json.dumps({'tls': 'validated with explicit CA', 'secureHttpOnlyCookie': True, 'runtimeRoleImport': 'passed', 'separateWorker': 'Completed', 'liveDashboardIsolation': 'passed', 'ruleset': assessment['rulesetVersion']}))
