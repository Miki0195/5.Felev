import shared


def main():
    print("Example:")
    example = "Hello World! ßíŐ"
    print(f"Input: {example}")
    print(f"Encrypted, padded to 50: {shared.encrypt(example, 50).encode()}")
    print(f"Decrypted: {shared.decrypt(shared.encrypt(example, 50))}")
    print()

    while True:
        encrypted = input("Text to decrypt: ")
        decrypted = shared.decrypt(encrypted)
        print(decrypted)
        print()


if __name__ == "__main__":
    main()
