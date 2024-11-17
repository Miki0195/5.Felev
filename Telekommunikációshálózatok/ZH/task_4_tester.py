from asyncio import sleep, wait_for, run, get_running_loop, TimeoutError, exceptions
from random import randint
from sys import exit
from hashlib import md5


UDP_PORT = 12305


class Proto:
    def __init__(self, c, t, n, on_exit):
        self.ctn = c, t, n
        self.on_exit = on_exit

    def connection_made(self, transport):
        self.transport = transport
        c, t, n = self.ctn
        print(f"Küldés alatt álló adat: {c} {t} {n}")
        self.transport.sendto(
            c.encode() + t.ljust(100, "\x00").encode() + n.to_bytes(4, "big")
        )
        print("Várakozás válaszra...")

    def datagram_received(self, r, addr):
        self.r = r
        print(
            f"Fogadott adat: {r[:4].decode()} {r[4:104].decode()} {int.from_bytes(r[104:], 'big')}"
        )
        self.transport.close()

    def error_received(self, exc):
        print("Hiba:", exc)
        print("Lehetséges, hogy nem sikerült elérni a proxy-t?")

    def connection_lost(self, exc):
        try:
            self.on_exit.set_result(True)
        except exceptions.InvalidStateError:
            pass


async def send(c, t, n):
    await sleep(randint(100, 500) / 1000)
    on_exit = get_running_loop().create_future()
    t, proto = await get_running_loop().create_datagram_endpoint(
        lambda: Proto(c, t, n, on_exit), remote_addr=("127.0.0.1", UDP_PORT)
    )
    try:
        await wait_for(on_exit, 3)
        return proto.r[4:104]
    except TimeoutError:
        print("Nem érkezett válasz időben")
        exit(1)
    finally:
        t.close()


async def main():
    x = await send("PUSH", "alpha", 42)
    x += await send("PUSH", "alpha", 99)
    x += await send("PUSH", "beta", 120)
    x += await send("DROP", "beta", 0)
    x += await send("COMP", "alpha", 0)
    x += await send("PUSH", "alpha", 42)
    x += await send("PUSH", "alpha", 99)
    x += await send("PUSH", "charlie", 1)
    x += await send("PUSH", "alpha", 100)
    x += await send("PUSH", "alpha", 42)
    x += await send("PUSH", "alpha", 55)
    x += await send("COMP", "alpha", 0)
    print(f"Sikeres tesztelés; Canvas kód: {md5(x).hexdigest()}")


if __name__ == "__main__":
    run(main())
