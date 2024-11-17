import socket

""" my_host = socket.gethostname() # Returns the name of the system you're currently using
addr = socket.gethostbyname('www.gogole.com') # Returns the IP adress of the given host
hostname, aliases, addrs = socket.gethostbyname_ex(my_host) # Returns a tuple consisting of the host's name, aliases and all of the interfaces
# hostname, aliases, addrs = socket.gethostbyaddr('157.181.161.79') -> Does the same as socket.gethostbyname_ex(), but it only works with IP adress
print(hostname, aliases, addrs)
print(addr) """

# For communication between two ends you need 5 things: 
# Sender IP + Sender port
# Receiver IP + Receiver port
# Portocol - TCP/UDP
for port_num in range(1024):
    try:
        service = socket.getservbyport(port_num) # Returns the server/application which is identified by the given port
        print(port_num, service)
    except OSError:
        pass

# Stream Socket