import socket

socket.gethostname() # 'dbpcw23'
socket.gethostbyname('inf.elte.hu') # '157.181.161.71'
hostname, aliasis, addresses = socket.gethostbyname_ex('web.elte.hu') # 'web-ospf.caesar.elte.hu', ['web.elte.hu'], ['157.181.1.225']
socket.gethostbyaddr('157.181.1.225')

for i in range(1,101):
    try:
        print(socket.getservbyport(i, 'tcp'))
    except OSError:
        try:
            print(socket.getservbyport(i, 'udp'))
        except OSError:
            print("Szar")


