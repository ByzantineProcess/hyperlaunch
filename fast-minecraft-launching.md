# Fast Modded Minecraft Launching
### Architecture Reference for Advanced Launcher Development

---

## Table of Contents

1. [The Core Problem](#1-the-core-problem)
2. [JVM Warmup — CDS and AOT Caching](#2-jvm-warmup--cds-and-aot-caching)
3. [CRaC — Checkpoint/Restore](#3-crac--checkpointrestore)
4. [The Java Agent](#4-the-java-agent)
5. [Registry and Data Caching](#5-registry-and-data-caching)
6. [C# Launcher Orchestration](#6-c-launcher-orchestration)
7. [Recommended Mods](#7-recommended-mods)
8. [Version Matrix](#8-version-matrix)
9. [Storage and Memory Costs](#9-storage-and-memory-costs)
10. [Full Architecture Decision Tree](#10-full-architecture-decision-tree)

---

## 1. The Core Problem

For a generic large Java app, JVM startup and class loading dominate launch time. For modded Minecraft, they are a small fraction of total load time. The actual bottleneck is:

| Phase | Vanilla | Modded (200 mods) |
|---|---|---|
| JVM startup + class loading | ~1–2s | ~2–4s |
| Mod discovery + transformation | N/A | **~15–60s** |
| Registry init (blocks, items, biomes) | ~2–4s | **~10–30s** |
| Resource loading + texture stitching | ~3–6s | **~10–40s** |
| OpenGL upload (atlas, models) | ~2–4s | ~4–10s |
| **Total** | **~8–16s** | **~40–140s** |

The fundamental strategy is to **pay the mod loading cost silently and invisibly**, ahead of user-visible time — not to eliminate it.

---

## 2. JVM Warmup — CDS and AOT Caching

### What They Do

**AppCDS** (Application Class Data Sharing) pre-parses class metadata into a shared archive (`.jsa`). On launch, the JVM maps this file instead of parsing JARs. Memory-mapped read-only and shared across all JVM processes using the same archive — net RAM cost per additional instance is near zero.

**AOT Cache** (Java 25, Project Leyden) additionally caches JIT-compiled native code from a training run. Saves warm-up time for data processing paths on top of CDS gains.

### Cache Key Strategy

Caches are coupled to the exact JDK build and classpath. Key on all three:

```csharp
record CacheKey(
    string AppVersion,      // hash of mod JAR set
    string JdkBuildId,      // from JAVA_HOME/release → IMPLEMENTOR_VERSION
    string ClasspathHash    // SHA of sorted jar paths + sizes
);
```

Read `JAVA_HOME/release` rather than invoking `java -version` — it's a static file and requires no process launch.

### Build Commands

```bash
# Phase 1 — Capture loaded class list
java -Xshare:off \
     -XX:DumpLoadedClassList=app-classes.lst \
     -cp "app.jar:lib/*" com.example.Main

# Phase 2 — Dump CDS archive
java -Xshare:dump \
     -XX:SharedClassListFile=app-classes.lst \
     -XX:SharedArchiveFile=app.jsa \
     -cp "app.jar:lib/*"

# Production launch with CDS
java -Xshare:on \
     -XX:SharedArchiveFile=app.jsa \
     -cp "app.jar:lib/*" com.example.Main

# AOT training run (Java 25+)
java -XX:AOTMode=record \
     -XX:AOTCache=app.aot \
     -cp "app.jar:lib/*" com.example.Main

# Production launch with AOT
java -XX:AOTMode=on \
     -XX:AOTCache=app.aot \
     -cp "app.jar:lib/*" com.example.Main
```

Use `-Xshare:auto` during testing — `on` crashes if the archive is stale, `auto` falls back silently.

### Realistic Gains for Minecraft

```
Vanilla 1.21, Java 21 + AOT:     saves ~1–2s of a ~10s launch  (~15%)
Modded Forge, CDS only:           saves ~1–3s of a ~90s launch  (~2–3%)
```

CDS/AOT is low effort and low risk but a small fraction of total gains for modded instances.

### JVM Flags (Apply Always)

```bash
# GC tuning — Minecraft's allocation pattern suits G1GC
-XX:+UseG1GC
-XX:MaxGCPauseMillis=130
-XX:+UnlockExperimentalVMOptions
-XX:+DisableExplicitGC
-XX:G1NewSizePercent=28
-XX:G1HeapRegionSize=16M
-XX:G1ReservePercent=20
-XX:G1MixedGCCountTarget=3
-XX:G1HeapWastePercent=20

# Class loading
-XX:+UseStringDeduplication
```

---

## 3. CRaC — Checkpoint/Restore

### What It Is

CRaC (Coordinated Restore at Checkpoint) is a JVM-level API built on top of **CRIU** (Checkpoint/Restore In Userspace), a Linux kernel mechanism that serialises an entire process's memory and kernel state to disk and restores it later.

From Minecraft's perspective: the JVM runs through mod loading, then freezes. On restore, it resumes from exactly that point in ~200ms — with warm heap, warm JIT, warm registries, warm everything.

### Why Linux Only

CRIU relies on Linux-specific kernel interfaces:
- `/proc/PID/maps` and `smaps` for memory layout
- `ptrace` extensions for thread register state
- `TCP_REPAIR` for socket state
- `CLONE_*` namespace flags
- `userfaultfd` for lazy page restore

None of these have equivalents on Windows or macOS. Windows has a fundamentally different process model. macOS additionally has SIP (System Integrity Protection) that would block the required memory introspection.

### Why Only Certain JVM Distributions

The CRaC patches have not been merged into OpenJDK mainline. They touch GC internals, the safepoint mechanism, and JNI. Distributions that ship CRaC builds:

| Distribution | CRaC Support |
|---|---|
| Azul Zulu | ✅ Explicit CRaC-labelled downloads |
| BellSoft Liberica | ✅ CRaC variants available |
| SAP SapMachine | ⚠️ Experimental |
| Eclipse Temurin | ❌ |
| Oracle JDK | ❌ |
| GraalVM | ❌ (uses Native Image instead) |

Detect availability at runtime:
```bash
java --list-modules | grep jdk.crac
```

### The Application API

```java
import jdk.crac.*;

public class MinecraftCracResource implements Resource {

    @Override
    public void beforeCheckpoint(Context<? extends Resource> context)
            throws Exception {
        // For Vanilla/Fabric: nothing needed — checkpoint fires before
        // glfwCreateWindow, so no GPU context exists yet.

        // For Forge: destroy the loading screen GL context
        loadingScreen.destroyGlContext();
        glfwDestroyWindow(loadingWindowHandle);
    }

    @Override
    public void afterRestore(Context<? extends Resource> context)
            throws Exception {
        // Mod loading is already done — proceed directly to main menu
        // or recreate the Forge loading window briefly
        proceedToMainMenu();
    }
}

// Register at startup
Core.getGlobalContext().register(new MinecraftCracResource());
```

### The Correct Checkpoint Point

The key insight: checkpoint **after mod loading, before GPU context creation**:

```
Vanilla / Fabric:
[mod loading] → ← CHECKPOINT HERE → [glfwCreateWindow] → [main menu]
No GPU teardown needed at all.

Forge / NeoForge:
[glfwCreateWindow (loading screen)] → [mod loading] → ← CHECKPOINT HERE → [main menu]
Only a simple 2D loading screen GL context to tear down — trivial vs. full game renderer.
```

### Storage Costs

A Minecraft JVM checkpointed after mod loading:
- **Disk:** ~600 MB – 1.6 GB per snapshot (dominated by heap pages)
- **RAM on restore:** ~1–2 GB private pages per instance
- **Pool of 3 restored instances:** ~3–6 GB RAM, ~1 GB disk (snapshot file is shared)

Snapshots are invalidated by any mod change, JDK update, or version change. Apply the same cache key strategy as CDS.

### CRaC vs Manual Serialisation

Everything possible with manual registry/DFU caching (Kryo serialisation, field injection via Unsafe, structural fingerprinting) is a partial, fragile approximation of what CRaC does automatically. CRaC is the correct solution where available. Manual caching is the Windows fallback.

---

## 4. The Java Agent

### Purpose

The agent is the universal mechanism for making all load time invisible to the user, regardless of platform or Java version. It:

1. Intercepts window creation
2. Hides the window via `GLFW_VISIBLE = FALSE`
3. Signals the C# launcher when mod loading is complete
4. Blocks until C# says to reveal the window

This works on all Minecraft versions, all mod loaders, and all platforms.

### Project Structure

```
launcher-agent/
├── build.gradle
└── src/main/
    ├── java/com/launcher/agent/
    │   ├── LauncherAgent.java            # premain entry point
    │   ├── GlfwInterceptTransformer.java # ASM bytecode transformer
    │   └── AgentBridge.java              # IPC + CountDownLatch gate
    └── resources/META-INF/
        └── MANIFEST.MF
```

### MANIFEST.MF

```
Manifest-Version: 1.0
Premain-Class: com.launcher.agent.LauncherAgent
Agent-Class: com.launcher.agent.LauncherAgent
Can-Redefine-Classes: true
Can-Retransform-Classes: true
```

### Entry Point

```java
public class LauncherAgent {
    public static void premain(String agentArgs, Instrumentation inst) {
        String pipeName = agentArgs != null ? agentArgs
                                            : "\\\\.\\pipe\\launcher-default";
        AgentBridge.initialise(pipeName);
        inst.addTransformer(new GlfwInterceptTransformer(), true);
    }
}
```

### Intercept Points by Version

| MC Version | LWJGL | Intercept target |
|---|---|---|
| 1.8–1.12.2 | LWJGL 2 | `org.lwjgl.opengl.Display.create()` |
| 1.13+ | LWJGL 3 | `org.lwjgl.glfw.GLFW.glfwCreateWindow(...)` |

### Bytecode Transformer (ASM)

```java
public class GlfwInterceptTransformer implements ClassFileTransformer {

    private static final String GLFW_CLASS    = "org/lwjgl/glfw/GLFW";
    private static final String DISPLAY_CLASS = "org/lwjgl/opengl/Display";

    @Override
    public byte[] transform(ClassLoader loader, String className,
            Class<?> classBeingRedefined, ProtectionDomain domain,
            byte[] classfileBuffer) {

        if (!GLFW_CLASS.equals(className) && !DISPLAY_CLASS.equals(className))
            return null; // fast reject

        try {
            ClassReader reader = new ClassReader(classfileBuffer);
            ClassWriter writer = new ClassWriter(
                reader, ClassWriter.COMPUTE_FRAMES | ClassWriter.COMPUTE_MAXS);
            reader.accept(new GlfwClassVisitor(writer, className),
                          ClassReader.EXPAND_FRAMES);
            return writer.toByteArray();
        } catch (Exception e) {
            System.err.println("[LauncherAgent] Transform failed: " + e.getMessage());
            return null; // fail safe — use original bytecode
        }
    }
}
```

The injected method prefix call:
```java
// Injected at visitCode() — fires at entry of glfwCreateWindow
mv.visitMethodInsn(INVOKESTATIC,
    "com/launcher/agent/AgentBridge",
    "onWindowCreationPoint", "()V", false);
```

### Agent Bridge — IPC and Gate

```java
public class AgentBridge {

    private static final CountDownLatch GATE = new CountDownLatch(1);

    public static void onWindowCreationPoint() {
        try {
            setGlfwHidden();   // GLFW_VISIBLE = FALSE via reflection
            signalReady();     // notify IPC thread to send READY to C#
            GATE.await();      // block until C# sends PROCEED
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
            // Fail open — do not block Minecraft forever
        }
    }

    public static void proceed() { GATE.countDown(); }

    private static void setGlfwHidden() throws Exception {
        Class<?> glfw = Class.forName("org.lwjgl.glfw.GLFW");
        glfw.getMethod("glfwWindowHint", int.class, int.class)
            .invoke(null, 0x00020004 /*GLFW_VISIBLE*/, 0 /*GLFW_FALSE*/);
    }
}
```

**Always fail open.** A blocked Minecraft process with no recovery is worse than launching without optimisations.

### Multiple Agents

Multiple `-javaagent:` arguments are fully supported and run in declaration order:

```bash
-javaagent:fabric-loader.jar
-javaagent:launcher-agent.jar=\\.\pipe\launcher-ipc
```

Each agent gets its own `premain` call. Bytecode transformers chain — each sees the output of the previous. There is no conflict with Forge/Fabric/Mixin transformers since no other agent touches LWJGL classes.

### Build (Gradle)

```groovy
jar {
    manifest {
        attributes(
            'Premain-Class'          : 'com.launcher.agent.LauncherAgent',
            'Can-Redefine-Classes'   : 'true',
            'Can-Retransform-Classes': 'true'
        )
    }
    // Fat JAR — agent must be self-contained
    from { configurations.runtimeClasspath.collect {
        it.isDirectory() ? it : zipTree(it) }
    }
    duplicatesStrategy = DuplicatesStrategy.EXCLUDE
}

dependencies {
    implementation 'org.ow2.asm:asm:9.6'
    implementation 'org.ow2.asm:asm-commons:9.6'
}
```

---

## 5. Registry and Data Caching

### Why This Is Hard

DashLoader and similar mods work by intercepting the output of Minecraft's loading pipelines and serialising the resulting object graphs. The objects change structure between versions. A version-agnostic agent-level implementation can only safely target phases whose output is **pure data with stable boundaries**.

### Structural Fingerprinting

Before trusting any cached object graph, verify the relevant classes haven't changed:

```java
public static String fingerprint(Class<?> cls) {
    return Arrays.stream(cls.getDeclaredFields())
        .map(f -> f.getType().getName() + " " + f.getName())
        .collect(Collectors.joining(","));
    // SHA-256 the result and store alongside the cache
}
```

### Feasibility by Target

| Cache Target | Version Agnostic? | Gain | Feasibility |
|---|---|---|---|
| DFU schema graph skip | ✅ High | 5–15s | ✅ Feasible — pure data, stable boundary |
| Pre-bake JSON / UnbakedModel | ⚠️ Medium | 10–25s | ⚠️ Needs fingerprinting |
| Atlas layout (not pixel data) | ⚠️ Medium | 2–5s | ⚠️ Layout only, not GPU upload |
| Baked models | ❌ Low | 15–30s | ❌ Too version-coupled |

### DFU Cache — Highest Value Target

`DataFixerBuilder.build()` is stable across versions. Its output is deterministic for a given MC version and mod set. Strategy: intercept completion, memoise the result, skip the build entirely on subsequent launches by returning the cached `DataFixer`.

### Serialisation with Kryo

```java
Kryo kryo = new Kryo();
kryo.setRegistrationRequired(false);
kryo.setInstantiatorStrategy(new StdInstantiatorStrategy()); // bypass constructors

// Registry references by key, not by object
kryo.addDefaultSerializer(ResourceLocation.class,
    new ResourceLocationSerializer());
```

### CRaC Supersedes This on Linux

All manual caching work is the **Windows fallback**. On Linux with a CRaC-capable JDK, a single checkpoint after mod loading captures everything — warm heap, warm JIT, warm registries — with no custom serialisation code.

---

## 6. C# Launcher Orchestration

### Named Pipe IPC

```csharp
// Server side — started before Java process launch
var pipe = new NamedPipeServerStream("launcher-ipc",
    PipeDirection.InOut, 1, PipeTransmissionMode.Byte);

await pipe.WaitForConnectionAsync();
var reader = new StreamReader(pipe);
var writer = new StreamWriter(pipe) { AutoFlush = true };

string message = await reader.ReadLineAsync(); // "READY"
// Do launcher UI transition here
writer.WriteLine("PROCEED");
```

### Launch Decision Flow

```csharp
ProcessStartInfo BuildLaunchInfo(InstanceConfig config)
{
    var key = new CacheKey(
        GetModpackHash(config),
        GetJdkBuildId(config.JavaHome),
        GetClasspathHash(config.Classpath));

    var args = new List<string>();

    // CRaC (Linux + CRaC JDK only)
    if (IsLinux && IsCracJdk(config.JavaHome) && CracSnapshotExists(key))
    {
        args.Add($"-XX:CRaCRestoreFrom={GetSnapshotPath(key)}");
    }
    else
    {
        // AOT (Java 21+ only)
        if (config.JavaMajorVersion >= 21 && AotCacheExists(key))
        {
            args.Add("-XX:AOTMode=on");
            args.Add($"-XX:AOTCache={GetAotPath(key)}");
        }
        // CDS (all versions)
        else if (CdsCacheExists(key))
        {
            args.Add("-Xshare:on");
            args.Add($"-XX:SharedArchiveFile={GetJsaPath(key)}");
        }

        // Agent (all versions, all platforms)
        args.Add($"-javaagent:{AgentJarPath}=\\\\.\\pipe\\launcher-ipc");

        // If no cache exists, regenerate in background
        if (!AnyCacheExists(key))
            _ = Task.Run(() => RegenerateCacheAsync(config, key));
    }

    return new ProcessStartInfo {
        FileName = Path.Combine(config.JavaHome, "bin", "java"),
        Arguments = string.Join(" ", args.Concat(config.AppArgs))
    };
}
```

### Disk Prewarming

On launch intent detection (hover, focus, schedule), read all mod JARs sequentially into the OS page cache before the Java process starts:

```csharp
void PrewarmDiskCache(IEnumerable<string> jarPaths)
{
    foreach (var path in jarPaths)
    {
        using var fs = new FileStream(path, FileMode.Open,
            FileAccess.Read, FileShare.Read,
            bufferSize: 1024 * 1024, // 1MB reads
            useAsync: false);
        var buffer = new byte[1024 * 1024];
        while (fs.Read(buffer, 0, buffer.Length) > 0) { }
        // Data is now in OS page cache — JVM reads will hit RAM
    }
}
```

### Full Launch Flow

```
User intent detected (hover / click / schedule)
    ↓
C# prewarms disk cache (background)
C# starts named pipe server
    ↓
C# starts Java process (invisible — agent will hide window)
    ↓
[Mod loading, registry init, resource processing — silent]
    ↓
Agent signals READY on pipe
    ↓
C# performs launcher UI transition, then sends PROCEED
    ↓
Agent releases gate → glfwCreateWindow executes with GLFW_VISIBLE=TRUE
    ↓
Window appears: already past loading screen, at main menu
User-visible wait: near zero
```

---

## 7. Recommended Mods

Surface these in the launcher UI for compatible versions. They compound with the agent's gains.

| Mod | Effect | Versions |
|---|---|---|
| **ModernFix** | Parallelises Forge loading pipeline; ~20–50% faster mod load | 1.16+ |
| **LazyDFU** | Defers DataFixerUpper init; saves 5–15s | 1.16+ |
| **Starlight / Moonrise** | Faster lighting engine; speeds world load | Various |
| **DashLoader** | Caches baked models; the baked model win your agent can't safely reach | Fabric, specific versions |

---

## 8. Version Matrix

| MC Version | Java | CDS | AOT | CRaC | Agent Intercept |
|---|---|---|---|---|---|
| 1.8–1.12.2 | 8 | ✅ | ❌ | ❌ | `Display.create()` |
| 1.13–1.16 | 8/11 | ✅ | ❌ | ❌ | `glfwCreateWindow` |
| 1.17 | 16 | ✅ | ❌ | ❌ | `glfwCreateWindow` |
| 1.18–1.20.4 | 17 | ✅ | ❌ | ⚠️ Linux | `glfwCreateWindow` |
| 1.21+ | 21 | ✅ | ✅ | ⚠️ Linux | `glfwCreateWindow` |

CRaC requires Azul Zulu or BellSoft Liberica. Detect via:
```bash
java --list-modules | grep jdk.crac
```

---

## 9. Storage and Memory Costs

### Per-Mechanism Breakdown (Heavy Modpack Estimate)

| Mechanism | Disk | Extra RAM per JVM | Pool of 3 — RAM |
|---|---|---|---|
| CDS archive | 60–100 MB | ~0 (shared mapping) | ~0 |
| AOT cache | 150–300 MB | ~0 (shared mapping) | ~0 |
| CRaC snapshot | 600 MB – 1.6 GB | ~1–2 GB private | ~3–6 GB |

### JVM Memory Profile (Warm, Post Mod Load)

| Region | Estimate |
|---|---|
| Metaspace | 200–500 MB |
| Heap | 300 MB – 2 GB |
| JIT code cache | 100–250 MB |
| Native/direct buffers | 100 MB – 1 GB |
| **Total RSS** | **~1–4 GB** |

---

## 10. Full Architecture Decision Tree

```
Target instance
    │
    ├─ Linux + CRaC-capable JDK?
    │   ├─ Yes → CRaC checkpoint (supersedes all manual caching)
    │   │         + Agent for window reveal coordination
    │   │         + CDS/AOT as baseline (still worth having)
    │   │
    │   └─ No (Windows / non-CRaC JDK)
    │       ├─ Java 21+ → CDS + AOT cache
    │       ├─ Java 17   → CDS only
    │       ├─ Java 8–16 → AppCDS only
    │       └─ All:  Agent (hidden window, PROCEED gate)
    │                Manual DFU cache (highest value feasible target)
    │                Surface DashLoader recommendation (Fabric versions)
    │
    └─ All paths:
        Aikar flags + -XX:+DisableExplicitGC + -XX:+UseStringDeduplication
        Disk prewarming on launch intent
        ModernFix + LazyDFU recommendations
        Per-instance cache keyed on (JDK build, modpack hash, classpath hash)
        Background cache regeneration on modpack change
        Fail-open on all cache and IPC paths
```

### Expected Outcomes

On a heavy modpack (~90s cold load):

| Metric | Without | With (Windows) | With (Linux + CRaC) |
|---|---|---|---|
| User-visible wait after click | ~90s | Near zero* | Near zero* |
| Total wall-clock load time | ~90s | ~85–88s | ~85–88s first run |
| Subsequent launches | ~90s | ~85–88s | ~200ms restore |

*Load time paid silently in background before user clicks Play.

CRaC does not reduce first-run load time — it eliminates load time on all subsequent runs by restoring a completed state.

---

*Document covers: AppCDS, AOT Cache (Java 25), CRaC (Linux), Java Agent bytecode interception, IPC via named pipes, registry caching feasibility, mod loader specifics (Vanilla, Fabric, Forge, NeoForge), version compatibility, storage/memory budgeting.*
