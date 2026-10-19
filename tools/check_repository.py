"""Inspect the Git index before creating a source release. Does not modify Git."""
from pathlib import Path
import json
import re
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parent.parent
git = shutil.which("git") or str(ROOT / ".local/git/cmd/git.exe")


def run(*args):
    return subprocess.check_output([git, "-C", str(ROOT), *args])


def main():
    names = run("ls-files", "--cached", "-z").decode("utf-8").strip("\0").split("\0")
    if names == [""]:
        raise SystemExit("No staged source files; run git add first")
    errors = []
    forbidden_roots = {".local", ".venv", ".platformio", ".dotnet", ".dotnet-home", ".nuget", "reports", "artifacts", "upstream", "frimvare"}
    forbidden_suffixes = {".bin", ".zip", ".7z", ".exe", ".msi", ".log", ".key", ".pem", ".elf", ".map"}
    patterns = [re.compile(r"[A-Za-z]:[\\/]Users[\\/][^\s<>]+"),
                re.compile(r"S-1-5-21-\d+-\d+-\d+-\d+"),
                re.compile(r"-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----"),
                re.compile(r"\bAKIA[A-Z0-9]{16}\b")]
    for name in names:
        path = Path(name)
        if path.parts[0] in forbidden_roots or any(part in {"bin", "obj", ".pio"} for part in path.parts) or path.suffix.lower() in forbidden_suffixes:
            errors.append("Generated/private file: " + name)
        if name == "firmware/data/config.json":
            errors.append("Personal firmware config: " + name)
        data = run("show", ":" + name)
        try:
            text = data.decode("utf-8-sig")
        except UnicodeDecodeError:
            continue
        if any(pattern.search(text) for pattern in patterns):
            errors.append("Possible personal path/credential: " + name)
        if path.suffix in {".json", ".example"}:
            try:
                config = json.loads(text)
                if isinstance(config, dict) and any(config.get(key) for key in ("wifi_password", "api_key", "access_token")):
                    errors.append("Non-empty credential in template: " + name)
            except json.JSONDecodeError:
                pass
        if path.suffix == ".md":
            prose = re.sub(r"```.*?```", "", text, flags=re.S)
            for link in re.findall(r"!?\[[^\]]*\]\(([^)]+)\)", prose):
                link = link.strip("<>").split("#")[0]
                if not link or ":" in link:
                    continue
                target = (ROOT / path.parent / link).resolve()
                if not target.exists():
                    errors.append(f"Broken relative link: {name} -> {link}")
    if errors:
        print("\n".join(errors))
        return 1
    print(f"PASS: {len(names)} indexed files; no excluded artifacts, detected credentials or broken local documentation links")
    return 0


if __name__ == "__main__":
    sys.exit(main())
