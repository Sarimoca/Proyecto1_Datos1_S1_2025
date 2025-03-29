using System;
using System.Net.Sockets;
using System.Text;

namespace MessageQueueClient
{
    // Clase Topic que encapsula el nombre del tema
    public class Topic
    {
        private string topicName;

        public Topic(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("El nombre del tema no puede estar vacío", nameof(name));
            }
            topicName = name;
        }

        public string Name => topicName;

        public override string ToString()
        {
            return topicName;
        }
    }

    // Clase Message que encapsula el contenido del mensaje
    public class Message
    {
        private string content;

        public Message(string content)
        {
            this.content = content ?? "";
        }

        public Message() : this("") { }

        public string Content
        {
            get => content;
            set => content = value ?? "";
        }

        public static Message FromResponse(string response)
        {
            if (response.StartsWith("OK|"))
            {
                string content = response.Substring(3); // Skip "OK|"
                return new Message(content);
            }
            return new Message();
        }

        public override string ToString()
        {
            return content;
        }
    }

    // Definir excepciones personalizadas para MQClient
    public class MQClientException : Exception
    {
        public MQClientException(string message) : base(message) { }
    }

    public class MQSubscriptionException : MQClientException
    {
        public MQSubscriptionException(string message) : base(message) { }
    }

    public class MQPublishException : MQClientException
    {
        public MQPublishException(string message) : base(message) { }
    }

    public class MQReceiveException : MQClientException
    {
        public MQReceiveException(string message) : base(message) { }
    }

    // Clase principal MQClient
    public class MQClient
    {
        private readonly string brokerIp;
        private readonly int brokerPort;
        private readonly Guid appId;

        // Constructor que crea un nuevo MQClient
        public MQClient(string ip, int port, Guid appId)
        {
            if (string.IsNullOrWhiteSpace(ip))
            {
                throw new ArgumentException("La dirección IP no puede estar vacía", nameof(ip));
            }

            if (port <= 0 || port > 65535)
            {
                throw new ArgumentException("El puerto debe estar entre 1 y 65535", nameof(port));
            }

            if (appId == Guid.Empty)
            {
                throw new ArgumentException("El AppID no puede ser vacío", nameof(appId));
            }

            brokerIp = ip;
            brokerPort = port;
            this.appId = appId;
        }

        // Envía un mensaje al broker y recibe la respuesta
        private string SendRequest(string message)
        {
            TcpClient client = null;

            try
            {
                client = new TcpClient();

                // Establecer timeout para la conexión (5 segundos)
                var result = client.BeginConnect(brokerIp, brokerPort, null, null);
                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5));

                if (!success)
                {
                    throw new MQClientException("Timeout al conectar con el broker");
                }

                client.EndConnect(result);

                using (NetworkStream stream = client.GetStream())
                {
                    // Establecer timeout para operaciones en el stream
                    stream.ReadTimeout = 5000;
                    stream.WriteTimeout = 5000;

                    // Convertir el mensaje a bytes y enviarlo
                    byte[] data = Encoding.UTF8.GetBytes(message);
                    stream.Write(data, 0, data.Length);

                    // Establecer buffer para recibir la respuesta
                    byte[] buffer = new byte[4096];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);

                    return Encoding.UTF8.GetString(buffer, 0, bytesRead);
                }
            }
            catch (SocketException se)
            {
                throw new MQClientException($"Error de socket: {se.Message}");
            }
            catch (Exception ex) when (!(ex is MQClientException))
            {
                throw new MQClientException($"Error al comunicarse con el broker: {ex.Message}");
            }
            finally
            {
                client?.Close();
            }
        }

        // Suscribe la aplicación a un tema específico
        public bool Subscribe(Topic topic)
        {
            if (topic == null)
            {
                throw new ArgumentNullException(nameof(topic), "El tema no puede ser nulo");
            }

            try
            {
                string request = $"SUBSCRIBE|{topic.Name}|{appId}";
                string response = SendRequest(request);

                if (response.StartsWith("OK|"))
                {
                    return true;
                }
                else if (response.StartsWith("ERROR|"))
                {
                    string errorMessage = response.Substring(6); // Skip "ERROR|"
                    if (errorMessage.Contains("Ya está suscrito"))
                    {
                        // Si ya está suscrito, consideramos que la operación es exitosa
                        return true;
                    }
                    throw new MQSubscriptionException(errorMessage);
                }
                else
                {
                    throw new MQSubscriptionException("Respuesta inesperada del broker");
                }
            }
            catch (MQClientException ex) when (!(ex is MQSubscriptionException))
            {
                throw new MQSubscriptionException($"Error al suscribirse: {ex.Message}");
            }
        }

        // Cancela la suscripción de la aplicación a un tema específico
        public bool Unsubscribe(Topic topic)
        {
            if (topic == null)
            {
                throw new ArgumentNullException(nameof(topic), "El tema no puede ser nulo");
            }

            try
            {
                string request = $"UNSUBSCRIBE|{topic.Name}|{appId}";
                string response = SendRequest(request);

                if (response.StartsWith("OK|"))
                {
                    return true;
                }
                else if (response.StartsWith("ERROR|"))
                {
                    string errorMessage = response.Substring(6); // Skip "ERROR|"
                    if (errorMessage.Contains("No está suscrito"))
                    {
                        // Si no está suscrito, consideramos que ya está en el estado deseado
                        return true;
                    }
                    throw new MQSubscriptionException(errorMessage);
                }
                else
                {
                    throw new MQSubscriptionException("Respuesta inesperada del broker");
                }
            }
            catch (MQClientException ex) when (!(ex is MQSubscriptionException))
            {
                throw new MQSubscriptionException($"Error al desuscribirse: {ex.Message}");
            }
        }

        // Publica un mensaje en un tema específico
        public bool Publish(Message message, Topic topic)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message), "El mensaje no puede ser nulo");
            }

            if (topic == null)
            {
                throw new ArgumentNullException(nameof(topic), "El tema no puede ser nulo");
            }

            try
            {
                string request = $"PUBLISH|{topic.Name}|{message.Content}|{appId}";
                string response = SendRequest(request);

                if (response.StartsWith("OK|"))
                {
                    return true;
                }
                else if (response.StartsWith("ERROR|"))
                {
                    string errorMessage = response.Substring(6); // Skip "ERROR|"
                    throw new MQPublishException(errorMessage);
                }
                else
                {
                    throw new MQPublishException("Respuesta inesperada del broker");
                }
            }
            catch (MQClientException ex) when (!(ex is MQPublishException))
            {
                throw new MQPublishException($"Error al publicar: {ex.Message}");
            }
        }
        // Recibe un mensaje de un tema específico
        public Message Receive(Topic topic)
        {
            if (topic == null)
            {
                throw new ArgumentNullException(nameof(topic), "El tema no puede ser nulo");
            }

            try
            {
                string request = $"RECEIVE|{topic.Name}|{appId}";
                string response = SendRequest(request);

                if (response.StartsWith("OK|"))
                {
                    // Extraer el contenido del mensaje
                    string content = response.Substring(3); // Skip "OK|"
                    return new Message(content);
                }
                else if (response.StartsWith("ERROR|"))
                {
                    string errorMessage = response.Substring(6); // Skip "ERROR|"
                    throw new MQReceiveException(errorMessage);
                }
                else
                {
                    throw new MQReceiveException("Respuesta inesperada del broker");
                }
            }
            catch (MQClientException ex) when (!(ex is MQReceiveException))
            {
                throw new MQReceiveException($"Error al recibir: {ex.Message}");
            }
        }
    }
}
