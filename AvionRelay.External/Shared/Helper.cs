using System.Net;
using System.Net.Sockets;
using Serilog;

namespace AvionRelay.External;

public static class Helper
{
    /// <summary>
    /// Gets the systems preferred IP Address by opening a socket and reading its local endpoint value
    /// </summary>
    /// <returns></returns>
    public static Uri GetPreferredIPAddress()
    {
        string localIP = "127.0.0.1";
        try
        {
            
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint? endPoint = socket.LocalEndPoint as IPEndPoint;
                localIP = endPoint != null ? endPoint.Address.ToString() : "127.0.0.1";
            }
        }
        catch (Exception e)
        {
            Log.Logger.Error(e, "Error encountered {ErrorMessage}", e.Message);
        }
        HelpfulTypesAndExtensions.DebugHelp.DebugWriteLine($"Preferred IP: {localIP}");
        return new UriBuilder(localIP).Uri;
    }
    
}