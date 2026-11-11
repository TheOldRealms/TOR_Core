/*
 * mono-2.0-sgen.dll proxy for Bannerlord on Proton.
 *
 * Bannerlord.Native.exe hosts CoreCLR via TaleWorlds.Native.dll, but
 * TaleWorlds.Native.dll also imports many symbols from mono-2.0-sgen.dll
 * (a legacy runtime bundled with the game). Under Proton, if those imports
 * resolve directly to TW's mono, the game runs normally BUT the Mono
 * soft-debugger agent is never activated by the host, so Rider can't attach.
 *
 * This proxy replaces mono-2.0-sgen.dll with a shim that:
 *   1. Forwards ~1065 exports to a renamed copy of the original DLL
 *      ("monosgenorig.dll"). The renamed DLL cannot contain dots in its
 *      basename because Wine's PE forwarder resolver splits on the wrong
 *      dot when the target module name contains any (see err:module:
 *      find_forwarded_export logged when target was "mono-2.0-sgen-orig").
 *   2. LoadLibrary()s the renamed original in DllMain, because Wine's
 *      loader does NOT auto-load forwarder targets like Windows does —
 *      without preloading, all 1065 forwarded imports return NULL and
 *      TaleWorlds.Native.dll crashes later calling into null pointers.
 *   3. Intercepts the mono init functions actually called by TW.Native.dll:
 *        - mono_set_dirs   (canary — proves the proxy is on the hot path)
 *        - mono_jit_init_version  (main runtime entry)
 *        - mono_jit_init          (alternate entry)
 *      In these hooks, before delegating, we (a) call mono_debug_init(
 *      MONO_DEBUG_FORMAT_MONO), required or the soft-debugger asserts
 *      later with `mono_debug_initialized' not met (mono-debug.c:1115),
 *      and (b) call mono_jit_parse_options with our --debugger-agent=...
 *      arg to activate the TCP soft-debug agent on port 56000.
 *
 * Env vars:
 *   MONO_DEBUG_PROXY_ADDR   overrides the default debugger-agent options
 *                           (value goes verbatim after "--debugger-agent=").
 *                           Example: "transport=dt_socket,server=y,
 *                                     address=127.0.0.1:59000,suspend=y"
 *
 * Diagnostic log at Z:\tmp\monoproxy.log (i.e. /tmp/monoproxy.log on host).
 */

#include <windows.h>
#include <stdio.h>
#include <string.h>

typedef int   (*jit_parse_options_fn)(int argc, char *argv[]);
typedef void* (*jit_init_version_fn)(const char *root_domain_name, const char *runtime_version);
typedef void* (*jit_init_fn)(const char *root_domain_name);
typedef void  (*set_dirs_fn)(const char *assembly_dir, const char *config_dir);
typedef void  (*debug_init_fn)(int format);

/* MONO_DEBUG_FORMAT enum values from mono/metadata/mono-debug.h:
 *   MONO_DEBUG_FORMAT_NONE = 0
 *   MONO_DEBUG_FORMAT_MONO = 1  (portable PDB / mdb — what we want)
 *   MONO_DEBUG_FORMAT_DEBUGGER = 2
 */
#define MONO_DEBUG_FORMAT_MONO 1

static HMODULE g_orig = NULL;
static int g_agent_setup_done = 0;

static const char *default_agent_args =
    "--debugger-agent=transport=dt_socket,server=y,address=0.0.0.0:56000,embedding=1,suspend=n,loglevel=1";

static void dbg(const char *msg) {
    /* File-based logging: under Wine, Z:\tmp\... is /tmp/... on the Linux host. */
    FILE *f = fopen("Z:\\tmp\\monoproxy.log", "a");
    if (f) {
        fputs(msg, f);
        fclose(f);
    }
    OutputDebugStringA(msg); /* also emit on Wine's debugstr channel, harmless if unread */
}

static void ensure_orig_loaded(void) {
    if (g_orig) return;
    g_orig = LoadLibraryA("monosgenorig.dll");
    if (!g_orig) {
        char buf[128];
        _snprintf(buf, sizeof(buf), "[mono-proxy] FATAL: LoadLibrary(monosgenorig.dll) failed err=%lu\n", GetLastError());
        dbg(buf);
    } else {
        dbg("[mono-proxy] loaded original monosgenorig.dll\n");
    }
}

