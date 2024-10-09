import time
import sys


def read_input_win(timeout):
    # from https://ggombos.web.elte.hu/oktatas/SzamHalo/gyak4/
    import msvcrt

    start_time = time.time()
    input = ""
    while True:
        if msvcrt.kbhit():
            chr = msvcrt.getche()
            if ord(chr) == 13:  # enter_key
                break
            elif ord(chr) >= 32:  # space_char
                input += chr.decode()
            elif ord(chr) == 8:  # backspace_char
                input = input[:-1]
                sys.stdout.write("\r")
                sys.stdout.write(input)
                sys.stdout.write(" ")
                sys.stdout.write("\r")
                sys.stdout.write(input)
        if len(input) == 0 and (time.time() - start_time) > timeout:
            break

    # print ''  # needed to move to next line
    if len(input) > 0:
        return input
    else:
        return ""


def read_input_linux(timeout):
    import select

    # select doesn't work with sys.stdin on Windows
    readables, _, _ = select.select([sys.stdin], [], [], timeout)
    if len(readables) > 0:
        return sys.stdin.readline().strip()
    else:
        return ""


def read_input(timeout=1):
    from importlib.util import find_spec

    # Different solutions are required on Windows and Linux to poll stdin
    if find_spec("msvcrt"):  # This library is Windows-specific
        return read_input_win(timeout)
    else:
        return read_input_linux(timeout)
