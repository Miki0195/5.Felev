import socket
import hashlib
import sys

def receive_file(server_ip, server_port, file_path):
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        s.bind((server_ip, server_port))
        s.listen(1)
        conn, addr = s.accept()
        with conn:
            with open(file_path, 'wb') as f:
                while True:
                    data = conn.recv(1024)
                    if not data:
                        break
                    f.write(data)
    print("Fájl fogadása befejeződött.")

def calculate_md5(file_path):
    md5_hash = hashlib.md5()
    with open(file_path, 'rb') as f:
        for byte_block in iter(lambda: f.read(4096), b""):
            md5_hash.update(byte_block)
    return md5_hash.hexdigest()

def verify_checksum(file_id, chsum_srv_ip, chsum_srv_port, local_md5):
    request = f"KI|{file_id}"
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        s.connect((chsum_srv_ip, chsum_srv_port))
        s.sendall(request.encode())
        response = s.recv(1024).decode().strip()
        if response.startswith("0|"):
            print("Nincs érvényes checksum a szerveren.")
            return False
        checksum_len, remote_md5 = response.split("|")
        return local_md5 == remote_md5

if __name__ == "__main__":
    if len(sys.argv) != 7:
        print("Használat: python3 netcopy_srv.py <srv_ip> <srv_port> <chsum_srv_ip> <chsum_srv_port> <file_id> <file_path>")
        sys.exit(1)

    srv_ip = sys.argv[1]
    srv_port = int(sys.argv[2])
    chsum_srv_ip = sys.argv[3]
    chsum_srv_port = int(sys.argv[4])
    file_id = sys.argv[5]
    file_path = sys.argv[6]

    receive_file(srv_ip, srv_port, file_path)
    
    local_md5 = calculate_md5(file_path)
    
    if verify_checksum(file_id, chsum_srv_ip, chsum_srv_port, local_md5):
        print("CSUM OK")
    else:
        print("CSUM CORRUPTED")
