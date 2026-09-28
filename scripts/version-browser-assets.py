#!/usr/bin/env python3
"""Use content-versioned URLs so browsers never mix files across game updates."""
import hashlib
from pathlib import Path
import re
import sys
root = Path(sys.argv[1])
index = root / 'index.html'
def version(match):
    path = match.group(1)
    digest = hashlib.sha256((root / path).read_bytes()).hexdigest()[:16]
    return path + '?v=' + digest
text = re.sub(r'(Build/[A-Za-z0-9_.-]+)(?:\?v=[a-f0-9]+)?', version, index.read_text())
index.write_text(text)
print('Browser asset URLs versioned by content.')
