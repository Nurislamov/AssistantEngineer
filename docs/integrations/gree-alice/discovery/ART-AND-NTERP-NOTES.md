# ART And Nterp Notes

This document is project-specific. It is not a general Android exploitation guide.

## jmethodID versus ArtMethod

v1.0.44c confirmed eight non-null JNI method IDs. v1.0.48 later showed that the best plausible register at `ExecuteNterpImpl` entry was `x0`, but exact matches against those JNI method ID pointers were zero.

Confirmed: `x0` is ArtMethod-like in this lab.

Not confirmed: `x0` equals the JNI `jmethodID` value.

## Slot +16

v1.0.46 showed slot `+16` was executable-looking, stable, and unique across the eight target methods.

v1.0.47a corrected the interpretation: slot `+16` is likely CodeItem-like data, not a native ARM64 entrypoint. It has structurally plausible DEX CodeItem fields for the target methods.

Consequence: do not place a native Interceptor directly on slot `+16`.

## Slot +24

v1.0.46 showed slot `+24` was executable, stable, and shared across all eight methods.

v1.0.47a associated this shared slot with `libart.so` `ExecuteNterpImpl`.

Consequence: shared nterp entry observation is possible, but target method correlation requires safe discrimination.

## CodeItem-like structure

The CodeItem observation is structural: registers size, input size, output size, debug info offset, and instruction size are plausible. This supports the conclusion that slot `+16` is method code metadata.

It does not by itself prove a runtime hit for any specific target method.

## ExecuteNterpImpl

v1.0.48 installed one exact native Interceptor on the shared `ExecuteNterpImpl` address and captured seven stub hits. It detached the listener cleanly and exported only safe counters and structural checks.

## x0

v1.0.48 result:

- Total stub hits: 7.
- `x0` plausible ArtMethod hits: 7.
- Other registers plausible: 0.
- Exact method pointer matches: 0.
- Exact CodeItem pointer matches: 0.

This confirms `x0` as a candidate ArtMethod register, not a target match.

## Why direct pointer equality did not work

The evidence indicates two failed equality assumptions:

- `x0 == jmethodID` is not supported.
- Direct CodeItem equality through live dereference is unsafe in the hot callback and did not complete.

## Why live dereference crashed or detached

v1.0.49a attempted CodeItem correlation based on `*(x0 + 16)`. The run ended with process termination before target gate completion. The task context records a bad access caused by invalid address. Therefore live dereference inside the hot `ExecuteNterpImpl` callback is treated as unsafe for this target.

## Deferred classification concept

The intended v1.0.49b design was:

- do not read `*(x0 + 16)` inside the Interceptor callback;
- collect bounded candidates only;
- detach listener;
- classify after detach;
- do not export raw pointer values.

The concept remains the safer direction, but the provided v1.0.49b report does not validate it because the process terminated before hook-ready and before any candidate capture.

## Proven versus hypothesis

Proven:

- Eight MethodChannel/FlutterJNI method IDs exist after bypass.
- Slot `+16` is CodeItem-like, not a native hook target.
- Slot `+24` is a shared `ExecuteNterpImpl`-like ART stub.
- `x0` is the best plausible ArtMethod register at the shared stub in v1.0.48.

Hypothesis:

- A future deferred candidate classifier can safely map `x0` candidates back to target CodeItems after detach.

Invalidated:

- Native Interceptor directly on slot `+16`.
- Treating `x0` as directly equal to JNI method IDs.
- Treating v1.0.49b as a successful deferred-classification pass.
