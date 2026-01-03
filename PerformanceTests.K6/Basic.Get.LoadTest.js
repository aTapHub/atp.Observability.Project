import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    vus: 5,
    duration: '30s',

    thresholds: {
        'http_req_duration': ['p(99) < 200'],
        'checks': ['rate==1.00'],
    },
};

export default function () {
    const BASE_URL = 'http://localhost';

    let res = http.get(BASE_URL + '/');

    check(res, {
        'GET status is 200': (r) => r.status === 200,
        'Response body has 5 or more initial posts': (r) => {
            try {
                
                const posts = JSON.parse(r.body);
                return posts.length >= 5;
            } catch (e) {
                return false; 
            }
        },
    });

    
    sleep(0.5); 
}