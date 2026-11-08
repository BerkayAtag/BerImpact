"""Package previously published binaries/images; never include workstation secrets."""
import hashlib
import json
from pathlib import Path
import shutil
import tarfile
import zipfile

ROOT = Path(__file__).resolve().parents[1]
VERSION = "0.1.1"
OUT = ROOT / "artifacts" / "release"
OUT.mkdir(parents=True, exist_ok=True)


def digest(path):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def prepare(folder):
    shutil.copytree(ROOT / "docs", folder / "docs", dirs_exist_ok=True)
    files = sorted(p for p in folder.rglob("*") if p.is_file() and p.name != "manifest.json")
    for path in files:
        assert not path.is_symlink()
        assert path.suffix.lower() not in {".pfx", ".key", ".dump", ".env"}, path
        assert ".local" not in path.parts, path
    (folder / "manifest.json").write_text(json.dumps([
        {"Path": p.relative_to(folder).as_posix(), "SHA256": digest(p)} for p in files
    ], indent=2), encoding="utf-8")
    return files + [folder / "manifest.json"]


windows = ROOT / "artifacts" / f"windows-x64-{VERSION}"
assert (windows / "api/BerImpact.Api.exe").is_file()
assert (windows / "worker/BerImpact.Workers.exe").is_file()
archive = OUT / f"berimpact-{VERSION}-windows-x64.zip"
with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED, compresslevel=6) as output:
    for path in prepare(windows):
        output.write(path, path.relative_to(windows).as_posix())
with zipfile.ZipFile(archive) as output:
    assert output.testzip() is None
print(archive.name, archive.stat().st_size, flush=True)

offline = ROOT / "artifacts/offline"
assert (offline / "berimpact-images.tar").is_file()
archive = OUT / f"berimpact-{VERSION}-linux-offline.tar.gz"
with tarfile.open(archive, "w:gz", compresslevel=3) as output:
    for path in prepare(offline):
        output.add(path, arcname=path.relative_to(offline).as_posix(), recursive=False)
with tarfile.open(archive) as output:
    manifest = json.load(output.extractfile("manifest.json"))
    for item in manifest:
        assert hashlib.file_digest(output.extractfile(item["Path"]), "sha256").hexdigest() == item["SHA256"]
print(archive.name, archive.stat().st_size, flush=True)

packages = sorted(OUT.glob(f"berimpact-{VERSION}-*"))
(OUT / "SHA256SUMS.txt").write_text("".join(f"{digest(p)}  {p.name}\n" for p in packages), encoding="utf-8")
print("Archive integrity and manifests verified.")
