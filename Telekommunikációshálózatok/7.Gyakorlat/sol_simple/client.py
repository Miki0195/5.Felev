import socket

TCP_PORT = 12345

with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as client:
    tcp_server_address = ("localhost", TCP_PORT)
    client.connect(tcp_server_address)

    print("Sending: Kerek feladatot!")
    client.sendall("Kerek feladatot!".encode())

    data_bytes = client.recv(4096)
    data_string = data_bytes.decode()
    print(f"Received: {data_string}")

    if data_string == "Tessek a feladat!":
        print("Sending: Koszonjuk")
        client.sendall("Koszonjuk".encode())
        print(f"Final receive returned: {client.recv(4096).decode()}")
    elif data_string != "Meg nincs":
        print("Invalid message")
