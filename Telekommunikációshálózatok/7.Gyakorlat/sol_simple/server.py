import socket
import sys
import select

TCP_PORT = 12345
UDP_PORT = 12346

if len(sys.argv) < 2:
    print("Error: no limit argument given")
    exit(1)

limit = int(sys.argv[1])
print(f"Limit is {limit}")
if limit < 1 or limit > 5:
    print("Error: limit must be between 1 and 5")
    exit(2)

with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as tcp_sock:
    tcp_sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    tcp_server_address = ("", TCP_PORT)
    tcp_sock.bind(tcp_server_address)
    tcp_sock.listen()

    inputs = [tcp_sock]
    while True:
        readables, _, _ = select.select(inputs, [], [])
        for s in readables:
            if s is tcp_sock:
                client, client_address = s.accept()
                inputs.append(client)
                print(f"Connected: {client_address}")
                continue

            data_bytes = s.recv(4096)
            if len(data_bytes) == 0:
                inputs.remove(s)
                s.close()
                print("A client disconnected")
                continue

            data_string = data_bytes.decode()
            print(f"Received: {data_string}")
            if data_string == "Kerek feladatot!":
                limit -= 1
                if limit > 0:
                    print("Sending: Meg nincs")
                    s.sendall("Meg nincs".encode())
                else:
                    print("Requesting a task from UDP server...")
                    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as udp_client:
                        udp_server_address = ("localhost", UDP_PORT)
                        udp_client.sendto("Keres".encode(), udp_server_address)
                        task_bytes, _ = udp_client.recvfrom(256)
                        print(f"Received task: {task_bytes.decode()}")

                    print("Sending: Tessek a feladat!")
                    s.sendall("Tessek a feladat!".encode())
            elif data_string == "Koszonjuk":
                print("Sending: Szivesen")
                s.sendall("Szivesen".encode())
            else:
                print("Invalid message")
