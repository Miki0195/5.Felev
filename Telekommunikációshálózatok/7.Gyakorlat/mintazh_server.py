import socket
import select
import time
import random

def server():
    host = '127.0.0.1'
    port = 65432
    tavalyi_zh_host = '127.0.0.1'
    tavalyi_zh_port = 65433

    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        s.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        s.bind((host, port))
        s.listen()
        print("HALOGYVEZ server is listening...")

        inputs = [s]
        clients = {}

        max_requests = random.randint(1, 5)
        print(f"Max requests set to: {max_requests}")
        request_counter = 0

        while inputs:
            readable, _, _ = select.select(inputs, [], inputs)

            for sock in readable:
                if sock is s:
                    conn, addr = s.accept()
                    print(f"Connected by {addr}")
                    inputs.append(conn)
                    clients[conn] = {'addr': addr, 'data': b''}
                else:
                    data = sock.recv(1024)
                    if data:
                        message = data.decode('utf-8')
                        print(f"Received message from {clients[sock]['addr']}: {message}")
                        if message == 'Kerek feladatot':
                            request_counter += 1
                            if request_counter < max_requests:
                                response = 'Meg nincs'
                                sock.sendall(response.encode('utf-8'))
                            else:
                                with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as udp_sock:
                                    udp_sock.sendto(b'Keres', (tavalyi_zh_host, tavalyi_zh_port))
                                    udp_data, _ = udp_sock.recvfrom(1024)
                                    response = udp_data.decode('utf-8')
                                for client_sock in clients:
                                    client_sock.sendall(response.encode('utf-8'))
                                request_counter = 0
                                max_requests = random.randint(1, 5)
                                print(f"Max requests set to: {max_requests}")
                        elif message == 'Koszonjuk':
                            response = 'Szivesen'
                            sock.sendall(response.encode('utf-8'))
                        else:
                            response = 'Ismeretlen uzenet'
                            sock.sendall(response.encode('utf-8'))
                        time.sleep(1)
                    else:
                        print(f"Closing connection to {clients[sock]['addr']}")
                        inputs.remove(sock)
                        sock.close()
                        del clients[sock]

if __name__ == "__main__":
    server()