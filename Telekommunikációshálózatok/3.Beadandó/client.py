import socket
import struct
import time
import random

def binary_search_client(hostname, port):
    client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    client_socket.connect((hostname, port))
    print("Connected to server")

    low = 1
    high = 100
    game_over = False

    while not game_over:
        middle = (low + high) // 2

        if low > high:
            break

        if low == middle:
            guess_char = '=' 
        elif middle < high:
            guess_char = '<'  
        else:
            guess_char = '>' 

        print(f"Sending guess: {guess_char} {middle}")
        client_socket.send(struct.pack('ci', guess_char.encode('utf-8'), middle))

        response = client_socket.recv(8)
        response_char, _ = struct.unpack('ci', response)
        response_char = response_char.decode('utf-8')

        print(f"Received response: {response_char}")

        if response_char == 'Y':
            print("Nyertél!")
            game_over = True
        elif response_char == 'K':
            print("Kiestél!")
            game_over = True
        elif response_char == 'V':
            print("Vége a játéknak!")
            game_over = True
        elif response_char == 'I':  
            high = middle 
        elif response_char == 'N':  
            low = middle

        print(f"Current range: low={low}, high={high}, middle={middle}")

        time.sleep(random.randint(1, 5))

    client_socket.close()
    print("Connection closed")

if __name__ == "__main__":
    import sys
    if len(sys.argv) != 3:
        print("Használat: python3 client.py <hostname> <port>")
        sys.exit(1)

    hostname = sys.argv[1]
    port = int(sys.argv[2])
    binary_search_client(hostname, port)
