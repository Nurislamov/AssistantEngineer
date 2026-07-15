# MethodChannel Entrypoints

These eight methods were confirmed by v1.0.44c direct JNI lookup after bypass. The evidence confirms class/method/signature availability and non-null method IDs. It does not prove business meaning, payload content, or runtime hits for these methods.

Source:

```text
D:\AssistantEngineer-live-evidence\GREE-ALICE-API-DISCOVERY\03-android-lab\post-bypass-direct-jni-methodchannel-gate\run-20260715-144607\sanitized-report\result.json
```

Summary SHA256: `DF9D8FCC39E304920F5AC410EF661640EC5B494EC90AB0ACD290810C929321FB`.

| Class | Method | Signature | Static | Java/JNI discovery source | ArtMethod/CodeItem status | Runtime hit status | Safety restrictions |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `t7.l` | `c` | `(Ljava/lang/String;Ljava/lang/Object;)V` | false | v1.0.44c direct JNI | v1.0.46 slot `+16` unique; v1.0.47a CodeItem-like; slot `+24` shared `ExecuteNterpImpl` | Not confirmed | Do not read argument object fields or payloads. |
| `t7.l` | `d` | `(Ljava/lang/String;Ljava/lang/Object;Lt7/l$d;)V` | false | v1.0.44c direct JNI | Same as above | Not confirmed | Do not infer callback semantics beyond signature. |
| `t7.l` | `e` | `(Lt7/l$c;)V` | false | v1.0.44c direct JNI | Same as above | Not confirmed | No Java method hook was installed in v1.0.44c. |
| `l7.a` | `e` | `(Ljava/lang/String;Ljava/nio/ByteBuffer;)V` | false | v1.0.44c direct JNI | Same as above | Not confirmed | Raw ByteBuffer reads are forbidden. |
| `l7.a` | `l` | `(Ljava/lang/String;Ljava/nio/ByteBuffer;Lt7/d$b;)V` | false | v1.0.44c direct JNI | Same as above | Not confirmed | Raw ByteBuffer reads and callback payload exports are forbidden. |
| `com.gree.adapter.GreeFlutterActivity` | `w` | `(Lt7/k;Lt7/l$d;)V` | false | v1.0.44c direct JNI | Same as above | Not confirmed | Do not infer HVAC command behavior from this method name. |
| `com.gree.adapter.GreeFlutterActivity` | `t` | `(Lcom/gree/adapter/GreeFlutterActivity;Lt7/k;Lt7/l$d;)V` | true | v1.0.44c direct JNI | Same as above | Not confirmed | Static method only; no business semantics inferred. |
| `io.flutter.embedding.engine.FlutterJNI` | `handlePlatformMessage` | `(Ljava/lang/String;Ljava/nio/ByteBuffer;IJ)V` | false | v1.0.44c direct JNI | Same as above | Not confirmed | Channel/method identifiers only; no raw ByteBuffer or payload decoding. |

## What v1.0.44c proves

- Application found.
- Application ClassLoader found.
- 4/4 classes found.
- 8/8 method IDs found.
- Leak check PASS.
- No Java method hooks, native Interceptor hooks, raw ByteBuffer reads, payload exports, network hooks, or HVAC commands.

## What remains unproved

- Whether any of these exact methods fire during a tested UI interaction.
- Which method maps to a specific cloud or device action.
- Whether a given method carries read-only status, control, auth, or unrelated internal traffic.
