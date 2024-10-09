import select
import socket
import sys


ADDRESS = ("localhost", 12345)


def logic(sock):
    print("Connected! Send and receive messages...")

    while True:
        readables, _, _ = select.select([sys.stdin, sock], [], [])

        # Send a message
        if sys.stdin in readables:
            to_send = sys.stdin.readline().strip()
            print(f"<<< {to_send}")
            sock.sendall(to_send.encode())

        # Receive a message
        if sock in readables:
            received = sock.recv(4096)
            # recv returns with a 0-length bytes object when the connection is closed
            if len(received) > 0:
                print(f">>> {received.decode()}")
            else:
                print("Connection lost")
                break


def main_server():
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
        sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        sock.bind(ADDRESS)
        print("Waiting for a client to connect...")
        sock.listen()
        client, client_address = sock.accept()
        print(f"Client connected: {client_address}")
        logic(client)
        client.close()


def main_client():
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
        sock.connect(ADDRESS)
        logic(sock)


if __name__ == "__main__":
    try:
        if sys.argv[1] == "server":
            main_server()
        else:
            main_client()
    except KeyboardInterrupt:
        pass  # Don't print an error when Ctrl+C is pressed
