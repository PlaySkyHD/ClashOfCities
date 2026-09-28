#!/usr/bin/env python3
"""Prepare native builds locally or verify/export them in the release workflow."""
import argparse
import hashlib
import io
import json
from pathlib import Path
import shutil
import tarfile
import zipfile

ROOT = Path(__file__).resolve().parent.parent
PLATFORMS = {
    'Linux': ('ClashOfCities.x86_64', 'tar.gz'),
    'Windows': ('ClashOfCities.exe', 'zip'),
    'macOS': ('ClashOfCities.app/Contents/MacOS/Clash of Cities', 'tar.gz'),
}


def source_hash(root):
    digest = hashlib.sha256()
    for folder in ('Assets', 'Packages', 'ProjectSettings'):
        for path in sorted((root / folder).rglob('*')):
            if path.is_file():
                digest.update(path.relative_to(root).as_posix().encode() + b'\0')
                digest.update(hashlib.sha256(path.read_bytes()).digest())
    return digest.hexdigest()


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def prepare(root):
    destination = root / 'native'
    destination.mkdir(exist_ok=True)
    manifest = {'source_sha256': source_hash(root), 'platforms': {}}
    for platform, (executable, extension) in PLATFORMS.items():
        source = root / 'Builds' / platform
        if not (source / executable).is_file():
            raise ValueError(f'Missing native player: {source / executable}')
        paths = [p for p in sorted(source.rglob('*')) if p.is_file()]
        for path in paths:
            if path.is_symlink():
                raise ValueError(f'Unexpected symlink: {path}')
        # Debug-only folders are not necessary for playing or distribution.
        paths = [p for p in paths if not any('BackUpThisFolder' in x or 'DoNotShip' in x for x in p.parts)]
        archive = destination / f'{platform}.{extension}'
        prefix = f'ClashOfCities-{platform}'
        instructions = {
            'Linux': 'Entpacken und ClashOfCities.x86_64 starten. Der gesamte Ordner wird benötigt.',
            'Windows': 'ZIP vollständig entpacken und ClashOfCities.exe starten. Alle DLLs und Datenordner behalten.',
            'macOS': 'Entpacken und ClashOfCities.app öffnen. Universal-Build für Intel und Apple Silicon.\nNicht mit einem Apple Developer-Zertifikat signiert/notarisiert; macOS kann den Start blockieren.\nAuf einem echten Mac noch nicht getestet.',
        }[platform].encode()
        if extension == 'zip':
            with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED) as z:
                for path in paths:
                    z.write(path, f'{prefix}/{path.relative_to(source).as_posix()}')
                z.writestr(f'{prefix}/SPIELEN.txt', instructions)
        else:
            with tarfile.open(archive, 'w:gz') as tar:
                for path in paths:
                    name = f'{prefix}/{path.relative_to(source).as_posix()}'
                    tar.add(path, arcname=name, recursive=False)
                info = tarfile.TarInfo(f'{prefix}/SPIELEN.txt')
                info.size = len(instructions)
                info.mode = 0o644
                tar.addfile(info, io.BytesIO(instructions))
        if archive.stat().st_size >= 95 * 1024 * 1024:
            raise ValueError(f'Archive too large for Git: {archive}')
        manifest['platforms'][platform] = {'file': archive.name, 'sha256': sha(archive)}
        print(f'Prepared {platform}: {archive.stat().st_size // 1024 // 1024} MiB')
    (destination / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')


def export(root, version, output):
    # Reuse the browser packager's version convention before deriving filenames.
    import re
    if not re.fullmatch(r'v\d+\.\d+\.\d+', version):
        raise ValueError('Version must have the format v1.2.3')
    manifest = json.loads((root / 'native/manifest.json').read_text())
    if manifest['source_sha256'] != source_hash(root):
        raise ValueError('Native builds are stale: rebuild all platforms and run scripts/native-release.py prepare')
    output.mkdir(parents=True, exist_ok=True)
    sums = []
    for platform, (_, extension) in PLATFORMS.items():
        item = manifest['platforms'][platform]
        if item['file'] != f'{platform}.{extension}':
            raise ValueError(f'Unexpected native archive name for {platform}')
        archive = root / 'native' / item['file']
        digest = sha(archive)
        if digest != item['sha256']:
            raise ValueError(f'Native archive checksum mismatch: {platform}')
        target = output / f'ClashOfCities-{version}-{platform}.{extension}'
        shutil.copyfile(archive, target)
        sums.append(f'{digest}  {target.name}\n')
    with (output / 'SHA256SUMS.txt').open('a') as f:
        f.writelines(sums)
    print('Native Linux, Windows and macOS archives verified and exported')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('command', choices=['prepare', 'export'])
    parser.add_argument('--version')
    parser.add_argument('--output', type=Path, default=Path('dist'))
    args = parser.parse_args()
    if args.command == 'prepare':
        prepare(ROOT)
    else:
        if not args.version:
            parser.error('export requires --version')
        export(ROOT, args.version, args.output)
