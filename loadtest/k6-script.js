// Load test cơ bản cho các API đọc dữ liệu phổ biến nhất của TaskMgmt (mục 4.6).
// Chạy: docker run --rm -i -v "<repo>/loadtest:/scripts" grafana/k6 run /scripts/k6-script.js
//
// Chỉ test các endpoint ĐỌC (list/detail/dashboard/notifications) - đây là loại traffic
// chiếm đa số trong thực tế sử dụng của app quản lý công việc. Đăng nhập 1 lần duy nhất ở
// setup() để lấy token dùng chung cho mọi VU, tránh đụng rate-limit của /auth/login (10
// request/phút/IP, xem mục 4.3) vốn được thiết kế để chặn brute-force chứ không phải cho
// load test.
import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://host.docker.internal:5299/api/v1';
const EMAIL = __ENV.LOAD_TEST_EMAIL || 'test2@k-app.local';
const PASSWORD = __ENV.LOAD_TEST_PASSWORD || 'Test1234';

export const options = {
  scenarios: {
    read_traffic: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '10s', target: 20 },
        { duration: '30s', target: 20 },
        { duration: '10s', target: 0 },
      ],
    },
  },
  thresholds: {
    // Chỉ tiêu chính của mục 4.6: P95 toàn bộ request phải dưới 300ms.
    http_req_duration: ['p(95)<300'],
    http_req_failed: ['rate<0.01'],
  },
};

export function setup() {
  const loginRes = http.post(
    `${BASE_URL}/auth/login`,
    JSON.stringify({ email: EMAIL, password: PASSWORD }),
    { headers: { 'Content-Type': 'application/json' } },
  );

  if (loginRes.status !== 200) {
    throw new Error(`Setup login failed: ${loginRes.status} ${loginRes.body}`);
  }

  const token = loginRes.json('accessToken');

  const tasksRes = http.get(`${BASE_URL}/worktasks?pageSize=1`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  const firstTaskId = tasksRes.json('items.0.id');

  return { token, firstTaskId };
}

export default function (data) {
  const headers = { Authorization: `Bearer ${data.token}` };

  const responses = http.batch([
    ['GET', `${BASE_URL}/worktasks?pageSize=20`, null, { headers, tags: { name: 'GetWorkTasks' } }],
    ['GET', `${BASE_URL}/dashboard/stats`, null, { headers, tags: { name: 'GetDashboardStats' } }],
    ['GET', `${BASE_URL}/notifications?pageSize=20`, null, { headers, tags: { name: 'GetNotifications' } }],
    ['GET', `${BASE_URL}/notifications/unread-count`, null, { headers, tags: { name: 'GetUnreadCount' } }],
    ...(data.firstTaskId
      ? [['GET', `${BASE_URL}/worktasks/${data.firstTaskId}`, null, { headers, tags: { name: 'GetWorkTaskById' } }]]
      : []),
  ]);

  for (const res of responses) {
    check(res, { 'status is 200': (r) => r.status === 200 });
  }

  sleep(1);
}
