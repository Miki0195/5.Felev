import socket
import select
import struct

HOST = 'localhost'  
PORT = 12345       


def task3():
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server_socket.bind((HOST, PORT))
    server_socket.listen()

    inputs = [server_socket]
    clients = {}

    try:
        while True:
            read_sockets, _, _ = select.select(inputs, [], [])
            
            for sock in read_sockets:
                if sock == server_socket:
                    client_socket, client_address = server_socket.accept()
                    inputs.append(client_socket)
                    clients[client_socket] = client_address
                else:
                    try:
                        data = sock.recv(1024)
                        if data:
                            unpacked_data = struct.unpack("! 32s i", data)
                            received_text = unpacked_data[0].decode('utf-8').rstrip('\x00')
                            received_length = unpacked_data[1]
                            
                            is_length_match = len(received_text) == received_length
                            response = struct.pack("?", is_length_match)
                            
                            sock.sendall(response)
                        else:
                            inputs.remove(sock)
                            del clients[sock]
                            sock.close()
                    except Exception as e:
                        inputs.remove(sock)
                        del clients[sock]
                        sock.close()

    finally:
        for sock in inputs:
            sock.close()

if __name__ == "__main__":
    task3()
