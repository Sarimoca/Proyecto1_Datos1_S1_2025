using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Threading; // Agregar Threading

class MQBroker
{
    private TcpListener server;
    private Dictionary<string, List<Guid>> suscriptores = new();
    private Dictionary<string, Dictionary<Guid, Queue<string>>> colasMensajes = new();

    public MQBroker(string ip, int port)
    {
        server = new TcpListener(IPAddress.Parse(ip), port);
    }

    public void Start()
    {
        server.Start();
        Console.WriteLine("MQBroker escuchando...");

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            var clientThread = new Thread(() => HandleClient(client));
            clientThread.Start();
        }
    }

    private void HandleClient(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);
        string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        Console.WriteLine($"Recibido: {request}");
        string response = ProcesarPeticion(request);

        byte[] responseBytes = Encoding.UTF8.GetBytes(response);
        stream.Write(responseBytes, 0, responseBytes.Length);
        client.Close();
    }

    private string ProcesarPeticion(string request)
    {
        string[] partes = request.Split('|');
        string comando = partes[0];

        switch (comando)
        {
            case "SUBSCRIBE":
                return Subscribe(partes[1], Guid.Parse(partes[2]));
            case "UNSUBSCRIBE":
                return Unsubscribe(partes[1], Guid.Parse(partes[2]));
            case "PUBLISH":
                return Publish(partes[1], partes[2], Guid.Parse(partes[3]));
            case "RECEIVE":
                return Receive(partes[1], Guid.Parse(partes[2]));
            default:
                return "ERROR|Comando desconocido";
        }
    }

    private string Subscribe(string tema, Guid appId)
    {
        if (!suscriptores.ContainsKey(tema))
            suscriptores[tema] = new List<Guid>();

        if (!suscriptores[tema].Contains(appId))
        {
            suscriptores[tema].Add(appId);
            if (!colasMensajes.ContainsKey(tema))
                colasMensajes[tema] = new Dictionary<Guid, Queue<string>>();

            colasMensajes[tema][appId] = new Queue<string>();
            return "OK|Suscripción exitosa";
        }

        return "ERROR|Ya está suscrito";
    }

    private string Unsubscribe(string tema, Guid appId)
    {
        if (suscriptores.ContainsKey(tema) && suscriptores[tema].Contains(appId))
        {
            suscriptores[tema].Remove(appId);
            colasMensajes[tema].Remove(appId);
            return "OK|Desuscripción exitosa";
        }
        return "ERROR|No está suscrito";
    }

    private string Publish(string tema, string mensaje, Guid appId)
    {
        if (!suscriptores.ContainsKey(tema))
            return "ERROR|Tema no existe";

        foreach (var subscriptor in suscriptores[tema])
        {
            colasMensajes[tema][subscriptor].Enqueue(mensaje);
        }

        return "OK|Mensaje publicado";
    }

    private string Receive(string tema, Guid appId)
    {
        if (!suscriptores.ContainsKey(tema) || !suscriptores[tema].Contains(appId))
            return "ERROR|No está suscrito";

        if (colasMensajes[tema][appId].Count == 0)
            return "ERROR|No hay mensajes";

        string mensaje = colasMensajes[tema][appId].Dequeue();
        return $"OK|{mensaje}";
    }
}

// Clase separada para el cliente
class MQClient
{
    public static void Main()
    {
        MQBroker broker = new MQBroker("127.0.0.1", 5000);
        broker.Start();
    }
}
