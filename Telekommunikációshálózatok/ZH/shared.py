import base64
from cryptography.hazmat.primitives.ciphers import Cipher, algorithms, modes


def _create_cipher() -> Cipher:
    key = "yvlsmlqkn4tA1AS3"
    iv = "amkldfjf3z45723z"
    return Cipher(algorithms.AES(key.encode()), modes.CTR(iv.encode()))


def encrypt(x: str, pad_length: int) -> str:
    encryptor = _create_cipher().encryptor()
    encrypted = encryptor.update(x.encode()) + encryptor.finalize()
    encoded = base64.urlsafe_b64encode(encrypted).decode()
    return encoded.ljust(pad_length, "\x00")


def decrypt(x: str) -> str:
    decryptor = _create_cipher().decryptor()
    x = x.rstrip("\x00")
    decoded = base64.urlsafe_b64decode(x)
    decrypted = decryptor.update(decoded) + decryptor.finalize()
    return decrypted.decode()
