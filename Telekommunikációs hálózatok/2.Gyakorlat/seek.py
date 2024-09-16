with open('alma.txt', 'r') as f:
  sor = f.readline()
  print('jelenlegi sor:', sor.strip()) # jelenlegi sor 1. sor

  sor = f.readline()
  print('jelenlegi sor:', sor.strip()) # jelenlegi sor 2. sor

  f.seek(0, 0)                         # f.seek(offset, whence)

  sor = f.readline()
  print('jelenlegi sor:', sor.strip()) # jelenlegi sor 1. sor
