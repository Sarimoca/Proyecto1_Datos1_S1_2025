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

            // Estado inicial
            UpdateStatus("Desconectado");
        }

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

                if (!int.TryParse(textBox1.Text, out int port) || port <= 0 || port > 65534)
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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al conectar: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogMessage($"Error de conexión: {ex.Message}");
                UpdateStatus("Error al conectar");
            }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            string topicName = textBox4.Text.Trim();
            if (string.IsNullOrEmpty(topicName))
            {
                MessageBox.Show("Debe ingresar un nombre de tema", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                Topic topic = new Topic(topicName);
                bool result = client.Subscribe(topic);

                if (result)
                {
                    // Agregar tema a la lista de suscritos si no existe
                    if (!subscribedTopics.ContainsKey(topicName))
                    {
                        subscribedTopics[topicName] = true;

                        // Agregar a ListView en lugar de DataGridView
                        ListViewItem item = new ListViewItem(topicName);
                        item.SubItems.Add("Suscrito");
                        listView1.Items.Add(item);

                        comboBox1.Items.Add(topicName);
                        comboBox2.Items.Add(topicName);
                    }

                    LogMessage($"Suscrito al tema: {topicName}");
                }
            }
            catch (MQSubscriptionException ex)
            {
                MessageBox.Show($"Error al suscribirse: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogMessage($"Error de suscripción: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inesperado: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogMessage($"Error: {ex.Message}");
            }
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            string topicName = textBox4.Text.Trim();
            if (string.IsNullOrEmpty(topicName))
            {
                MessageBox.Show("Debe ingresar un nombre de tema", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
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

                        // Remover de los combos
                        comboBox1.Items.Remove(topicName);
                        comboBox2.Items.Remove(topicName);
                    }

                    LogMessage($"Desuscrito del tema: {topicName}");
                }
            }
            catch (MQSubscriptionException ex)
            {
                MessageBox.Show($"Error al desuscribirse: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogMessage($"Error de desuscripción: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inesperado: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogMessage($"Error: {ex.Message}");
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem == null)
            {
                MessageBox.Show("Debe seleccionar un tema", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string topicName = comboBox1.SelectedItem.ToString();
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
            textBox4.Enabled = enabled;
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
    }
}