static void setup_debugger_agent_once(void) {
    static char arg_buf[512];

    if (g_agent_setup_done) return;
    g_agent_setup_done = 1;

    if (!g_orig) return;

    const char *env_override = getenv("MONO_DEBUG_PROXY_ADDR");
    const char *arg;
    if (env_override && env_override[0]) {
        _snprintf(arg_buf, sizeof(arg_buf), "--debugger-agent=%s", env_override);
        arg = arg_buf;
    } else {
        arg = default_agent_args;
    }

    /* Mono debug system MUST be initialized before the JIT starts, or the
     * soft-debug agent asserts (mono-debug.c:1115: mono_debug_initialized).
     * Do this before parsing the --debugger-agent= flag. */
    debug_init_fn debug_init =
        (debug_init_fn)GetProcAddress(g_orig, "mono_debug_init");
    if (debug_init) {
        debug_init(MONO_DEBUG_FORMAT_MONO);
        dbg("[mono-proxy] mono_debug_init(MONO_DEBUG_FORMAT_MONO) called\n");
    } else {
        dbg("[mono-proxy] WARN: mono_debug_init not resolvable\n");
    }

    jit_parse_options_fn parse =
        (jit_parse_options_fn)GetProcAddress(g_orig, "mono_jit_parse_options");
    if (!parse) {
        dbg("[mono-proxy] ERROR: mono_jit_parse_options not resolvable\n");
        return;
    }

    char *argv[1];
    argv[0] = (char *)arg;

    char logbuf[640];
    _snprintf(logbuf, sizeof(logbuf), "[mono-proxy] enabling soft-debug: %s\n", arg);
    dbg(logbuf);

    parse(1, argv);
    dbg("[mono-proxy] mono_jit_parse_options returned; agent should be active\n");
}

__declspec(dllexport) void* mono_jit_init_version(const char *root_domain_name, const char *runtime_version) {
    dbg("[mono-proxy] mono_jit_init_version hook fired\n");
    ensure_orig_loaded();
    setup_debugger_agent_once();

    jit_init_version_fn real =
        (jit_init_version_fn)GetProcAddress(g_orig, "mono_jit_init_version");
    if (!real) {
        dbg("[mono-proxy] FATAL: real mono_jit_init_version not resolvable\n");
        return NULL;
    }
    return real(root_domain_name, runtime_version);
}

/* Canary hook: mono_set_dirs is called by embedded hosts BEFORE mono_jit_init*.
 * Its firing in the log proves our proxy is on the runtime init path. */
__declspec(dllexport) void mono_set_dirs(const char *assembly_dir, const char *config_dir) {
    dbg("[mono-proxy] mono_set_dirs canary fired\n");
    ensure_orig_loaded();
    set_dirs_fn real = (set_dirs_fn)GetProcAddress(g_orig, "mono_set_dirs");
    if (real) real(assembly_dir, config_dir);
}

__declspec(dllexport) void* mono_jit_init(const char *root_domain_name) {
    dbg("[mono-proxy] mono_jit_init hook fired\n");
    ensure_orig_loaded();
    setup_debugger_agent_once();

    jit_init_fn real = (jit_init_fn)GetProcAddress(g_orig, "mono_jit_init");
    if (!real) {
        dbg("[mono-proxy] FATAL: real mono_jit_init not resolvable\n");
        return NULL;
    }
    return real(root_domain_name);
}

BOOL WINAPI DllMain(HINSTANCE hinst, DWORD reason, LPVOID reserved) {
    (void)hinst; (void)reserved;
    if (reason == DLL_PROCESS_ATTACH) {
        /* Eagerly load the original mono DLL so Wine's PE loader can resolve
         * our forwarded exports (Wine does not auto-load forwarder targets
         * like Windows does). Without this, every non-hooked mono_* function
         * returns NULL to TaleWorlds.Native.dll and the game crashes with
         * cascading null-pointer calls. */
        ensure_orig_loaded();
        dbg("[mono-proxy] proxy DLL_PROCESS_ATTACH (orig preloaded)\n");
    }
    return TRUE;
}
