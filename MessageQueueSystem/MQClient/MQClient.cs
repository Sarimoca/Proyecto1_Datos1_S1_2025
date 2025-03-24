using System;
using System.Net.Sockets;
using System.Text;

class MQClient
{
    private string brokerIp;
    private int brokerPort;

    public MQClient(string ip, int port)
    {
        brokerIp = ip;
        brokerPort = port;
    }

    private string SendMessage(string message)
    {
        using TcpClient client = new TcpClient(brokerIp, brokerPort);
        using NetworkStream stream = client.GetStream();

        byte[] data = Encoding.UTF8.GetBytes(message);
        stream.Write(data, 0, data.Length);

        byte[] buffer = new byte[1024];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);
        return Encoding.UTF8.GetString(buffer, 0, bytesRead);
    }

    public void Subscribe(string topic, Guid clientId)
    {
        string response = SendMessage($"SUBSCRIBE|{topic}|{clientId}");
        Console.WriteLine(response);
    }

    public void Publish(string topic, string message, Guid clientId)
    {
        string response = SendMessage($"PUBLISH|{topic}|{message}|{clientId}");
        Console.WriteLine(response);
    }

    public void Receive(string topic, Guid clientId)
    {
        string response = SendMessage($"RECEIVE|{topic}|{clientId}");
        Console.WriteLine(response);
    }

    static void Main()
    {
        MQClient client = new MQClient("127.0.0.1", 5000);
        Guid clientId = Guid.NewGuid();

        client.Subscribe("Noticias", clientId);
        client.Publish("Noticias", "¡Hola, esto es una prueba!", clientId);
        client.Receive("Noticias", clientId);
    }
}
