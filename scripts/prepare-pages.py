#!/usr/bin/env python3
"""Export only the browser player's public files for GitHub Pages."""
import os
import shutil
from pathlib import Path

root = Path(__file__).resolve().parent.parent
source = Path(os.environ.get('CLASH_WEB_OUTPUT', root / 'Builds/Web'))
target = root / 'web'
files = ['index.html', 'touch.js', 'Build/Web.loader.js',
         'Build/Web.data.unityweb', 'Build/Web.framework.js.unityweb',
         'Build/Web.wasm.unityweb']
if (source / 'StreamingAssets').exists():
    files += [str(p.relative_to(source)) for p in (source / 'StreamingAssets').rglob('*') if p.is_file()]
for name in files:
    path = source / name
    if not path.is_file() or path.stat().st_size == 0:
        raise SystemExit(f'Missing or empty browser asset: {name}')
    if path.is_symlink() or not path.resolve().is_relative_to(source.resolve()):
        raise SystemExit(f'Unsafe browser asset: {name}')
# Validate the entire input before replacing the generated output.
if target.is_symlink():
    raise SystemExit('web must not be a symlink')
if target.exists():
    shutil.rmtree(target)
for name in files:
    destination = target / name
    destination.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source / name, destination)
(target / '.nojekyll').touch()
print(f'GitHub Pages: {len(files)} browser assets exported to {target}')
