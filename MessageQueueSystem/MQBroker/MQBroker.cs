using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

// Implementación de una lista genérica personalizada
class MyList<T>
{
    private T[] items;
    private int count;
    private int capacity;

    public MyList(int initialCapacity = 4)
    {
        capacity = initialCapacity;
        items = new T[capacity];
        count = 0;
    }

    public int Count => count;

    public void Add(T item)
    {
        if (count == capacity)
        {
            // Duplicar la capacidad si es necesario
            capacity *= 2;
            T[] newItems = new T[capacity];
            for (int i = 0; i < count; i++)
            {
                newItems[i] = items[i];
            }
            items = newItems;
        }
        items[count] = item;
        count++;
    }

    public bool Contains(T item)
    {
        for (int i = 0; i < count; i++)
        {
            if (EqualityComparer<T>.Default.Equals(items[i], item))
            {
                return true;
            }
        }
        return false;
    }

    public bool Remove(T item)
    {
        for (int i = 0; i < count; i++)
        {
            if (EqualityComparer<T>.Default.Equals(items[i], item))
            {
                // Mover todos los elementos hacia atrás
                for (int j = i; j < count - 1; j++)
                {
                    items[j] = items[j + 1];
                }
                count--;
                return true;
            }
        }
        return false;
    }

    public T[] ToArray()
    {
        T[] result = new T[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = items[i];
        }
        return result;
    }

    public T this[int index]
    {
        get
        {
            if (index < 0 || index >= count)
            {
                throw new IndexOutOfRangeException();
            }
            return items[index];
        }
        set
        {
            if (index < 0 || index >= count)
            {
                throw new IndexOutOfRangeException();
            }
            items[index] = value;
        }
    }
}

// Implementación de una cola (queue) personalizada
class MyQueue<T>
{
    private T[] items;
    private int head;
    private int tail;
    private int count;
    private int capacity;

    public MyQueue(int initialCapacity = 4)
    {
        capacity = initialCapacity;
        items = new T[capacity];
        head = 0;
        tail = 0;
        count = 0;
    }

    public int Count => count;

    public void Enqueue(T item)
    {
        if (count == capacity)
        {
            // Duplicar la capacidad si es necesario
            int newCapacity = capacity * 2;
            T[] newItems = new T[newCapacity];
            for (int i = 0; i < count; i++)
            {
                newItems[i] = items[(head + i) % capacity];
            }
            items = newItems;
            head = 0;
            tail = count;
            capacity = newCapacity;
        }

        items[tail] = item;
        tail = (tail + 1) % capacity;
        count++;
    }

    public T Dequeue()
    {
        if (count == 0)
        {
            throw new InvalidOperationException("La cola está vacía");
        }

        T item = items[head];
        head = (head + 1) % capacity;
        count--;
        return item;
    }

    public T Peek()
    {
        if (count == 0)
        {
            throw new InvalidOperationException("La cola está vacía");
        }

        return items[head];
    }
}

// Implementación de un diccionario personalizado simple
class MyDictionary<TKey, TValue>
{
    private struct KeyValuePair
    {
        public TKey Key;
        public TValue Value;
    }

    private MyList<KeyValuePair> items;

    public MyDictionary()
    {
        items = new MyList<KeyValuePair>();
    }

    public void Add(TKey key, TValue value)
    {
        if (ContainsKey(key))
        {
            throw new ArgumentException("La clave ya existe en el diccionario");
        }

        items.Add(new KeyValuePair { Key = key, Value = value });
    }

    public bool ContainsKey(TKey key)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (EqualityComparer<TKey>.Default.Equals(items[i].Key, key))
            {
                return true;
            }
        }
        return false;
    }

    public bool Remove(TKey key)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (EqualityComparer<TKey>.Default.Equals(items[i].Key, key))
            {
                KeyValuePair lastItem = items[items.Count - 1];
                items[i] = lastItem;
                items.Remove(lastItem);
                return true;
            }
        }
        return false;
    }

    public TValue this[TKey key]
    {
        get
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (EqualityComparer<TKey>.Default.Equals(items[i].Key, key))
                {
                    return items[i].Value;
                }
            }
            throw new KeyNotFoundException();
        }
        set
        {
            bool found = false;
            for (int i = 0; i < items.Count; i++)
            {
                if (EqualityComparer<TKey>.Default.Equals(items[i].Key, key))
                {
                    KeyValuePair pair = items[i];
                    pair.Value = value;
                    items[i] = pair;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                items.Add(new KeyValuePair { Key = key, Value = value });
            }
        }
    }

    // Método para obtener todas las claves
    public MyList<TKey> Keys
    {
        get
        {
            MyList<TKey> keys = new MyList<TKey>();
            for (int i = 0; i < items.Count; i++)
            {
                keys.Add(items[i].Key);
            }
            return keys;
        }
    }
}

