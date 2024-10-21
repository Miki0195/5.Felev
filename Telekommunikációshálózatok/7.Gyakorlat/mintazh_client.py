import socket

def client():
    host = '127.0.0.1' 
    port = 65432  

    try:
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            s.connect((host, port))
            while True:
                s.sendall(b'Kerek feladatot')
                data = s.recv(1024)
                response = data.decode('utf-8')
                print(f"Received {response}")
                if response.startswith('feladat'):
                    s.sendall(b'Koszonjuk')
                    data = s.recv(1024)
                    print(f"Received {data.decode('utf-8')}")
                    break
    except ConnectionAbortedError as e:
        print(f"Connection was aborted: {e}")
    except Exception as e:
        print(f"An error occurred: {e}")

if __name__ == "__main__":
    client()