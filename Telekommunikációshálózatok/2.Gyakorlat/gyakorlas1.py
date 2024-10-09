import struct
import socket
import sys

# A bináris fájl struktúrája: 20 karakter (domain - 20s) + 4 byte (port - 4i)
record_format = struct.Struct('20s i')

def read_binary_file(file_name, row_number):
    with open(file_name, 'rb') as f:
        # Kiszámítjuk, hogy hol van a megfelelő sor
        f.seek(row_number * record_format.size)
        # Kiolvassuk a sor tartalmát
        data = f.read(record_format.size)
        if not data:
            print(f"Nem található adat a {row_number}. sorban.")
            return None
        domain, port = record_format.unpack(data)
        # A domain nevét bytes-ről string-re alakítjuk és levágjuk a felesleges null karaktereket
        domain = domain.decode('utf-8').strip('\x00')
        return domain, port

def get_service_by_port(port):
    try:
        service = socket.getservbyport(port, 'tcp')
        print(f"Port {port} szolgáltatás: {service} (TCP)")
    except OSError:
        try:
            service = socket.getservbyport(port, 'udp')
            print(f"Port {port} szolgáltatás: {service} (UDP)")
        except OSError:
            print(f"Port {port} szolgáltatás: Nincs ismert szolgáltatás")

def get_ip_by_domain(domain):
    try:
        ip_address = socket.gethostbyname(domain)
        print(f"A(z) {domain} domain IP címe: {ip_address}")
    except socket.gaierror:
        print(f"Nem található IP cím a(z) {domain} domainhez.")

def main():
    # Ha nincs paraméter
    if len(sys.argv) == 1:
        hostname = socket.gethostname()
        get_ip_by_domain(hostname)
        return

    # Ha van paraméter
    action = sys.argv[1]
    row_number = int(sys.argv[2]) - 1  
    file_name = 'domains.bin'

    result = read_binary_file(file_name, row_number)
    if result is None:
        return
    domain, port = result

    if action == 'port':
        get_service_by_port(port)
    elif action == 'domain':
        get_ip_by_domain(domain)
    else:
        print("Hibás paraméter. Használat: 'port <sor>' vagy 'domain <sor>'")

if __name__ == '__main__':
    main()
