import socket
import random
import struct
import select

def start_server(hostname, port):
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.bind((hostname, port))
    server_socket.listen(5)

    print(f"Szerver indítása: {hostname}:{port}")
    
    secret_number = random.randint(1, 100)
    print(f"A titkos szám: {secret_number}")
    
    clients = []
    game_over = False

    while True:
        read_sockets, _, _ = select.select([server_socket] + clients, [], [])

        for sock in read_sockets:
            if sock is server_socket:
                client_socket, address = server_socket.accept()
                print(f"Kapcsolódott: {address}")
                clients.append(client_socket)
            else:
                try:
                    data = sock.recv(8)
                    if not data:
                        clients.remove(sock)
                        sock.close()
                        continue

                    guess_char, guess_number = struct.unpack('ci', data)
                    guess_char = guess_char.decode('utf-8')

                    print(f"Received guess: {guess_char} {guess_number}")

                    if game_over:
                        response = struct.pack('ci', b'V', 0)
                        sock.send(response)
                        clients.remove(sock)
                        sock.close()
                        continue

                    if guess_char == '=':
                        if guess_number == secret_number:
                            response = struct.pack('ci', b'Y', 0)
                            game_over = True
                        else:
                            response = struct.pack('ci', b'K', 0)
                    elif guess_char == '<':
                        if guess_number > secret_number:
                            response = struct.pack('ci', b'I', 0)
                        else:
                            response = struct.pack('ci', b'N', 0)
                    elif guess_char == '>':
                        if guess_number < secret_number:
                            response = struct.pack('ci', b'I', 0)
                        else:
                            response = struct.pack('ci', b'N', 0)

                    print(f"Sending response: {response}")

                    sock.send(response)

                    if game_over:
                        for client in clients:
                            client.send(struct.pack('ci', b'V', 0))
                            client.close()
                        clients = []
                        secret_number = random.randint(1, 100)
                        print(f"Új titkos szám: {secret_number}")
                        game_over = False

                except Exception as e:
                    print(f"Hiba: {e}")
                    clients.remove(sock)
                    sock.close()

if __name__ == "__main__":
    import sys
    if len(sys.argv) != 3:
        print("Használat: python3 server.py <hostname> <port>")
        sys.exit(1)

    hostname = sys.argv[1]
    port = int(sys.argv[2])
    start_server(hostname, port)