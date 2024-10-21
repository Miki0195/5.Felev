import socket
import random

def tavalyi_zh_server():
    host = '127.0.0.1'
    port = 65433

    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as s:
        s.bind((host, port))
        print("Tavalyi ZH-k server is listening...")

        while True:
            data, addr = s.recvfrom(1024)
            message = data.decode('utf-8')
            print(f"Received message from {addr}: {message}")
            if message == 'Keres':
                task_number = random.randint(1, 10)
                response = f'feladat{task_number}'
                print(f"Sending response to {addr}: {response}")
                s.sendto(response.encode('utf-8'), addr)

if __name__ == "__main__":
    tavalyi_zh_server()