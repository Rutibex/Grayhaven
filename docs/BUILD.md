# Build and dependency policy

First supported target: Windows x64. Install Git, Python 3.10+, SDK 10.0.402 and .NET Framework 4.8 Developer Pack. `Foundation.cmd` provides a double-click menu; `Build.cmd` builds all targets.

Dependency pins come from upstream Git trees whose checked source blobs match the supplied archives. Exact observed commits and comparison counts are in dependency-pin-evidence.json. The ZIP origin remains identified by SHA-256 rather than an invented commit.

The release ZIP bundles dependency source without Git databases; it verifies those bytes against bundled-dependencies.json. A fresh source checkout uses bootstrap to fetch the same pinned commits. Run bootstrap, then verify, then build. Bootstrap checks out detached immutable commits and follows recursively pinned gitlinks. It refuses nonempty unknown directories and modified dependency checkouts. It never selects current HEAD.

Server build output is redirected into artifacts/build/server to avoid overwriting source configuration. The server entry point builds Scripts.csproj, which references Server and Ultima; this avoids relying on the old solution's IDE configuration. The client and editor retain FNA and their supplied targets. Do not run the legacy applications against downloaded EA data as part of CI.

Package versions declared by the archives are recorded separately from resolved NuGet lock files. The first restore creates package locks; subsequent restores use --locked-mode. Preserve those locks in the repository. Transitive packages in submodules retain their original licenses.

Successful compilation is not runtime certification. FNA native binaries are inherited and require matching platform/runtime checks. The original Windows interface, graphics and audio need a later Windows runtime smoke test.

Source snapshot verification deliberately flags modifications to baseline files. Future engine changes should record a reviewed baseline update or a documented fork patch rather than silently overwriting hashes. Generated obj/bin directories and newly created package locks are not part of the original snapshot.

To create a local Git repository after extracting the ZIP, run `python tools/project.py init-repository`. This stages the baseline without changing your global Git identity.

The root Directory.Build.targets disables the client's automatic API-documentation generation because upstream writes into tracked source files. Runtime code is unchanged. Documentation regeneration can be added as a separate command later.

The resolved-lock mirror in manifests/nuget-resolved.json recreates missing package lock files (including ignored dependency subtrees) before restore, and refuses to replace a different existing lock. This preserves the same package graph in a fresh Git checkout.

Foundation client builds set SelfContained=false so package locks remain runtime-neutral across developer operating systems. Standalone Windows publishing and native-runtime packaging are later milestones. Framework targets and client source remain unchanged.
