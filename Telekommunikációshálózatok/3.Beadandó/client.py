import socket
import struct
import sys

def binary_search_client(hostname, port):
    client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    client_socket.connect((hostname, port))

    lower_bound = 1
    upper_bound = 100
    game_active = True

    while game_active:
        if lower_bound > upper_bound:
            print("A szám nem található!")
            break

        guess = (lower_bound + upper_bound) // 2
        print(f"Tipp: {guess}")

        # Küldjük el a tippünket
        command = b'='  # Az előző tipp alapján
        if guess < lower_bound or guess > upper_bound:
            print("Érvénytelen tipp!")
            break

        # Küldjük a kérdést
        data = struct.pack('!cI', command, guess)
        client_socket.send(data)

        # Válasz fogadása
        response_data = client_socket.recv(8)
        if len(response_data) < 8:
            print("Hiba: A válasz nem a várt hosszúságú.")
            break

        response = struct.unpack('!cI', response_data)
        response_char = response[0].decode('utf-8')

        if response_char == 'Y':
            print("Nyertél!")
            game_active = False
        elif response_char == 'K':
            print("Kiestél!")
            game_active = False
        elif response_char == 'V':
            print("A játék véget ért.")
            game_active = False
        elif response_char == 'I':
            print("Folytassuk!")
            # A szám kitalálása következő tipphez
            continue
        elif response_char == 'N':
            print("Próbálj meg egy másik számot.")
            if command == b'>':
                upper_bound = guess - 1
            elif command == b'<':
                lower_bound = guess + 1
        else:
            print("Ismeretlen válasz érkezett.")

    client_socket.close()

if __name__ == "__main__":
    if len(sys.argv) != 3:
        print("Használat: python3 client.py <hostname> <port>")
        sys.exit(1)
    
    hostname = sys.argv[1]
    port = int(sys.argv[2])
    binary_search_client(hostname, port)
