using System.Reflection;

using Microsoft.FlightSimulator.SimConnect;

string flightPlansDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "Packages",
    "Microsoft.Limitless_8wekyb3d8bbwe",
    "LocalState"
    );
FileInfo flightPlanFileInfo = Directory.EnumerateFiles(flightPlansDir, "*.pln")
    .Select(plnFilePath => new FileInfo(plnFilePath))
    .OrderByDescending(plnFileInfo => plnFileInfo.LastWriteTime)
    .First();
Console.WriteLine($"Selecting flight plan file: {flightPlanFileInfo.Name}");

var programRef = Assembly.GetExecutingAssembly().GetName();
using var eventHandle = new AutoResetEvent(false);
using var simconnect = new SimConnect(programRef.Name, hEventHandle: eventHandle, hWnd: IntPtr.Zero, UserEventWin32: default, ConfigIndex: default);
simconnect.OnRecvOpen += OnSimConnectSessionOpen;

bool shouldTerminate = false;
bool imminentTerminate = false;
TimeSpan simConnectTimeout = TimeSpan.FromSeconds(1);

while (!shouldTerminate)
{
    if (eventHandle.WaitOne(simConnectTimeout))
    {
        simconnect.ReceiveMessage();
        if (imminentTerminate)
            shouldTerminate = true;
    }
}

void OnSimConnectSessionOpen(SimConnect simconnect, SIMCONNECT_RECV_OPEN session)
{
    Console.WriteLine($"""
        SimConnect session connected to:
            App:        {session.szApplicationName}
            Version:    {session.dwApplicationVersionMajor}.{session.dwApplicationVersionMinor}.{session.dwApplicationBuildMajor}.{session.dwApplicationBuildMinor}
            SimConnect: {session.dwSimConnectVersionMajor}.{session.dwSimConnectVersionMinor}.{session.dwSimConnectBuildMajor}.{session.dwSimConnectBuildMinor}
        """);
    LoadFlightPlan(simconnect);
}

void LoadFlightPlan(SimConnect simconnect)
{
    simconnect.FlightPlanLoad(flightPlanFileInfo.FullName);
    Console.WriteLine("Selected flight plan loaded.");
    imminentTerminate = true;
}
