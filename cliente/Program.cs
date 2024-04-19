﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Cliente
{
    public static void Main()
    {
        // Definir o endereço IP e a porta para o servidor
        string ip = "127.0.0.1";
        int porta = 11000;
        IPAddress enderecoIP = IPAddress.Parse(ip);
        IPEndPoint ipEndPoint = new IPEndPoint(enderecoIP, porta);

        try
        {
            // Conectar-se ao servidor
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Connect(ipEndPoint);
                Console.WriteLine("Conectado ao servidor.");

                // Obter stream de entrada e saída para comunicação com o servidor
                NetworkStream stream = new NetworkStream(socket);
                byte[] buffer = new byte[1024];
                int bytesRead;

                try
                {
                    // Receber mensagem de boas-vindas do servidor
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string welcomeMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine("Servidor: " + welcomeMessage);

                    // Enviar ID do cliente para o servidor
                    Console.Write("Escreva o seu ID de cliente: ");
                    string clientId = Console.ReadLine();
                    byte[] clientIdBytes = Encoding.UTF8.GetBytes(clientId);
                    stream.Write(clientIdBytes, 0, clientIdBytes.Length);

                    // Loop para enviar mensagens para o servidor
                    while (true)
                    {
                        Console.Write("Escreva 'NOVA TAREFA', 'CONCLUIDA' ou 'QUIT': ");
                        string mensagem = Console.ReadLine();
                        byte[] mensagemBytes = Encoding.UTF8.GetBytes(mensagem);
                        stream.Write(mensagemBytes, 0, mensagemBytes.Length);

                        // Verificar se o cliente deseja encerrar a comunicação
                        if (mensagem == "QUIT")
                        {
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Erro: " + e.Message);
                }
                finally
                {
                    // Fechar a conexão com o servidor
                    stream.Close();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro ao conectar-se ao servidor: " + ex.Message);
        }
    }
}
