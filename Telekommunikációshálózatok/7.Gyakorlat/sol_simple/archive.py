import socket
import random

UDP_PORT = 12346

with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as udp_sock:
    udp_server_address = ("", UDP_PORT)
    udp_sock.bind(udp_server_address)

    while True:
        data_bytes, client_address = udp_sock.recvfrom(4096)
        data_string = data_bytes.decode()
        print(f"Received: {data_string}, from: {client_address}")
        if data_string == "Keres":
            to_send_string = f"feladat{random.randint(1, 10)}"
            print(f"Sending: {to_send_string}")
            udp_sock.sendto(to_send_string.encode(), client_address)
        else:
            print("Invalid message")
