import socket

hostname = 'HALOGYVEZ'
ip_address = socket.gethostbyname(hostname)

print(f"Scanning server: {hostname} ({ip_address})")

ports_to_scan = [80, 443, 22, 8080, 3306, 21, 25, 53]

for port in ports_to_scan:
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.settimeout(1)  # Set timeout to 1 second
    result = sock.connect_ex((ip_address, port))
    if result == 0:
        print(f"Port {port} is open on {ip_address}")
    else:
        print(f"Port {port} is closed on {ip_address}")
    sock.close()
