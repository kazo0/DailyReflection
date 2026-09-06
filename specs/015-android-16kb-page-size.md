# Spec 015 — Android 16 KB page size compliance

* **Status:** ✅ Verified compliant (2026-09-06). **No code change required.**
* **Severity:** 🟡 (release-gate audit — would have been 🔴 had it failed)
* **Gaps closed:** — (post-parity release readiness; closes no §10 gap)
* **Depends on:** 009 (Android manifest / target SDK), 011 (bundle id + package pins)

## Summary

Google Play requires apps targeting Android 15 (API 35) or higher to support **16 KB
memory page sizes** on 64-bit devices. Daily Reflection targets API 36, so it is in
scope. This spec records an audit of the shipped native libraries against that
requirement.

**Verdict: the app already complies, with no changes needed.** All 350 native
libraries in a freshly published release AAB (175 × `arm64-v8a`, 175 × `x86_64`)
have ELF `PT_LOAD` segment alignment of 16384 bytes. The secondary zip-alignment
rule does not apply, because the app ships compressed native libraries
(`android:extractNativeLibs="true"`).

This is a *verification* spec, not a change spec: it exists so the result is
reproducible, so the conditions that would silently break it are written down, and
so the next person to touch the native dependency set knows what to re-check.

## Background — what the requirement actually is

The rule has two parts, and only the first one binds this app.

**1. ELF segment alignment (applies to every app).** Each `.so` must have all
`PT_LOAD` program-header segments aligned to at least 2^14 = 16384 bytes. A library
built for 4 KB pages (`align 2**12`) will fail to load on a 16 KB device. NDK r28
and newer emit 16 KB alignment by default; older toolchains do not.

**2. Zip alignment inside the APK (applies only to *uncompressed* libraries).** If
an app ships native libraries uncompressed — `extractNativeLibs="false"`, so the
loader maps them straight out of the APK — each `.so` must additionally start on a
16 KB boundary within the archive. If instead the libraries are compressed, they are
extracted to the app's native library directory at install time and `dlopen`'d from
the filesystem, so their offset inside the APK is irrelevant. Google's own
remediation guidance for older AGP versions is precisely to switch *to* compressed
libraries (`useLegacyPackaging true`).

Daily Reflection is in the second category: `.NET for Android` emits
`android:extractNativeLibs="true"` by default, so part 2 does not apply.

**Timeline.** Play Console began surfacing this for new apps and updates targeting
API 35+ in November 2025; per the current Android documentation the hard cutoff,
after which non-compliant updates cannot be released, is **1 February 2027**.

Reference: <https://developer.android.com/guide/practices/page-sizes>

## Scope trigger

`DailyReflection/Platforms/Android/AndroidManifest.xml:13`

```xml
<uses-sdk android:minSdkVersion="21" android:targetSdkVersion="36" />
```

`targetSdkVersion="36"` is above the API 35 threshold, so the requirement binds.
(Spec 009 raised this from the Xamarin-era 33; see the supersession note there.)

## Results

Verified against a release AAB published on **2026-09-06** from
`docs/015-android-16kb-page-size`'s merge base (`57f4dbe`), and cross-checked
against the pre-existing artifact built 2026-07-24 — byte-identical conclusions, so
nothing regressed or was accidentally fixed in between.

| Check | Expected | Actual | Result |
|---|---|---|---|
| ELF `PT_LOAD` min alignment, `arm64-v8a` (175 `.so`) | ≥ 16384 | 16384 (0x4000) | ✅ |
| ELF `PT_LOAD` min alignment, `x86_64` (175 `.so`) | ≥ 16384 | 16384 (0x4000) | ✅ |
| Libraries below 16 KB alignment | 0 | 0 | ✅ |
| `android:targetSdkVersion` | — | 36 (in scope) | ℹ️ |
| `android:extractNativeLibs` | — | `true` | ✅ zip rule N/A |
| Stored-but-misaligned `.so` in APK | 0 | 0 | ✅ |
| `.so` entries DEFLATED in APK | — | 350 of 350 | ✅ consistent with above |

### Libraries covered

The full set is 350 files, dominated by per-assembly AOT stubs
(`libaot-*.dll.so`). The ones that carry real third-party native code — the only
plausible failure candidates — are:

| Library | Origin |
|---|---|
| `libmonosgen-2.0.so`, `libmonodroid.so`, `libassembly-store.so`, `libarc.bin.so`, `libxamarin-app.so` | .NET 10 Android runtime |
| `libSystem.Native.so`, `libSystem.Globalization.Native.so`, `libSystem.IO.Compression.Native.so`, `libSystem.Security.Cryptography.Native.Android.so` | .NET 10 BCL native shims |
| `libSkiaSharp.so`, `libHarfBuzzSharp.so` | Skia renderer (`UnoFeatures: SkiaRenderer`) |
| `libe_sqlite3.so` | `SQLitePCLRaw.lib.e_sqlite3` 3.50.3 |

