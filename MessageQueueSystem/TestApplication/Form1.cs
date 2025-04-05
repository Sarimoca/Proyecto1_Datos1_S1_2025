using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MessageQueueClient;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace TestApplication
{
    public partial class Form1 : Form
    {
        private MQClient client;
        private Guid appId;
        private Dictionary<string, bool> subscribedTopics = new Dictionary<string, bool>();

        public Form1()
        {
            InitializeComponent();
            Initialize();
        }

        private void Initialize()
        {
            // Generar un GUID único para esta aplicación
            appId = Guid.NewGuid();
            textBox3.Text = appId.ToString();



            // Configurar ListView de suscripciones
            listView1.View = View.Details;
            listView1.FullRowSelect = true;
            listView1.GridLines = true;
            listView1.Columns.Add("Tema", 260);
            listView1.Columns.Add("Estado", 102);

            // En el método Initialize() o LoadSampleTopics():
            comboBox1.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            comboBox1.AutoCompleteSource = AutoCompleteSource.ListItems;
            comboBox2.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            comboBox2.AutoCompleteSource = AutoCompleteSource.ListItems;
            comboBox3.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            comboBox3.AutoCompleteSource = AutoCompleteSource.ListItems;

            // Configurar ListView de mensajes recibidos
            listView2.View = View.Details;
            listView2.FullRowSelect = true;
            listView2.Columns.Add("Tema", 80);
            listView2.Columns.Add("Contenido", 180);
            listView2.Columns.Add("Fecha/Hora", 102);
            // Estado inicial
            UpdateStatus("Desconectado");
            EnableControls(false);
        }

        private void LoadSampleTopics()
        {
            // Lista de 10 temas de ejemplo
            string[] sampleTopics = new string[]
            {
        "Tema/Noticias/Deportes",
        "Tema/Noticias/Tecnologia",
        "Tema/Noticias/Finanzas",
        "Tema/Sensores/Temperatura",
        "Tema/Sensores/Humedad",
        "Tema/Transacciones/Pagos",
        "Tema/Transacciones/Reembolsos",
        "Tema/Usuarios/Registros",
        "Tema/Sistema/Alertas",
        "Tema/Pruebas/Integracion"
            };

            // Limpiar y agregar temas al ComboBox de suscripción (textBox4 se reemplaza por comboBox3)
            comboBox3.Items.AddRange(sampleTopics);
            

            // También agregar a los ComboBox de publicación y recepción
            comboBox1.Items.AddRange(sampleTopics);
            comboBox2.Items.AddRange(sampleTopics);
        }


        //BOTÓN DE CONEXIÓN AL PUERTO

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                string ip = textBox1.Text.Trim();
                if (string.IsNullOrEmpty(ip))
                {
                    MessageBox.Show("Debe ingresar la dirección IP del broker", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!int.TryParse(textBox2.Text, out int port) || port <= 0 || port > 65535)
                {
                    MessageBox.Show("El puerto debe ser un número entre 1 y 65535", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Si el usuario ingresó un GUID específico, usarlo, de lo contrario usar el generado
                if (!string.IsNullOrEmpty(textBox3.Text) && Guid.TryParse(textBox3.Text, out Guid customAppId))
                {
                    appId = customAppId;
                }

                // Crear el cliente MQ
                client = new MQClient(ip, port, appId);

                // Habilitar controles de la aplicación
                EnableControls(true);

                UpdateStatus("Conectado");
                LogMessage($"Conectado al broker en {ip}:{port} con AppID: {appId}");
                // Cargar temas solo si la conexión es exitosa
                LoadSampleTopics();
                UpdateStatus("Conectado");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al conectar: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogMessage($"Error de conexión: {ex.Message}");
                UpdateStatus("Error al conectar");
            }
        }

        //BOTÓN DE SUBSCRIPCIÓN

        private void button2_Click(object sender, EventArgs e)
        {
            string topicName = comboBox3.Text.Trim();

            if (string.IsNullOrEmpty(topicName))
            {
                MessageBox.Show("Seleccione un tema primero", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Topic topic = new Topic(topicName);
                bool success = client.Subscribe(topic);

                // Verificar si ya estaba suscrito localmente
                if (subscribedTopics.ContainsKey(topicName) && subscribedTopics[topicName])
                {
                    MessageBox.Show($"Ya está suscrito al tema: {topicName}", "Información",
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LogMessage($"Intento de suscripción duplicada: {topicName}");
                    return;
                }

                // Si llega aquí, es una suscripción nueva
                UpdateSubscriptionList(topicName, "Suscrito");
                subscribedTopics[topicName] = true;

                // Agregar a los ComboBox si no existen
                if (!comboBox1.Items.Contains(topicName))
                    comboBox1.Items.Add(topicName);
                if (!comboBox2.Items.Contains(topicName))
                    comboBox2.Items.Add(topicName);

                LogMessage($"Suscripción exitosa: {topicName}");
                MessageBox.Show($"Suscrito exitosamente al tema: {topicName}", "Éxito",
                              MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (MQSubscriptionException ex)
            {
                if (ex.Message.Contains("Ya está suscrito"))
                {
                    MessageBox.Show($"Ya está suscrito al tema: {topicName}", "Información",
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Error al suscribirse: {ex.Message}", "Error",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                LogMessage($"Error de suscripción: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inesperado: {ex.Message}", "Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogMessage($"Error general: {ex.Message}");
            }
        }

        

        private void UpdateSubscriptionList(string topicName, string status)
        {
            // Buscar si el tema ya existe en el ListView
            foreach (ListViewItem item in listView1.Items)
            {
                if (item.Text == topicName)
                {
                    item.SubItems[1].Text = status;
                    return;
                }
            }

            // Si no existe, agregarlo
            ListViewItem newItem = new ListViewItem(topicName);
            newItem.SubItems.Add(status);
            listView1.Items.Add(newItem);
        }


        //BOTÓN DE DESUBSCRIPCIÓN

        private void button3_Click_1(object sender, EventArgs e)
        {
            string topicName = comboBox3.Text.Trim();

            if (string.IsNullOrEmpty(topicName))
            {
                MessageBox.Show("Debe seleccionar un tema de la lista", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // Verificar primero si está suscrito localmente
                if (!subscribedTopics.ContainsKey(topicName) || !subscribedTopics[topicName])
                {
                    MessageBox.Show($"No se encuentra suscrito al tema: {topicName}", "Información",
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LogMessage($"Intento de desuscripción fallido: No suscrito a {topicName}");
                    return;
                }

                Topic topic = new Topic(topicName);
                bool result = client.Unsubscribe(topic);

                if (result)
                {
                    // Remover tema de la lista
                    if (subscribedTopics.ContainsKey(topicName))
                    {
                        subscribedTopics.Remove(topicName);

                        // Remover de la lista visual (ListView)
                        foreach (ListViewItem item in listView1.Items)
                        {
                            if (item.Text == topicName)
                            {
                                listView1.Items.Remove(item);
                                break;
                            }
                        }
                    }

                    LogMessage($"Desuscrito del tema: {topicName}");
                    MessageBox.Show($"Desuscripción exitosa del tema: {topicName}", "Éxito",
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (MQSubscriptionException ex)
            {
                if (ex.Message.Contains("No está suscrito"))
                {
                    MessageBox.Show($"No se encuentra suscrito al tema: {topicName}", "Información",
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Error al desuscribirse: {ex.Message}", "Error",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                LogMessage($"Error de desuscripción: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inesperado: {ex.Message}", "Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogMessage($"Error: {ex.Message}");
            }
        }


        //BOTÓN DE PUBLISH

        private void button4_Click(object sender, EventArgs e)
        {
            // Obtener el tema directamente del texto del ComboBox
            string topicName = comboBox1.Text.Trim(); // Cambiado de SelectedItem a Text

            if (string.IsNullOrEmpty(topicName))
            {
                MessageBox.Show("Debe ingresar un tema", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string messageContent = richTextBox1.Text;

            try
            {
                Topic topic = new Topic(topicName);
                MessageQueueClient.Message message = new MessageQueueClient.Message(messageContent);
                bool result = client.Publish(message, topic);

                if (result)
                {
                    LogMessage($"Mensaje publicado en el tema: {topicName}");
                    label8.Text = "Resultado: Mensaje publicado exitosamente";
                    richTextBox1.Clear();

                    // Opcional: agregar el tema a la lista si no existe
                    if (!comboBox1.Items.Contains(topicName))
                    {
                        comboBox1.Items.Add(topicName);
                    }
                }
            }
            catch (MQPublishException ex)
            {
                label8.Text = $"Resultado: Error: {ex.Message}";
                LogMessage($"Error al publicar: {ex.Message}");
            }
            catch (Exception ex)
            {
                label8.Text = $"Error: {ex.Message}";
                LogMessage($"Error: {ex.Message}");
            }
        }

        //BOTÓN DE RECIEVE

        private void button5_Click(object sender, EventArgs e)
        {
            if (comboBox2.SelectedItem == null)
            {
                MessageBox.Show("Debe seleccionar un tema", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string topicName = comboBox2.SelectedItem.ToString();

            try
            {
                Topic topic = new Topic(topicName);
                MessageQueueClient.Message message = client.Receive(topic);

                if (message != null && !string.IsNullOrEmpty(message.Content))
                {
                    // Agregar mensaje a la lista de mensajes recibidos (ListView)
                    ListViewItem item = new ListViewItem(topicName);
                    item.SubItems.Add(message.Content);
                    item.SubItems.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    listView2.Items.Add(item);

                    LogMessage($"Mensaje recibido del tema: {topicName}");
                }
            }
            catch (MQReceiveException ex)
            {
                // No mostrar error si simplemente no hay mensajes
                if (!ex.Message.Contains("No hay mensajes en la cola"))
                {
                    MessageBox.Show($"Error al recibir mensaje: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                LogMessage($"Recepción: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inesperado: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogMessage($"Error: {ex.Message}");
            }
        }

        private void UpdateStatus(string status)
        {
            label4.Text = $"Estado: {status}";
        }

        private void LogMessage(string message)
        {
            richTextBox2.AppendText($"[{DateTime.Now.ToString("HH:mm:ss")}] {message}\r\n");
            // Desplazar al final
            richTextBox2.SelectionStart = richTextBox2.Text.Length;
            richTextBox2.ScrollToCaret();
        }

        private void EnableControls(bool enabled)
        {
            // Habilitar/deshabilitar controles según el estado de conexión
            textBox1.Enabled = !enabled;
            textBox2.Enabled = !enabled;
            textBox3.Enabled = !enabled;
            button1.Enabled = !enabled;

            // Habilitar los controles de suscripción y publicación
            comboBox3.Enabled = enabled;
            button2.Enabled = enabled;
            button3.Enabled = enabled;
            comboBox1.Enabled = enabled;
            richTextBox1.Enabled = enabled;
            button4.Enabled = enabled;
            comboBox2.Enabled = enabled;
            button5.Enabled = enabled;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Desuscribir de todos los temas antes de cerrar
            if (client != null && subscribedTopics.Count > 0)
            {
                foreach (string topicName in subscribedTopics.Keys)
                {
                    try
                    {
                        Topic topic = new Topic(topicName);
                        client.Unsubscribe(topic);
                    }
                    catch
                    {
                        // Ignorar errores al cerrar
                    }
                }
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                string selectedTopic = listView1.SelectedItems[0].Text;
                comboBox3.Text = selectedTopic;
            }

        }
    }
}