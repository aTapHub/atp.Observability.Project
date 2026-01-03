import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter } from 'k6/metrics';

const postSuccesses = new Counter('post_successes');

// 1. Configuration: Stepping Load until failure
export const options = {
    stages: [
        { duration: '30s', target: 100 },
        { duration: '30s', target: 250 },
        { duration: '30s', target: 500 }, // NEW: Max VUs at 500
        { duration: '10s', target: 0 },
    ],

    thresholds: {
        'http_req_duration': ['p(95) < 500'], 
        'checks': ['rate>0.99'], 
    },
};

// 2. The Workflow Model (Read/Write)
export default function () {
    const BASE_URL = 'http://localhost';

    // A. READ Operation (High Frequency)
    let getRes = http.get(BASE_URL + '/');
    check(getRes, {
        'GET status is 200': (r) => r.status === 200,
    });

    // B. WRITE Operation (Low Frequency - 1 out of every 5 VUs)
    if (__VU % 5 === 0) { 
        const payload = JSON.stringify({
            id: 0, 
            title: `Breakpoint Post VU ${__VU} Iter ${__ITER}`,
        });

        const params = { headers: { 'Content-Type': 'application/json' } };

        let postRes = http.post(BASE_URL + '/blog', payload, params);

        const postCheck = check(postRes, {
            'POST status is 201 Created': (r) => r.status === 201,
        });

        if (postCheck) {
            postSuccesses.add(1);
        }
    }
    
    // 3. Pacing
    // Wait for 1 second
    sleep(1);
}