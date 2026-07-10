# Local Bridge Operator Smoke Checklist

Operator smoke status: NOT APPROVED

Use this checklist manually. Mark PASS only after local/offline evidence is collected and masked.

- [ ] Repository commit recorded
- [ ] Working tree clean before smoke
- [ ] dotnet restore PASS
- [ ] dotnet build PASS
- [ ] dotnet test PASS
- [ ] git diff --check PASS
- [ ] Static safety scans PASS
- [ ] Local smoke harness PASS
- [ ] Provider readiness remains NOT READY
- [ ] Production pilot remains NOT APPROVED
- [ ] No real Yandex calls performed
- [ ] No real OAuth used
- [ ] No real credentials/tokens used
- [ ] No live Gree+ Cloud calls performed
- [ ] No MQTT performed
- [ ] No device control performed
- [ ] No production deployment performed
- [ ] /health offline check PASS, if API is run locally
- [ ] /devices offline check PASS, if API is run locally
- [ ] /query offline check PASS, if API is run locally
- [ ] /action fail-closed check PASS, if API is run locally
- [ ] /unlink offline check PASS, if API is run locally
- [ ] HTTP smoke mode uses only http://localhost:<local-port> or http://127.0.0.1:<local-port>
- [ ] HTTP smoke rejects public hosts, https, production endpoints, Yandex/Gree domains, OAuth endpoints, and MQTT endpoints
- [ ] Unknown user fail-closed check PASS
- [ ] Unknown device fail-closed check PASS
- [ ] VRF child exposure check PASS
- [ ] Gateway hidden check PASS
- [ ] Evidence masking checked
- [ ] Evidence saved outside repository or in ignored local path
- [ ] Final operator decision

Final operator decision:

```text
NOT APPROVED
```
