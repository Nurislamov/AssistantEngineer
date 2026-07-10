# Gree Plus read-only live evidence summary template

## Capture

```text
Capture name:
Capture date:
Gree Plus version:
Android device:
Scenario:
Read-only proof:
```

## Region/server

```text
apiHost:
serverId:
status host:
```

## Auth/session evidence

```text
endpoint:
method:
required headers:
request body shape:
response envelope:
```

## Homes/devices evidence

```text
endpoint:
method:
request body shape:
response envelope:
```

## Status read evidence

```text
endpoint or callback:
method/transport:
request/subscription shape:
response/status envelope:
```

## Observed safe status fields

```text
Pow:
Mod:
SetTem:
TemUn:
WdSpd:
Quiet:
Tur:
SwUpDn:
SwingLfRig:
AllErr:
deviceState:
status:
```

## Negative evidence

```text
sendDataToDevice observed: no/yes
cmd/action/control observed: no/yes
MQTT publish observed: no/yes
write-like SetTem/Pow payload observed: no/yes
command payload sent observed: no/yes
```

## Redaction checklist

```text
email redacted: yes/no
uid/user id redacted: yes/no
homeId redacted: yes/no
deviceId redacted: yes/no
deviceid query redacted: yes/no
mac redacted: yes/no
access_token redacted: yes/no
refresh_token redacted: yes/no
Authorization header redacted: yes/no
cookie/session redacted: yes/no
phone redacted: yes/no
account name redacted: yes/no
local IP redacted: yes/no
task/window token redacted: yes/no
analytics user_id pValue redacted: yes/no
analytics appliance name pValue redacted: yes/no
raw logs/screenshots/PCAPs excluded: yes/no
extractor run on redacted input only: yes/no
focused extractor run on redacted input only: yes/no
generated extract files kept outside Git: yes/no
Android/Samsung noise rejected from focused evidence: yes/no
```

## Conclusion

```text
Contract status: unknown/partial/confirmed-read-only
Status callback shape confirmed: yes/no
Exact HTTP live read contract confirmed: yes/no
Remaining gaps:
Reviewer:
Review date:
```
