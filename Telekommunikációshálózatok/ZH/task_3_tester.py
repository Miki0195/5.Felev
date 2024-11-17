from asyncio import (
    sleep,
    open_connection,
    wait_for,
    TimeoutError,
    gather,
    run,
    get_running_loop,
)
from random import randint
from sys import exit
from base64 import b64decode as be


x = 12345

A = b"SGVsbG8gV29ybGQhAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAM", 1
B = b"VGVzenQgRWxlawAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADA5", 0
BE = [
    (b"YWZzZGxqZm5hbGtzamRmYW4AAAAAAAAAAAAAAAAAAAAAAAAR", 1),
    (b"YXNkZmzDqWtuYcOpc2RqZm5hw6lzZGpmAAAAAAAAAAAAAAAR", 0),
    (b"NDIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAq", 0),
    (b"YXNkZmphbsOpc2Rsam5hc2RhY3MAAAAAAAAAAAAAAAAAAAAU", 0),
    (b"ZmFzZGZhc2RmYXNkZmFzZGYAAAAAAAAAAAAAAAAAAAAAAAAQ", 0),
    (b"MjMxMjM0MTIzNTQxMzI0MTIzNAAAAAAAAAAAAAAAAAAAAAAT", 1),
    (b"YXBvZmthbXdmZWtsMm4zbTQ1AAAAAAAAAAAAAAAAAAAAAAAT", 0),
    (b"c2Fsa8OpZG5mYXVkaGZ1aWFzZGYAAAAAAAAAAAAAAAAAAAAK", 0),
    (b"bUMsTU5IV0JFRElVSFNMUE4AAAAAAAAAAAAAAAAAAAAAAAAO", 0),
    (b"YcOpbXNkw6Fmb2Fza2RtcGNxdWh0NzQzaHJ3ZXUAAAAAAAAb", 1),
]


async def ss():
    await sleep(randint(50, 550) / 1000)


async def run_client(b, e, f=None):
    await ss()
    r, w = await open_connection("localhost", 12345)
    try:
        if f:
            f[0].set_result(True)
            await f[1]
        await ss()
        w.write(be(b))
        await w.drain()
        await ss()
        if r := await wait_for(r.read(123), 1):
            global x
            x ^= r[0] * int.from_bytes(be(b)[-4:], "big")
            await ss()
        return len(r) == 1 and e == r[0]
    except TimeoutError:
        return False
    finally:
        w.close()
        await w.wait_closed()


async def test_1():
    if await run_client(*A) and await run_client(*B):
        return
    print("Hiba: egyszerre egy kliens esetén a szerver nem működik helyesen")
    exit(1)


async def test_2():
    async def body():
        f = get_running_loop().create_future(), get_running_loop().create_future()
        ff = get_running_loop().create_task(run_client(*A, f))
        await f[0]
        await run_client(*B)
        f[1].set_result(True)
        await ff

    try:
        await wait_for(body(), 10)
    except TimeoutError:
        print("Hiba: párhuzamosan két kliens esetén a szerver nem működik helyesen")
        exit(1)


async def test_3():
    if all(await gather(*(run_client(*a) for a in BE))):
        return
    print("Hiba: sok kliens esetén a szerver nem működik helyesen")
    exit(1)


async def main():
    print("Teszt 1...")
    await test_1()
    print("Teszt 2...")
    await test_2()
    print("Teszt 3...")
    await test_3()
    print(f"Sikeres tesztelés; Canvas kód: {hex(x)}")


if __name__ == "__main__":
    run(main())
