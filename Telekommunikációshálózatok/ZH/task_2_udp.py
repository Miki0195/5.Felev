import socket
import json
import struct

SERVER_ADDRESS = ('134.122.78.54', 12302)
MESSAGE = 'T5K17G'

def task2():
    s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    try:
        s.sendto(MESSAGE.encode('utf-8'), SERVER_ADDRESS)
       
        response_data, _ = s.recvfrom(1024)
        
        response_dict = json.loads(response_data.decode('utf-8'))
        
        status = response_dict.get('status')
        alpha = response_dict.get('alpha')
        beta = response_dict.get('beta')
        code = response_dict.get('code')
        
        if status != "OK":
            print(f"Hiba: {status}")
            return
        
        product = alpha * beta
        
        message = struct.pack("! i 100s", product, code.encode('utf-8'))
        s.sendto(message, SERVER_ADDRESS)
        
        final_response, _ = s.recvfrom(1024)
        print(final_response.decode('utf-8'))

    finally:
        s.close()

        
if __name__ == "__main__":
    task2()