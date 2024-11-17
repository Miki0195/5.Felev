import socket
import hashlib
import sys

def send_file_to_server(file_path, server_ip, server_port):
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        s.connect((server_ip, server_port))
        with open(file_path, 'rb') as f:
            data = f.read(1024)
            while data:
                s.sendall(data)
                data = f.read(1024)
    print("Fájl átvitele sikeres.")

def calculate_md5(file_path):
    md5_hash = hashlib.md5()
    with open(file_path, 'rb') as f:
        for byte_block in iter(lambda: f.read(4096), b""):
            md5_hash.update(byte_block)
    return md5_hash.hexdigest()

def send_checksum_to_server(checksum, file_id, chsum_srv_ip, chsum_srv_port):
    checksum_len = len(checksum)
    request = f"BE|{file_id}|60|{checksum_len}|{checksum}"
    
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        s.connect((chsum_srv_ip, chsum_srv_port))
        s.sendall(request.encode())
        response = s.recv(1024).decode().strip()
        if response == "OK":
            print("Checksum sikeresen feltöltve.")
        else:
            print("Hiba történt a checksum feltöltése során.")

if __name__ == "__main__":
    if len(sys.argv) != 7:
        print("Használat: python3 netcopy_cli.py <srv_ip> <srv_port> <chsum_srv_ip> <chsum_srv_port> <file_id> <file_path>")
        sys.exit(1)
    
    srv_ip = sys.argv[1]
    srv_port = int(sys.argv[2])
    chsum_srv_ip = sys.argv[3]
    chsum_srv_port = int(sys.argv[4])
    file_id = sys.argv[5]
    file_path = sys.argv[6]

    send_file_to_server(file_path, srv_ip, srv_port)
    
    checksum = calculate_md5(file_path)
    send_checksum_to_server(checksum, file_id, chsum_srv_ip, chsum_srv_port)
