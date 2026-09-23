import struct
import sys

dll = sys.argv[1]
want = sys.argv[2]
list_fields = "--fields" in sys.argv
d = open(dll, "rb").read()
pe = struct.unpack_from("<I", d, 0x3C)[0]
opt = pe + 24
magic = struct.unpack_from("<H", d, opt)[0]
dd_off = opt + (112 if magic == 0x20B else 96) + 14 * 8
rva, _size = struct.unpack_from("<II", d, dd_off)
nsec = struct.unpack_from("<H", d, pe + 6)[0]
opt_size = struct.unpack_from("<H", d, pe + 20)[0]
sec = pe + 24 + opt_size


def rva_off(r):
    for i in range(nsec):
        o = sec + i * 40
        vs, va, rs, ro = struct.unpack_from("<IIII", d, o + 8)
        if va <= r < va + max(vs, rs):
            return ro + (r - va)
    raise SystemExit("rva")


cli_off = rva_off(rva)
meta_rva = struct.unpack_from("<I", d, cli_off + 8)[0]
m = rva_off(meta_rva)
assert d[m : m + 4] == b"BSJB", d[m : m + 4]
ver_len = struct.unpack_from("<I", d, m + 12)[0]
p0 = (m + 16 + ver_len + 3) & ~3
_flags, streams = struct.unpack_from("<HH", d, p0)
q = p0 + 4
streams_map = {}
for _i in range(streams):
    off, sz = struct.unpack_from("<II", d, q)
    q += 8
    end = d.index(b"\x00", q)
    name = d[q:end].decode()
    q = (end + 1 + 3) & ~3
    streams_map[name] = (m + off, sz)

tilde, _tsz = streams_map["#~"]
strings, _ssz = streams_map["#Strings"]
blobs, _bsz = streams_map["#Blob"]


def blob_len(idx):
    b0 = d[blobs + idx]
    if b0 & 0x80 == 0:
        return b0, blobs + idx + 1
    if b0 & 0xC0 == 0x80:
        v = ((b0 & 0x3F) << 8) | d[blobs + idx + 1]
        return v, blobs + idx + 2
    v = ((b0 & 0x1F) << 24) | (d[blobs + idx + 1] << 16) | (d[blobs + idx + 2] << 8) | d[blobs + idx + 3]
    return v, blobs + idx + 4


def s(idx):
    if idx == 0:
        return ""
    end = d.index(b"\x00", strings + idx)
    return d[strings + idx : end].decode("utf-8", "replace")


heaps = d[tilde + 6]
valid = struct.unpack_from("<Q", d, tilde + 8)[0]
rows = []
pos = tilde + 24
for bit in range(64):
    if valid & (1 << bit):
        rows.append(struct.unpack_from("<I", d, pos)[0])
        pos += 4
    else:
        rows.append(0)

strw = 4 if heaps & 1 else 2
guidw = 4 if heaps & 2 else 2
blobw = 4 if heaps & 4 else 2


def iw(n):
    return 4 if n > 65535 else 2


def coded(tag_bits, tables):
    mx = max((rows[t] for t in tables), default=0)
    return 4 if mx >= (1 << (16 - tag_bits)) else 2


TypeDefOrRef = coded(2, [2, 1, 0x1B])
HasConstant = coded(2, [4, 8, 23])
HasCustomAttribute = coded(5, [6, 4, 1, 2, 8, 9, 10, 0, 14, 17, 20, 23, 26, 27, 32, 35, 38, 39, 40, 42, 44])
HasFieldMarshal = coded(1, [4, 8])
HasDeclSecurity = coded(2, [2, 6, 32])
MemberRefParent = coded(3, [2, 1, 26, 6, 27])
CustomAttributeType = coded(3, [6, 10])
ResolutionScope = coded(2, [0, 26, 35, 1])