All pass.

### The SQLite pin is doing double duty

`libe_sqlite3.so` was the highest-risk library in the set: it is the only native
binary that comes from neither Microsoft nor Uno, and SQLitePCLRaw's older builds
predate NDK r28.

The version actually shipped is **3.50.3**, pinned in
`DailyReflection/Directory.Packages.props` — and pinned there for an unrelated
reason: it keeps the vulnerable 2.1.2 (`NU1903`, `GHSA-2m69-gcr7-jv3q`), which
`sqlite-net-pcl`'s `bundle_green` would otherwise win with, out of the graph. The
mechanism is the `ExcludeAssets="all"` version-pin trick at
`DailyReflection/DailyReflection.Uno.csproj:83-84`.

That security pin is also what puts the app on a 16 KB-aligned SQLite build.
**Reverting or relaxing it would reintroduce both problems at once** — the CVE and a
likely 4 KB-aligned `libe_sqlite3.so`. Treat the pin as load-bearing for two
independent reasons and leave the comment in `Directory.Packages.props` intact.

## Reproducing the audit

### 1. Publish a release AAB

```bash
dotnet publish DailyReflection/DailyReflection.Uno.csproj \
  -f net10.0-android -c Release \
  -p:TargetFrameworkOverride=android \
  -o artifacts/android
```

> **Build gotcha, learned the hard way.** `TargetFrameworkOverride` must be passed
> as an MSBuild property (`-p:`) on a *single* invocation that does its own restore.
> Two failure modes, both hit during this audit:
>
> * Setting it as an **environment variable** does not reach restore → the assets
>   file has no `net10.0-android` target → `error NETSDK1005`.
> * Running `dotnet restore` separately **without `-c Release`** restores the Debug
>   graph, which includes `Uno.WinUI.DevServer` → the Release build then fails with
>   `error UNOB0019` (DevServer is not permitted in optimized builds).
>
> Deleting `DailyReflection/obj/project.assets.json` clears a poisoned restore.

### 2. Check ELF segment alignment

Extract the libraries and parse the ELF program headers directly. This deliberately
avoids `llvm-objdump` / `readelf`, which are not reliably present on a macOS dev box:

```bash
unzip -q -o artifacts/android/com.kazo0.dailyreflection.aab 'base/lib/*' -d /tmp/aabcheck
python3 elfalign.py /tmp/aabcheck
```

```python
# elfalign.py — reports the minimum PT_LOAD alignment of every .so under a root.
import struct, sys, os, glob
from collections import defaultdict

def load_aligns(path):
    with open(path, 'rb') as f:
        d = f.read(64)
        if d[:4] != b'\x7fELF':
            return None
        if d[4] != 2:            # ELFCLASS64 only
            return None
        e = '<' if d[5] == 1 else '>'
        e_phoff     = struct.unpack_from(e + 'Q', d, 0x20)[0]
        e_phentsize = struct.unpack_from(e + 'H', d, 0x36)[0]
        e_phnum     = struct.unpack_from(e + 'H', d, 0x38)[0]
        f.seek(e_phoff)
        ph = f.read(e_phentsize * e_phnum)
    aligns = []
    for i in range(e_phnum):
        o = i * e_phentsize
        if struct.unpack_from(e + 'I', ph, o)[0] == 1:            # PT_LOAD
            aligns.append(struct.unpack_from(e + 'Q', ph, o + 0x30)[0])
    return aligns

root = sys.argv[1]
buckets = defaultdict(list)
for p in sorted(glob.glob(os.path.join(root, '**', '*.so'), recursive=True)):
    a = load_aligns(p)
    if a is None:
        buckets['NOT-ELF64'].append(p)
        continue
    buckets[min(a) if a else 0].append(p)

for k in sorted(buckets, key=lambda x: (isinstance(x, str), x)):
    files = buckets[k]
    ok = isinstance(k, int) and k >= 16384
    label = f"{k} (0x{k:x})" if isinstance(k, int) else k
    print(f"{'OK  ' if ok else 'FAIL'} min PT_LOAD align = {label}: {len(files)} file(s)")
    if not ok:
        for f in files:
            print(f"       {os.path.basename(f)}")
```

**Expected output** (the pass condition — a single bucket, at or above 16384):

```
OK   min PT_LOAD align = 16384 (0x4000): 350 file(s)
```

Any `FAIL` line names the offending libraries.

### 3. Confirm the packaging mode

```bash
grep -o 'extractNativeLibs="[^"]*"' DailyReflection/obj/Release/net10.0-android/AndroidManifest.xml
# => extractNativeLibs="true"    → the zip-alignment rule does not apply
```

