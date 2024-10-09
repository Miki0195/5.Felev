import struct
import sys

if len(sys.argv) < 2:
    print("Usage: python", sys.argv[0], "file1 file2 file3 file4")
    sys.exit(1)

formats = [
    'c9si',  
    'if?',    
    '?9sc',   
    'icf'   
]

for file, fmt in zip(sys.argv[1:], formats):
    with open(file, 'rb') as f:
        data = f.read(struct.calcsize(fmt))  
        unpacked_data = struct.unpack(fmt, data) 
        print(unpacked_data)

data_to_pack = [
    ("13s i ?", ("elso".encode(), 53, True)),
    ("f ? c", (56.5, False, 'X'.encode())),
    ("i 11s f", (44, "masodik".encode(), 63.9)),
    ("c i 14s", ('Z'.encode(), 75, "harmadik".encode()))
]

def pack_values(fmt, *values):
    return struct.pack(fmt, *values)

for fmt, values in data_to_pack:
    packed_data = pack_values(fmt, *values)
    print(packed_data)
