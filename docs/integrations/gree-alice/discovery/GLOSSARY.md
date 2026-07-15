# Glossary

`GREE+`
: The Android application under lab observation, package `com.gree.greeplus`.

`foreground PID`
: The package process selected for observer attach after preparation. It must be chosen from launcher/run evidence, not guessed from any package PID list.

`original PID`
: The first process observed during launch/preparation before replacement or protection behavior completes.

`replacement PID`
: The process that becomes relevant after the protection/preparation flow replaces or shifts execution.

`MethodChannel`
: Flutter platform channel abstraction used to send method calls between Dart and platform code. In this project, channel identifiers are safe; payloads are not exported.

`FlutterJNI`
: Flutter runtime bridge class. `handlePlatformMessage` is one of the eight confirmed target methods.

`jmethodID`
: JNI method identifier returned by direct JNI lookup. In this evidence it is not proven equal to the `x0` value at `ExecuteNterpImpl`.

`ArtMethod`
: Android ART internal method representation. v1.0.48 indicates `x0` is ArtMethod-like at shared nterp entry.

`CodeItem`
: DEX method-code structure. Slot `+16` is CodeItem-like in v1.0.47a evidence.

`nterp`
: Android ART interpreter path represented here by the shared `ExecuteNterpImpl` entrypoint.

`ExecuteNterpImpl`
: Shared executable ART stub seen at slot `+24` for all eight target methods.

`Leak check`
: A report-local safety check for tokens, credentials, MAC/email patterns, raw payloads, raw ByteBuffer reads, arbitrary register export, and HVAC command evidence.

`sanitized report`
: Report directory or ZIP contents intended to contain only safe structural facts, counts, statuses, decisions, and hashes.

`observer`
: Frida/host stage that attaches to the prepared app process and records bounded safe evidence.

`preparation`
: Launcher/bypass phase that gets GREE+ into a stable, observable state before the main observer.

`bypass window`
: Timed preparation interval during which replacement attach, patch-set-ready, heartbeat, and cleanup are expected.

`CLEANUP marker`
: Evidence marker that the temporary preparation Frida session ended and the prepared process can be handed to the main observer.
