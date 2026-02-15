# Aspire Dashboard Login Token Guide

## Overview

When you run PCTama using `./build.sh run`, the Aspire framework automatically generates a **security token** that you need to access the dashboard. This token is displayed in the console output during startup.

## Where to Find Your Token

When you execute `./build.sh run`, watch the console output for this line:

```
info: Aspire.Hosting.DistributedApplication[0]
      Login to the dashboard at http://localhost:15000/login?t=1f2789a1695912eb51b9d1cfcedc928f
```

The part after `?t=` is your authentication token. In this example:
```
Token: 1f2789a1695912eb51b9d1cfcedc928f
```

## How to Access the Dashboard

**Option 1: Click the Full Login URL**
Copy the complete URL from the output and paste it into your browser:
```
http://localhost:15000/login?t=1f2789a1695912eb51b9d1cfcedc928f
```

**Option 2: Manual Token Entry**
1. Go to http://localhost:15000
2. When prompted for a token, paste the token value from the console

## Example Startup Output

```
╔════════════════════════════════════════════════════════════╗
║          PCTama Aspire Framework Starting                  ║
╠════════════════════════════════════════════════════════════╣
║  📊 Dashboard: http://localhost:15000                      ║
║  🔐 LOGIN TOKEN: Look for this line in output below:       ║
║                                                            ║
║  'Login to the dashboard at http://localhost:15000/...'   ║
╚════════════════════════════════════════════════════════════╝

info: Aspire.Hosting.DistributedApplication[0]
      Aspire version: 8.0.0+d215c528c07c7919c3ac30b35d92f4e51a60523b
info: Aspire.Hosting.DistributedApplication[0]
      Distributed application starting.
info: Aspire.Hosting.DistributedApplication[0]
      Now listening on: http://localhost:15000
info: Aspire.Hosting.DistributedApplication[0]
      Login to the dashboard at http://localhost:15000/login?t=1f2789a1695912eb51b9d1cfcedc928f  ← YOUR TOKEN IS HERE
```

## Token Security

- Each time you restart the application, a **new token** is generated
- The token is valid only for that session
- When you stop the application and restart it, you'll need the **new token**
- In production, additional security measures should be implemented

## Troubleshooting

**Can't find the token?**
- Make sure to look for the line starting with `Login to the dashboard at`
- It should appear within 10-15 seconds of starting the application
- Scroll up in your terminal if it has scrolled past

**Token doesn't work?**
- Verify the full URL including the `?t=` parameter is correct
- Try restarting the application with `./build.sh run`
- Clear browser cache and try again

**Port 15000 already in use?**
Run this to find and kill the process:
```bash
pkill -9 dotnet
```
Then run `./build.sh run` again.

## Related Documentation

- [RUNNING.md](./RUNNING.md) - Full guide to running PCTama
- [README.md](./README.md) - Project overview
- [QUICKSTART.md](./QUICKSTART.md) - Getting started guide