sizes = {
    0: 2 + strw + guidw * 3,
    1: ResolutionScope + strw + strw,
    2: 4 + strw + strw + TypeDefOrRef + iw(rows[4]) + iw(rows[6]),
    3: iw(rows[4]),
    4: 2 + strw + blobw,
    5: iw(rows[6]),
    6: 4 + 2 + 2 + strw + blobw + iw(rows[8]),
    7: iw(rows[8]),
    8: 2 + 2 + strw,
    9: iw(rows[2]) + TypeDefOrRef,
    10: MemberRefParent + strw + blobw,
    11: 2 + HasConstant + blobw,
    12: HasCustomAttribute + CustomAttributeType + blobw,
    13: HasFieldMarshal + blobw,
    14: 2 + HasDeclSecurity + blobw,
    15: 2 + 4 + iw(rows[2]),
    16: 4 + iw(rows[4]),
    17: blobw,
    18: iw(rows[2]) + iw(rows[20]),
    19: iw(rows[20]),
    20: 2 + strw + TypeDefOrRef,
    21: iw(rows[2]) + iw(rows[23]),
    22: iw(rows[23]),
    23: 2 + strw + blobw,
    24: 2 + iw(rows[6]) + coded(1, [20, 23]),
    25: iw(rows[2]) + coded(1, [6, 10]) + coded(1, [6, 10]),
    26: strw,
    27: blobw,
    28: 2 + coded(1, [4, 6]) + strw + coded(2, [38, 35, 39]),
    29: 4 + iw(rows[4]),
    30: 4 + 4,
    31: 4,
    32: 4 + 2 + 2 + 2 + 2 + 4 + blobw + strw + strw,
    33: 4,
    34: 4 + 4 + 4,
    35: 2 + 2 + 2 + 2 + 4 + blobw + strw + strw + blobw,
    36: 4 + iw(rows[35]),
    37: 4 + 4 + 4 + iw(rows[35]),
    38: 4 + strw + blobw,
    39: 4 + 4 + strw + strw + coded(2, [38, 35, 39]),
    40: 4 + 4 + strw + coded(2, [38, 35, 39]),
    41: iw(rows[2]) + iw(rows[2]),
}

order = [i for i in range(42) if rows[i]]
missing = [i for i in order if i not in sizes]
if missing:
    raise SystemExit(f"missing {missing}")
body = pos
offsets = {}
for i in order:
    offsets[i] = body
    body += rows[i] * sizes[i]


def row(table, index):
    return offsets[table] + (index - 1) * sizes[table]


def read_idx(buf_off, width):
    if width == 2:
        return struct.unpack_from("<H", d, buf_off)[0]
    return struct.unpack_from("<I", d, buf_off)[0]


matches = []
for i in range(1, rows[2] + 1):
    o = row(2, i)
    name = s(read_idx(o + 4, strw))
    ns = s(read_idx(o + 4 + strw, strw))
    if want in name:
        matches.append((i, ns, name))

print("matches", len(matches))
for i, ns, name in matches:
    if ns not in ("Sandbox", "Sandbox.UI", "Sandbox.TextRendering", ""):
        continue
    print(f"TYPE {ns}.{name} row {i}")
    if not list_fields:
        continue
    o = row(2, i)
    field_start = read_idx(o + 4 + strw + strw + TypeDefOrRef, iw(rows[4]))
    field_end = rows[4] + 1
    if i < rows[2]:
        o2 = row(2, i + 1)
        field_end = read_idx(o2 + 4 + strw + strw + TypeDefOrRef, iw(rows[4]))
    for fi in range(field_start, field_end):
        fo = row(4, fi)
        print("  field", s(read_idx(fo + 2, strw)))

typedef_row = None
for i, ns, name in matches:
    if name == want and ns == "Sandbox":
        typedef_row = i
        break
if typedef_row is None:
    for i, ns, name in matches:
        if name == want:
            typedef_row = i
            break
if typedef_row is None:
    raise SystemExit(0)

if typedef_row is None:
    raise SystemExit("not found")

for i in range(1, rows[21] + 1):
    o = row(21, i)
    parent = read_idx(o, iw(rows[2]))
    if parent != typedef_row:
        continue
    plist = read_idx(o + iw(rows[2]), iw(rows[23]))
    nxt = rows[23] + 1
    if i < rows[21]:
        o2 = row(21, i + 1)
        nxt = read_idx(o2 + iw(rows[2]), iw(rows[23]))
    print("properties:")
    for pidx in range(plist, nxt):
        po = row(23, pidx)
        pname = s(read_idx(po + 2, strw))
        bidx = read_idx(po + 2 + strw, blobw)
        blen, boff = blob_len(bidx)
        sig = d[boff : boff + min(blen, 24)]
        print(" ", pname, sig.hex())
    break

o = row(2, typedef_row)
method_start = read_idx(o + 4 + strw + strw + TypeDefOrRef + iw(rows[4]), iw(rows[6]))
method_end = rows[6] + 1
if typedef_row < rows[2]:
    o2 = row(2, typedef_row + 1)
    method_end = read_idx(o2 + 4 + strw + strw + TypeDefOrRef + iw(rows[4]), iw(rows[6]))
print("methods:")
for mi in range(method_start, method_end):
    mo = row(6, mi)
    print(" ", s(read_idx(mo + 8, strw)))
