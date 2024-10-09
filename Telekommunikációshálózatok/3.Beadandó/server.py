import socket
import random
import struct
import select

def start_server(hostname, port):
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.bind((hostname, port))
    server_socket.listen(5)

    print(f"Szerver indítása: {hostname}:{port}")
    
    # Véletlenszerű szám választása
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
                    data = sock.recv(8)  # 1 byte character + 4 byte integer
                    if data:
                        message = struct.unpack('!cI', data)
                        command = message[0].decode('utf-8')
                        guess = message[1]
                        
                        if command == '<':
                            response = 'I' if guess < secret_number else 'N'
                        elif command == '>':
                            response = 'I' if guess > secret_number else 'N'
                        elif command == '=':
                            response = 'Y'
                            game_over = True
                        else:
                            continue

                        sock.send(struct.pack('!cI', response.encode('utf-8'), 0))

                        if response == 'Y':
                            print("Valaki nyert!")
                            game_over = True
                    else:
                        print("A kliens bontotta a kapcsolatot.")
                        clients.remove(sock)
                        sock.close()
                except Exception as e:
                    print(f"Hiba: {e}")
                    clients.remove(sock)
                    sock.close()

        if game_over:
            for client in clients:
                client.send(struct.pack('!cI', b'V', 0))
            break

    for client in clients:
        client.close()
    server_socket.close()

if __name__ == "__main__":
    start_server('localhost', 10000)
