
# Konkurens programozás mintaZH

## Feladatleírás: Ticket Sale Simulator

A feladathoz kiindulásként szolgáló kódot ide kattintva lehet letölteni.

Ebben a feladatban egy koncertjegyeket árusító rendszert szimulálunk, amely három részből
áll; `main`, `sellerAction`, `customerAction`, illetve néhány konstansból.

- `TICKET_TYPES`: A jegyek lehetséges típusainak listája
- `TICKET_COUNT`: Egyféle jegyből hány darab létezik
- `CUSTOMER_COUNT`: A jegyet vásárló kliensek száma
- `SLEEP_TIME_MIN/MAX`: Minimum és maximum várakozási idő
- `SHUTDOWN_TIME`: Ennyi ideig fut a szimuláció (15 másodperc)
- `TICKET_INVENTORY`: Jegytípus-mennyiség tárolására egy map
- `TICKET_QUEUE`: A vásárlók ebből az adatszerkezetből jutnak hozza a jegyekhez

### Main függvény feladatok:

- Osztályszintű változók
    - Hozzuk létre a `TICKET_INVENTORY` mapet, amelyet a főprogram fel is tölt.
    - Hozzuk létre a `TICKET_QUEUE`t, amelynek kapacitása 1 legyen.
    - Hozzunk létre egy olyan `ExecutorService`t, ami minden szereplőnknek helyet biztosít.
- Főprogram
    - Minden típusú jegyre indítsunk külön szálon egy `sellerAction`-t-
    - Minden vásárlóra indítsunk el egy `customerAction`-t, de figyeljünk rá, hogy ne legyen igény több jegyre, mint amennyi létezik a `TICKET_INVENTORY`-ban, viszont azok fogyjanak el mindenképp (tehát 5 vásárló akar "sector A" jegyet, 5 "sector B" jegyet stb.).
    - A szimuláció álljon le `SHUTDOWN_TIME` eltelte után.

### `SellerAction` függvény feladatok:

- Addig fusson, amíg van még a megfelelő jegytípusból raktáron (`TICKET_INVENTORY`).
- Amennyiben van még a jegytípusból raktáron, vegyen ki egyet és adja hozza a `TICKET_QUEUE`-hoz.
- Csökkentse a `TICKET_INVENTORY` mennyiséget eggyel.
- Minden próbálkozás között várjon `SLEEP_TIME_MIN` és `SLEEP_TIME_MAX` közti, véletlenszerű időt.

### `CustomerAction` függvény feladatok:

- Nézze meg, hogy a `TICKET_QUEUE`-ban van-e olyan típusú jegy, amely őt érdekli.
- Ha van ilyen jegy, vegye ki és fejezze be futását.
- Amennyiben nincs, `SLEEP_TIME_MIN` és `SLEEP_TIME_MAX` közti véletlenszerű idő kivárása után nézze meg újra.
