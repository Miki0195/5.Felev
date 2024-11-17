import socket
import time

def client():
    host = '127.0.0.1'
    port = 65432

    try:
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            s.connect((host, port))
            s.sendall(b'Kerek feladatot')
            while True:
                data = s.recv(1024)
                response = data.decode('utf-8')
                print(f"Received {response}")
                if response.startswith('feladat'):
                    s.sendall(b'Koszonjuk')
                    data = s.recv(1024)
                    print(f"Received {data.decode('utf-8')}")
                    break
                elif response == 'Meg nincs':
                    print("Waiting for more clients to join...")
                    time.sleep(5)  # Wait before checking again
    except ConnectionRefusedError as e:
        print(f"Connection was refused: {e}")
    except Exception as e:
        print(f"An error occurred: {e}")

if __name__ == "__main__":
    client()