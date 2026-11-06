# Meshtastic board bring-up

How to get a LoRa board from the box to exchanging datagrams, and how to verify
the link with the `ShortP2P.LoRa.Demo` console app.

Works for any board supported by Meshtastic (see the device list in the
[web flasher](https://flasher.meshtastic.org)). Two boards are needed for an RX/TX test.

> **Tested on:** Heltec WiFi LoRa 32 V3 (PCB marking `HTIT-WB32LAF`, ESP32-S3 + SX1262,
> firmware target `heltec-v3`, Meshtastic 2.7.26). Button labels, USB behavior, and port
> names on other boards may differ — the overall flow is the same.

## 0. Prerequisites

- 2 × board + 2 × **data** USB cable (a charge-only cable shows no COM port at all)
- Antennas screwed onto **both** boards (TX without an antenna can damage the radio)
- Chrome/Edge for the web flasher and the web client
- .NET 10 SDK for the demo

## 1. Flash the Meshtastic firmware

Easiest: [flasher.meshtastic.org](https://flasher.meshtastic.org) → pick **your device** →
latest stable → *Full erase* on first flash.

Manual way:

1. Download the firmware zip **for your MCU platform** (`firmware-esp32s3-*.zip`,
   `firmware-esp32-*.zip`, `firmware-nrf52840-*.zip`, ...) from
   [meshtastic/firmware releases](https://github.com/meshtastic/firmware/releases/latest)
   and extract it.
2. Pick `firmware-<your-target>-<version>.factory.bin` (factory = first flash;
   the plain `.bin` is for OTA updates only).
3. Enter **download mode**:
   - **ESP32 / ESP32-S3 / C3:** hold **BOOT** → press **RST** → release **BOOT**.
     The screen stays blank. On native-USB boards (ESP32-S3) a **new COM port** appears
     in Device Manager — often a different number than the running-firmware one; use that.
   - **nRF52 (e.g. RAK4631, T114):** double-press **RST** — the board mounts as a USB drive;
     copy the `.uf2` firmware file onto it.
4. Flash (ESP32 example):

   ```powershell
   esptool --chip esp32s3 --port COM<new> --baud 115200 write_flash 0x0 firmware-<target>-<version>.factory.bin
   ```

5. Press **RST** — the board boots into Meshtastic.

Troubleshooting:

- `Failed to connect ... No serial data received` — you are flashing the *application* COM port
  or the board is not in download mode. Redo the button dance and use the **new** port.
- The bundled `device-install.bat` in the firmware zip has an inverted filename check
  (rejects valid `*.factory.bin` names); the esptool line above is equivalent to what it does
  for the main image. BLEOTA/littlefs parts it additionally flashes are optional.

## 2. Set the LoRa region (mandatory)

The board's own `lora.region` decides the frequency plan. Our demo's `--region` only
gates TX in software — it does **not** configure the board. Region/preset/channel must
match on both boards (defaults: preset `LONG_FAST`, primary channel).

Pick one (the tool holds the COM port — stop the demo first):

- **Web client** [client.meshtastic.org](https://client.meshtastic.org) (Chrome):
  Connect → Serial → pick the port → Config → LoRa → **Region** → Save (board reboots).
- **CLI** (`pip install meshtastic`):

  ```powershell
  meshtastic --port COMx --set lora.region RU      # or EU_868, US, ...
  meshtastic --port COMx --get lora.region         # verify
  ```

Phone over BLE also works: connect from the **Meshtastic app** (not from OS Bluetooth
settings), default PIN `123456`.

## 3. Verify with ShortP2P.LoRa.Demo

Build once:

```powershell
dotnet build ShortP2P.LoRa.slnx
```

Run on each board (one app instance per board/COM port):

```powershell
dotnet run --project src/ShortP2P.LoRa.Demo -- --port COMx --region RU
```

Options: `--port` / `--region` are required; `--to broadcast|0x<hex>|<dec>` (default broadcast);
`--interval <sec>` between datagrams (default 5).

Expected output on startup:

```
Local node: A1CE0001 (2714824705)
Region: RU; destination: 0xFFFFFFFF; interval: 5 s. Ctrl+C to stop.
```

Then every interval a 10-byte and a 1-KB datagram alternate:

```
17:33:20.184 TX    10 B seq 1 (1 radio frame)
17:33:25.267 TX  1024 B seq 2 (5 radio frames)
```

And on the **other** board each sent datagram must appear with an integrity check:

```
17:33:20.9xx RX    10 B from A1CE0001: seq 1: pattern OK (10 B)
17:33:26.0xx RX  1024 B from A1CE0001: seq 2: pattern OK (1024 B)
```

## 4. If RX is silent

1. Make sure you run a build that includes the serial RX fix (read loop used to die
   500 ms after start). A red line `Meshtastic read loop on COMx died` must never appear.
2. Watch the `[Debug] FromRadio field ...` lines on the receiving board:
   - `field 2 (wire 2, ...)` shows up when the other side transmits
     (even a `--sendtext` from the CLI shows it) → radio path + serial RX are fine.
   - Nothing at all → boards do not hear each other: check `lora.region` on **both**
     boards, antennas, distance.
3. Independent air check: `meshtastic --port COMb --sendtext "test"` from board B,
   or a text from the phone app — board A's screen should react.
4. Only one program may hold the COM port: stop the demo before using the CLI/web client.
