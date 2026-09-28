#!/usr/bin/env python3
"""Validate and package the committed WebGL player, without requiring Unity."""
import argparse
import gzip
import hashlib
from pathlib import Path
import re
import zipfile


def package(root, version, output):
    if not re.fullmatch(r'v\d+\.\d+\.\d+', version):
        raise ValueError('Version must have the format v1.2.3')
    required = {'index.html', 'touch.js', 'Build/Web.loader.js',
                'Build/Web.data.unityweb', 'Build/Web.framework.js.unityweb',
                'Build/Web.wasm.unityweb'}
    files = []
    for path in root.rglob('*'):
        if path.is_symlink():
            raise ValueError(f'Symlink not allowed: {path}')
        if not path.is_file():
            continue
        name = path.relative_to(root).as_posix()
        if name not in required | {'.nojekyll'} and not name.startswith('StreamingAssets/'):
            raise ValueError(f'Unexpected public file: {name}')
        if any(part.startswith('.') for part in Path(name).parts) and name != '.nojekyll':
            raise ValueError(f'Hidden file not allowed: {name}')
        files.append(path)
    present = {p.relative_to(root).as_posix() for p in files}
    if required - present:
        raise ValueError(f'Missing browser files: {sorted(required - present)}')
    for name in required:
        if not (root / name).stat().st_size:
            raise ValueError(f'Empty browser file: {name}')
    refs = re.findall(r'(Build/[A-Za-z0-9_.-]+)\?v=([a-f0-9]{16})', (root / 'index.html').read_text())
    if {name for name, _ in refs} != {name for name in required if name.startswith('Build/')}:
        raise ValueError('HTML must reference all four versioned Unity build files')
    for name, digest in refs:
        data = (root / name).read_bytes()
        if hashlib.sha256(data).hexdigest()[:16] != digest:
            raise ValueError(f'Stale build hash: {name}')
        if name.endswith('.unityweb'):
            gzip.decompress(data)
    output.mkdir(parents=True, exist_ok=True)
    archive = output / f'ClashOfCities-{version}-Browser.zip'
    with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED) as zip_file:
        for path in sorted(files):
            zip_file.write(path, path.relative_to(root))
        zip_file.writestr('SPIELEN.txt', 'Clash of Cities ' + version + '\n\nZIP entpacken. Im entpackten Ordner ausführen:\npython3 -m http.server 8765 --bind 127.0.0.1\n\nIm Browser http://localhost:8765 öffnen.\nindex.html nicht per Doppelklick öffnen.\n')
    checksum = hashlib.sha256(archive.read_bytes()).hexdigest()
    (output / 'SHA256SUMS.txt').write_text(f'{checksum}  {archive.name}\n')
    print(f'Validated and packaged: {archive}')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('version')
    parser.add_argument('--web', type=Path, default=Path('web'))
    parser.add_argument('--output', type=Path, default=Path('dist'))
    args = parser.parse_args()
    package(args.web, args.version, args.output)
