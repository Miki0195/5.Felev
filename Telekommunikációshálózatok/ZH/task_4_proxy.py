import socket
import struct

UDP_ADDRESS = ('127.0.0.1', 12305)
TCP_SERVER_ADDRESS = ('134.122.78.54', 12304)

packer = struct.Struct('! 4s 100s i')
num_packer = struct.Struct('! i')

topic_to_numbers = dict()

with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as udp_sock:
    udp_sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    udp_sock.bind(UDP_ADDRESS)  

    while True:
        received_bytes, client_address = udp_sock.recvfrom(108) 

        cmd, topic, num = packer.unpack(received_bytes)
        cmd = cmd.decode().strip()
        topic = topic.decode().strip()
        print(f'Fogadott üzenet: {cmd} {topic} {num}')

        numbers = topic_to_numbers.get(topic, None)
        if numbers is None:
            numbers = []
            topic_to_numbers[topic] = numbers

        if cmd == 'PUSH':
            numbers.append(num)
            response = packer.pack(b'RSP1', b'OK', 0)
            udp_sock.sendto(response, client_address)
            continue

        elif cmd == 'DROP':
            numbers.clear()
            response = packer.pack(b'RSP3', b'OK', 0)
            udp_sock.sendto(response, client_address)
            continue

        elif cmd == 'COMP':
            with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as tcp_sock:
                tcp_sock.connect(TCP_SERVER_ADDRESS)  

                to_send = num_packer.pack(len(numbers))  
                for n in numbers:
                    to_send += num_packer.pack(n)  
                
                tcp_sock.sendall(to_send) 
                tcp_response = tcp_sock.recv(4096) 
                print('Fogadott üzenet', tcp_response.decode())

            response = packer.pack(b'RSP2', tcp_response, 0)
            udp_sock.sendto(response, client_address)  

            numbers.clear()

        else:
            print('Helytelen parancs:', cmd)
            response = packer.pack(b'ERR!', b'Invalid command', 0)
            udp_sock.sendto(response, client_address)