// Implementación de un sistema EqualityComparer personalizado
class EqualityComparer<T>
{
    public static EqualityComparer<T> Default { get; } = new EqualityComparer<T>();

    public bool Equals(T x, T y)
    {
        if (x == null && y == null)
            return true;
        if (x == null || y == null)
            return false;
        return x.Equals(y);
    }
}

// Clase MQBroker
class MQBroker
{
    private TcpListener server;
    private MyDictionary<string, MyList<Guid>> suscriptores;
    private MyDictionary<string, MyDictionary<Guid, MyQueue<string>>> colasMensajes;

    public MQBroker(string ip, int port)
    {
        server = new TcpListener(IPAddress.Parse(ip), port);
        suscriptores = new MyDictionary<string, MyList<Guid>>();
        colasMensajes = new MyDictionary<string, MyDictionary<Guid, MyQueue<string>>>();
    }

    public void Start()
    {
        server.Start();
        Console.WriteLine("MQBroker escuchando en {0}", server.LocalEndpoint);

        try
        {
            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(state => HandleClient((TcpClient)state), client);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error en el servidor: " + ex.Message);
        }
        finally
        {
            server.Stop();
        }
    }

    private void HandleClient(TcpClient client)
    {
        try
        {
            using (NetworkStream stream = client.GetStream())
            {
                byte[] buffer = new byte[4096]; // Aumentar tamaño del buffer para mensajes más grandes
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                Console.WriteLine($"Recibido: {request}");

                string response = ProcesarPeticion(request);
                byte[] responseBytes = Encoding.UTF8.GetBytes(response);

                stream.Write(responseBytes, 0, responseBytes.Length);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error al manejar el cliente: " + ex.Message);
        }
        finally
        {
            client.Close();
        }
    }

    private string ProcesarPeticion(string request)
    {
        try
        {
            string[] partes = request.Split('|');

            if (partes.Length < 2)
            {
                return "ERROR|Formato de petición inválido";
            }

            string comando = partes[0];

            switch (comando)
            {
                case "SUBSCRIBE":
                    if (partes.Length < 3)
                        return "ERROR|Faltan parámetros para SUBSCRIBE";
                    return Subscribe(partes[1], Guid.Parse(partes[2]));

                case "UNSUBSCRIBE":
                    if (partes.Length < 3)
                        return "ERROR|Faltan parámetros para UNSUBSCRIBE";
                    return Unsubscribe(partes[1], Guid.Parse(partes[2]));

                case "PUBLISH":
                    if (partes.Length < 4)
                        return "ERROR|Faltan parámetros para PUBLISH";
                    return Publish(partes[1], partes[2], Guid.Parse(partes[3]));

                case "RECEIVE":
                    if (partes.Length < 3)
                        return "ERROR|Faltan parámetros para RECEIVE";
                    return Receive(partes[1], Guid.Parse(partes[2]));

                default:
                    return "ERROR|Comando desconocido: " + comando;
            }
        }
        catch (FormatException)
        {
            return "ERROR|Formato de GUID inválido";
        }
        catch (Exception ex)
        {
            return $"ERROR|Error interno: {ex.Message}";
        }
    }

    private string Subscribe(string tema, Guid appId)
    {
        lock (suscriptores) // Bloqueo para evitar condiciones de carrera
        {

           
            


            // Verificar si el tema existe
            if (!suscriptores.ContainsKey(tema))
            {
                suscriptores[tema] = new MyList<Guid>();
            }

            // Verificar si ya está suscrito
            if (suscriptores[tema].Contains(appId))
            {
                Console.WriteLine($"Ya está suscrito al tema: {tema}");
                return "ERROR|Ya está suscrito al tema: " + tema;
            }

            // Añadir a la lista de suscriptores
            suscriptores[tema].Add(appId);

            // Crear la cola para los mensajes
            if (!colasMensajes.ContainsKey(tema))
            {
                colasMensajes[tema] = new MyDictionary<Guid, MyQueue<string>>();
            }

            colasMensajes[tema][appId] = new MyQueue<string>();

            Console.WriteLine($"Subscripción exitosa: {appId} a {tema}");
            return "OK|Suscripción exitosa";
        }
    }

    private string Unsubscribe(string tema, Guid appId)
    {
        lock (suscriptores)
        {
            // Verificar si el tema existe y el AppID está suscrito
            if (!suscriptores.ContainsKey(tema) || !suscriptores[tema].Contains(appId))
            {
                return "ERROR|No está suscrito al tema: " + tema;
            }

            // Eliminar de la lista de suscriptores
            suscriptores[tema].Remove(appId);

            // Eliminar la cola de mensajes
            if (colasMensajes.ContainsKey(tema))
            {
                MyDictionary<Guid, MyQueue<string>> colas = colasMensajes[tema];
                if (colas.ContainsKey(appId))
                {
                    colas.Remove(appId);
                }
            }

            Console.WriteLine($"Desuscripción exitosa: {appId} de {tema}");
            return "OK|Desuscripción exitosa";
        }
    }

    private string Publish(string tema, string mensaje, Guid appId)
    {
        lock (suscriptores)
        {
            // Verificar si el tema existe
            if (!suscriptores.ContainsKey(tema) || suscriptores[tema].Count == 0)
            {
                Console.WriteLine($"Mensaje ignorado - Tema: {tema}");
                return "ERROR|No hay suscriptores para este tema: " + tema;
            }

            // Asegurarse de que existe el diccionario de colas para este tema
            if (!colasMensajes.ContainsKey(tema))
            {
                return "ERROR|Tema no configurado correctamente: " + tema;
            }

            int mensajesEnviados = 0;
            MyDictionary<Guid, MyQueue<string>> colas = colasMensajes[tema];

            // Para cada suscriptor, colocar el mensaje en su cola
            for (int i = 0; i < suscriptores[tema].Count; i++)
            {
                Guid subscriptor = suscriptores[tema][i];

                if (colas.ContainsKey(subscriptor))
                {
                    colas[subscriptor].Enqueue(mensaje);
                    mensajesEnviados++;
                }
            }

            Console.WriteLine($"Mensaje publicado en {tema}: {mensaje.Substring(0, Math.Min(20, mensaje.Length))}...");
            return $"OK|Mensaje publicado a {mensajesEnviados} suscriptores";
        }
    }

    private string Receive(string tema, Guid appId)
    {
        lock (suscriptores)
        {
            // Verificar si el AppID está suscrito al tema
            if (!suscriptores.ContainsKey(tema) || !suscriptores[tema].Contains(appId))
            {
                Console.WriteLine($"No se encuentra suscrito al tema: {tema}");
                return "ERROR|No está suscrito al tema: " + tema;
            }

            // Verificar si existe la cola y tiene mensajes
            if (!colasMensajes.ContainsKey(tema) || !colasMensajes[tema].ContainsKey(appId))
            {
                Console.WriteLine($"Mensaje no encontrado - Tema: {tema}");
                return "ERROR|Cola no encontrada";
            }

            MyQueue<string> cola = colasMensajes[tema][appId];

            if (cola.Count == 0)
            {
                Console.WriteLine($"No existen mensajes en cola del tema: {tema}");
                return "ERROR|No hay mensajes en la cola";
            }

            // Sacar un mensaje de la cola (FIFO)
            string mensaje = cola.Dequeue();

            Console.WriteLine($"Mensaje entregado a {appId} desde {tema}");
            return $"OK|{mensaje}";
        }
    }
}

// Clase para iniciar el broker
class Program
{
    public static void Main()
    {
        try
        {
            string ipAddress = "127.0.0.1";
            int port = 5000;

            Console.WriteLine($"Iniciando MQBroker en {ipAddress}:{port}...");
            MQBroker broker = new MQBroker(ipAddress, port);
            broker.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error al iniciar el broker: " + ex.Message);
            Console.WriteLine(ex.StackTrace);
        }
    }
}
