# -*- coding: utf-8 -*-
"""시안용 PNG 읽기/쓰기 - 순수 파이썬 (PIL 없음)

Tools/AssetGen의 생성기들은 PNG를 쓰기만 하지만, 시안을 만들려면 기존
스프라이트를 읽어야 한다. 디코더와 박스 축소를 여기 모아 둔다.
"""
import struct, zlib


def decode(path):
    raw = open(path, "rb").read()
    pos, idat, w, h = 8, b"", None, None
    while pos < len(raw):
        ln = struct.unpack(">I", raw[pos:pos + 4])[0]
        tag = raw[pos + 4:pos + 8]
        if tag == b"IHDR":
            w, h, bd, ct = struct.unpack(">IIBB", raw[pos + 8:pos + 18])
            assert bd == 8 and ct == 6, (path, bd, ct)
        elif tag == b"IDAT":
            idat += raw[pos + 8:pos + 8 + ln]
        pos += 12 + ln
    data = zlib.decompress(idat)
    stride, off = w * 4, 0
    out, prev = bytearray(stride * h), bytearray(stride)
    for y in range(h):
        f = data[off]; off += 1
        line = bytearray(data[off:off + stride]); off += stride
        if f == 1:
            for i in range(4, stride):
                line[i] = (line[i] + line[i - 4]) & 255
        elif f == 2:
            for i in range(stride):
                line[i] = (line[i] + prev[i]) & 255
        elif f == 3:
            for i in range(stride):
                a = line[i - 4] if i >= 4 else 0
                line[i] = (line[i] + ((a + prev[i]) >> 1)) & 255
        elif f == 4:
            for i in range(stride):
                a = line[i - 4] if i >= 4 else 0
                b, c = prev[i], (prev[i - 4] if i >= 4 else 0)
                p = a + b - c
                pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
                line[i] = (line[i] + (a if (pa <= pb and pa <= pc)
                                      else (b if pb <= pc else c))) & 255
        elif f != 0:
            raise SystemExit("filter %d" % f)
        out[y * stride:(y + 1) * stride] = line
        prev = line
    return w, h, out


def box(w, h, px, n):
    """n x n 으로 박스 다운샘플 - 알파 가중 평균이라 가장자리가 검게 죽지 않는다"""
    out = [[(0, 0, 0, 0)] * n for _ in range(n)]
    for oy in range(n):
        for ox in range(n):
            x0, x1 = ox * w // n, max(ox * w // n + 1, (ox + 1) * w // n)
            y0, y1 = oy * h // n, max(oy * h // n + 1, (oy + 1) * h // n)
            r = g = b = a = cnt = 0
            for y in range(y0, y1):
                for x in range(x0, x1):
                    i = (y * w + x) * 4
                    al = px[i + 3]
                    r += px[i] * al; g += px[i + 1] * al; b += px[i + 2] * al
                    a += al; cnt += 1
            if a:
                out[oy][ox] = (r // a, g // a, b // a, a // cnt)
    return out


def chunk(tag, data):
    return (struct.pack(">I", len(data)) + tag + data
            + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF))


def write_png(path, canvas):
    W, H = len(canvas[0]), len(canvas)
    raw = bytearray()
    for row in canvas:
        raw.append(0)
        for c in row:
            raw += bytes(c)
    open(path, "wb").write(
        b"\x89PNG\r\n\x1a\n"
        + chunk(b"IHDR", struct.pack(">IIBBBBB", W, H, 8, 2, 0, 0, 0))
        + chunk(b"IDAT", zlib.compress(bytes(raw), 9)) + chunk(b"IEND", b""))
    return W, H
