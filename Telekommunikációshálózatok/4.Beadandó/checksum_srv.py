import socket
import threading
import time

class ChecksumServer:
    def __init__(self, ip, port):
        self.server_ip = ip
        self.server_port = port
        self.data_store = {}  

    def start(self):
        server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        server_socket.bind((self.server_ip, self.server_port))
        server_socket.listen(5)
        print(f"Checksum szerver fut {self.server_ip}:{self.server_port} címen.")

        while True:
            client_socket, client_address = server_socket.accept()
            threading.Thread(target=self.handle_client, args=(client_socket,)).start()

    def handle_client(self, client_socket):
        request = client_socket.recv(1024).decode().strip()
        if request.startswith("BE|"):
            self.handle_insert(request, client_socket)
        elif request.startswith("KI|"):
            self.handle_query(request, client_socket)
        client_socket.close()

    def handle_insert(self, request, client_socket):
        try:
            _, file_id, validity, checksum_len, checksum = request.split("|")
            expiry_time = time.time() + int(validity)
            self.data_store[file_id] = (expiry_time, checksum_len, checksum)
            client_socket.sendall(b"OK")
        except Exception as e:
            print(f"Error handling insert: {e}")
            client_socket.sendall(b"ERROR")

    def handle_query(self, request, client_socket):
        try:
            _, file_id = request.split("|")
            current_time = time.time()
            if file_id in self.data_store:
                expiry_time, checksum_len, checksum = self.data_store[file_id]
                if current_time < expiry_time:
                    response = f"{checksum_len}|{checksum}"
                    client_socket.sendall(response.encode())
                else:
                    del self.data_store[file_id]
                    client_socket.sendall(b"0|")
            else:
                client_socket.sendall(b"0|")
        except Exception as e:
            print(f"Error handling query: {e}")
            client_socket.sendall(b"ERROR")

if __name__ == "__main__":
    import sys
    if len(sys.argv) != 3:
        print("Használat: python3 checksum_srv.py <ip> <port>")
        sys.exit(1)
    
    ip = sys.argv[1]
    port = int(sys.argv[2])
    
    server = ChecksumServer(ip, port)
    server.start()
