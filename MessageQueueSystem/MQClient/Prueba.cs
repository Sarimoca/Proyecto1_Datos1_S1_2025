using System;
using MessageQueueClient;

namespace MQClientTest
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Iniciando programa de prueba MQClient...");

                // Generar un ID único para esta instancia
                Guid clientId = Guid.NewGuid();
                Console.WriteLine($"ID de cliente: {clientId}");

                // Crear un cliente MQ
                MQClient client = new MQClient("127.0.0.1", 5000, clientId);

                // Crear un tema
                Topic topic = new Topic("Noticias");

                // Suscribirse al tema
                Console.WriteLine("Suscribiéndose al tema...");
                bool subscribed = client.Subscribe(topic);
                Console.WriteLine($"Suscripción exitosa: {subscribed}");

                // Publicar un mensaje
                Console.WriteLine("Publicando mensaje...");
                Message message = new Message("¡Hola, esto es una prueba!");
                bool published = client.Publish(message, topic);
                Console.WriteLine($"Publicación exitosa: {published}");

                // Recibir un mensaje
                Console.WriteLine("Recibiendo mensaje...");
                Message receivedMessage = client.Receive(topic);
                Console.WriteLine($"Mensaje recibido: {receivedMessage.Content}");

                // Cancelar suscripción
                Console.WriteLine("Cancelando suscripción...");
                bool unsubscribed = client.Unsubscribe(topic);
                Console.WriteLine($"Desuscripción exitosa: {unsubscribed}");
            }
            catch (MQClientException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inesperado: {ex.Message}");
            }

            Console.WriteLine("Presione cualquier tecla para finalizar...");
            Console.ReadKey();
        }
    }
}