If that ever reads `false`, the zip-alignment rule *does* apply and must be checked
too — every `.so` in the APK must be `ZIP_STORED` at an offset that is a multiple of
16384 (equivalently `zipalign -v -c -P 16 4 app.apk`).

## Risks — what would silently break this

Ordered by likelihood. None is currently triggered.

1. **Relaxing the SQLite pin.** Dropping to `SQLitePCLRaw` 2.1.x brings back a
   pre-NDK-r28 `libe_sqlite3.so`. See above — this also reintroduces `GHSA-2m69-gcr7-jv3q`.
2. **Adding a new native dependency.** Any new `PackageReference` shipping its own
   `.so` (a native image codec, an analytics SDK, a crypto library) is an unaudited
   binary. Re-run §2 after adding one.
3. **Downgrading the .NET Android workload or Uno.Sdk.** The runtime and Skia
   libraries come from these; an older pack could predate 16 KB alignment.
4. **Flipping `extractNativeLibs` to `false`** without also verifying zip alignment
   — see the follow-up below.

The audit is cheap (one publish plus one script), so re-run it before any release
that changes the native dependency set, and treat §2's output as the gate.

## Follow-up (optional, deliberately not done here)

`extractNativeLibs="true"` means every library is stored twice on device: once
compressed in the APK, once extracted on disk. With a 12.6 MB `libassembly-store.so`
inside a ~55 MB package, that is a meaningful install-size cost, and Google
recommends `false` for exactly this reason.

Switching would **pull the app into the zip-alignment requirement** (part 2 above),
which is currently moot. That makes it a deliberate, separately-tested change —
install-size work, not 16 KB work. It is explicitly **out of scope** for this spec
and must not be bundled into the first release.

## Incidental finding — release pipeline

`dotnet publish -c Release` for Android emits **both** `com.kazo0.dailyreflection.aab`
and `com.kazo0.dailyreflection-Signed.apk` into the output directory. `Configuration=Release`
defaults `AndroidPackageFormats` to `aab;apk`, and `_CalculateAndroidFilesToPublish`
copies both.

This confirms that the `dist/android/*.apk` glob in the final `gh release create` step
of `.github/workflows/release.yml` will match, and will not fail the run *after* the
Play and App Store uploads have already happened. Verified on a real publish, not
just read out of the SDK targets.

## Acceptance criteria

1. Every `.so` in a release AAB, across all shipped ABIs, has minimum `PT_LOAD`
   alignment ≥ 16384. ✅
2. The audit is reproducible from this document alone, on a clean checkout, without
   NDK or LLVM tooling installed. ✅
3. The packaging mode (`extractNativeLibs`) is recorded, with the reason the
   zip-alignment rule does or does not apply. ✅
4. The conditions that would regress compliance are enumerated. ✅
5. No production code, csproj, manifest, or package version is modified by this spec. ✅

## Done when

- [x] Fresh release AAB published and audited (2026-09-06).
- [x] All 350 `.so` files confirmed at 16384-byte `PT_LOAD` alignment.
- [x] Cross-checked against the 2026-07-24 artifact — no drift.
- [x] `extractNativeLibs="true"` recorded; zip-alignment rule documented as N/A.
- [x] `libe_sqlite3.so` 3.50.3 identified as the load-bearing pin, for two reasons.
- [x] Reproduction script and expected output captured inline.
- [x] Regression risks enumerated.
- [x] `release.yml` APK glob validated as a side effect.
- [ ] *(Optional, separate spec)* Evaluate `extractNativeLibs="false"` for install size.
- [ ] *(Confirmation)* Play Console reports no 16 KB warning on the first real upload.

## Verification environment

| Component | Version |
|---|---|
| .NET SDK | 10.0.103 |
| .NET Android workload | 36.1.30 / 10.0.100 |
| `Uno.Sdk` | 6.8.0-dev.7 (`global.json`) |
| `SQLitePCLRaw.lib.e_sqlite3` | 3.50.3 |
| Host | macOS (Darwin 25.6.0), arm64 |
| Artifact | `com.kazo0.dailyreflection.aab`, 55,685,729 bytes, 2026-09-06 |

**Caveat on method.** The audited AAB was built locally and is not signed with the
production keystore. Signing rewrites the archive's signature blocks but does not
alter ELF program headers, so it cannot affect segment alignment. The final
confirmation (criterion in "Done when") is the absence of a 16 KB warning in Play
Console on the first real upload.

## Numbering note

This spec is numbered 015 rather than 012 because specs 012–014 exist on the
in-flight `feat/flipview-pips-pager` branch and are not yet on `master`. 015 avoids a
collision when that branch merges.
