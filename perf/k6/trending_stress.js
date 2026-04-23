import http from "k6/http";
import { check, sleep } from "k6";

// Стрес по ендпоінту трендів
export const options = {
  stages: [
    { duration: "20s", target: 40 },
    { duration: "40s", target: 120 },
    { duration: "20s", target: 0 },
  ],
  thresholds: {
    http_req_duration: ["p(95)<2000"],
    http_req_failed: ["rate<0.05"],
  },
};

const base = __ENV.BASE_URL || "http://localhost:8080";

export default function () {
  const res = http.get(`${base}/api/articles/trending`);
  check(res, {
    "тренди 200": (r) => r.status === 200,
  });
  sleep(0.02);
}
