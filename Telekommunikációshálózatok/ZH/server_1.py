import socket
import struct
import select
from typing import Dict
import shared
import traceback

SERVER_ADDRESS = ("0.0.0.0", 12301)
packer_in = struct.Struct("! 50s i")
packer_out = struct.Struct("! 10p 100s")


def reply(sock: socket.socket, sock_addr: str, status: str, msg: bytes):
    print(f"{sock_addr}: sending {status} -> {msg}")
    sock.sendall(packer_out.pack(status.encode(), msg))


def logic(
    sock_to_addr: Dict[socket.socket, str], server_sock: socket.socket, s: socket.socket
):
    # Handle new connection
    if s is server_sock:
        client, client_address = s.accept()
        sock_to_addr[client] = client_address
        print(f"{client_address}: Connected")
        return

    received_bytes = s.recv(4096)
    s_addr = sock_to_addr[s]
    print(f"{s_addr} sent: {received_bytes}")

    # Handle disconnect
    if len(received_bytes) == 0:
        del sock_to_addr[s]
        s.close()
        print(f"{s_addr}: disconnected")
        return

    # Handle invalid format
    print(len(received_bytes))
    if len(received_bytes) != packer_in.size:
        reply(
            s,
            s_addr,
            "HIBA",
            f"{packer_in.size} darab byte-ra számított a szerver, de nem annyi érkezett".encode(),
        )
        return

    # Handle unpacking
    try:
        msg, num = packer_in.unpack(received_bytes)
    except BaseException:
        print(traceback.format_exc())
        reply(s, s_addr, "HIBA", "Nem sikerült az adatot értelmezni".encode())
        return

    # Handle invalid number
    if num != 42:
        reply(s, s_addr, "HIBA", "Nem a megfelelő szám érkezett meg".encode())
        return

    # Handle success
    reply(s, s_addr, "OK", shared.encrypt(msg.decode() + "_success", 100).encode())


def run_server():
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as server_sock:
        server_sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        server_sock.bind(SERVER_ADDRESS)
        server_sock.listen()
        print("TCP server is running...")

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
