import socket
import struct

NAME = 'Buchsbaum Miklos'  
NUMBER = 42
SERVER_IP = '134.122.78.54'  
SERVER_PORT = 12301     

def task1():
    try:
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            s.connect((SERVER_IP, SERVER_PORT))
            
            packed_data = struct.pack("! 50s i", NAME.encode('utf-8'), NUMBER)
            s.sendall(packed_data)

            response_format = "! 10p 100s" 
            response_size = struct.calcsize(response_format)
            response_data = s.recv(response_size)

            status, message = struct.unpack(response_format, response_data)
            status = status.decode('utf-8').strip(b'\x00'.decode()) 
            message = message.decode('utf-8').rstrip('\x00') 

            if status == "OK":
                print("Szerver válasza:", message)
            else:
                print("Nem válaszol a szerver:", status)


            s.close()
    except Exception as e:
        print(f"Hiba: {e}")

if __name__ == "__main__":
    task1()
