# GREE-ALICE Internal Offline RC Checklist

RC status: NOT CUT

Use this checklist as the manual gate before cutting an internal/offline release candidate.

- [ ] Repository branch recorded
- [ ] Repository commit recorded
- [ ] Full validation PASS
- [ ] GreeAlice tests PASS
- [ ] Local smoke harness PASS
- [ ] Local HTTP smoke PASS if local API available
- [ ] PROJECT_STATE updated
- [ ] Release readiness audit reviewed
- [ ] No production claims
- [ ] No real Yandex calls
- [ ] No real OAuth credentials
- [ ] No real tokens
- [ ] No live Gree+ Cloud
- [ ] No MQTT
- [ ] No device control
- [ ] No production deployment
- [ ] Docs reviewed
- [ ] Evidence masked
- [ ] Final RC decision

Final RC decision:

```text
NOT CUT
```
