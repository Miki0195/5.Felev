import struct
import sys

if len(sys.argv) < 2:
    print("Usage: python", sys.argv[0], "file1 file2 file3 file4")
    sys.exit(1)

formats = [
    '9sif',  
    'fb?',    
    'c9si',   
    'f9s?'   
]

for file, fmt in zip(sys.argv[1:], formats):
    with open(file, 'rb') as f:
        data = f.read(struct.calcsize(fmt))  
        unpacked_data = struct.unpack(fmt, data) 
        print(unpacked_data)

data_to_pack = [
    ("17s i ?", ("elso".encode(), 85, True)),
    ("f ? c", (88.5, False, 'X'.encode())),
    ("i 15s f", (76, "masodik".encode(), 95.9)),
    ("c i 18s", ('Z'.encode(), 107, "harmadik".encode()))
]

def pack_values(fmt, *values):
    return struct.pack(fmt, *values)

for fmt, values in data_to_pack:
    packed_data = pack_values(fmt, *values)
    print(packed_data)
