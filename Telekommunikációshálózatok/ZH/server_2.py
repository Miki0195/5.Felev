import socket
import struct
import shared
import random
import json
import traceback

SERVER_ADDRESS = ("0.0.0.0", 12302)
packer_msg_2 = struct.Struct("! i 100s")


def reply_json(sock, addr, json_pyobj):
    reply_string(sock, addr, json.dumps(json_pyobj))


def reply_string(sock, addr, s: str):
    print(f"{addr}: sending: {s}")
    sock.sendto(s.encode(), addr)


def handle_message_1(sock, addr, neptun: str):
    a, b = random.randint(1, 99), random.randint(1, 99)
    msg = {
        "status": "OK",
        "alpha": a,
        "beta": b,
        "code": shared.encrypt(neptun + f"/{a * b}", 100),
        "garbage123": "ignore this",
    }
    reply_json(sock, addr, msg)


def handle_message_2(sock, addr, num: int, code: bytes):
    try:
        neptun_and_expected = shared.decrypt(code.decode())
        neptun, expected = neptun_and_expected.split("/")
    except BaseException:
        print(traceback.format_exc())
        reply_string(sock, addr, "Hiba: érvénytelen 'code' érkezett")
        return

    print(f"{addr} decoded: num={num} neptun={neptun} expected={expected}")
    if num != int(expected):
        reply_string(sock, addr, "Hiba: helytelen szorzat érkezett")
        return

    reply_string(sock, addr, "Siker! A kód: " + shared.encrypt(f"{neptun}/+1", 100))


def logic(sock: socket.socket):
    received_bytes, client_addr = sock.recvfrom(1024)
    print(f"{client_addr} sent bytes: {received_bytes}")

    # Handle Neptun code
    if len(received_bytes) == 6:
        handle_message_1(sock, client_addr, received_bytes.decode())
        return

    # Handle invalid message length
    if len(received_bytes) != packer_msg_2.size:
        reply_json(
            sock,
            client_addr,
            {
                "status": "HIBA: 6 vagy 104 darab byte-ra számít a szerver",
            },
        )
        return

    # Handle unpacking
    try:
        num, code = packer_msg_2.unpack(received_bytes)
    except BaseException:
        print(traceback.format_exc())
        reply_json(
            sock,
            client_addr,
            {
                "status": "HIBA: nem sikerült dekódolni a kapott üzenetet",
            },
        )
        return

    # Handle final message
    handle_message_2(sock, client_addr, num, code)


def run_server():
    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as sock:
        sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        sock.bind(SERVER_ADDRESS)
        print("UDP server is running...")

        while True:
            try:
                logic(sock)
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
