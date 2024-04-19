using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Servidor
{
    private static Mutex mutexClienteServicoMap = new Mutex();
    private static Mutex mutexTarefasConcluidas = new Mutex();
    private static Dictionary<string, string> clienteServicoMap = new Dictionary<string, string>(); // Mapeia IDs de clientes para serviços
    private static Dictionary<string, bool> tarefasConcluidas = new Dictionary<string, bool>(); // Mapeia IDs de tarefas para seu estado de conclusão

    public static void Main()
    {
        // Carregar informações de tarefas e alocações de clientes a partir dos arquivos CSV
        LoadDataFromCSV();

        // Definir o endereço IP e a porta para o servidor
        string ip = "127.0.0.1";
        int porta = 11000;
        IPAddress enderecoIP = IPAddress.Parse(ip);
        IPEndPoint ipEndPoint = new IPEndPoint(enderecoIP, porta);

        // Inicializar o servidor e aguardar conexões
        TcpListener listener = new TcpListener(ipEndPoint);
        listener.Start();
        Console.WriteLine("Servidor iniciado. A aguardar conexões...");

        while (true)
        {
            // Aceitar cliente
            TcpClient client = listener.AcceptTcpClient();
            Console.WriteLine("Cliente conectado.");

            // Processar a comunicação com o cliente em uma nova thread
            Thread clientThread = new Thread(() => HandleClient(client));
            clientThread.Start();
        }
    }

    private static void HandleClient(TcpClient client)
    {
        // Obter stream de entrada e saída para comunicação com o cliente
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead;

        try
        {
            // Envio de mensagem de boas-vindas ao cliente
            string welcomeMessage = "100 OK";
            byte[] welcomeMessageBytes = Encoding.UTF8.GetBytes(welcomeMessage);
            stream.Write(welcomeMessageBytes, 0, welcomeMessageBytes.Length);

            // Receber ID do cliente
            bytesRead = stream.Read(buffer, 0, buffer.Length);
            string clientId = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine("ID do cliente recebido: " + clientId);

            // Loop para receber mensagens do cliente
            while (true)
            {
                bytesRead = stream.Read(buffer, 0, buffer.Length);
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine("Mensagem do cliente: " + message);

                // o cliente escreve QUIT para acabar sesssao
                if (message == "QUIT")
                {
                    string byeMessage = "400 BYE";
                    byte[] byeMessageBytes = Encoding.UTF8.GetBytes(byeMessage);
                    stream.Write(byeMessageBytes, 0, byeMessageBytes.Length);
                    Console.WriteLine("Cliente desconectado.");
                    break;
                }
                // o cliente escreve CONCLUIDA para indicar a conclusao da tarefa
                else if (message.StartsWith("CONCLUIDA"))
                {
                    string taskId = message.Split(':')[1].Trim();
                    mutexTarefasConcluidas.WaitOne();
                    tarefasConcluidas[taskId] = true;
                    mutexTarefasConcluidas.ReleaseMutex();
                    UpdateTaskStatus(taskId, "Nao Alocado");
                    string ackMessage = "Tarefa concluída: " + taskId;
                    byte[] ackMessageBytes = Encoding.UTF8.GetBytes(ackMessage);
                    stream.Write(ackMessageBytes, 0, ackMessageBytes.Length);
                }
                // o cliente escreve LISTAR TAREFAS para solicitar a lista de tarefas disponíveis
                else if (message == "LISTAR TAREFAS")
                {
                    string taskList = GetTaskList();
                    byte[] taskListBytes = Encoding.UTF8.GetBytes(taskList);
                    stream.Write(taskListBytes, 0, taskListBytes.Length);
                }
                // o cliente escreve ESCOLHER TAREFA:ID_TAREFA para escolher uma tarefa específica
                else if (message.StartsWith("ESCOLHER TAREFA"))
                {
                    string taskId = message.Split(':')[1].Trim();
                    UpdateTaskStatus(taskId, "Em curso");
                    string ackMessage = "Tarefa " + taskId + " escolhida e marcada como Em curso.";
                    byte[] ackMessageBytes = Encoding.UTF8.GetBytes(ackMessage);
                    stream.Write(ackMessageBytes, 0, ackMessageBytes.Length);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro: " + ex.Message);
        }
        finally
        {
            // Fechar a conexão com o cliente
            stream.Close();
            client.Close();
        }
    }

    // Carregar informações de tarefas e alocações de clientes a partir dos arquivos CSV
    private static void LoadDataFromCSV()
    {
        try
        {
            // Carregar arquivo de alocações de clientes para serviços
            using (StreamReader reader = new StreamReader("Alocacao_Cliente_Servico.csv"))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    string[] parts = line.Split(',');
                    if (parts.Length == 2)
                    {
                        clienteServicoMap[parts[0]] = parts[1];
                    }
                }
            }

            // Carregar arquivo de tarefas concluídas
            using (StreamReader reader = new StreamReader("tarefas.csv"))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    string[] parts = line.Split(',');
                    if (parts.Length == 4)
                    {
                        string taskId = parts[0];
                        bool concluida = parts[2].ToLower() == "concluido";
                        tarefasConcluidas[taskId] = concluida;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Erro ao carregar dados do CSV: " + e.Message);
        }
    }

    // Retorna uma nova tarefa disponível para o serviço especificado
    private static string GetNewTask(string service)
    {
        try
        {
            // Lista para armazenar as tarefas disponíveis para o serviço especificado
            List<string> availableTasks = new List<string>();

            // Percorrer o dicionário de tarefas concluídas para encontrar as tarefas ainda não concluídas
            foreach (var taskEntry in tarefasConcluidas)
            {
                // Verificar se a tarefa não foi concluída e está associada ao serviço especificado
                if (!taskEntry.Value && taskEntry.Key.StartsWith(service))
                {
                    availableTasks.Add(taskEntry.Key);
                }
            }

            // Verificar se existem tarefas disponíveis para o serviço especificado
            if (availableTasks.Count > 0)
            {
                // Retornar uma tarefa aleatória entre as disponíveis
                Random random = new Random();
                int index = random.Next(availableTasks.Count);
                return availableTasks[index];
            }
            else
            {
                Console.WriteLine("Nenhuma tarefa disponível para o serviço: " + service);
                return null;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Erro ao selecionar nova tarefa: " + e.Message);
            return null;
        }
    }

    // Retorna a lista de tarefas disponíveis
    private static string GetTaskList()
    {
        StringBuilder taskListBuilder = new StringBuilder();
        foreach (var taskEntry in tarefasConcluidas)
        {
            if (!taskEntry.Value)
            {
                taskListBuilder.AppendLine(taskEntry.Key);
            }
        }
        return taskListBuilder.ToString();
    }

    // Atualiza o estado da tarefa para o novo estado especificado no arquivo CSV
    private static void UpdateTaskStatus(string taskId, string newStatus)
    {
        try
        {
            string filePath = "tarefas.csv";
            List<string> lines = new List<string>();
            using (StreamReader reader = new StreamReader(filePath))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    string[] parts = line.Split(',');
                    if (parts.Length == 4 && parts[0] == taskId)
                    {
                        parts[2] = newStatus; // Atualizar o estado da tarefa
                        line = string.Join(",", parts);
                    }
                    lines.Add(line);
                }
            }

            // Escrever as linhas atualizadas de volta ao arquivo
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (string line in lines)
                {
                    writer.WriteLine(line);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Erro ao atualizar o estado da tarefa: " + e.Message);
        }
    }
}
