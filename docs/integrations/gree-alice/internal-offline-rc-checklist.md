# GREE-ALICE Internal Offline RC Checklist

RC status: CUT LOCALLY / PUSH PENDING

RC name: GREE-ALICE-RC1
Base commit: b60fb382
Branch: master
Scope: internal/offline engineering release candidate
Production Yandex release status: NOT READY

Local HTTP smoke: PASS on RC1 at http://localhost:5005.

Use this checklist as the manual gate before cutting an internal/offline release candidate.

- [x] Repository branch recorded
- [x] Repository commit recorded
- [x] Full validation PASS
- [x] GreeAlice tests PASS
- [x] Local smoke harness PASS
- [x] Local HTTP smoke PASS if local API available
- [x] PROJECT_STATE updated
- [x] Release readiness audit reviewed
- [x] No production claims
- [x] No real Yandex calls
- [x] No real OAuth credentials
- [x] No real tokens
- [x] No live Gree+ Cloud
- [x] No MQTT
- [x] No device control
- [x] No production deployment
- [x] Docs reviewed
- [x] Evidence masked
- [x] Final RC decision

Final RC decision:

```text
CUT LOCALLY / PUSH PENDING
```
