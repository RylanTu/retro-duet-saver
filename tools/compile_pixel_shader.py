"""Rebuild the embedded WPF shader on Windows: python tools/compile_pixel_shader.py."""
import ctypes as c
from pathlib import Path
root = Path(__file__).resolve().parents[1]
source = (root / 'shaders/PixelShrink.hlsl').read_bytes()
dll = c.WinDLL('d3dcompiler_47.dll')
compile_shader = dll.D3DCompile
compile_shader.restype = c.c_long
compile_shader.argtypes = [c.c_void_p, c.c_size_t, c.c_char_p, c.c_void_p, c.c_void_p, c.c_char_p, c.c_char_p, c.c_uint, c.c_uint, c.POINTER(c.c_void_p), c.POINTER(c.c_void_p)]
def blob_data(blob):
    table = c.cast(blob, c.POINTER(c.POINTER(c.c_void_p))).contents
    ptr = c.WINFUNCTYPE(c.c_void_p, c.c_void_p)(table[3])(blob)
    size = c.WINFUNCTYPE(c.c_size_t, c.c_void_p)(table[4])(blob)
    return c.string_at(ptr, size)
def release(blob):
    if blob:
        table = c.cast(blob, c.POINTER(c.POINTER(c.c_void_p))).contents
        c.WINFUNCTYPE(c.c_ulong, c.c_void_p)(table[2])(blob)
code, errors = c.c_void_p(), c.c_void_p()
try:
    result = compile_shader(source, len(source), b'PixelShrink.hlsl', None, None, b'main', b'ps_2_0', 1 << 15, 0, c.byref(code), c.byref(errors))
    if result < 0:
        raise RuntimeError(blob_data(errors).decode(errors='replace') if errors else hex(result & 0xffffffff))
    target = root / 'shaders/PixelShrink.ps'
    target.write_bytes(blob_data(code))
    print(f'{target}: {target.stat().st_size} bytes')
finally:
    release(code)
    release(errors)
