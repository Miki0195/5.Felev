def is_leap_year(year):
    if (year % 4 == 0 and year % 100 != 0) or (year % 400 == 0):
        return True
    return False

def read(filename):
    with open(filename, 'r') as file:
        years = [int(line.strip()) for line in file]
    return years


def main():
    filename = 'evek.txt'
    years = read(filename)

    for year in years:
        if is_leap_year(year):
            print(f"{year} szökőév.")
        else:
            print(f"{year} nem szökőév.")

if __name__ == "__main__":
    main()
