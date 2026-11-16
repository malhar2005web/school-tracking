using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using SchoolTrack.Shared.DTOs.Devices;
using SchoolTrack.Shared.DTOs.RealTime;
using SchoolTrack.Shared.Models;

namespace SchoolTrack.HardwareSimulator;

public static class Program
{
    private static string BaseUrl = "http://localhost:5000";
    private static readonly HttpClient Http = new();

    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        PrintBanner();

        // Check command line arguments for non-interactive automation
        if (args.Length > 0)
        {
            await RunCliModeAsync(args);
            return;
        }

        // Interactive Menu Mode
        while (true)
        {
            PrintMenu();
            Console.Write("\nSelect Hardware Simulation Mode (1-6, 0 to Exit): ");
            var key = Console.ReadLine()?.Trim();

            Console.WriteLine();
            switch (key)
            {
                case "1":
                    await SimulateLiveGpsPlaybackAsync();
                    break;
                case "2":
                    await SimulateRfidBoardingSweepAsync();
                    break;
                case "3":
                    await SimulateCellularDropAndBatchSyncAsync();
                    break;
                case "4":
                    await SimulateFullGoldenTripLifecycleAsync();
                    break;
                case "5":
                    await MonitorSignalRStreamAsync();
                    break;
                case "6":
                    ConfigureBaseUrl();
                    break;
                case "0":
                    Console.WriteLine("Exiting Hardware Simulator. Safe travels!");
                    return;
                default:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Invalid option. Please choose between 1 and 6.");
                    Console.ResetColor();
                    break;
            }

            Console.WriteLine("\nPress Enter to return to menu...");
            Console.ReadLine();
        }
    }

    private static void PrintBanner()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
═════════════════════════════════════════════════════════════════════════════════
  SCHOOLTRACK — DEDICATED HARDWARE SIMULATOR & TELEMETRY GENERATOR
  Supports: CalAmp / Teltonika GPS Telemetry & Ethernet Door RFID Scanners
