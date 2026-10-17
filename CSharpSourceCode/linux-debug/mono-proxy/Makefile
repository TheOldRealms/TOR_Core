# Build and install the Bannerlord mono-2.0-sgen.dll debug proxy.
#
# Prereqs:  mingw64-gcc, mingw64-binutils (Fedora), or wine-mingw-w64 on other distros.
# Usage:    make            build proxy from source
#           make install    build + rename original mono, install proxy
#           make uninstall  restore original mono, remove proxy
#           make clean      remove build artifacts
#           make redeploy   rebuild + copy over installed proxy (no rename dance)

CC       := x86_64-w64-mingw32-gcc
DLLTOOL  := x86_64-w64-mingw32-dlltool
OBJDUMP  := x86_64-w64-mingw32-objdump

GAMEBIN  := $(HOME)/.local/share/Steam/steamapps/common/Mount & Blade II Bannerlord/bin/Win64_Shipping_Client
LIVE     := $(GAMEBIN)/mono-2.0-sgen.dll
ORIG     := $(GAMEBIN)/monosgenorig.dll
BACKUP   := $(HOME)/.cache/tor-linux-debug/mono-2.0-sgen.dll.original

TARGET   := mono-2.0-sgen.dll
CFLAGS   := -shared -O2 -s -static-libgcc
LDFLAGS  := -Wl,--kill-at

.PHONY: all install uninstall redeploy clean check-deps

all: check-deps $(TARGET)

check-deps:
	@which $(CC) >/dev/null 2>&1 || { echo "ERROR: $(CC) not found. Install: sudo dnf install mingw64-gcc mingw64-binutils"; exit 1; }
	@which $(DLLTOOL) >/dev/null 2>&1 || { echo "ERROR: $(DLLTOOL) not found."; exit 1; }

# Extract the export table from whichever copy of the original DLL is available.
# If proxy is already installed, use $(ORIG). Else use $(LIVE) (still the original).
exports.txt:
	@if [ -f "$(ORIG)" ]; then \
		src="$(ORIG)"; \
	elif [ -f "$(LIVE)" ]; then \
		src="$(LIVE)"; \
	else \
		echo "ERROR: neither $(LIVE) nor $(ORIG) found"; exit 1; \
	fi; \
	echo "extracting exports from: $$src"; \
	$(OBJDUMP) -p "$$src" \
	  | awk 'BEGIN{t=0} /\[Ordinal\/Name Pointer\] Table/{t=1;next} t && /^$$/{t=0} t && /\+base\[/{print $$NF}' > $@; \
	echo "found $$(wc -l < $@) exports"

proxy.def: exports.txt gen-def.sh
	./gen-def.sh exports.txt proxy.def

proxy.exp: proxy.def
	$(DLLTOOL) -d proxy.def -e proxy.exp -D mono-2.0-sgen.dll

$(TARGET): proxy.c proxy.exp
	$(CC) $(CFLAGS) -o $@ proxy.c proxy.exp $(LDFLAGS)
	@echo "built $$(du -h $@ | cut -f1) $@"

install: $(TARGET)
	@mkdir -p "$$(dirname "$(BACKUP)")"
	@if [ ! -f "$(BACKUP)" ] && [ -f "$(LIVE)" ] && [ ! -f "$(ORIG)" ]; then \
		echo "backing up original mono -> $(BACKUP)"; \
		cp "$(LIVE)" "$(BACKUP)"; \
	fi
	@if [ ! -f "$(ORIG)" ]; then \
		if [ -f "$(LIVE)" ] && [ $$(stat -c%s "$(LIVE)") -gt 1000000 ]; then \
			echo "renaming live mono -> $(ORIG)"; \
			mv "$(LIVE)" "$(ORIG)"; \
		elif [ -f "$(BACKUP)" ]; then \
			echo "restoring $(ORIG) from backup"; \
			cp "$(BACKUP)" "$(ORIG)"; \
		else \
			echo "ERROR: no original mono to rename or restore"; exit 1; \
		fi; \
	fi
	@echo "installing proxy -> $(LIVE)"
	@cp $(TARGET) "$(LIVE)"
	@echo "install ok — TCP :56000 will open at next game launch"

redeploy: $(TARGET)
	@[ -f "$(ORIG)" ] || { echo "ERROR: proxy not installed. Run 'make install' first."; exit 1; }
	@cp $(TARGET) "$(LIVE)"
	@echo "redeployed"

uninstall:
	@if [ -f "$(ORIG)" ]; then \
		echo "restoring original mono"; \
		mv "$(ORIG)" "$(LIVE)"; \
	elif [ -f "$(BACKUP)" ]; then \
		echo "restoring from backup"; \
		cp "$(BACKUP)" "$(LIVE)"; \
	else \
		echo "ERROR: nothing to restore. Steam verify integrity to redownload mono-2.0-sgen.dll."; exit 1; \
	fi
	@echo "uninstalled"

clean:
	rm -f proxy.def proxy.exp exports.txt $(TARGET)
