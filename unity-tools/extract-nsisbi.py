"""Extract the signed Unity NSISBI solid LZMA payload as ordinary files.

Uses the source-defined compiled-header parser from KokerZhou/NSISExtractor.
No installer instructions, registry writes or elevation are executed.
"""
import hashlib
import json
import lzma
from pathlib import Path
import struct
import sys
import re

sys.path[:0] = [r'D:\Unity\Tools\NSISExtractor', r'D:\Unity\Tools\NSISExtractor\dependencies']
from nsis_extractor.nsis_parser import _parse_first_header, _parse_header
from nsis_extractor.formats.nsis3 import parse_compiled_header, decode_unicode_string

source = Path(sys.argv[1])
output = Path(sys.argv[2]).resolve()
scratch = output.parent / (output.name + '-solid-data.bin')
output.mkdir(parents=True, exist_ok=True)

with source.open('rb') as f:
    prefix = f.read(1024 * 1024)
    signature = b'\xef\xbe\xad\xdeNullsoftInst'
    start = prefix.index(signature) - 4
    if start % 512:
        raise RuntimeError('NSIS stream is not aligned')
    first = _parse_first_header(prefix[start:start + 36])
    data_size = first.nsisbi_datablock_size
    if not data_size or data_size > 40 * 1024**3:
        raise RuntimeError('Unexpected payload size')
    expected = data_size + 8 + first.length_of_header
    f.seek(start + 36)
    total = 0
    last_report = 0
    if '--reuse-solid' in sys.argv and scratch.exists() and scratch.stat().st_size == expected:
        total = expected
    else:
        with scratch.open('wb') as out:
            while True:
                length_bytes = f.read(3)
                if len(length_bytes) != 3:
                    raise RuntimeError('Truncated chunk prefix')
                length = int.from_bytes(length_bytes, 'little')
                if length == 0:
                    break
                block = f.read(length)
                if len(block) != length or length < 6:
                    raise RuntimeError('Truncated compressed chunk')
                props = block[0]
                lc = props % 9
                lp = (props // 9) % 5
                pb = props // 45
                dictionary = int.from_bytes(block[1:5], 'little')
                if dictionary > 64 * 1024**2:
                    raise RuntimeError('Unexpected dictionary size')
                decoder = lzma.LZMADecompressor(format=lzma.FORMAT_RAW, filters=[{'id': lzma.FILTER_LZMA1, 'dict_size': dictionary, 'lc': lc, 'lp': lp, 'pb': pb}])
                decoded = decoder.decompress(block[5:], max_length=16 * 1024**2)
                if not decoder.eof or decoder.unused_data:
                    raise RuntimeError('Chunk did not decode exactly')
                out.write(decoded)
                total += len(decoded)
                if total > expected:
                    raise RuntimeError('Payload exceeds declared size')
                if total - last_report > 512 * 1024**2:
                    print(f'Decompressed {total // (1024**2)} / {expected // (1024**2)} MiB', flush=True)
                    last_report = total
    if total != expected:
        raise RuntimeError(f'Decompressed size differs: {total} != {expected}')

with scratch.open('rb') as f:
    header_size = int.from_bytes(f.read(8), 'little')
    if header_size != first.length_of_header:
        raise RuntimeError('Compiled header length differs')
    header = f.read(header_size)
    common = _parse_header(header)
    parsed = parse_compiled_header(header, common, first)
    if parsed is None:
        raise RuntimeError('Unsupported compiled header structure')
    base = 8 + header_size
    # Recursive File directives save their root in NSIS's internal _OUTDIR.
    # Reconstruct just these path assignments; never run installer control flow.
    variables = {'OUTDIR': '', '_OUTDIR': '', 'INSTDIR': '$INSTDIR'}
    if 'WebGL-Support' in source.name:
        variables['VAR_95'] = '/Editor/Data/PlaybackEngines/WebGLSupport'
    strings = header[common.block_headers[3].offset:common.block_headers[4].offset]
    directories = {}
    def resolve(value):
        for _ in range(6):
            updated = re.sub(r'\$(?:VAR_\d+|[A-Za-z_]+|R?\d+)', lambda m: variables.get(m.group()[1:], m.group()), value)
            if updated == value:
                return updated
            value = updated
        return value
    for index in range(parsed.entry_count):
        values = struct.unpack_from('<9i', header, common.block_headers[2].offset + index * 36)
        if values[0] == 11 and values[2]:
            variables['OUTDIR'] = resolve(decode_unicode_string(strings, values[1]))
        elif values[0] == 27 and values[1] == 31:
            variables['_OUTDIR'] = resolve(decode_unicode_string(strings, values[2]))
        elif values[0] == 20:
            directories[index] = variables['OUTDIR']
    entries = []
    seen = set()
    for entry in parsed.extract_file_entries:
        if entry.encoded_name.startswith('$'):
            continue
        directory = directories.get(entry.entry_index, entry.output_directory or '')
        local = (directory + '/' + entry.encoded_name).replace('\\', '/')
        if local.startswith('$INSTDIR/'):
            local = local[len('$INSTDIR/'):]
        local = local.lstrip('/')
        # Only the Editor tree is needed. Keep auxiliary installer plugin roots out.
        if not local.startswith('Editor/'):
            continue
        identity = (local, entry.data_block_offset)
        if identity in seen:
            continue
        seen.add(identity)
        destination = (output / local).resolve()
        if not destination.is_relative_to(output):
            raise RuntimeError('Path escaped output directory')
        f.seek(base + entry.data_block_offset)
        length = int.from_bytes(f.read(8), 'little')
        if length > total - f.tell():
            raise RuntimeError('File range exceeds payload: ' + local)
        destination.parent.mkdir(parents=True, exist_ok=True)
        digest = hashlib.sha256()
        with destination.open('wb') as out:
            remaining = length
            while remaining:
                block = f.read(min(1024**2, remaining))
                if not block:
                    raise RuntimeError('Truncated file: ' + local)
                out.write(block)
                digest.update(block)
                remaining -= len(block)
        entries.append({'path': local, 'bytes': length, 'sha256': digest.hexdigest()})
    report = {'installer': str(source), 'decompressedBytes': total, 'files': entries}
    (output / 'extraction-report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps({'files': len(entries), 'editorExists': (output / 'Editor/Unity.exe').exists(), 'output': str(output)}), flush=True)
