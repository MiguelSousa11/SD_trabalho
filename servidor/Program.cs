using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class Servidor
{
    private TcpListener listener;
    private int port = 8888; // Porta de escuta

    public Servidor()
    {
        listener = new TcpListener(IPAddress.Any, port);
    }

    public void Start()
    {
        listener.Start();
        Console.WriteLine("Servidor iniciado e escutando na porta " + port);

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClient));
            clientThread.Start(client);
        }
    }

    private void HandleClient(object obj)
    {
        TcpClient client = (TcpClient)obj;
        StreamReader reader = new StreamReader(client.GetStream());
        StreamWriter writer = new StreamWriter(client.GetStream()) { AutoFlush = true };

        string message = reader.ReadLine();
        Console.WriteLine("Recebido: " + message);

        if (message.ToUpper().StartsWith("ID"))
        {
            // Processa identificação
            Console.WriteLine("Cliente identificado: " + message);
        }
        else if (message.ToUpper() == "QUIT")
        {
            // Finaliza conexão
            writer.WriteLine("400 BYE");
            client.Close();
            Console.WriteLine("Conexão encerrada.");
            return;
        }

        // Loop para manter a comunicação, pode ser adaptado conforme necessário
        while (message != null && !message.ToUpper().Equals("QUIT"))
        {
            message = reader.ReadLine();
            Console.WriteLine("Recebido: " + message);
        }

        writer.WriteLine("400 BYE");
        client.Close();
    }

    static void Main(string[] args)
    {
        Servidor server = new Servidor();
        server.Start();
    }
}