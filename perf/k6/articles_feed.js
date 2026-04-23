import http from "k6/http";
import { check, sleep } from "k6";

// Навантаження стрічки статей з пагінацією
export const options = {
  stages: [
    { duration: "20s", target: 10 },
    { duration: "40s", target: 25 },
    { duration: "20s", target: 0 },
  ],
  thresholds: {
    http_req_duration: ["p(95)<800"],
    http_req_failed: ["rate<0.02"],
  },
};

const base = __ENV.BASE_URL || "http://localhost:8080";

export default function () {
  const page = Math.floor(Math.random() * 30) + 1;
  const res = http.get(
    `${base}/api/articles?page=${page}&pageSize=20`
  );
  check(res, {
    "статус 200": (r) => r.status === 200,
  });
  sleep(0.05);
}
