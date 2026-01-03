import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter } from 'k6/metrics';

// Custom metric to track successful POST requests
const postSuccesses = new Counter('post_successes');

// 1. Configuration: Higher load to stress the database
export const options = {
    // Stage 1: Ramp up to 20 Virtual Users (VUs) over 15s
    // Stage 2: Hold 20 VUs for 45s (The main test duration)
    stages: [
        { duration: '15s', target: 20 }, 
        { duration: '45s', target: 20 },
        { duration: '10s', target: 0 }, 
    ],
    // SLOs
    thresholds: {
        // Assert that 95% of all requests must complete in under 300ms
        'http_req_duration': ['p(95) < 300'], 
        // Assert that 99% of requests must succeed (status 2xx)
        'checks': ['rate>0.99'], 
    },
};

// 2. The Workflow Model
export default function () {
    const BASE_URL = 'http://localhost';

    // --- A. READ Operation (High Frequency) ---
    // Every VU performs this action in every iteration.
    let getRes = http.get(BASE_URL + '/');
    check(getRes, {
        'GET status is 200': (r) => r.status === 200,
        'GET response is valid JSON': (r) => {
             try { JSON.parse(r.body); return true; } catch(e) { return false; }
        },
    });

    // --- B. WRITE Operation (Low Frequency) ---
    // Use the k6 __VU (Virtual User ID) and __ITER (Iteration Count) variables 
    // to make the post data unique and to control the frequency.
    
    // Only 1 out of every 5 VUs will attempt a POST in a given iteration
    if (__VU % 5 === 0) { 
        const payload = JSON.stringify({
            // Note: ID is ignored by the API but required for the C# model
            id: 0, 
            // Unique title for easier tracking in the database
            title: `Scenario Post VU ${__VU} Iteration ${__ITER}`,
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
    // Wait for 1 second before the VU starts its next iteration.
    sleep(1);
}