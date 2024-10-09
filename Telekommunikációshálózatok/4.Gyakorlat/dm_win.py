import socket
import sys
import input_timeout


ADDRESS = ("localhost", 12345)
RECEIVE_TIMEOUT = 0.0  # Non-blocking mode
SEND_TIMEOUT = 1.0


def logic(sock):
    sock.settimeout(RECEIVE_TIMEOUT)
    print('Connected! Send and receive messages...')

    while True:
        # Send a message
        to_send = input_timeout.read_input(SEND_TIMEOUT)
        if len(to_send) > 0:
            print(f"<<< {to_send}")
            sock.sendall(to_send.encode())

        # Receive a message
        try:
            received = sock.recv(4096)
            # recv returns with a 0-length bytes object when the connection is closed
            if len(received) > 0:
                print(f">>> {received.decode()}")
            else:
                print('Connection lost')
                break
        except (socket.timeout, BlockingIOError):
            pass  # An exception is raised when recv has no data to read within RECEIVE_TIMEOUT


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