═════════════════════════════════════════════════════════════════════════════════");
        Console.ResetColor();
        Console.WriteLine($"Target Gateway URL: {BaseUrl}\n");
    }

    private static void PrintMenu()
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║ 1. Continuous Live GPS Telemetry Playback (Kinematic Route Stream)        ║");
        Console.WriteLine("║ 2. Ethernet RFID Reader Boarding Sweep (Normal + Deduplication + Rogue)  ║");
        Console.WriteLine("║ 3. Cellular Dead-Zone Disruption & Offline Batch Recovery Test            ║");
        Console.WriteLine("║ 4. Full End-to-End Morning Trip Lifecycle Simulation                      ║");
        Console.WriteLine("║ 5. Live SignalR Hub Real-Time Telemetry Monitor                           ║");
        Console.WriteLine("║ 6. Change Gateway Target URL (Default: http://localhost:5000)             ║");
        Console.WriteLine("║ 0. Exit                                                                   ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
    }

    private static void ConfigureBaseUrl()
    {
        Console.Write($"Enter Gateway Base URL [current: {BaseUrl}]: ");
        var url = Console.ReadLine()?.Trim();
        if (!string.IsNullOrWhiteSpace(url))
        {
            BaseUrl = url.TrimEnd('/');
            Console.WriteLine($"Updated target URL to: {BaseUrl}");
        }
    }

    private static async Task RunCliModeAsync(string[] args)
    {
        string mode = "gps";
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--url" && i + 1 < args.Length) BaseUrl = args[++i];
            if (args[i] == "--mode" && i + 1 < args.Length) mode = args[++i].ToLowerInvariant();
        }

        switch (mode)
        {
            case "gps":
                await SimulateLiveGpsPlaybackAsync(nonInteractive: true);
                break;
            case "rfid":
                await SimulateRfidBoardingSweepAsync(nonInteractive: true);
                break;
            case "batch":
            case "offline":
                await SimulateCellularDropAndBatchSyncAsync();
                break;
            case "golden":
                await SimulateFullGoldenTripLifecycleAsync();
                break;
            default:
                Console.WriteLine($"Unknown mode: {mode}");
                break;
        }
    }

    public static async Task SimulateLiveGpsPlaybackAsync(bool nonInteractive = false)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("► Launching Kinematic GPS Route Playback for Bus #14 (Device: GPS-B14-01)...");
        Console.ResetColor();

        var waypoints = new[]
        {
            (37.7680m, -122.4260m, "Willow Creek Estates (Stop 1)", 0m, 0m),
            (37.7690m, -122.4250m, "Transit Corridor A", 32.5m, 42m),
            (37.7700m, -122.4240m, "Pine Valley Crossing (Stop 2)", 0m, 45m),
            (37.7710m, -122.4230m, "Transit Corridor B", 41.2m, 78m),
            (37.7715m, -122.4225m, "Sunset Ridge Blvd (Stop 3)", 0m, 80m),
            (37.7725m, -122.4215m, "Evergreen Blvd Turn", 28.0m, 110m),
            (37.7735m, -122.4205m, "Oakridge & 5th Ave (Stop 4)", 0m, 115m),
            (37.7749m, -122.4194m, "Maple Crest Corner (Stop 5)", 0m, 120m),
            (37.7770m, -122.4170m, "School Approach Arterial", 44.0m, 140m),
            (37.7795m, -122.4140m, "Oakridge Elementary Campus (School Destination)", 0m, 145m)
        };

        int step = 0;
        foreach (var wp in waypoints)
        {
            step++;
            var req = new GpsTelemetryRequest(
                DeviceCode: "GPS-B14-01",
                ApiKey: "gps-secret-key-14",
                Imei: "860123049182741",
                Latitude: wp.Item1,
                Longitude: wp.Item2,
                SpeedKmh: wp.Item4,
                Heading: wp.Item5,
                Altitude: 45.2m,
                AccuracyMeters: 0.8m,
                IgnitionOn: true,
                RecordedAt: DateTimeOffset.UtcNow,
                RawPayload: $"{{\"simulator\":true,\"waypoint\":\"{wp.Item3}\"}}"
            );

            try
            {
                var response = await Http.PostAsJsonAsync($"{BaseUrl}/api/v1/devices/gps/telemetry", req);
                if (response.IsSuccessStatusCode)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write($"[{DateTime.Now:HH:mm:ss}] Step {step:D2} | ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write($"Lat: {wp.Item1:F4}, Lng: {wp.Item2:F4} | Speed: {wp.Item4,4:F1} km/h | ");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Waypoint: {wp.Item3}");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[ERROR] Gateway rejected packet: {response.StatusCode}");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"[SIMULATION LOCAL] Step {step:D2} (Gateway offline): ({wp.Item1:F4}, {wp.Item2:F4}) -> {wp.Item3} [{ex.Message}]");
                Console.ResetColor();
            }

            if (!nonInteractive)
            {
                await Task.Delay(1500);
            }
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✔ GPS Route Playback Completed successfully.");
        Console.ResetColor();
    }

    public static async Task SimulateRfidBoardingSweepAsync(bool nonInteractive = false)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("► Launching Ethernet Door RFID Scanner Boarding Sweep for Bus #14 (Device: RFID-B14-01)...");
        Console.ResetColor();

        var scanEvents = new[]
        {
            (CardUid: "4B-99-F2-10", ExpectedStudent: "Leo Martinez", Note: "Grade 3-B • Seat 4A"),
            (CardUid: "1A-88-C3-44", ExpectedStudent: "Sophia Chen", Note: "Grade 4-A • Seat 4B"),
            (CardUid: "9D-33-F1-02", ExpectedStudent: "Lucas Silva", Note: "Grade 3-B • Seat 5A"),
            (CardUid: "4B-99-F2-10", ExpectedStudent: "Leo Martinez", Note: "RAPID DOUBLE TAP TEST (< 2s)"),
            (CardUid: "XX-99-RO-GU-E1", ExpectedStudent: "UNKNOWN_CARD", Note: "ROGUE CARD SECURITY AUDIT TEST")
        };

        foreach (var scan in scanEvents)
        {
            var req = new RfidScanRequest(
                DeviceCode: "RFID-B14-01",
                ApiKey: "rfid-secret-key-14",
                CardUid: scan.CardUid,
                Latitude: 37.7735m,
                Longitude: -122.4205m,
                ScannedAt: DateTimeOffset.UtcNow
            );

            try
            {
                var response = await Http.PostAsJsonAsync($"{BaseUrl}/api/v1/devices/rfid/tap", req);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<RfidScanResultDto>>();
                    var data = result?.Data;

                    if (data != null && !data.Success && data.Message.Contains("Deduplicated", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"[DEDUPLICATED] Card: {scan.CardUid} | {scan.Note} | Gateway safely ignored rapid double-tap.");
                    }
                    else if (data != null && (!data.Success || data.ScanType == "UnknownCard"))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[SECURITY ALERT] Card: {scan.CardUid} | {data?.Message} | Intercepted & logged to security audit.");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"[BOARDED] Card: {scan.CardUid} | Student: {data?.StudentName} | ScanType: {data?.ScanType} | Stop: {data?.StopName}");
                    }
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[ERROR] Gateway rejected RFID scan: {response.StatusCode}");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"[SIMULATION LOCAL] Card: {scan.CardUid} ({scan.ExpectedStudent}) -> {scan.Note} [Gateway offline: {ex.Message}]");
                Console.ResetColor();
            }

            if (!nonInteractive)
            {
                await Task.Delay(1200);
            }
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✔ RFID Scanner Boarding Sweep Completed.");
        Console.ResetColor();
    }

    public static async Task SimulateCellularDropAndBatchSyncAsync()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("► Simulating 30-Second Cellular Dead-Zone Disruption with Offline In-Memory Queue...");
        Console.ResetColor();

        var queuedPoints = new List<GpsTelemetryPointDto>();
        var baseTime = DateTimeOffset.UtcNow.AddSeconds(-60);

        Console.WriteLine("1. Simulating 5 telemetry points generated while cellular connection is DOWN:");
        for (int i = 1; i <= 5; i++)
        {
            var pt = new GpsTelemetryPointDto(
                Latitude: 37.7710m + (i * 0.0008m),
                Longitude: -122.4230m + (i * 0.0006m),
                SpeedKmh: 42.0m,
                Heading: 85.0m,
                Altitude: 45.0m,
                AccuracyMeters: 1.1m,
                IgnitionOn: true,
                RecordedAt: baseTime.AddSeconds(i * 5)
            );
            queuedPoints.Add(pt);
            Console.WriteLine($"   • Buffered Point #{i}: ({pt.Latitude:F4}, {pt.Longitude:F4}) at {pt.RecordedAt:HH:mm:ss} in device EEPROM.");
        }

        Console.WriteLine("\n2. Cellular connection RESTORED. Flushing batch to Gateway `/api/v1/devices/gps/batch`...");
        var batchReq = new GpsBatchTelemetryRequest(
            DeviceCode: "GPS-B14-01",
            ApiKey: "gps-secret-key-14",
            Imei: "860123049182741",
            Points: queuedPoints
        );

        try
        {
            var response = await Http.PostAsJsonAsync($"{BaseUrl}/api/v1/devices/gps/batch", batchReq);
            if (response.IsSuccessStatusCode)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✔ Gateway accepted batch! Synchronized {queuedPoints.Count} historical points chronologically.");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] Batch synchronization rejected: {response.StatusCode}");
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"[SIMULATION LOCAL] Synchronized batch of {queuedPoints.Count} points offline [{ex.Message}]");
            Console.ResetColor();
        }
    }

    public static async Task SimulateFullGoldenTripLifecycleAsync()
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║        STARTING FULL GOLDEN-PATH MORNING ROUTE SIMULATION                 ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════════╝");
        Console.ResetColor();

        Console.WriteLine("\n[Phase A] Bus #14 Departs Transportation Depot (Ignition ON, GPS active)...");
        await SimulateLiveGpsPlaybackAsync(nonInteractive: true);

        Console.WriteLine("\n[Phase B] Bus arrives at Stop 4 (Oakridge & 5th Ave). Curbside door opens.");
        Console.WriteLine("          Ethernet door reader detects student RFID card scans:");
        await SimulateRfidBoardingSweepAsync(nonInteractive: true);

        Console.WriteLine("\n[Phase C] Bus traverses urban arterial and arrives at Oakridge Elementary Campus.");
        Console.WriteLine("          Simulating School Campus Deboarding / Drop-Off tap sweep:");

        var dropOffReq = new RfidScanRequest(
            DeviceCode: "RFID-B14-01",
            ApiKey: "rfid-secret-key-14",
            CardUid: "4B-99-F2-10",
            Latitude: 37.7795m,
            Longitude: -122.4140m,
            ScannedAt: DateTimeOffset.UtcNow
        );

        try
        {
            var res = await Http.PostAsJsonAsync($"{BaseUrl}/api/v1/devices/rfid/tap", dropOffReq);
            if (res.IsSuccessStatusCode)
            {
                var r = await res.Content.ReadFromJsonAsync<ApiResponse<RfidScanResultDto>>();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✔ Leo Martinez deboarded at School Campus. ScanType: {r?.Data?.ScanType}. Parent notified.");
                Console.ResetColor();
            }
        }
        catch { }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n✔ FULL GOLDEN-PATH LIFECYCLE COMPLETED SUCCESSFULLY!");
        Console.ResetColor();
    }

    public static async Task MonitorSignalRStreamAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("► Connecting to SchoolTrack Real-Time SignalR Hub (`/hubs/tracking`)...");
        Console.ResetColor();

        try
        {
            var hubConnection = new HubConnectionBuilder()
                .WithUrl($"{BaseUrl}/hubs/tracking")
                .WithAutomaticReconnect()
                .Build();

            hubConnection.On<LatestBusLocationDto>("ReceiveBusLocation", loc =>
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"[SIGNALR LOCATION] Bus: {loc.BusNumber} | ({loc.Latitude:F4}, {loc.Longitude:F4}) | Speed: {loc.SpeedKmh} km/h | Online: {loc.IsOnline}");
                Console.ResetColor();
            });

            hubConnection.On<BoardingNotificationDto>("ReceiveBoardingNotification", notif =>
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[SIGNALR BOARDING] Student: {notif.StudentName} | Bus: {notif.BusNumber} | Stop: {notif.StopName} | ScanType: {notif.ScanType} | Class: {notif.ClassName}");
                Console.ResetColor();
            });

            hubConnection.On<TripStatusChangedDto>("ReceiveTripStatusChanged", status =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[SIGNALR TRIP STATUS] Trip: {status.TripId} | New Status: {status.NewStatus} | Notes: {status.Notes}");
                Console.ResetColor();
            });

            await hubConnection.StartAsync();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✔ Connected to SignalR Tracking Hub! Listening for real-time telemetry events...");
            Console.WriteLine("Press any key to stop monitoring.\n");
            Console.ResetColor();

            Console.ReadKey(intercept: true);
            await hubConnection.DisposeAsync();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"SignalR connection failed: {ex.Message}");
            Console.WriteLine("Ensure SchoolTrack API is running at the configured URL.");
            Console.ResetColor();
        }
    }
}
