# Phase 0 validation

Environment: Linux x64, .NET SDK 10.0.402. Original framework targets retained: server net48; client/editor net10.0. Foundation client builds are framework-dependent (SelfContained=false) to keep package locks runtime-neutral. This is source compilation, not a Windows graphical runtime test.

| Target | Result | Warnings | Errors |
|---|---|---:|---:|
| server | PASS | 0 | 0 |
| client | PASS | 73 | 0 |
| editor | PASS | 134 | 0 |
| contracts | PASS | 0 | 0 |

Additional checks: six foundation unit tests pass; preserved source fingerprints and all eight direct dependency revisions verify; recursively pinned dependencies are initialized; original package descriptors and legacy-ID bridge validate. Static audit records 334 candidate UO-data references. Source archive integrity and source/dependency verification are checked again from a fresh extraction before delivery.

Build logs are included under docs/build-logs. Existing compiler warnings and NuGet advisories are retained visibly. In particular, inherited ImageSharp 3.1.11 and SQLitePCLRaw.lib.e_sqlite3 2.1.11 produce advisories in this restore. These packages have not been upgraded during baseline capture. Review/update and re-pin before exposing imported/untrusted content or distributing the playable demo.

The root build override suppresses upstream client API-document generation, which otherwise modifies source documentation. Server binaries go into artifacts/build/server rather than replacing source configuration. The tooling avoids compiler-server and parallel-MSBuild named pipes, which this environment blocks.

Not tested: hosted Windows CI, Windows graphics/audio, local server/client gameplay, save/load, or asset-independent runtime boot. These are later milestone checks. No generated game art or island content is included in Phase 0.

The source ZIP includes dependency source plus byte-level manifests, but omits Git databases, build outputs and NuGet caches. First-time package restore still requires access to NuGet. Verification is a source/package-graph baseline, not a claim of bit-for-bit reproducible executable output.
