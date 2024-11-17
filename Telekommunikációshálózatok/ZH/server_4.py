import hashlib
import socket
import struct
import select
import traceback
from typing import Dict

SERVER_ADDRESS = ("0.0.0.0", 12304)


def reply(sock, sock_addr, msg: str):
    print(f"{sock_addr}: sending: {msg}")
    sock.sendall(struct.pack("! 100s", msg.encode()))


def logic(
    sock_to_addr: Dict[socket.socket, str], server_sock: socket.socket, s: socket.socket
):
    # Handle new connection
    if s is server_sock:
        client, client_address = s.accept()
        sock_to_addr[client] = client_address
        print(f"{client_address}: Connected")
        return

    received_bytes = s.recv(4)
    s_addr = sock_to_addr[s]
    print(f"{s_addr} sent: {received_bytes}")

    # Handle disconnect
    if len(received_bytes) == 0:
        del sock_to_addr[s]
        s.close()
        print(f"{s_addr}: disconnected")
        return

    # We send a reply to the server when we receive an incorrect byte/count value.
    # If the server continues to send more bytes after we send our error message,
    # then we will interpret those extra bytes as subsequent "count" messages.
    # This can cause a BrokenPipeError later on: we attempt to reply with our error message multiple times,
    # but the client won't wait for all of our replies: it will disconnect as soon as it read the first reply.

    # Handle invalid format
    if len(received_bytes) != 4:
        reply(s, s_addr, "Hiba: 4 bájtra számított a szerver")
        return

    count = struct.unpack("! i", received_bytes)[0]
    if count > 100:
        reply(s, s_addr, f"Hiba: túl magas a számok darabszáma ({count})")
        return

    # Receive N numbers
    numbers = []
    s.settimeout(1.0)
    try:
        for _ in range(count):
            # TODO recv might return e.g. 0 bytes
            numbers.append(struct.unpack("! i", s.recv(4))[0])
    finally:
        s.settimeout(None)

    # Reduce the list and reply with the result
    result = hashlib.md5(
        ("A válasz... " + " ".join(sorted([str(i) for i in numbers]))).encode()
    ).hexdigest()
    reply(s, s_addr, result)


def run_server():
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as server_sock:
        server_sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        server_sock.bind(SERVER_ADDRESS)
        server_sock.listen()
        print("Proxy's TCP server is running...")

        sock_to_addr = {}
        while True:
            readables, _, _ = select.select(
                [server_sock] + list(sock_to_addr.keys()), [], []
            )
            for s in readables:
                try:
                    logic(sock_to_addr, server_sock, s)
                except KeyboardInterrupt:
                    raise
                except BaseException:
                    print(traceback.format_exc())
                    print("Unexpeted exception, continuing without resetting state...")


def main():
    while True:
        try:
            run_server()
        except KeyboardInterrupt:
            print("Interrupted")
            break
        except BaseException:
            print(traceback.format_exc())
            print("Restarting server...")


if __name__ == "__main__":
    main()
